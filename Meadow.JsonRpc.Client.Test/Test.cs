using Meadow.Core.EthTypes;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.JsonRpc.Client.Test
{
    public class Test
    {
        [Fact]
        public void InitializeClient()
        {
            var client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{9999}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
        }

        [Fact]
        public async Task DynamicMethodLookup()
        {
            var client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{44999}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
            await Assert.ThrowsAsync<HttpRequestException>(async () => await client.Accounts());
        }

        [Fact]
        public async Task DirectImplementionMethod()
        {
            IRpcControllerMinimal client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{44999}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
            await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetTransactionReceipt(Hash.Zero));
        }
    }
}
