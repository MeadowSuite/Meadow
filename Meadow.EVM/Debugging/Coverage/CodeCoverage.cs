using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Instructions;
using Meadow.EVM.EVM.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Debugging.Coverage
{
    /// <summary>
    /// Tracks code execution coverage for testing purposes.
    /// </summary>
    public class CodeCoverage
    {
        #region Fields
        /// <summary>
        /// A lookup for contract addresses to their coverage maps (at the time of deployment).
        /// </summary>
        private Dictionary<Address, CoverageMap> _coverageMapsUndeployed;
        /// <summary>
        /// A lookup for contract addresses to their coverage maps (after deployment).
        /// </summary>
        private Dictionary<Address, CoverageMap> _coverageMapsDeployed;
        #endregion

        #region Properties
        /// <summary>
        /// Indicates whether or not coverage maps are being recorded.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// A set of addresses which will be ignored when constructing the coverage map.
        /// </summary>
        public HashSet<Address> IgnoreList { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// The default constructor for the coverage map.
        /// </summary>
        public CodeCoverage()
        {
            // Initialize our coverage map lookup and ignore list
            _coverageMapsUndeployed = new Dictionary<Address, CoverageMap>();
            _coverageMapsDeployed = new Dictionary<Address, CoverageMap>();
            IgnoreList = new HashSet<Address>();

            // Set coverage maps as disabled by default
            Enabled = false;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Gets an existing coverage map from the specified address or returns null if it does not exist.
        /// </summary>
        /// <param name="contractAddress">The address of the contract to provide a coverage map for.</param>
        public (CoverageMap undeployedMap, CoverageMap deployedMap) Get(Address contractAddress)
        {
            // Try to obtain our undeployed code coverage.
            CoverageMap undeployedResult = null;
            _coverageMapsUndeployed.TryGetValue(contractAddress, out undeployedResult);

            // Try to obtain our deployed code coverage.
            CoverageMap deployedResult = null;
            _coverageMapsDeployed.TryGetValue(contractAddress, out deployedResult);

            // And we return it
            return (undeployedResult, deployedResult);
        }

        /// <summary>
        /// Gets all existing coverage maps that exist in this code coverage configuration.
        /// </summary>
        public (CoverageMap undeployedMap, CoverageMap deployedMap)[] GetAll()
        {
            // Create list of all coverage tuples.
            List<(CoverageMap undeployedMap, CoverageMap deployedMap)> result = new List<(CoverageMap undeployedMap, CoverageMap deployedMap)>();

            // Add the deployed map/undeployed map for that each address we have in deployed contracts.
            foreach (Address contractAddress in _coverageMapsDeployed.Keys)
            {
                // Obtain the maps for this address.
                result.Add(Get(contractAddress));
            }

            // We'll want to obtain maps which have undeployed maps but not deployed.
            foreach (Address contractAddress in _coverageMapsUndeployed.Keys)
            {
                // If the address is already in deployed list, we already processed it
                if (_coverageMapsDeployed.ContainsKey(contractAddress))
                {
                    continue;
                }

                // Obtain the maps for this address.
                result.Add(Get(contractAddress));
            }

            // Return all code coverage maps
            return result.ToArray();
        }

        /// <summary>
        /// Gets an existing coverage map from the specified address, or creates one if it doesn't exist, with the specified code.
        /// </summary>
        /// <param name="code">The code which we are producing a coverage map for.</param>
        public CoverageMap Register(EVMMessage message, Memory<byte> code)
        {            
            // If the code size is 0, we don't create any coverage maps.
            if (code.Length == 0)
            {
                return null;
            }

            // Determine our deployed address
            Address deployedAddress = message.GetDeployedCodeAddress();

            // If we're disabled or on the ignore list, we don't create a coverage map.
            if (!Enabled || IgnoreList.Contains(deployedAddress))
            {
                return null;
            }

            // Determine if we're deploying
            bool undeployed = message.CodeAddress != deployedAddress;

            // We use seperate maps to check out our coverage map.
            var lookup = undeployed ? _coverageMapsUndeployed : _coverageMapsDeployed;

            // If we haven't initialized our coverage map, we do that.
            if (!lookup.TryGetValue(deployedAddress, out var coverageMap))
            {
                coverageMap = new CoverageMap(deployedAddress, code);
                lookup[deployedAddress] = coverageMap;
            }

            // And we return it
            return coverageMap;
        }

        /// <summary>
        /// Clears all coverage maps.
        /// </summary>
        public void Clear(bool deployedMaps = true, bool undeployedMaps = true)
        {
            // Clear all coverage maps
            if (deployedMaps)
            {
                _coverageMapsDeployed.Clear();
            }

            if (undeployedMaps)
            {
                _coverageMapsUndeployed.Clear();
            }
        }

        /// <summary>
        /// Clears the coverage map for the contract at the provided address.
        /// </summary>
        /// <param name="contractAddress">The address of the contract for which we wish to clear the coverage map.</param>
        /// <returns>Returns true if the contract address was in the list and removed, false if it was not in the list at all.</returns>
        public bool Clear(Address contractAddress, bool deployedMaps = true, bool undeployedMaps = true)
        {
            // Remove the coverage map for the given contract address.
            bool removed = false;
            if (deployedMaps)
            {
                removed |= _coverageMapsDeployed.Remove(contractAddress);
            }

            if (undeployedMaps)
            {
                removed |= _coverageMapsUndeployed.Remove(contractAddress);
            }

            // Return our removed status.
            return removed;
        }
        #endregion

        #region Classes
        public class CoverageMap
        {
            #region Properties
            public Address ContractAddress { get; }
            public HashSet<int> JumpOffsets { get; }
            public HashSet<int> NonJumpOffsets { get; }
            public Memory<uint> Map { get; }
            public Memory<byte> Code { get; }
            #endregion

            #region Constructor
            public CoverageMap(Address contractAddress, Memory<byte> code)
            {
                // Set our fields
                Code = code;
                Map = new uint[code.Length];
                JumpOffsets = new HashSet<int>();
                NonJumpOffsets = new HashSet<int>();

                // Set our properties
                ContractAddress = contractAddress;
            }
            #endregion

            #region Functions
            /// <summary>
            /// Records the access of data in our coverage map at the provided offset with the provided size.
            /// </summary>
            /// <param name="offset">The offset at which we wish to record an access.</param>
            /// <param name="size">The size of the access to record.</param>
            public void RecordExecution(uint offset, uint size)
            {
                // Obtain a span from this map
                Span<uint> coverageMapSpan = Map.Span;

                // We loop for every byte we wish to cover.
                for (uint i = 0; i < size; i++)
                {
                    // Increment the counter for that byte.
                    coverageMapSpan[(int)(offset + i)]++;
                }
            }

            /// <summary>
            /// Records a branch in our coverage map,
            /// </summary>
            /// <param name="offset"></param>
            /// <param name="jumped"></param>
            public void RecordBranch(uint offset, bool jumped)
            {
                // Add our instruction offset to the relevant list.
                if (jumped)
                {
                    JumpOffsets.Add((int)offset);
                }
                else
                {
                    NonJumpOffsets.Add((int)offset);
                }
            }
            #endregion
        }
        #endregion
    }
}
