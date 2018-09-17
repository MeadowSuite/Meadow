using System;
using System.Net;
using System.Threading.Tasks;

namespace Meadow.JsonRpc.Server.Proxy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TODO: command line args for port and host

            // Default ganache gui port.
            int targetServerPort = 7545;

            // Default to localhost.
            string host = IPAddress.Loopback.ToString();

            int proxyServerPort = 7117;

            string proxyUri = $"http://{IPAddress.Loopback}:{proxyServerPort}";
            string serverUri = $"http://{host}:{targetServerPort}";

            Console.WriteLine($"Proxying calls to server at {serverUri}");

            Console.WriteLine($"Launching server proxy, listening at {proxyUri}");
            var rpcServerProxy = new RpcServerProxy(host, proxyServerPort, targetServerPort);
            await rpcServerProxy.StartServerAsync();
            Console.WriteLine("Server started and listening...");

            await rpcServerProxy.WaitForServerShutdownAsync();
        }
    }
}
