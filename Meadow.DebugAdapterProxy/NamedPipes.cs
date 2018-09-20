using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.DebugAdapterProxy
{
    public static class NamedPipes
    {
        public static NamedPipeServerStream CreatePipeServer(string pipeName)
        {
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            return server;
        }

        public static NamedPipeClientStream CreatePipeClient(string pipeName)
        {
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            return client;            
        }

        public static async Task ConnectStream(Stream from, Stream to, Stream copy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var buffer = new byte[1024];
                int read;
                while (from.CanRead && to.CanWrite)
                {
                    read = await from.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (copy != null)
                    {
                        await copy.WriteAsync(buffer, 0, read, cancellationToken);
                    }

                    await to.WriteAsync(buffer, 0, read, cancellationToken);
                }
            }
            catch (NotSupportedException) { }
            catch (OperationCanceledException) { }
            catch (IOException) { }
        }
    }
}
