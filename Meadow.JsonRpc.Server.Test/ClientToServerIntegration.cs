
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
        public readonly IJsonRpcClient HttpClient;
        public readonly IJsonRpcClient WebSocketClient;

        public RpcApp()
        {
            Server = new MockServerApp();
            Server.RpcServer.WebHost.Start();
            var port = Server.RpcServer.ServerPort;

            HttpClient = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{port}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
            WebSocketClient = JsonRpcClient.Create(new Uri($"ws://{IPAddress.Loopback}:{port}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);
        }

        public void Dispose()
        {
            HttpClient.Dispose();
            WebSocketClient.Dispose();
            Task.Run(async () => await Server.RpcServer.WebHost.StopAsync());
            Server.RpcServer.WebHost.Dispose();
        }
    }

    public class ClientToServerIntegration : IClassFixture<RpcApp>
    {
        public readonly MockServerApp Server;
        public readonly IJsonRpcClient HttpClient;
        public readonly IJsonRpcClient WebSocketClient;

        public ClientToServerIntegration(RpcApp rpcApp)
        {
            Server = rpcApp.Server;
            HttpClient = rpcApp.HttpClient;
            WebSocketClient = rpcApp.WebSocketClient;
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
            await HttpClient.Syncing();
            await WebSocketClient.Syncing();
        }

        [Fact]
        public async Task GetFilterLogs()
        {
            await HttpClient.GetFilterLogs(0);
            await WebSocketClient.GetFilterLogs(0);
        }

        [Fact]
        public async Task Mine()
        {
            await HttpClient.Mine();
            await WebSocketClient.Mine();
        }

        [Fact]
        public async Task Version()
        {
            await HttpClient.Version();
            await WebSocketClient.Version();
        }

        [Fact]
        public async Task ProtocolVersion()
        {
            await HttpClient.ProtocolVersion();
            await WebSocketClient.ProtocolVersion();
        }

        [Fact]
        public async Task Accounts()
        {
            await HttpClient.Accounts();
            await WebSocketClient.Accounts();
        }

        [Fact]
        public async Task GetBalance()
        {
            var accounts = await HttpClient.Accounts();
            await HttpClient.GetBalance(accounts[0], DefaultBlockParameter.Default);

            var accounts2 = await WebSocketClient.Accounts();
            await WebSocketClient.GetBalance(accounts2[0], DefaultBlockParameter.Default);
        }

        [Fact]
        public async Task GetBalance2()
        {
            var accounts = await HttpClient.Accounts();
            await HttpClient.GetBalance(accounts[0], 1234);

            var accounts2 = await WebSocketClient.Accounts();
            await WebSocketClient.GetBalance(accounts2[0], 1234);
        }

        [Fact]
        public async Task BlockNumber()
        {
            await HttpClient.BlockNumber();
            await WebSocketClient.BlockNumber();
        }


    }
}
