using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.JsonRpc.Server.Test
{
    public class Test
    {
        [Fact]
        public async Task SetupTeardown()
        {
            var httpServer = new JsonRpcHttpServer(new MockRpcController());
            await httpServer.StartAsync();
            await Task.Delay(100);
            await httpServer.StopAsync();
            httpServer.Dispose();
        }
    }
}
