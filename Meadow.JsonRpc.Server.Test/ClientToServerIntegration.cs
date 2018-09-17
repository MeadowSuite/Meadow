
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.JsonRpc.Server.Test
{
    public class RpcApp : IDisposable
    {
        public readonly MockServerApp Server;
        public readonly IJsonRpcClient Client;

        public RpcApp()
        {
            Server = new MockServerApp();
            Server.RpcServer.WebHost.Start();
            var port = Server.RpcServer.ServerPort;

            Client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{port}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
        }

        public void Dispose()
        {
            Task.Run(async () => await Server.RpcServer.WebHost.StopAsync());
            Server.RpcServer.WebHost.Dispose();
        }
    }

    public class ClientToServerIntegration : IClassFixture<RpcApp>
    {
        public readonly MockServerApp Server;
        public readonly IJsonRpcClient Client;

        public ClientToServerIntegration(RpcApp rpcApp)
        {
            Server = rpcApp.Server;
            Client = rpcApp.Client;
        }

        /*
        [Fact]
        public async Task ExampleContractTest()
        {
            Meadow.Contract.ContractFactory.DefaultRpcClient = Client;
            Address from = "0x2a65aca4d5fc5b5c859090a6c34d164135398226";
            var exContract = await ExampleContract.New(
                ("my contract", true, 359845),
                new SendParams { From = "0x32be343b94f860124dc4fee278fdcbd38c102d88" },
                defaultFromAccount: from
            );
            var res = await exContract.myFunc(4359845);
        }
        */

        [Fact]
        public async Task Syncing()
        {
            var syncing = await Client.Syncing();
        }

        [Fact]
        public async Task GetFilterLogs()
        {
            var filterLogs = await Client.GetFilterLogs(0);
        }

        [Fact]
        public async Task Mine()
        {
            await Client.Mine();
        }

        [Fact]
        public async Task Version()
        {
            var ver = await Client.Version();
        }

        [Fact]
        public async Task ProtocolVersion()
        {
            var protoVer = await Client.ProtocolVersion();
        }

        [Fact]
        public async Task Accounts()
        {
            var accounts = await Client.Accounts();
        }

        [Fact]
        public async Task GetBalance()
        {
            var accounts = await Client.Accounts();
            var balance = await Client.GetBalance(accounts[0], DefaultBlockParameter.Default);
        }

        [Fact]
        public async Task GetBalance2()
        {
            var accounts = await Client.Accounts();
            var balance2 = await Client.GetBalance(accounts[0], 1234);
        }

        [Fact]
        public async Task BlockNumber()
        {
            var blockNum = await Client.BlockNumber();
        }


    }
}
