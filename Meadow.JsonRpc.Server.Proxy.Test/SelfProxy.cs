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
    public class SelfProxy
    {
        [Fact]
        public async Task SimpleTest()
        {
            // TODO: setup command line arg parsing in Meadow.JsonRpc.Server.Proxy.Program
            //       and call it with the correct port, then implement some simple rpc tests.
            /*
            using (var server = new TestNodeServer())
            {
                await server.RpcServer.StartAsync();
                int port = server.RpcServer.ServerPort;

                var client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{port}"));



            }
            */

            await Task.CompletedTask;
        }
    }
}
