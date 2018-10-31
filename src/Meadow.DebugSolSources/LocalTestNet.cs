using Meadow.CoverageReport.Debugging;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.TestNode;
using System;
using System.Threading.Tasks;

namespace Meadow.DebugSolSources
{
    class LocalTestNet : IDisposable
    {
        public TestNodeServer TestNodeServer { get; private set; }
        public IJsonRpcClient RpcClient { get; private set; }
        public Core.EthTypes.Address[] Accounts { get; private set; }

        public static async Task<LocalTestNet> Setup()
        {
            var localTestNet = new LocalTestNet();

            try
            {
                // Bootstrap local testnode
                // TODO: check cmd args for account options
                localTestNet.TestNodeServer = new TestNodeServer();
                localTestNet.TestNodeServer.RpcServer.Start();

                // Setup rpcclient
                // TODO: check cmd args for gas options
                localTestNet.RpcClient = JsonRpcClient.Create(
                    localTestNet.TestNodeServer.RpcServer.ServerAddress,
                    ArbitraryDefaults.DEFAULT_GAS_LIMIT,
                    ArbitraryDefaults.DEFAULT_GAS_PRICE);

                // Enable Meadow debug RPC features.
                await localTestNet.RpcClient.SetTracingEnabled(true);

                // Configure Meadow rpc error formatter.
                localTestNet.RpcClient.ErrorFormatter = GetExecutionTraceException;

                // Get accounts.
                localTestNet.Accounts = await localTestNet.RpcClient.Accounts();

            }
            catch
            {
                localTestNet.Dispose();
                throw;
            }

            return localTestNet;
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

        public void Dispose()
        {
            try
            {
                RpcClient?.Dispose();
            }
            catch { }
            try
            {
                TestNodeServer?.Dispose();
            }
            catch { }
        }
    }
}