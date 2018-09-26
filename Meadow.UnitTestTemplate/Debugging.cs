using Meadow.CoverageReport.Debugging;
using Meadow.DebugAdapterServer;
using Meadow.JsonRpc.Client;
using Meadow.MSTest.Runner;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{
    public class Debugging : IDisposable
    {
        public static void Launch()
        {
            var debugSessionID = Environment.GetEnvironmentVariable("DEBUG_SESSION_ID");
            if (string.IsNullOrWhiteSpace(debugSessionID))
            {
                ApplicationTestRunner.RunAllTests(Assembly.GetExecutingAssembly());
            }
            else
            {
                var debugStopOnEntry = (Environment.GetEnvironmentVariable("DEBUG_STOP_ON_ENTRY") ?? string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase);

                if (debugStopOnEntry && !Debugger.IsAttached)
                {
                    Debugger.Launch();
                }

                using (var debuggingInstance = new Debugging(debugSessionID))
                {
                    debuggingInstance.InitializeDebugConnection();
                    debuggingInstance.SetupRpcDebuggingHook();

                    // Run all tests (blocking)
                    ApplicationTestRunner.RunAllTests(Assembly.GetExecutingAssembly());
                    Console.WriteLine("Tests completed");
                }
            }
        }

        readonly string _debugSessionID;
        readonly NamedPipeServerStream _pipeServer;
        readonly MeadowSolidityDebugAdapter _debugAdapter;

        private Debugging(string debugSessionID)
        {
            _debugSessionID = debugSessionID;
            _pipeServer = new NamedPipeServerStream(_debugSessionID, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _debugAdapter = new MeadowSolidityDebugAdapter();
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
            JsonRpcClient.JsonRpcExecutionAnalysis = RpcExecutionCallback;
        }

        public async Task RpcExecutionCallback(IJsonRpcClient client)
        {
            // Obtain an execution trace from our client.
            var executionTrace = await client.GetExecutionTrace();
            var executionTraceAnalysis = new ExecutionTraceAnalysis(executionTrace);

            // Process our execution trace in the debug adapter.
            await _debugAdapter.ProcessExecutionTraceAnalysis(executionTraceAnalysis);

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
