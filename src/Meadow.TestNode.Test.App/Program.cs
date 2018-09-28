using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Meadow.EVM.Data_Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Meadow.CoverageReport;
using System.IO;
using System.Globalization;
using System.Collections;
using Meadow.CoverageReport.Debugging;
using Meadow.Core.Utils;
using Meadow.JsonRpc.Types.Debugging;
using System.Net;

namespace Meadow.TestNode.Test.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            int port;
            bool meadow = true;
            if (!meadow)
            {
                port = 7545;
            }
            else
            {
                // Bootstrap our meadow test node server.
                var testNode = new Meadow.TestNode.TestNodeServer();

                // Print our undefined rpc methods.
                var undefinedRpcMethods = testNode.GetUndefinedRpcMethods();
                Console.WriteLine("Warning: following RPC methods are not defined: \n" + string.Join(", ", undefinedRpcMethods.Select(r => r.Value())));

                // Start up the server and obtain the port.
                await testNode.RpcServer.StartAsync();

                port = testNode.RpcServer.ServerPort;
            }

            async Task<Exception> GetExecutionTraceException(IJsonRpcClient rpcClient, JsonRpcError error)
            {
                if (meadow)
                {
                    var executionTrace = await rpcClient.GetExecutionTrace();
                    var traceAnalysis = new ExecutionTraceAnalysis(executionTrace);

                    // Build our aggregate exception
                    var aggregateException = traceAnalysis.GetAggregateException(error.ToException());
                    return aggregateException;
                }
                else
                {
                    if (error != null)
                    {
                        throw error.ToException();
                    }
                    else
                    {
                        throw new Exception("Execution exception");
                    }
                }
            }

            // Connect our client to the server.
            var client = JsonRpcClient.Create(
                new Uri($"http://{IPAddress.Loopback}:{port}"),
                defaultGasLimit: 6_000_000,
                defaultGasPrice: 0);

            client.ErrorFormatter = GetExecutionTraceException;

            // If we're testing meadow, we enable special coverage/tracing options.
            if (meadow)
            {
                await client.SetCoverageEnabled(true);
                await client.SetTracingEnabled(true);
            }

            //var missingConstructorContract = await MissingConstructorChild.New(client);
            //var someNum = await missingConstructorContract.someNum().Call();
            #region ErrorContract
            // Try deploying the error generating contract and test some calls.
            var errorContract = await ErrorContract.New(client, new TransactionParams { Gas = 4712388 });
            //await errorContract.doRevert().ExpectRevertTransaction();
            //await errorContract.doAssert().ExpectRevertTransaction();
            //await errorContract.doThrow().ExpectRevertCall();
            #endregion

            #region Inheritance Tests
            // Test inheritance with an inherited contract.
            var inheritedContract = await InheritedContract.New(client, new TransactionParams { Gas = 4712388 });
            await inheritedContract.testFunction();
            await inheritedContract.testFunctionWithInheritedModifier();

            // Test inheritance with a seperate file which we inherited from.
            var multiInheritedContract = await MultifileInheritedContract.New(client, new TransactionParams { Gas = 4712388 });
            await multiInheritedContract.testFunction();
            await multiInheritedContract.testFunctionWithInheritedModifier();
            #endregion

            try
            {
                await FailDeploymentContract.New(client);
            }
            catch { }

            #region Callstack Tests
            // Try throwing asserts in further call depth and spanning more than one file.
            await multiInheritedContract.testInheritedAssertThrow().ExpectRevertTransaction();

            if (meadow)
            {
                // Test parsing an execution trace of the last call/transaction.
                var executionTrace = await client.GetExecutionTrace();
                ExecutionTraceAnalysis traceAnalysis = new ExecutionTraceAnalysis(executionTrace);

                // Testing: Build our aggregate exception
                var aggregateException = traceAnalysis.GetAggregateException();
                try
                {
                    throw aggregateException;
                }
                catch { }
            }
            #endregion

            #region ExampleContract Tests
            // Deploy our main example contract
            var exContract = await ExampleContract.New($"TestName", true, 34, client, new TransactionParams { Gas = 4712388 });
            await exContract.testRevert().ExpectRevertTransaction();

            await exContract.testBranchCoverage().ExpectRevertTransaction();
            if (meadow)
            {
                // Test parsing an execution trace of the last call/transaction.
                var executionTrace = await client.GetExecutionTrace();
                ExecutionTraceAnalysis traceAnalysis = new ExecutionTraceAnalysis(executionTrace);

                // Testing: Build our aggregate exception
                var aggregateException = traceAnalysis.GetAggregateException();
                try
                {
                    throw aggregateException;
                }
                catch { }
            }

            // Test the modexp precompile.
            var modExpTestBytes = await exContract.testModExp(
                BigIntegerConverter.GetBytes(BigInteger.Parse("1212121323543453245345678346345737475734753745737774573475377734577", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("3", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("4345328123928357434573234217343477", CultureInfo.InvariantCulture)))
                .Call();
            var modExpTest = BigIntegerConverter.GetBigInteger(modExpTestBytes, false, modExpTestBytes.Length);
            // should be 856753145937825219130387866259147

            // Test events
            var eventTest = await exContract.emitDataEvent(1, 2, 3).TransactionReceipt();
            var eventLogFirst = eventTest.FirstEventLog<ExampleContract.DataEvent>();
            var eventLogLast = eventTest.LastEventLog<ExampleContract.DataEvent>();
            var eventLogAll = eventTest.EventLogs<ExampleContract.DataEvent>();
            var eventLogsBase = eventTest.EventLogs();

            // Test for chris
            var result = await exContract.getFirstByteFromByte32(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 }).Call();

            // Try testing branch coverage (which ends in an assert being thrown)
            await exContract.testBranchCoverage().ExpectRevertTransaction();

            //await exContract.noopFunc();
            var echoGasEst = await exContract.echoInt24(34).EstimateGas();

            await exContract.testInstructions1().TransactionReceipt();

            var transactionReceipt = await exContract.echoInt24(22).TransactionReceipt();
            #endregion

            #region Performance Tests
            // Define start time for an execution loop to test performance of a few basic calls.
            DateTime start = DateTime.Now;
            DateTime iteration = start;

            // Note: Change the loop condition here as needed.
            for (int i = 0; i < 1; i++)
            {
                var addr = await exContract.echoAddress(new Address(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }));
                var int24Test = await exContract.echoInt24(7777);
                var stringTest = await exContract.echoString("This is a string.");
                var enabledThingTest = await exContract.enabledThing();
                var isNineTest = await exContract.myFunc(9);
                var givenName = await exContract.givenName();

                // Precompile hash tests
                var sha256HashTest = await exContract.sha256str("hello world"); // should be 0xb94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
                var ripemd160HashTest = await exContract.ripemd160str("hello world"); // should be 0x98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f

                // This ECRecover test should yield 0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd. This test data is taken from SigningTests.cs
                var ecRecoverTest = await exContract.testECRecover(
                    new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f },
                    0x1c,
                    BigIntegerConverter.GetBytes(BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637", CultureInfo.InvariantCulture)),
                    BigIntegerConverter.GetBytes(BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683", CultureInfo.InvariantCulture)));

                Console.WriteLine($"#{i}: {DateTime.Now - iteration}");
                iteration = DateTime.Now;
            }

            DateTime end = DateTime.Now;
            #endregion

            // If this is meadow, we do special post-execution tasks.
            if (meadow)
            {
                // Test obtaining coverage maps, disabling them, clearing them.
                var coverageMaps = await client.GetAllCoverageMaps();
                await client.SetCoverageEnabled(false);
                await client.ClearCoverage();

                // Obtain our generated solc data, create a report, and open it.
                var solcData = GeneratedSolcData.Default.GetSolcData();

                // Match coverage contract addresses with deployed contracts that the client keeps track of.
                var contractInstances = GeneratedSolcData.Default.MatchCoverageData(coverageMaps);

                var reportPath = Path.GetFullPath("Report");
                MiscUtil.ResetDirectory(reportPath);
                ReportGenerator.CreateReport(GeneratedSolcData.Default.SolidityCompilerVersion, contractInstances, solcData.SolcSourceInfo, solcData.SolcBytecodeInfo, null, null, reportPath);
                MiscUtil.OpenBrowser(Path.Join(reportPath, ReportGenerator.REPORT_INDEX_FILE));
            }

            // Output our execution loop time.
            Console.WriteLine($"Total Time: {end - start}");
            Console.ReadLine();
        }
    }
}
