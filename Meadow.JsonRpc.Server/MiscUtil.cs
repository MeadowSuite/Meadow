using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Meadow.JsonRpc.Server
{
    public static class MiscUtil
    {
        public static int GetAvailablePort()
        {
            IPEndPoint defaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(defaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

    }
}
