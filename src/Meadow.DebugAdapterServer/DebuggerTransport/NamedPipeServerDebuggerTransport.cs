using System.IO;
using System.IO.Pipes;

namespace Meadow.DebugAdapterServer.DebuggerTransport
{
    public class NamedPipeServerDebuggerTransport : IDebuggerTransport
    {
        readonly NamedPipeServerStream _pipeServer;

        public Stream InputStream => _pipeServer;
        public Stream OutputStream => _pipeServer;

        public void Dispose()
        {
            try
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
            catch { }

            _pipeServer.Dispose();
        }

        public NamedPipeServerDebuggerTransport()
        {
            // Setup named pipe server.
            var debugSessionID = SolidityDebugger.SolidityDebugSessionID;
            _pipeServer = new NamedPipeServerStream(debugSessionID, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            // Wait for debug adapter proxy to connect.
            _pipeServer.WaitForConnection();
        }

    }

}
