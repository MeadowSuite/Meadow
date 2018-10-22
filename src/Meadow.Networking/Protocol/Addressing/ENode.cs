using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Meadow.Networking.Protocol.Addressing
{
    /*
     * References:
     * https://github.com/ethereum/wiki/wiki/enode-url-format
     */

    /// <summary>
    /// Represents the URI scheme for an Ethereum node.
    /// </summary>
    public class ENode
    {
        #region Constant
        /// <summary>
        /// The scheme of the URI, the beginning portion which indicates the protocol type.
        /// </summary>
        public const string URI_SCHEME = "enode";
        /// <summary>
        /// The size of a node id/username in the ENode format (in bytes, before being converted to a hex string).
        /// </summary>
        public const int NODE_ID_SIZE = EthereumEcdsa.PUBLIC_KEY_SIZE;
        #endregion

        #region Properties
        /// <summary>
        /// The identifier for this node, which is represented by a hex string of the user's public key.
        /// </summary>
        public byte[] NodeId { get; }
        /// <summary>
        /// The IP address to reach the node at.
        /// </summary>
        public IPAddress Address { get; }
        /// <summary>
        /// The TCP port which is used to listen for incoming connections on.
        /// </summary>
        public int TCPListeningPort { get; }
        /// <summary>
        /// The UDP port used for broadcasting/discovery.
        /// </summary>
        public int UDPDiscoveryPort { get; }

        public string NodeUri { get; }
        #endregion

        #region Constructor
        public ENode(string nodeUri)
        {
            // We design our regular expression to capture data as follows:
            // 1) "^\s*{URI_SCHEME}\:\/\/" : Verifies the underlying URI scheme and the formatting surrounding it. Allows leading whitespace.
            // 2) "([a-fA-F0-9]+)" : We verify the NodeId/Username follows (1), this verifies it is a hex string.
            // 3) "\@" : Seperates NodeId and Address
            // 4) "(\S+)" : Captures any non-whitespace character for the Address portion. This is verified later for more fine exception handling.
            // 5) "\:" : Seperates the Address and TCPListeningPort sections of the URI.
            // 6) "(\d+)" : Captures the TCPListeningPort number.
            // 7) "(?:\?discport=(\d+))?$" : The optional differing UDPDiscoveryPort, used if it differs from TCPListeningPort. 
            //      This captures the port number and verifies the string ends, allowing for trailing whitespace.

            // Create our regular expression to capture all information from our type.
            Regex regularExpression = new Regex($@"^\s*{URI_SCHEME}\:\/\/([a-fA-F0-9]+)\@(\S+)\:(\d+)(?:\?discport=(\d+))?\s*$", RegexOptions.IgnoreCase);

            // Match the regular expression pattern against a text string.
            Match match = regularExpression.Match(nodeUri);

            // Verify we matched, if not, then there was no location in the type to extract, instead the whole string is likely just base type.
            if (!match.Success)
            {
                throw new ArgumentException("Invalid ENode URI format.");
            }

            // Obtain the node id from the first group.
            NodeId = match.Groups[1].Value.HexToBytes();

            // Verify the length of our node id.
            if (NodeId.Length != NODE_ID_SIZE)
            {
                throw new ArgumentException($"Invalid NodeId provided when parsing ENode URI format. Expected length is {NODE_ID_SIZE} bytes. Given {NodeId.Length} bytes.");
            }

            // Parse the address component of the uri
            string ipAddressStr = match.Groups[2].Value;
            if (IPAddress.TryParse(ipAddressStr, out IPAddress address))
            {
                Address = address;
            }
            else
            {
                throw new ArgumentException($"Invalid IP Address provided when parsing ENode URI format. Given: {ipAddressStr}");
            }

            // Parse the TCP listening port (and set the UDP one as the same by default, if one is explicitly stated later, it'll be overriden).
            if (ushort.TryParse(match.Groups[3].Value, out ushort tcpListeningPort))
            {
                TCPListeningPort = tcpListeningPort;
                UDPDiscoveryPort = TCPListeningPort;
            }
            else
            {
                throw new ArgumentException($"Invalid TCP listening port provided when parsing ENode URI format. Must be between 0-{ushort.MaxValue}");
            }

            // Parse the UDP discovery port.
            if (!string.IsNullOrEmpty(match.Groups[4].Value))
            {
                if (ushort.TryParse(match.Groups[4].Value, out ushort udpDiscoveryPort))
                {
                    UDPDiscoveryPort = udpDiscoveryPort;
                }
                else
                {
                    throw new ArgumentException($"Invalid UDP discovery port provided when parsing ENode URI format. Must be between 0-{ushort.MaxValue}");
                }
            }

            // Set our node URI
            NodeUri = nodeUri;
        }

        public ENode(byte[] nodeId, IPAddress address, int port) : this(nodeId, address, port, port) { }

        public ENode(byte[] nodeId, IPAddress address, int tcpPort, int udpPort)
        {
            // Set our properties
            NodeId = nodeId;
            Address = address;
            TCPListeningPort = tcpPort;
            UDPDiscoveryPort = udpPort;

            // Next, we format our uri string

            // Obtain the username/node id as a string
            string nodeIdStr = NodeId.ToHexString(false);

            // Obtain the 
            string endpointStr = new IPEndPoint(Address, TCPListeningPort).ToString();

            // Obtain the remainder of the string (port string).
            string discPortStr = "";
            if (TCPListeningPort != UDPDiscoveryPort)
            {
                discPortStr = $"?discport={UDPDiscoveryPort}";
            }
           
            // Format the enode string.
            NodeUri = $"{URI_SCHEME}://{nodeIdStr}@{endpointStr}{discPortStr}";
        }
        #endregion

        #region Functions
        public override string ToString()
        {
            // Return our uri
            return NodeUri;
        }
        #endregion
    }
}
