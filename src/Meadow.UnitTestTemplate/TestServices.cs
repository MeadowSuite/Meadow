using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Client;
using Meadow.TestNode;

namespace Meadow.UnitTestTemplate
{
    public class TestServices
    {
        #region Properties
        /// <summary>
        /// The current <see cref="JsonRpcClient"/> used to run tests with.
        /// </summary>
        public IJsonRpcClient TestNodeClient { get; }

        /// <summary>
        /// The <see cref="TestNodeServer"/> used to run tests against.
        /// NULL if this is testing against an external node.
        /// </summary>
        public TestNodeServer TestNodeServer { get; }

        /// <summary>
        /// The cache of initially created accounts on the test node.
        /// </summary>
        public Address[] Accounts { get; }

        /// <summary>
        /// Indicates the test services are running on an external node, not provided in this object.
        /// </summary>
        public bool IsExternalNode
        {
            get
            {
                return TestNodeServer == null;
            }
        }
        #endregion

        #region Constructors
        public TestServices(IJsonRpcClient testNodeClient, TestNodeServer testNodeServer, Address[] accounts)
        {
            TestNodeClient = testNodeClient;
            TestNodeServer = testNodeServer;
            Accounts = accounts;
        }
        #endregion
    }
}
