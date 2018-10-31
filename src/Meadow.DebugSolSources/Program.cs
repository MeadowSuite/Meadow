using McMaster.Extensions.CommandLineUtils;
using Meadow.Contract;
using Meadow.Core.AbiEncoding;
using Meadow.Core.Utils;
using Meadow.CoverageReport.Debugging;
using Meadow.DebugAdapterServer;
using Meadow.DebugAdapterServer.DebuggerTransport;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Meadow.SolCodeGen;
using Meadow.TestNode;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.DebugSolSources
{
    class Program
    {

        static async Task<int> Main(string[] args)
        {
            if (SolidityDebugger.DebugStopOnEntry)
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Launch();
                }

            }

            // Parse process arguments.
            var opts = ProcessArgs.Parse(args);
            string entryContractName = "Main";
            string entryContractFunctionName = null;
            if (!string.IsNullOrWhiteSpace(opts.Entry))
            {
                var entryParts = opts.Entry.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
                entryContractName = entryParts[0];
                if (entryParts.Length > 1)
                {
                    entryContractFunctionName = entryParts[1];
                }
            }

            const string generatedNamespace = "Meadow.DebugSol.Generated";
            const string generatedDataDir = ".meadow-generated";

            string workspaceDir;
            if (!string.IsNullOrEmpty(opts.Directory))
            {
                workspaceDir = opts.Directory.Replace('\\', '/');
            }
            else if (!string.IsNullOrEmpty(opts.SingleFile))
            {
                // If workspace is not provided, derive a determistic temp directory for the single file.
                workspaceDir = Path.Combine(Path.GetTempPath(), HexUtil.GetHexFromBytes(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(opts.SingleFile))));
                Directory.CreateDirectory(workspaceDir);
                workspaceDir = workspaceDir.Replace('\\', '/');
            }
            else
            {
                Console.Error.WriteLine("A directory or single file for debugging must be specified.");
                return 1;
            }

            string outputDir = workspaceDir + "/" + generatedDataDir;
            string srcOutputDir = outputDir + "/src";
            string buildOutputDir = outputDir + "/build";

            string solCompilationSourcePath = workspaceDir;

            string singleFile = null;
            if (!string.IsNullOrEmpty(opts.SingleFile))
            {
                // Normalize file path.
                singleFile = opts.SingleFile.Replace('\\', '/');

                // Check if provided file is inside the workspace directory.
                if (singleFile.StartsWith(workspaceDir, StringComparison.OrdinalIgnoreCase))
                {
                    singleFile = singleFile.Substring(workspaceDir.Length).Trim('/');
                }
                else
                {
                    // File is outside of workspace so setup special pathing.
                    solCompilationSourcePath = singleFile;
                }
            }

            // Hook up debugger callbacks.
            var debuggerCancelToken = new CancellationTokenSource();
            var stdInOutDebuggerTransport = new StandardInputOutputDebuggerTransport();
            var debuggerDisposable = SolidityDebugger.AttachSolidityDebugger(stdInOutDebuggerTransport, debuggerCancelToken, useContractsSubDir: false);

            try
            {
                // Perform sol compilation if out-of-date.
                var solCodeGenArgs = new CommandArgs
                {
                    Generate = GenerateOutputType.Source | GenerateOutputType.Assembly,
                    Namespace = generatedNamespace,
                    OutputDirectory = srcOutputDir,
                    AssemblyOutputDirectory = buildOutputDir,
                    SolSourceDirectory = solCompilationSourcePath
                };
                var solCodeGenResults = CodebaseGenerator.Generate(solCodeGenArgs, logger: msg => { });

                // Load compiled assembly.
                var generatedAsm = AppDomain.CurrentDomain.Load(
                    File.ReadAllBytes(solCodeGenResults.CompilationResults.AssemblyFilePath),
                    File.ReadAllBytes(solCodeGenResults.CompilationResults.PdbFilePath));

                // Load solc data from assembly resources.
                var generatedSolcData = GeneratedSolcData.Create(generatedAsm);


                (string ContractPath, string ContractName, Abi[] Abi) entryContract;

                // If both singleFile and entryPoint are specified..
                if (!string.IsNullOrEmpty(opts.SingleFile) && !string.IsNullOrEmpty(opts.Entry))
                {
                    var matchingContracts = generatedSolcData.ContractAbis
                        .Where(c => c.Key.FilePath.Equals(singleFile, StringComparison.OrdinalIgnoreCase))
                        .Where(c => c.Key.ContractName.Equals(entryContractName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (matchingContracts.Length == 0)
                    {
                        Console.Error.WriteLine($"No matching contracts found for file ${singleFile} and contract '${entryContractName}'");
                        return 1;
                    }
                    else if (matchingContracts.Length > 1)
                    {
                        Console.Error.WriteLine($"Multiple matching contracts found for file ${singleFile} and contract '${entryContractName}'");
                        return 1;
                    }

                    entryContract = (matchingContracts[0].Key.FilePath, matchingContracts[0].Key.ContractName, matchingContracts[0].Value);
                }

                // If only singleFile is specified..
                else if (!string.IsNullOrEmpty(opts.SingleFile))
                {
                    var matchingContracts = generatedSolcData.ContractAbis
                        .Where(c => c.Key.FilePath.Equals(singleFile, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (matchingContracts.Length == 0)
                    {
                        Console.Error.WriteLine($"No matching contracts found for file ${singleFile}'");
                        return 1;
                    }
                    else if (matchingContracts.Length > 1)
                    {
                        // Found multiple contracts in file, see if one is the default "Main" contract.
                        var mainContract = matchingContracts
                            .Where(c => c.Key.ContractName.Equals("Main", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

                        if (mainContract.Length == 1)
                        {
                            entryContract = (mainContract[0].Key.FilePath, mainContract[0].Key.ContractName, mainContract[0].Value);
                        }
                        else
                        {
                            Console.Error.WriteLine($"Multiple contracts found for file ${singleFile}. Specify an entry point contract.");
                            return 1;
                        }
                    }
                    else
                    {
                        entryContract = (matchingContracts[0].Key.FilePath, matchingContracts[0].Key.ContractName, matchingContracts[0].Value);
                    }
                }

                // If only entryPoint is specified..
                else if (!string.IsNullOrEmpty(opts.Entry))
                {
                    var matchingContracts = generatedSolcData.ContractAbis
                        .Where(c => c.Key.ContractName.Equals(entryContractName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (matchingContracts.Length == 0)
                    {
                        Console.Error.WriteLine($"No matching contracts found matching '${entryContractName}'");
                        return 1;
                    }
                    else if (matchingContracts.Length > 1)
                    {
                        Console.Error.WriteLine($"Multiple contracts found matching '{entryContractName}'");
                        return 1;
                    }
                    else
                    {
                        entryContract = (matchingContracts[0].Key.FilePath, matchingContracts[0].Key.ContractName, matchingContracts[0].Value);
                    }
                }

                // If neither entryPoint nor singleFile are specified..
                else
                {
                    if (generatedSolcData.ContractAbis.Count == 0)
                    {
                        Console.Error.WriteLine($"No contracts founds.");
                        return 1;
                    }
                    else if (generatedSolcData.ContractAbis.Count > 1)
                    {
                        Console.Error.WriteLine($"Multiple contracts found. Specify an entry point contract.");
                        return 1;
                    }
                    else 
                    {
                        var item = generatedSolcData.ContractAbis.First();
                        entryContract = (item.Key.FilePath, item.Key.ContractName, item.Value);
                    }
                }

                entryContractName = entryContract.ContractName;
                var contractFilePath = entryContract.ContractPath;
                var contractBytecode = generatedSolcData.GetSolcBytecodeInfo(contractFilePath, entryContractName);

                // Validate that there is a constructor.
                var constructor = entryContract.Abi.FirstOrDefault(f => f.Type == AbiType.Constructor);
                if (constructor == null)
                {
                    Console.Error.WriteLine($"The contract '{entryContractName}' does not have a constructor.");
                    return 1;
                }

                // Validate that the constructor does not have any input parameteters.
                if (constructor.Inputs?.Length > 0)
                {
                    Console.Error.WriteLine($"The contract '{entryContractName}' constructor cannot have input parameters.");
                    return 1;
                }

                // Validate that contract is not "abstract" (does not have unimplemented function).
                if (string.IsNullOrEmpty(contractBytecode.Bytecode))
                {
                    Console.Error.WriteLine($"The contract '{entryContractName}' does not implement all functions and cannot be deployed.");
                    return 1;
                }


                // Find entry function if specified.
                Abi entryFunction = null;
                if (!string.IsNullOrWhiteSpace(entryContractFunctionName))
                {
                    var candidateFunctions = entryContract.Abi
                        .Where(f => f.Type == AbiType.Function)
                        .Where(f => f.Name == entryContractFunctionName)
                        .ToArray();

                    if (candidateFunctions.Length >= 1 && candidateFunctions.All(f => f.Inputs?.Length > 0))
                    {
                        Console.Error.WriteLine($"The entry function '{entryContractFunctionName}' cannot contain parameters.");
                        return 1;
                    }
                    else if (candidateFunctions.Length == 0)
                    {
                        Console.Error.WriteLine($"Cannot find entry function '{entryContractFunctionName}'.");
                        return 1;
                    }

                    entryFunction = candidateFunctions.First(f => f.Inputs?.Length == 0);
                }


                // Bootstrap local testnode
                // TODO: check cmd args for account options
                var testNode = new TestNodeServer();
                testNode.RpcServer.Start();


                // Setup rpcclient
                // TODO: check cmd args for gas options
                var rpcClient = JsonRpcClient.Create(testNode.RpcServer.ServerAddress, ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);

                // Enable Meadow debug RPC features.
                await rpcClient.SetTracingEnabled(true);

                // Configure Meadow rpc error formatter.
                rpcClient.ErrorFormatter = GetExecutionTraceException;

                // Get accounts.
                var accounts = await rpcClient.Accounts();

                // Perform the contract deployment transaction
                var deployParams = new TransactionParams
                {
                    From = accounts[0],
                    Gas = ArbitraryDefaults.DEFAULT_GAS_LIMIT,
                    GasPrice = ArbitraryDefaults.DEFAULT_GAS_PRICE
                };
                var contractAddress = await ContractFactory.Deploy(rpcClient, HexUtil.HexToBytes(contractBytecode.Bytecode), deployParams);
                var contractInstance = new ContractInstance(
                    contractFilePath, entryContractName,
                    rpcClient, contractAddress, accounts[0]);


                // If entry function is specified then send transasction to it.
                if (entryFunction != null)
                {
                    var callData = EncoderUtil.GetFunctionCallBytes($"{entryFunction.Name}()");
                    var ethFunc = EthFunc.Create(contractInstance, callData);
                    var funcTxParams = new TransactionParams
                    {
                        From = accounts[0],
                        Gas = ArbitraryDefaults.DEFAULT_GAS_LIMIT,
                        GasPrice = ArbitraryDefaults.DEFAULT_GAS_PRICE
                    };
                    await ethFunc.SendTransaction(funcTxParams);
                }


                // Notify debugging ended
                debuggerDisposable.Dispose();


                // Dispose of testnode server and client
                rpcClient.Dispose();
                testNode.Dispose();

                return 0;
            }
            finally
            {
                debuggerDisposable.Dispose();
            }
        }

        static async Task<Exception> GetExecutionTraceException(IJsonRpcClient rpcClient, JsonRpcError error)
        {
            var executionTrace = await rpcClient.GetExecutionTrace();
            var traceAnalysis = new ExecutionTraceAnalysis(executionTrace);

            // Build our aggregate exception
            var aggregateException = traceAnalysis.GetAggregateException(error.ToException());

            if (aggregateException == null)
            {
                throw new Exception("RPC error occurred with tracing enabled but no exceptions could be found in the trace data. Please report this issue.", error.ToException());
            }

            return aggregateException;
        }
    }
}
