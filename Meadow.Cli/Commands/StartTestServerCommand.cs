using Meadow.JsonRpc.Client;
using Meadow.TestNode;
using System;
using System.Management.Automation;
using System.Net;
using System.Threading.Tasks;

namespace Meadow.Cli.Commands
{
    [Cmdlet(ApprovedVerbs.Start, "TestServer")]
    [Alias("startTestServer")]
    public class StartTestServerCommand : PSCmdlet
    {

        protected override void EndProcessing()
        {
            Console.WriteLine("Starting RPC test server...");

            var config = this.ReadConfig();

            var testNodeServer = new TestNodeServer((int)config.NetworkPort, new AccountConfiguration
            {
                AccountGenerationCount = config.AccountCount,
                DefaultAccountEtherBalance = config.AccountBalance
            });
            testNodeServer.RpcServer.Start();

            var serverAddress = testNodeServer.RpcServer.ServerAddresses[0];
            Console.WriteLine($"Started RPC test server at {serverAddress}");

            SessionState.SetTestNodeServer(testNodeServer);
        }

    }
}
