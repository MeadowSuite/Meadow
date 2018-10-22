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
        public bool Listening { get; set; }
        public IPAddress ListeningHost { get; set; }
        public ushort ListeningPort { get; set; }

        public List<ENode> BootstrapNodes { get; set; }
        #endregion

        #region Constructors
        public NetworkConfiguration()
        {
            // TODO: Implement
        }
        #endregion
    }
}
