using Meadow.JsonRpc.Client;
using Meadow.TestNode;
using System;
using System.Management.Automation;
using System.Net;
using System.Threading.Tasks;
using Meadow.JsonRpc.Server;

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

            var testNodeServer = new TestNodeServer(port: (int)config.NetworkPort, accountConfig: new AccountConfiguration
            {
                AccountGenerationCount = config.AccountCount,
                DefaultAccountEtherBalance = config.AccountBalance
            });
            testNodeServer.RpcServer.Start();

            var serverAddress = testNodeServer.RpcServer.ServerAddresses[0];

            Console.WriteLine($"Started RPC test server at {serverAddress}\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Server instance information has been saved to the global variable $testNodeServer\n");
            Console.WriteLine("$testNodeServer can be used to control the server instance i.e $testNodeServer.RpcServer.Stop()...");
            Console.WriteLine("...or with the Stop-TestServer command i.e Stop-TestServer -TestNodeServer $testNodeServer");
            Console.ResetColor();

            SessionState.SetTestNodeServer(testNodeServer);

            WriteObject(new { TestNodeServer = testNodeServer });
        }
    }

    [Cmdlet(VerbsLifecycle.Stop, "TestServer")]
    [Alias("stopTestServer")]
    public class StopTestServerCommand : PSCmdlet
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public TestNodeServer TestNodeServer { get; set; }

        protected override void EndProcessing()
        {
            Console.WriteLine("Stopping RPC test server...\n");
            
            var testNodeServer = TestNodeServer;
            testNodeServer.RpcServer.Stop();                //Will replace this with the appropriate async method MM

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Test server is stopped.");
            Console.ResetColor();
        }
    }
}
