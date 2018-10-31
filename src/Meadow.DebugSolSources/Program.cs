using McMaster.Extensions.CommandLineUtils;
using Meadow.Contract;
using Meadow.Core.AbiEncoding;
using Meadow.Core.Utils;
using Meadow.DebugAdapterServer;
using Meadow.DebugAdapterServer.DebuggerTransport;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Types;
using Meadow.SolCodeGen;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.DebugSolSources
{

    class Program
    {

        static async Task<int> Main(string[] args)
        {
            // Break program if requested.
            if (SolidityDebugger.DebugStopOnEntry && !System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
        
            // Setup direct stdin/stdout connection to the debugger host app (vscode).
            var stdInOutDebuggerTransport = new StandardInputOutputDebuggerTransport();

            // Connect to the debugger host app (vscode).
            using (var solidityDebugger = SolidityDebugger.AttachSolidityDebugger(stdInOutDebuggerTransport, useContractsSubDir: false))
            {
                try
                {
                    await Run(args);
                }
                catch (Exception ex)
                {
                    solidityDebugger.DebugAdapter.SendProblemMessage(ex.Message, ex.ToString());
                    Console.Error.WriteLine(ex);
                    return 1;
                }
            }

            return 0;
        }

        static async Task Run(string[] args)
        {
            // Parse debugger program options.
            AppOptions opts = AppOptions.ParseProcessArgs(args);

            // Compile (or retrieve from cache) the solc data for the solidity sources provided in the app options.
            GeneratedSolcData generatedSolcData = GetSolcCompilationData(opts);

            // Identify the contract to deploy from either the provided app options, or heuristically if none provided. 
            // TODO: if multiple candidates are found, prompt with list to GUI for user to pick.
            EntryPointContract entryContract = EntryPointContract.FindEntryPointContract(opts, generatedSolcData);

            // Get the solc output for the entry point contract.
            SolcBytecodeInfo contractBytecode = generatedSolcData.GetSolcBytecodeInfo(entryContract.ContractPath, entryContract.ContractName);

            // Ensure contract constructor has no input parameters, and that the contract is deployable (has all inherited abstract functions implemented).
            ValidateContractConstructor(entryContract, contractBytecode);

            // Find an entry point function to call after deployment (optionally specified).
            Abi entryFunction = GetContractEntryFunction(opts, entryContract);

            // Bootstrap a local test node and rpc client with debugging/tracing enabled.
            using (LocalTestNet localTestNet = await LocalTestNet.Setup())
            {
                await PerformContractTransactions(localTestNet, entryContract, contractBytecode, entryFunction);
            }
        }

        static async Task PerformContractTransactions(LocalTestNet localTestNet, EntryPointContract entryContract, SolcBytecodeInfo contractBytecode, Abi entryFunction)
        {
            // Perform the contract deployment transaction
            var deployParams = new TransactionParams
            {
                From = localTestNet.Accounts[0],
                Gas = ArbitraryDefaults.DEFAULT_GAS_LIMIT,
                GasPrice = ArbitraryDefaults.DEFAULT_GAS_PRICE
            };
            var contractAddress = await ContractFactory.Deploy(localTestNet.RpcClient, HexUtil.HexToBytes(contractBytecode.Bytecode), deployParams);
            var contractInstance = new ContractInstance(
                entryContract.ContractPath, entryContract.ContractName,
                localTestNet.RpcClient, contractAddress, localTestNet.Accounts[0]);


            // If entry function is specified then send transasction to it.
            if (entryFunction != null)
            {
                var callData = EncoderUtil.GetFunctionCallBytes($"{entryFunction.Name}()");
                var ethFunc = EthFunc.Create(contractInstance, callData);
                var funcTxParams = new TransactionParams
                {
                    From = localTestNet.Accounts[0],
                    Gas = ArbitraryDefaults.DEFAULT_GAS_LIMIT,
                    GasPrice = ArbitraryDefaults.DEFAULT_GAS_PRICE
                };
                await ethFunc.SendTransaction(funcTxParams);
            }
        }

        static GeneratedSolcData GetSolcCompilationData(AppOptions opts)
        {
            const string GENERATED_NAMESPACE = "Meadow.DebugSol.Generated";

            // Setup codegen/compilation options.
            var solCodeGenArgs = new CommandArgs
            {
                Generate = GenerateOutputType.Source | GenerateOutputType.Assembly,
                Namespace = GENERATED_NAMESPACE,
                OutputDirectory = opts.SourceOutputDir,
                AssemblyOutputDirectory = opts.BuildOutputDir,
                SolSourceDirectory = opts.SolCompilationSourcePath
            };

            // Perform sol compilation if out-of-date.
            var solCodeGenResults = CodebaseGenerator.Generate(solCodeGenArgs, logger: msg => { });

            // Load compiled assembly.
            var generatedAsm = AppDomain.CurrentDomain.Load(
                File.ReadAllBytes(solCodeGenResults.CompilationResults.AssemblyFilePath),
                File.ReadAllBytes(solCodeGenResults.CompilationResults.PdbFilePath));

            // Load solc data from assembly resources.
            return GeneratedSolcData.Create(generatedAsm);
        }


        static void ValidateContractConstructor(EntryPointContract entryContract, SolcBytecodeInfo contractBytecode)
        {
            // Validate that there is a constructor.
            var constructor = entryContract.Abi.FirstOrDefault(f => f.Type == AbiType.Constructor);
            if (constructor == null)
            {
                throw new Exception($"The contract '{entryContract.ContractName}' does not have a constructor.");
            }

            // Validate that the constructor does not have any input parameteters.
            if (constructor.Inputs?.Length > 0)
            {
                throw new Exception($"The contract '{entryContract.ContractName}' constructor cannot have input parameters.");
            }

            // Validate that contract is not "abstract" (does not have unimplemented function).
            if (string.IsNullOrEmpty(contractBytecode.Bytecode))
            {
                throw new Exception($"The contract '{entryContract.ContractName}' does not implement all functions and cannot be deployed.");
            }

        }

        static Abi GetContractEntryFunction(AppOptions opts, EntryPointContract entryContract)
        {
            Abi entryFunction = null;
            if (!string.IsNullOrWhiteSpace(opts.EntryContractFunctionName))
            {
                var candidateFunctions = entryContract.Abi
                    .Where(f => f.Type == AbiType.Function)
                    .Where(f => f.Name == opts.EntryContractFunctionName)
                    .ToArray();

                if (candidateFunctions.Length >= 1 && candidateFunctions.All(f => f.Inputs?.Length > 0))
                {
                    throw new Exception($"The entry function '{opts.EntryContractFunctionName}' cannot contain parameters.");
                }
                else if (candidateFunctions.Length == 0)
                {
                    throw new Exception($"Cannot find entry function '{opts.EntryContractFunctionName}'.");
                }

                entryFunction = candidateFunctions.First(f => f.Inputs?.Length == 0);
            }

            return entryFunction;
        }

    }
}