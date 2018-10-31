using System;
using System.IO;

namespace Meadow.DebugAdapterServer.DebuggerTransport
{
    public interface IDebuggerTransport : IDisposable
    {
        Stream InputStream { get; }
        Stream OutputStream { get; }
    }

}
