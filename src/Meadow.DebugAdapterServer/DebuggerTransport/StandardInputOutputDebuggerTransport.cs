using System;
using System.IO;

namespace Meadow.DebugAdapterServer.DebuggerTransport
{
    public class StandardInputOutputDebuggerTransport : IDebuggerTransport
    {
        public Stream InputStream { get; }

        public Stream OutputStream { get; }

        public StandardInputOutputDebuggerTransport()
        {
            InputStream = Console.OpenStandardInput();
            OutputStream = Console.OpenStandardOutput();
        }

        public void Dispose() { }
    }

}
