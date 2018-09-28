using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types.Debugging;
using Meadow.TestNode;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{

    [TestClass]
    public abstract class ContractTest
    {
        #region Fields
        private ulong _baseSnapshotID;
        private static Semaphore _sequentialExecutionSemaphore = new Semaphore(1, 1);
        #endregion

        #region Properties
        internal InternalTestState InternalTestState
        {
            get
            {
                return TestContext.GetInternalTestState();
            }
            set
            {
                TestContext?.SetInternalTestState(value);
            }
        }

        public TestContext TestContext { get; set; }

        public TestServices TestServices { get; private set; }

        public Address[] Accounts => TestServices.Accounts;
        public IJsonRpcClient RpcClient => TestServices.TestNodeClient;
        public TestNodeServer TestNodeServer => TestServices.TestNodeServer;

        public MeadowAsserter Assert { get; } = MeadowAsserter.Instance;

        public CollectionAsserter CollectionAssert { get; } = new CollectionAsserter();

        #endregion

        #region Functions
        /// <summary>
        /// Virtual method which is run before every test is executed. Can be overriden to
        /// deploy contracts before a test automatically, or etc.
        /// </summary>
        /// <returns></returns>
        protected virtual Task BeforeEach() => Task.CompletedTask;

        [TestInitialize]
        public async Task OnTestInitialize()
        {
            try
            {

                if (!Global.IsInitialized)
                {
                    throw new Exception("Test harness has not been initialized. Ensure \"Global.Init()\" has been called from a [AssemblyInitialize] method.");
                }

                // With parallel tests, all code should execute in pairs (main vs. external for every test)
                // so we need to ensure sequential execution to avoid issues with other tests running and trying
                // to snapshot, execute, restore on the external node, ending up with a race condition.

                // In general, if we are executing on an external node, to avoid race conditions with other threads, we lock.
                if (Global.ExternalNodeTestServices != null)
                {
                    _sequentialExecutionSemaphore.WaitOne();
                }

                // If we only want to use the external node, set the external node override.
                if (Global.OnlyUseExternalNode.GetValueOrDefault())
                {
                    InternalTestState.InExternalNodeContext = true;
                }

                // Determine if we're using an external testing service or one of the built in ones.
                if (InternalTestState.InExternalNodeContext)
                {
                    // Using an external node for testing.
                    TestServices = Global.ExternalNodeTestServices;
                }
                else
                {
                    // Using a built in node from our pool for testing.
                    TestServices = await Global.TestServicesPool.Get();
                }

                // We set the test start recording time initially in case we hit an exception while processing the underlying code.
                InternalTestState.StartTime = DateTimeOffset.UtcNow;

                // Create a snapshot to revert to when test is completed.
                _baseSnapshotID = await TestServices.TestNodeClient.Snapshot();

                // Determine what message to log
                if (InternalTestState.InExternalNodeContext)
                {
                    LogDebug($"Using external node at host name \"{Global.ExternalNodeHost.Host}\" on port \"{Global.ExternalNodeHost.Port}\", snapshot: {_baseSnapshotID}");
                }
                else
                {
                    LogDebug($"Using built-in local test node on port {TestNodeServer.RpcServer.ServerPort}, snapshot: {_baseSnapshotID}");
                }

                // Execute our pre-test method
                await BeforeEach();

                // Set our initialization as successful
                InternalTestState.InitializationSuccess = true;

                // If we haven't hit an exception, then we refresh start recording time from this point (the end of our test initialization).
                InternalTestState.StartTime = DateTimeOffset.UtcNow;

            }
            catch (Exception ex)
            {
                Log($"Exception in {nameof(ContractTest)}.{nameof(OnTestInitialize)}: " + ex.ToString());
                throw;
            }
        }

        [TestCleanup]
        public async Task OnTestCleanup()
        {
            try
            {
                // Obtain our end time.
                InternalTestState.EndTime = DateTime.Now;

                // If we're testing a built in node, we'll want to be collecting relevant coverage information.
                if (!InternalTestState.InExternalNodeContext)
                {
                    // Get all coverage map data from the node.
                    var coverageMapData = await RpcClient.GetAllCoverageMaps();

                    // Clear coverage for the next unit test that uses this node. 
                    await RpcClient.ClearCoverage();

                    if (!TestContext.Properties.ContainsKey(nameof(SkipCoverageAttribute)))
                    {
                        // Match coverage contract addresses with deployed contracts that the client keeps track of.
                        var contractInstances = GeneratedSolcData.Default.MatchCoverageData(coverageMapData);

                        // Store the coverage data for the report generation at end of tests.
                        Global.CoverageMaps.Add(contractInstances);
                    }
                }

                // Revert the node chain for the next unit test to start with a clean slate.
                await TestServices.TestNodeClient.Revert(_baseSnapshotID);

                // Calculate our time elapsed.
                var testDuration = (InternalTestState.EndTime - InternalTestState.StartTime);

                // Log the duration to the console.
                LogDebug($"{TestContext.CurrentTestOutcome.ToString()} - {Math.Round(testDuration.TotalMilliseconds)} ms");

                // If the built in node, we'll want to be collecting relevant testing data.
                if (!InternalTestState.InExternalNodeContext)
                {
                    // Initialize our test outcome/results.
                    var testOutcome = new UnitTestResult
                    {
                        Namespace = TestContext.FullyQualifiedTestClassName,
                        TestName = TestContext.TestName,
                        Passed = TestContext.CurrentTestOutcome == UnitTestOutcome.Passed,
                        Duration = testDuration
                    };

                    // Add the test to our global testing result test output.
                    Global.UnitTestResults.Add(testOutcome);

                    // As this is our built in node, we put the test services back in the pool.
                    await Global.TestServicesPool.PutAsync(TestServices);
                }

                // Blank out the local test services after the test has completed.
                TestServices = null;

                // Set our cleanup as a success
                InternalTestState.CleanupSuccess = true;

                // If we are running an external node, we'll want to make tests run sequentially.
                if (Global.ExternalNodeTestServices != null)
                {
                    _sequentialExecutionSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Critical exeption: Cleanup failed in ContractTest - " + ex.Message, ex);
            }
            finally
            {

            }
        }



        /// <summary>
        /// Temporarily disable the contract size limit during the execution of the provided callback.
        /// </summary>
        public async Task DisableContractSizeLimit(Func<Task> executionCallback)
        {
            await RpcClient.SetContractSizeCheckDisabled(true);
            try
            {
                await executionCallback();
            }
            finally
            {
                await RpcClient.SetContractSizeCheckDisabled(false);
            }
        }

        /// <summary>
        /// Log a message to the console/debug output with a standardized format.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Log(object msg)
        {
            var msgStr = msg.ToString();
            TestContext.WriteLine(msgStr);

            var globalMsg = $"[{TestContext.FullyQualifiedTestClassName}] {msgStr}";

            Console.WriteLine(globalMsg);
            Debug.WriteLine(globalMsg);
        }

        [Conditional("DEBUG_UNITTESTS")]
        public void LogDebug(object msg)
        {
            Log(msg);
        }

        #endregion
    }
}
