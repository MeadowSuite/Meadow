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
                // Add the geth bootstrap node.
                BootstrapNodes.Add(new ENodeUri("enode://6cdd090303f394a1cac34ecc9f7cda18127eafa2a3a06de39f6d920b0e583e062a7362097c7c65ee490a758b442acd5c80c6fce4b148c6a391e946b45131365b@54.169.166.226:30303"));
            }
        }
        #endregion
    }
}
