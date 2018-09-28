using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.TestNode.Test
{
    public class ParallelTests
    {
        [Fact]
        public void RunParallelNodes()
        {
            var tasks = new List<Task>();
            for (var thread = 0; thread < 4; thread++)
            {
                var task = Task.Run(CreateRunIteration);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        static async Task CreateRunIteration()
        {
            using (var server = new TestNodeServer())
            {
                await server.RpcServer.StartAsync();
                int port = server.RpcServer.ServerPort;

                var client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{port}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);

                await client.SetCoverageEnabled(true);

                var accounts = await client.Accounts();
                var contract = await BasicContract.New($"TestName", true, 34, client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
                var snapshotID = await client.Snapshot();
                var initialValCounter = await contract.getValCounter().Call();
                await contract.incrementValCounter();
                var valCounter2 = await contract.getValCounter().Call();
                await client.Revert(snapshotID);
                var finalValCounter = await contract.getValCounter().Call();
                Assert.Equal(0, initialValCounter);
                Assert.Equal(2, valCounter2);
                Assert.Equal(0, finalValCounter);

                var snapshotID2 = await client.Snapshot();
                var contract2 = await BasicContract.New($"TestName", true, 34, client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
                await contract2.incrementValCounter();
                await client.Revert(snapshotID2);

                var coverage = await client.GetCoverageMap(contract.ContractAddress);
            }
        }
    }
}
