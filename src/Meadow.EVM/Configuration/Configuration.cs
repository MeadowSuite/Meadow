using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.Data_Types.Chain;
using Meadow.EVM.Data_Types.Databases;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.Data_Types.Transactions;
using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Debugging.Coverage;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Meadow.EVM.Debugging;
using Meadow.Core.Utils;
using Meadow.Core.Cryptography;

namespace Meadow.EVM.Configuration
{
    // Genesis Block Source: https://etherscan.io/block/0

    public class Configuration
    {
        #region Properties
        /// <summary>
        /// The key-value storage database.
        /// </summary>
        public BaseDB Database { get; private set; }
        /// <summary>
        /// The consensus mechanism we are currently executing on.
        /// </summary>
        public ConsensusBase Consensus { get; set; }

        /// <summary>
        /// The initial/genesis block on our chain, the special case, as all blocks should have parents.
        /// </summary>
        public Block GenesisBlock { get; private set; }
        /// <summary>
        /// A snapshot of the state we begin with when creating our chain.
        /// </summary>
        public StateSnapshot GenesisStateSnapshot { get; private set; }

        /// <summary>
        /// The current Ethereum release/version which we are operating on.
        /// </summary>
        public EthereumRelease Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents the ID of the current chain we are operating on.
        /// </summary>
        public EthereumChainID ChainID
        {
            get;
            set;
        }

        /// <summary>
        /// Represents a lookup of Ethereum releases to a block number at which they should be considered active.
        /// </summary>
        public Dictionary<EthereumRelease, BigInteger> EthereumReleaseBlockStart { get; set; }

        public int PreviousHashDepth { get; set; }

        // Accounts
        public BigInteger InitialAccountNonce { get; set; }

        // Uncles
        /// <summary>
        /// The maximum number of uncles a block can have.
        /// </summary>
        public BigInteger MaxUncles { get; set; }
        /// <summary>
        /// The maximum block distance that the uncle will be held onto.
        /// </summary>
        public int MaxUncleDepth { get; set; }
        /// <summary>
        /// The amount of reward penalty depending on the depth at which the uncle was processed.
        /// </summary>
        public int UncleDepthPenaltyFactor { get; set; }

        // Block Reward
        public BigInteger BlockReward { get; set; }
        public BigInteger NephewReward { get; set; }
        public BigInteger BlockRewardByzantium { get; set; }
        public BigInteger NephewRewardByzantium { get; set; }

        // Block Difficulty
        public BigInteger MinDifficulty { get; set; }
        public BigInteger DifficultyFactor { get; set; }
        public BigInteger DifficultyAdjustmentCutOff { get; set; }
        public BigInteger DifficultyAdjustmentCutOffByzantium { get; set; }
        public BigInteger DifficultyAdjustmentCutOffHomestead { get; set; }
        public BigInteger DifficultyExponentialPeriod { get; set; }
        public BigInteger DifficultyExponentialFreePeriods { get; set; }
        public BigInteger DifficultyExponentialFreePeriodsByzantium { get; set; }

        // Gas Limits
        /// <summary>
        /// The numerator for the fraction which indicates the weight of the previous block to use in moving average calculation.
        /// </summary>
        public BigInteger GasLimitFactorNumerator { get; set; }
        /// <summary>
        /// The denominator for the fraction which indicates the weight of the previous block to use in moving average calculation.
        /// </summary>
        public BigInteger GasLimitFactorDenominator { get; set; }
        /// <summary>
        /// The minimum the gas limit can be on a block.
        /// </summary>
        public BigInteger MinGasLimit { get; set; }
        /// <summary>
        /// The maximum the gas limit can be on a block.
        /// </summary>
        public BigInteger MaxGasLimit { get; set; }
        /// <summary>
        /// Used in gas limit moving average calculation.
        /// </summary>
        public BigInteger GasLimitExponentialMovingAverageFactor { get; set; }
        /// <summary>
        /// Used in gas limit moving average calculation.
        /// </summary>
        public BigInteger GasLimitAdjustmentMaxFactor { get; set; }

        // DAO Fork Block Extra Data
        /// <summary>
        /// The data that is expected to be included on the 10 blocks leading up to the DAO fork.
        /// </summary>
        public byte[] DAOForkBlockExtraData { get; set; }
        /// <summary>
        /// Ignores Ethash verification so mining can be avoided in order to save time.
        /// </summary>
        public bool IgnoreEthashVerification { get; set; }

        // Timestamp
        /// <summary>
        /// Represents the current time, offset by our current time stamp offset.
        /// </summary>
        public long CurrentTimestamp
        {
            get
            {
                // Return the unix timestamp for the time now.
                return ((DateTimeOffset)DateTime.UtcNow.Add(CurrentTimestampOffset)).ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// Adjustable time offset for our current time stamp.
        /// </summary>
        public TimeSpan CurrentTimestampOffset { get; set; }

        // Debugging
        public DebugConfiguration DebugConfiguration { get; private set; }
        /// <summary>
        /// Tracks code coverage/execution optionally for testing purposes.
        /// </summary>
        public CodeCoverage CodeCoverage { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the configuration.
        /// </summary>
        /// <param name="database">The database to use as a key-value store for the chain, if none is provided, a new one is created.</param>
        /// <param name="version">An optional version we can supply to override the version numbers derived from our current block number, and the starting block number for a new version.</param>
        /// <param name="genesisBlockHeader">An optional genesis block header to use instead of the default. Expected to have no transactions or uncles.</param>
        /// <param name="genesisState">An optional genesis state to use instead of the default. If this is provided, the genesis block's state root hash will be replaced with this state's root hash, and the database argument should be this genesis state's database, so lookups will resolve to any modified genesis state trie objects.</param>
        public Configuration(BaseDB database = null, EthereumRelease? version = null, BlockHeader genesisBlockHeader = null, State genesisState = null)
        {
            // Set our database, or create one if we don't have a provided one.
            Database = database ?? new BaseDB();

            // Initialize our debug items
            DebugConfiguration = new DebugConfiguration(database);
            CodeCoverage = new CodeCoverage();

            // We set our timestamp offset as zero
            CurrentTimestampOffset = TimeSpan.Zero;

            #region Versioning
            // Create our block number lookup.
            EthereumReleaseBlockStart = new Dictionary<EthereumRelease, BigInteger>();

            // If our version to operate under is non-null, then we set it.
            if (version != null)
            {
                SetEthereumReleaseBlockStarts((EthereumRelease)version);
            }
            else
            {
                // Otherwise we initialize with default parameters.
                EthereumReleaseBlockStart[EthereumRelease.Frontier] = 0; // This is actually 1, but to avoid future logic errors, we'll include the genesis block.
                EthereumReleaseBlockStart[EthereumRelease.Homestead] = 1150000;
                EthereumReleaseBlockStart[EthereumRelease.DAO] = 1920000;
                EthereumReleaseBlockStart[EthereumRelease.TangerineWhistle] = 2463000;
                EthereumReleaseBlockStart[EthereumRelease.SpuriousDragon] = 2675000;
                EthereumReleaseBlockStart[EthereumRelease.Byzantium] = 4370000;
                EthereumReleaseBlockStart[EthereumRelease.WIP_Constantinople] = EVMDefinitions.UINT256_MAX_VALUE; // Work in progress
                EthereumReleaseBlockStart[EthereumRelease.WIP_Serenity] = EVMDefinitions.UINT256_MAX_VALUE; // Work in progress
            }
            #endregion

            #region Genesis Block/State
            // Set up our genesis state if we don't have one.
            if (genesisState == null)
            {
                // We create a genesis state from this state.
                genesisState = new State(this);
            }
            else
            {
                // We override our given genesis state configuration with this one.
                genesisState.Configuration = this;
            }

            // Set up our genesis block header if we don't have one.
            if (genesisBlockHeader == null)
            {
                genesisBlockHeader = new BlockHeader(
                    new byte[KeccakHash.HASH_SIZE], // previous hash
                    new byte[KeccakHash.HASH_SIZE], // uncles hash (recalculated later)
                    new Address(0), // coinbase
                    Trie.BLANK_NODE_HASH, // state root hash (taken from our genesis state)
                    Trie.BLANK_NODE_HASH, // transaction root hash
                    Trie.BLANK_NODE_HASH, // receipts root hash
                    0, // bloom
                    1145132613, // difficulty
                    0, // block number
                    1296127060, // gas limit
                    0, // gas used
                    709953967, // timestamp
                    "506F6B6F72612F4C6974746C652028486F73686F2047726F757029".HexToBytes(), // extra data
                    new byte[KeccakHash.HASH_SIZE], // mix hash
                    new byte[KeccakHash.HASH_SIZE]); // nonce
            }

            // We set our genesis state trie root hash for our genesis block header
            genesisBlockHeader.StateRootHash = genesisState.Trie.GetRootNodeHash();

            // Initialize our genesis block
            GenesisBlock = new Block(genesisBlockHeader, Array.Empty<Transaction>(), Array.Empty<BlockHeader>());
            GenesisBlock.Header.UpdateUnclesHash(GenesisBlock.Uncles);

            // Set our genesis state snapshot
            GenesisStateSnapshot = genesisState.Snapshot();
            #endregion

            // Accounts
            InitialAccountNonce = 0;

            // Blocks
            PreviousHashDepth = 256;

            // Uncles
            MaxUncles = 2;
            MaxUncleDepth = 6;
            UncleDepthPenaltyFactor = 8;

            // Block Rewards
            BlockReward = 5000 * BigInteger.Pow(10, 15);
            NephewReward = 5000 * BigInteger.Pow(10, 15) / 32;
            BlockRewardByzantium = 3000 * BigInteger.Pow(10, 15);
            NephewRewardByzantium = 3000 * BigInteger.Pow(10, 15) / 32;

            // Block Difficulty
            MinDifficulty = 131072;
            DifficultyFactor = 2048;
            DifficultyAdjustmentCutOff = 13;
            DifficultyAdjustmentCutOffByzantium = 9;
            DifficultyAdjustmentCutOffHomestead = 10;
            DifficultyExponentialPeriod = 100000;
            DifficultyExponentialFreePeriods = 2;
            DifficultyExponentialFreePeriodsByzantium = 30;

            // Gas limits
            MinGasLimit = 5000;
            MaxGasLimit = BigInteger.Pow(2, 63) - 1;
            GasLimitExponentialMovingAverageFactor = 1024;
            GasLimitAdjustmentMaxFactor = 1024;

            GasLimitFactorNumerator = 3;
            GasLimitFactorDenominator = 2;

            // By default we want to validate ethash.
            IgnoreEthashVerification = false;

            // Set our extra data for our dao fork blocks.
            DAOForkBlockExtraData = Encoding.UTF8.GetBytes("dao-hard-fork");
        }
        #endregion

        #region Functions
        /// <summary>
        /// Given a state, updates the current configuration's Ethereum release to accomodate for the current block number.
        /// </summary>
        /// <param name="state">The state which we wish to based our versioning off of.</param>
        public void UpdateEthereumRelease(State state)
        {
            // Declare our release type
            EthereumRelease releaseType = EthereumRelease.Frontier;

            // Obtain all release options.
            EthereumRelease[] options = (EthereumRelease[])Enum.GetValues(typeof(EthereumRelease));

            // Loop through every option, if we passed the declared block number, and this version is higher, set the version.
            foreach (EthereumRelease option in options)
            {
                if (EthereumReleaseBlockStart.TryGetValue(option, out var blockNum))
                {
                    if (state?.CurrentBlock?.Header?.BlockNumber >= blockNum && option > releaseType)
                    {
                        releaseType = option;
                    }
                }
            }

            // Set our release type
            Version = releaseType;
        }

        /// <summary>
        /// Sets the Ethereum release to be used in the configuration.
        /// </summary>
        /// <param name="releaseVersion">The release version to set this configuration to.</param>
        public void SetEthereumReleaseBlockStarts(EthereumRelease releaseVersion)
        {
            // Obtain all release options.
            EthereumRelease[] options = (EthereumRelease[])Enum.GetValues(typeof(EthereumRelease));

            // Loop through every option, if we are at this version or below, set the block number to 0 so we'll always be considered in this version, set all other block numbers to max so we never reach it.
            foreach (EthereumRelease option in options)
            {
                if (option <= releaseVersion)
                {
                    EthereumReleaseBlockStart[option] = 0;
                }
                else
                {
                    EthereumReleaseBlockStart[option] = EVMDefinitions.UINT256_MAX_VALUE;
                }
            }

            // TODO: Temporary cheap fix. Replace this.
            EthereumReleaseBlockStart[EthereumRelease.DAO] = EVMDefinitions.UINT256_MAX_VALUE;
        }

        /// <summary>
        /// Obtains a release version's starting block number.
        /// </summary>
        /// <param name="releaseVersion">The release version to obtain the starting block number for (inclusive).</param>
        /// <returns>Returns the block number which begins the release version provided.</returns>
        public BigInteger GetReleaseStartBlockNumber(EthereumRelease releaseVersion)
        {
            return EthereumReleaseBlockStart[releaseVersion];
        }
        #endregion
    }
}
