using Meadow.JsonRpc.Client;
using Meadow.TestNode;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.JsonRpc.Server.Proxy.Test
{
    public class IntegrationTests
    {
        [Theory]
        [InlineData("http")]
        [InlineData("ws")]
        public async Task ProxyIntegrationTest(string protocol)
        {
            // create test node server
            using (var server = new TestNodeServer())
            {
                await server.RpcServer.StartAsync();

                // create proxy server pointed to test node server
                var httpServerEndPoint = new Uri($"{protocol}://{IPAddress.Loopback}:{server.RpcServer.ServerPort}");
                using (var proxyServer = new RpcServerProxy(httpServerEndPoint))
                {
                    await proxyServer.StartServerAsync();

                    // create rpc client pointed to proxy server
                    var proxyServerUri = new Uri($"{protocol}://{IPAddress.Loopback}:{proxyServer.ProxyServerPort}");
                    using (var client = JsonRpcClient.Create(proxyServerUri, 100, 100))
                    {
                        var version = await client.Version();
                    }
                }
            }
        }
    }
}
