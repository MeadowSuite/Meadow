using Meadow.Core.AccountDerivation;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.TestNode.Host
{
    class Program
    {

        static async Task Main(string[] args)
        {
            var opts = ProcessArgs.Parse(args);

            if (!string.IsNullOrWhiteSpace(opts.Proxy))
            {
                // TODO: testnode.host proxy support
                throw new NotImplementedException();
            }

            IPAddress host;
            if (string.IsNullOrWhiteSpace(opts.Host) || opts.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                host = IPAddress.Loopback;
            }
            else if (opts.Host == "*")
            {
                host = IPAddress.Any;
            }
            else if (IPAddress.TryParse(opts.Host, out var addr))
            {
                host = addr;
            }
            else
            {
                var entry = await Dns.GetHostEntryAsync(opts.Host);
                host = entry.AddressList[0];
            }

            // Setup account derivation / keys
            IAccountDerivation accountDerivation;
            if (string.IsNullOrWhiteSpace(opts.Mnemonic))
            {
                var hdWalletAccountDerivation = HDAccountDerivation.Create();
                accountDerivation = hdWalletAccountDerivation;
                Console.WriteLine($"Using mnemonic phrase: '{hdWalletAccountDerivation.MnemonicPhrase}'");
                Console.WriteLine("Warning: this private key generation is not secure and should not be used in production.");
            }
            else
            {
                accountDerivation = new HDAccountDerivation(opts.Mnemonic);
            }


            var accountConfig = new AccountConfiguration
            {
                AccountGenerationCount = (int)(opts.AccountCount ?? 100),
                DefaultAccountEtherBalance = opts.AccountBalance ?? 1000,
                AccountDerivationMethod = accountDerivation
            };

            // Create our local test node.
            using (var testNodeServer = new TestNodeServer(
                port: (int)opts.Port.GetValueOrDefault(),
                address: host,
                accountConfig: accountConfig))
            {
                Console.WriteLine("Starting server...");

                // Start our local test node.
                await testNodeServer.RpcServer.StartAsync();

                // Create an RPC client for our local test node.
                var serverAddresses = string.Join(", ", testNodeServer.RpcServer.ServerAddresses);
                Console.WriteLine($"Test node server listening on: {serverAddresses}");

                // Listen for exit request.
                var exitEvent = new SemaphoreSlim(0, 1);
                Console.CancelKeyPress += (s, e) =>
                {
                    exitEvent.Release();
                    e.Cancel = true;
                };

                // Shutdown.
                await exitEvent.WaitAsync();
                Console.WriteLine("Stopping server and exiting...");
                await testNodeServer.RpcServer.StopAsync();
            }
        }
    }
}
