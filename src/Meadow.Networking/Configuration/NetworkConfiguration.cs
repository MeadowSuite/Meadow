using Meadow.Networking.Protocol.Addressing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Meadow.Networking.Configuration
{
    public class NetworkConfiguration
    {
        #region Properties
        public bool ListeningEnabled { get; set; }
        public IPAddress ListeningHost { get; set; }
        public ushort ListeningPort { get; set; }

        public List<ENodeUri> BootstrapNodes { get; set; }
        #endregion

        #region Constructors
        public NetworkConfiguration(bool defaultBootstrapServers = true)
        {
            // Initialize our properties with default values
            BootstrapNodes = new List<ENodeUri>();

            // Add our default bootstrap nodes
            if (defaultBootstrapServers)
            {
                // Add the bootstrap nodes.
                BootstrapNodes.AddRange(DefaultBootstrapNodes.MainNetNodes);
            }
        }
        #endregion
    }
}
