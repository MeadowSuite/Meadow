using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types.Debugging
{
    public class CompoundCoverageMap
    {
        #region Properties
        /// <summary>
        /// Represents the coverage map for the undeployed/deploying code.
        /// </summary>
        [JsonProperty("undeployedMap")]
        public CoverageMap UndeployedMap { get; set; }
        /// <summary>
        /// Represents the coverage map for the deployed code.
        /// </summary>
        [JsonProperty("deployedMap")]
        public CoverageMap DeployedMap { get; set; }
        /// <summary>
        /// Obtain a relevant contract address from our underlying deployment maps (or returns zero if both deployment maps are null).
        /// </summary>
        public Address ContractAddress
        {
            get
            {
                if (DeployedMap != null)
                {
                    return DeployedMap.ContractAddress;
                }

                if (UndeployedMap != null)
                {
                    return UndeployedMap.ContractAddress;
                }

                return Address.Zero;
            }
        }
        #endregion
    }
}
