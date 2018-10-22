using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using Meadow.Networking.Protocol.Addressing;
using System;
using System.Net;
using Xunit;

namespace Meadow.Networking.Test
{
    public class ENodeTests
    {
        private void AssertComponentsToENodeUri(byte[] nodeId, IPAddress address, int tcpPort, int udpPort, bool assertENodeUri = true)
        {
            // Declare our ENode which will be parsed
            ENode node = null;

            // Determine if we should expect an error
            if (nodeId == null)
            {
                // An ArgumentNullException should occur.
                Assert.Throws<ArgumentNullException>(() => { node = new ENode(nodeId, address, tcpPort, udpPort); });
            }
            else if (nodeId.Length != ENode.NODE_ID_SIZE)
            {
                // An ArgumentException should occur.
                Assert.Throws<ArgumentException>(() => { node = new ENode(nodeId, address, tcpPort, udpPort); });
            }

            // We should be able to initialize successfully.
            node = new ENode(nodeId, address, tcpPort, udpPort);

            // Determine the address portion of the string
            string addressStr = address.ToString();
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                addressStr = $"[{addressStr}]";
            }

            // Determine the port portion of our string
            string expectedPortStr = $"{tcpPort}";
            if (tcpPort != udpPort)
            {
                expectedPortStr += $"?discport={node.UDPDiscoveryPort}";
            }

            // Determine the full format of our expected string
            string expectedStr = $"{ENode.URI_SCHEME}://{nodeId.ToHexString(false)}@{addressStr}:{expectedPortStr}";
            string actualStr = node.ToString();

            // Assert our string formatting is correct.
            Assert.Equal(expectedStr, actualStr);

            // If we are to assert the created uri, we do that now
            if (assertENodeUri)
            {
                AssertENodeUri(actualStr, node.NodeId, node.Address, node.TCPListeningPort, node.UDPDiscoveryPort);
            }
        }

        private void AssertENodeUri(string eNodeUri, byte[] nodeId, IPAddress address, int tcpPort, int udpPort)
        {
            // Parse the node
            ENode node = new ENode(eNodeUri);

            // Assert all components
            Assert.Equal(nodeId, node.NodeId);
            Assert.Equal(ENode.NODE_ID_SIZE, node.NodeId.Length);
            Assert.Equal(address, node.Address);
            Assert.Equal(tcpPort, node.TCPListeningPort);
            Assert.Equal(udpPort, node.UDPDiscoveryPort);
        }

        [Fact]
        public void ENodeFromComponents()
        {
            // Create a new node id.
            byte[] nodeId = new byte[ENode.NODE_ID_SIZE];

            // Create our IP address
            IPAddress address = IPAddress.Loopback;

            // Fill it with random data
            Random random = new Random();
            random.NextBytes(nodeId);

            // Test ENode with same port numbers. (IPv4)
            AssertComponentsToENodeUri(nodeId, address, 800, 800);

            // Test ENode with different port numbers. (IPv4)
            AssertComponentsToENodeUri(nodeId, address, 800, 801);

            // Change the IP address to an IPv6 address.
            address = IPAddress.IPv6Loopback;

            // Test ENode with same port numbers. (IPv6)
            AssertComponentsToENodeUri(nodeId, address, 800, 800);

            // Test ENode with different port numbers. (IPv6)
            AssertComponentsToENodeUri(nodeId, address, 800, 801);
        }

        [Fact]
        public void ENodeFromUriSamePortImplicit()
        {
            // Create an ENode URI
            string nodeUri = "enode://00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000@127.0.0.1:800";

            // Define our expected variables.
            byte[] expectedNodeId = new byte[ENode.NODE_ID_SIZE];
            IPAddress expectedIPAddress = IPAddress.Parse("127.0.0.1");
            int expectedTcpPort = 800;
            int expectedUdpPort = 800;

            // Parse our node
            ENode node = new ENode(nodeUri);

            // Verify the components
            Assert.Equal(expectedNodeId, node.NodeId);
            Assert.Equal(expectedIPAddress, node.Address);
            Assert.Equal(expectedTcpPort, node.TCPListeningPort);
            Assert.Equal(expectedUdpPort, node.UDPDiscoveryPort);
        }

        [Fact]
        public void ENodeFromUriSamePortExplicit()
        {
            // Create an ENode URI
            string nodeUri = "enode://ff0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000aa@127.0.0.1:800?discport=800";

            // Define our expected variables.
            byte[] expectedNodeId = new byte[ENode.NODE_ID_SIZE];
            expectedNodeId[0] = 0xff;
            expectedNodeId[expectedNodeId.Length - 1] = 0xaa;
            IPAddress expectedIPAddress = IPAddress.Parse("127.0.0.1");
            int expectedTcpPort = 800;
            int expectedUdpPort = 800;

            // Parse our node
            ENode node = new ENode(nodeUri);

            // Verify the components
            Assert.Equal(expectedNodeId, node.NodeId);
            Assert.Equal(expectedIPAddress, node.Address);
            Assert.Equal(expectedTcpPort, node.TCPListeningPort);
            Assert.Equal(expectedUdpPort, node.UDPDiscoveryPort);
        }

        [Fact]
        public void ENodeFromUriDifferentPort()
        {
            // Create an ENode URI
            string nodeUri = "enode://ff0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000aa@101.101.101.101:800?discport=801";

            // Define our expected variables.
            byte[] expectedNodeId = new byte[ENode.NODE_ID_SIZE];
            expectedNodeId[0] = 0xff;
            expectedNodeId[expectedNodeId.Length - 1] = 0xaa;
            IPAddress expectedIPAddress = IPAddress.Parse("101.101.101.101");
            int expectedTcpPort = 800;
            int expectedUdpPort = 801;

            // Parse our node
            ENode node = new ENode(nodeUri);

            // Verify the components
            Assert.Equal(expectedNodeId, node.NodeId);
            Assert.Equal(expectedIPAddress, node.Address);
            Assert.Equal(expectedTcpPort, node.TCPListeningPort);
            Assert.Equal(expectedUdpPort, node.UDPDiscoveryPort);
        }

        [Fact]
        public void ENodeVerifyValuesExceptions()
        {
            // Verify our node fails with the incorrect scheme. (0x3f instead of expected 0x40)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enodee://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f@101.101.101.101:800?discport=801"); });
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f@101.101.101.101:800?discport=801"); });

            // Verify our node fails with the incorrect size node id. (0x3f instead of expected 0x40)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f@101.101.101.101:800?discport=801"); });

            // Verify our node fails with the incorrect size node id. (0x41 instead of expected 0x40)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f4041@101.101.101.101:800?discport=801"); });

            // Verify our node fails with the invalid IP.
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=801"); });

            // Verify our node fails with the invalid TCP port. (Too Big)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:80000?discport=801"); });

            // Verify our node fails with the invalid TCP port. (Too Small)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:-1?discport=801"); });

            // Verify our node fails with the invalid UDP port. (Too Big)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=80000"); });

            // Verify our node fails with the invalid UDP port. (Too Small)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=-1"); });

            // Verify our node fails with the seperator symbols being invalid. (incorrect slash direction, should be //)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode:\\0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=800"); });

            // Verify our node fails with the seperator symbols being invalid. (replaced @ with !)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40!256.101.101.101:800?discport=800"); });

            // Verify our node fails with the seperator symbols being invalid. (replaced : with ;)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101;800?discport=800"); });

            // Verify our node fails with the seperator symbols being invalid. (replaced ? with &)
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800&discport=800"); });

            // Verify our node fails with data leading and trailing before it
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("blah blah enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=800"); });
            Assert.Throws<ArgumentException>(() => { ENode node = new ENode("enode://0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40@256.101.101.101:800?discport=800 blah blah"); });
        }
    }
}
