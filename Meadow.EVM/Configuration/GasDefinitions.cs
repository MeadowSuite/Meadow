using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Instructions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Configuration
{
    public abstract class GasDefinitions
    {
        #region Constants
        // Transaction Gas Definitions
        /// <summary>
        /// The amount of gas a transaction uses just to be processed.
        /// </summary>
        public const int GAS_TRANSACTION = 21000;
        /// <summary>
        /// The amount of gas a transaction uses when it has a zero byte in it's transaction data.
        /// </summary>
        public const int GAS_TRANSACTION_DATA_ZERO = 4;
        /// <summary>
        /// The amount of gas a transaction uses when it has a non-zero byte in it's transaction data.
        /// </summary>
        public const int GAS_TRANSACTION_DATA_NON_ZERO = 68;

        // EVM Gas Definitions

        // Memory
        /// <summary>
        /// The amount of gas used to allocate a word of memory.
        /// </summary>
        public const int GAS_MEMORY_BASE = 3;
        /// <summary>
        /// The amount of gas used to copy a word into memory.
        /// </summary>
        public const int GAS_MEMORY_COPY = 3; // cost per WORD copy

        // Arithmetic
        /// <summary>
        /// The amount of gas used for every byte of the exponent when executing an exponent instruction (pre-spurious dragon).
        /// </summary>
        public const int GAS_EXP_BYTE = 10;
        /// <summary>
        /// The amount of gas used for every byte of the exponent when executing an exponent instruction (post-spurious dragon).
        /// </summary>
        public const int GAS_EXP_BYTE_SPURIOUS_DRAGON = 50;

        // Cryptography
        /// <summary>
        /// The amount of gas used to keccak hash a word of data.
        /// </summary>
        public const int GAS_SHA3_WORD = 6;
        /// <summary>
        /// The amount of gas to execute the ECRecover Precompile.
        /// </summary>
        public const int GAS_PRECOMPILE_ECRECOVER = 3000;
        /// <summary>
        /// The amount of gas used just to execute a SHA256 precompile.
        /// </summary>
        public const int GAS_PRECOMPILE_SHA256_BASE = 60;
        /// <summary>
        /// The amount of gas used to SHA256 hash a word of data.
        /// </summary>
        public const int GAS_PRECOMPILE_SHA256_WORD = 12;
        /// <summary>
        /// The amount of gas used just to execute a RIPEMD160 precompile.
        /// </summary>
        public const int GAS_PRECOMPILE_RIPEMD160_BASE = 600;
        /// <summary>
        /// The amount of gas used to RIPEMD160 hash a word of data.
        /// </summary>
        public const int GAS_PRECOMPILE_RIPEMD160_WORD = 120;

        // 
        /// <summary>
        /// The amount of gas used just to execute an identity/memcpy precompile.
        /// </summary>
        public const int GAS_PRECOMPILE_IDENTITY_BASE = 15;
        /// <summary>
        /// The amount of gas used to identity/memcpy a word of data.
        /// </summary>
        public const int GAS_PRECOMPILE_IDENTITY_WORD = 3;

        //
        public const int GAS_PRECOMPILE_MODEXP_QUAD_DIVISOR = 20;

        // Storage
        /// <summary>
        /// The amount of gas used to add a key-value to account storage.
        /// </summary>
        public const int GAS_SSTORE_ADD = 20000;
        /// <summary>
        /// The amount of gas used to modify an existing value in account storage.
        /// </summary>
        public const int GAS_SSTORE_MODIFY = 5000;
        /// <summary>
        /// The amount of gas used to delete an existing value in account storage.
        /// </summary>
        public const int GAS_SSTORE_DELETE = 5000;
        /// <summary>
        /// The amount of gas refunded for deleting an existing key-value from account storage.
        /// </summary>
        public const int GAS_SSTORE_REFUND = 15000;

        // Logging
        /// <summary>
        /// The amount of gas used for every byte logged.
        /// </summary>
        public const int GAS_LOG_BYTE = 8;

        // Calls
        /// <summary>
        /// The amount of gas charged for calling an account that doesn't exist.
        /// </summary>
        public const int GAS_CALL_NEW_ACCOUNT = 25000;
        /// <summary>
        /// The amount of gas used to transfer value in a transaction.
        /// </summary>
        public const int GAS_CALL_VALUE = 9000;
        /// <summary>
        /// The amount of gas an inner call is given when transfering value in a transaction.
        /// </summary>
        public const int GAS_CALL_VALUE_STIPEND = 2300;
        /// <summary>
        /// The amount of gas that is refunded when an account self destructs.
        /// </summary>
        public const int GAS_SELF_DESTRUCT_REFUND = 24000;

        // Contract Creation
        /// <summary>
        /// The amount of gas used for every byte of a contract when creating a contract.
        /// </summary>
        public const int GAS_CONTRACT_BYTE = 200;
        #endregion

        #region Fields
        private static ConcurrentDictionary<EthereumRelease, Dictionary<InstructionOpcode, uint>> _baseGasLookup;
        #endregion

        #region Constructors
        /// <summary>
        /// The default static constructor which creates a base gas lookup out of attributes on the opcodes for every Ethereum release version to be used globally for quick access and simple maintenance when introducing new opcodes.
        /// </summary>
        static GasDefinitions()
        {
            // Initialize our instruction base cost lookup.
            _baseGasLookup = new ConcurrentDictionary<EthereumRelease, Dictionary<InstructionOpcode, uint>>();

            // Obtain all opcodes, ethereum releases, and initialize sublookups for every ethereum release.
            InstructionOpcode[] opcodes = (InstructionOpcode[])Enum.GetValues(typeof(InstructionOpcode));
            EthereumRelease[] releases = (EthereumRelease[])Enum.GetValues(typeof(EthereumRelease)); // assumes this is ascending, hence don't mess up order by adding number values on this enum.
            foreach (EthereumRelease release in releases)
            {
                _baseGasLookup[release] = new Dictionary<InstructionOpcode, uint>();
            }

            // For every instruction opcode
            foreach (InstructionOpcode opcode in opcodes)
            {
                // Obtain our base gas costs. If we don't have any, skip to the next opcode.
                var baseGasCosts = opcode.GetBaseGasCosts();
                if (baseGasCosts == null || baseGasCosts.Length == 0)
                {
                    continue;
                }

                // Load all of our base gas cost attribute's definitions into our lookup.
                foreach (var baseGasCost in baseGasCosts)
                {
                    _baseGasLookup[baseGasCost.Version][opcode] = baseGasCost.BaseGasCost;
                }

                // Loop through all ethereum release versions (ascending) and set any cost as our last obtained cost.
                uint? lastCost = null;
                for (int i = 0; i < releases.Length; i++)
                {
                    // If we have a base cost declared for this version, make note of it, otherwise set this one as the last seen one (if any).
                    if (_baseGasLookup.TryGetValue(releases[i], out var baseGas) && baseGas.ContainsKey(opcode))
                    {
                        lastCost = baseGas[opcode];
                    }
                    else if (lastCost != null)
                    {
                        _baseGasLookup[releases[i]][opcode] = (uint)lastCost;
                    }
                }
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Obtains the base gas cost of executing the given opcode on the given Ethereum release version.
        /// </summary>
        /// <param name="currentVersion">The release of Ethereum to assume when obtaining the base gas cost for the given opcode.</param>
        /// <param name="opcode">The opcode to obtain the base gas cost for.</param>
        /// <returns>Returns the base gas cost for the given instruction on the given Ethereum release.</returns>
        public static uint? GetInstructionBaseGasCost(EthereumRelease currentVersion, InstructionOpcode opcode)
        {
            // Obtain the cost for this version. It is null if it does not exist yet, or was never declared.
            uint cost = 0;
            if (_baseGasLookup[currentVersion].TryGetValue(opcode, out cost))
            {
                return cost;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Provides the cost of gas for a given size of memory in words.
        /// </summary>
        /// <param name="wordCount">The target amount of words of memory.</param>
        /// <returns>Returns the cost of gas for the given size of memory.</returns>
        public static BigInteger GetMemoryAllocationCost(EthereumRelease currentVersion, BigInteger wordCount)
        {
            // Calculate the cost of gas for the given amount of words of memory.
            BigInteger cost = (wordCount * GAS_MEMORY_BASE) + (BigInteger.Pow(wordCount, 2) / 512);
            return cost;
        }

        /// <summary>
        /// Provides the cost of gas for a given
        /// </summary>
        /// <param name="currentVersion">The Ethereum release currently being executed.</param>
        /// <param name="size">The size</param>
        /// <returns></returns>
        public static BigInteger GetMemoryCopyCost(EthereumRelease currentVersion, BigInteger size)
        {
            return EVMDefinitions.GetWordCount(size) * GAS_MEMORY_COPY;
        }

        /// <summary>
        /// Calculates the maximum amount of gas a call can take, given the current gas amount. This is currently (63/64) * gas.
        /// </summary>
        /// <param name="gas">The current amount of gas.</param>
        /// <returns>Returns the maximum amount of gas a call can take.</returns>
        public static BigInteger GetMaxCallGas(BigInteger gas)
        {
            return gas - (gas / 64);
        }

        /// <summary>
        /// Calculates a new gas limit for the next block by using exponential moving averages.
        /// </summary>
        /// <param name="parentBlock">The previous block processed which we base our calculations off of.</param>
        /// <param name="configuration">The current configuration.</param>
        /// <returns>Returns a computed gas limit for the next block.</returns>
        public static BigInteger CalculateGasLimit(BlockHeader parentBlock, Configuration configuration)
        {
            // Obtain our removal amount from our transaction (one of our factor slivers)
            BigInteger removal = parentBlock.GasLimit / configuration.GasLimitExponentialMovingAverageFactor;

            // Obtain our new addition/factor to add in. (the given fraction of the previous block's gas used, divided by moving average factor).
            BigInteger addition = ((parentBlock.GasUsed * configuration.GasLimitFactorNumerator) / configuration.GasLimitFactorDenominator) / configuration.GasLimitExponentialMovingAverageFactor;

            // Calculate our new gas limit (lower bound by our minimum gas limit)
            BigInteger newGasLimit = BigInteger.Max(configuration.MinGasLimit, parentBlock.GasLimit - removal + addition);

            // We add a special case for our genesis block
            if (newGasLimit < configuration.GenesisBlock.Header.GasLimit)
            {
                // If we were below our gas limit, we instead set gas limit as the parents gas limit plus what would've been decayed, capped to the genesis gas limit.
                newGasLimit = BigInteger.Min(parentBlock.GasLimit + removal, configuration.GenesisBlock.Header.GasLimit);
            }

            // Return our new gas limit.
            return newGasLimit;
        }

        /// <summary>
        /// Checks that a given parent block gas limit, and current block gas limit meet the gas adjustment requirements defined by the configuration.
        /// </summary>
        /// <param name="parentGasLimit">The current block's parent's gas limit.</param>
        /// <param name="blockGasLimit">The current block's gas limit.</param>
        /// <param name="configuration">The configuration which defines the gas adjustment requirements.</param>
        /// <returns>Returns true if the gas limits are within our requirements, false otherwise.</returns>
        public static bool CheckGasLimit(BigInteger parentGasLimit, BigInteger blockGasLimit, Configuration configuration)
        {
            // We obtain the gas adjustment amount to make sure we don't exceed it.
            BigInteger gasAdjustment = parentGasLimit / configuration.GasLimitAdjustmentMaxFactor;

            // The difference in our adjustment can't exceed what is derived from our adjustment factor.
            if (BigInteger.Abs(blockGasLimit - parentGasLimit) > gasAdjustment)
            {
                return false;
            }

            // If we don't meet our minimum gas limit, return false.
            if (blockGasLimit < configuration.MinGasLimit)
            {
                return false;
            }

            // Otherwise we met requirements
            return true;
        }
        #endregion
    }
}
