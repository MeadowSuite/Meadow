using Meadow.CoverageReport.Debugging;
using Meadow.DebugAdapterServer;
using Meadow.JsonRpc.Client;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{
    public class Debugging : IDisposable
    {

        public static bool IsSolidityDebuggerAttached { get; set; }

        public static string SolidityDebugSessionID => Environment.GetEnvironmentVariable("DEBUG_SESSION_ID");

        public static bool HasSolidityDebugAttachRequest => !string.IsNullOrWhiteSpace(SolidityDebugSessionID);


        public static void Launch()
        {
            if (!HasSolidityDebugAttachRequest)
            {
                MSTestRunner.RunAllTests(Assembly.GetExecutingAssembly());
            }
            else
            {
                var debugStopOnEntry = (Environment.GetEnvironmentVariable("DEBUG_STOP_ON_ENTRY") ?? string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase);

                if (debugStopOnEntry && !Debugger.IsAttached)
                {
                    Debugger.Launch();
                }


                var cancelToken = new CancellationTokenSource();

                using (AttachSolidityDebugger(cancelToken))
                {
                    // Run all tests (blocking)
                    MSTestRunner.RunAllTests(Assembly.GetExecutingAssembly(), cancelToken.Token);
                    Console.WriteLine("Tests completed");
                }
            }
        }

        public static IDisposable AttachSolidityDebugger(CancellationTokenSource cancelToken)
        {
            var debuggingInstance = new Debugging(SolidityDebugSessionID);

            debuggingInstance.InitializeDebugConnection();
            IsSolidityDebuggerAttached = true;
            debuggingInstance.SetupRpcDebuggingHook();

            debuggingInstance.OnDebuggerDisconnect += () =>
            {
                // If the C# debugger is not attached, we don't care about running the rest of the tests
                // so exit program
                if (!Debugger.IsAttached)
                {
                    cancelToken.Cancel();
                    //Environment.Exit(0);
                }
            };

            return debuggingInstance;
        }

        readonly string _debugSessionID;
        readonly NamedPipeServerStream _pipeServer;
        readonly MeadowSolidityDebugAdapter _debugAdapter;

#pragma warning disable CA1710 // Identifiers should have correct suffix
        public event Action OnDebuggerDisconnect;
#pragma warning restore CA1710 // Identifiers should have correct suffix

        private Debugging(string debugSessionID)
        {
            _debugSessionID = debugSessionID;
            _pipeServer = new NamedPipeServerStream(_debugSessionID, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _debugAdapter = new MeadowSolidityDebugAdapter();
            _debugAdapter.OnDebuggerDisconnect += DebugAdapter_OnDebuggerDisconnect;
            _debugAdapter.OnDebuggerDisconnect += TeardownRpcDebuggingHook;
        }

        public void InitializeDebugConnection()
        {
            // Wait for debug adapter proxy to connect.
            _pipeServer.WaitForConnection();

            // Connect IPC stream to debug adapter handler.
            _debugAdapter.InitializeStream(_pipeServer, _pipeServer);

            // Starts the debug protocol dispatcher background thread.
            _debugAdapter.Protocol.Run();

            // Wait until the debug protocol handshake has completed.
            _debugAdapter.CompletedConfigurationDoneRequest.Task.Wait();

            _debugAdapter.Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint) { ThreadId = 1 });

        }

        public void SetupRpcDebuggingHook()
        {
            // Set our method to execute for our hook.
            JsonRpcClient.JsonRpcExecutionAnalysis = RpcExecutionCallback;
        }
        
        public void TeardownRpcDebuggingHook()
        {
            // Teardown our hook by setting the target as null.
            TeardownRpcDebuggingHook(null);
        }

        private void TeardownRpcDebuggingHook(MeadowSolidityDebugAdapter debugAdapter)
        {
            // Teardown our hook by setting the target as null.
            JsonRpcClient.JsonRpcExecutionAnalysis = null;


        }

        private void DebugAdapter_OnDebuggerDisconnect(MeadowSolidityDebugAdapter sender)
        {
            TeardownRpcDebuggingHook(sender);
            OnDebuggerDisconnect?.Invoke();
        }


        public async Task RpcExecutionCallback(IJsonRpcClient client, bool expectingException)
        {
            // Obtain an execution trace from our client.
            var executionTrace = await client.GetExecutionTrace();
            var executionTraceAnalysis = new ExecutionTraceAnalysis(executionTrace);

            // Process our execution trace in the debug adapter.
            await _debugAdapter.ProcessExecutionTraceAnalysis(client, executionTraceAnalysis, expectingException);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_debugAdapter.Protocol?.IsRunning == true)
            {
                // Cleanly close down debugging
                _debugAdapter.SendTerminateAndExit();
                _debugAdapter.Protocol.Stop(2000);
                _debugAdapter.Protocol.WaitForReader();
            }

            _pipeServer.Disconnect();
            _pipeServer.Dispose();
        }
    }
}
