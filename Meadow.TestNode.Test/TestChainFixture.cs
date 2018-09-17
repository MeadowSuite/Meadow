using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Server;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.TestNode.Test
{
    /// <summary>
    /// Represents shared data to be used across tests within the same class.
    /// </summary>
    public class TestChainFixture : IDisposable
    {
        #region Properties
        /// <summary>
        /// The shared server which tests should use.
        /// </summary>
        public TestNodeServer Server { get; private set; }
        /// <summary>
        /// The shared client which tests should use.
        /// </summary>
        public IJsonRpcClient Client { get; private set; }

        #endregion

        #region Constructor
        /// <summary>
        /// The default constructor, creates a test chain for tests to share.
        /// </summary>
        public TestChainFixture()
        {
            // Create a test node on this port.
            Server = new Meadow.TestNode.TestNodeServer();
            Server.RpcServer.Start();
            int port = Server.RpcServer.ServerPort;

            // Create our client and grab our account list.
            Client = JsonRpcClient.Create(new Uri($"http://{IPAddress.Loopback}:{port}"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);

        }
        #endregion


        /// <summary>
        /// Disposes of the fixture and its underlying objects.
        /// </summary>
        public void Dispose()
        {
            // Stop the server
            Server.RpcServer.Dispose();
        }

    }
}
