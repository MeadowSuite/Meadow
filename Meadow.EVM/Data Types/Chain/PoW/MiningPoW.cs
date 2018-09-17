using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Block;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.EVM.Data_Types.Chain.PoW
{
    /// <summary>
    /// Ethereum proof of work validation and proof of work calculation routines.
    /// </summary>
    public abstract class MiningPoW
    {
        #region Functions
        /// <summary>
        /// Given a block header, checks if the proof of work is valid on the block header. That is, if the header values, nonce, mix hash and difficulty are all suitable.
        /// </summary>
        /// <param name="blockHeader">The header of the block which we wish to validate.</param>
        /// <returns>Returns true if the proof of work is valid, returns false is the proof is invalid.</returns>
        public static bool CheckProof(BlockHeader blockHeader)
        {
            // Wrap check proof with our block header variables
            return CheckProof(blockHeader.BlockNumber, blockHeader.GetMiningHash(), blockHeader.MixHash, blockHeader.Nonce, blockHeader.Difficulty);
        }

        /// <summary>
        /// Given a block header, checks if the proof of work is valid on the block header. That is, if the header values, nonce, mix hash and difficulty are all suitable.
        /// </summary>
        /// <param name="blockNumber">The number of the block which we wish to validate.</param>
        /// <param name="headerHash">Hash of a portion of the block header which is used with the nonce to generate a seed for the proof.</param>
        /// <param name="mixHash">The resulting mix hash for the provided nonce after running the hashimoto algorithm.</param>
        /// <param name="nonce">The nonce which, along with the other provided values, will calculate the mix hash to be verified.</param>
        /// <param name="difficulty">The difficulty controls filtering for a plausible solution to the block which we wish to mine.</param>
        /// <returns>Returns true if the proof of work is valid, returns false is the proof is invalid.</returns>
        public static bool CheckProof(BigInteger blockNumber, byte[] headerHash, byte[] mixHash, byte[] nonce, BigInteger difficulty)
        {
            // Verify the length of our hashes and nonce
            if (headerHash.Length != KeccakHash.HASH_SIZE || mixHash.Length != KeccakHash.HASH_SIZE || nonce.Length != 8)
            {
                return false;
            }

            // Flip endianness if we need to (should be little endian).
            byte[] nonceFlipped = (byte[])nonce.Clone();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(nonceFlipped);
            }

            // Obtain our cache
            Memory<byte> cache = Ethash.MakeCache(blockNumber); // TODO: Make a helper function for this to cache x results.

            // Hash our block with the given nonce and etc.
            var result = Ethash.HashimotoLight(cache, blockNumber, headerHash, nonceFlipped);

            // Verify our mix hash matches
            if (!result.MixHash.ValuesEqual(mixHash))
            {
                return false;
            }

            // Convert the result to a big integer.
            BigInteger upperBoundInclusive = BigInteger.Pow(2, EVMDefinitions.WORD_SIZE_BITS) / BigInteger.Max(difficulty, 1);
            BigInteger resultInteger = BigIntegerConverter.GetBigInteger(result.Result);
            return resultInteger <= upperBoundInclusive;
        }

        /// <summary>
        /// Mines a given block starting from the provided nonce for the provided number of rounds.
        /// </summary>
        /// <param name="blockHeader">The header of the block to mine.</param>
        /// <param name="startNonce">The starting value for our nonce, from which we will iterate consecutively until we find a nonce which produces a valid mix hash.</param>
        /// <param name="rounds">The number of steps to take from our starting nonce before giving up. Use ulong.MaxValue to try all.</param>
        /// <returns>Returns the nonce and mixhash if block is successfully mined, otherwise both are null.</returns>
        public static (byte[] Nonce, byte[] MixHash) Mine(BlockHeader blockHeader, ulong startNonce, ulong rounds)
        {
            // We wrap our other mining function
            return Mine(blockHeader.BlockNumber, blockHeader.Difficulty, blockHeader.GetMiningHash(), startNonce, rounds);
        }

        /// <summary>
        /// Mines a given block starting from the provided nonce for the provided number of rounds.
        /// </summary>
        /// <param name="blockNumber">The number of the block which we are mining.</param>
        /// <param name="difficulty">The difficulty of the block which we are mining.</param>
        /// <param name="miningHash">The mining hash (partial header hash) of the block which we are mining.</param>
        /// <param name="startNonce">The starting nonce we will use and iterate through to try and find a suitable one for the reward.</param>
        /// <param name="rounds">The number of steps to take from our starting nonce before giving up. Use ulong.MaxValue to try all.</param>
        /// <returns>Returns the nonce and mixhash if block is successfully mined, otherwise both are null.</returns>
        public static (byte[] Nonce, byte[] MixHash) Mine(BigInteger blockNumber, BigInteger difficulty, byte[] miningHash, ulong startNonce, ulong rounds)
        {
            // Verify the length of our hashes and nonce
            if (miningHash == null || miningHash.Length != KeccakHash.HASH_SIZE)
            {
                return (null, null);
            }

            // Get our cache, set our start nonce and rounds remaining
            Memory<byte> cache = Ethash.MakeCache(blockNumber);
            ulong nonce = startNonce;
            BigInteger roundsRemaining = rounds;

            // Obtain our upper bound.
            BigInteger upperBoundInclusive = (BigInteger.Pow(2, EVMDefinitions.WORD_SIZE_BITS) / BigInteger.Max(difficulty, 1)) - 1;

            // Loop for each round.
            for (ulong i = 0; i <= rounds; i++)
            {
                // Increment our nonce.
                nonce++;

                // Obtain the bytes for it
                byte[] nonceData = BitConverter.GetBytes(nonce);

                // Flip endianness if we need to (should be little endian).
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(nonceData);
                }

                // Obtain our mix hash with this nonce.
                var result = Ethash.HashimotoLight(cache, blockNumber, miningHash, nonceData);
                BigInteger resultInteger = BigIntegerConverter.GetBigInteger(result.Result);

                // If our result is below our difficulty bound.
                if (resultInteger <= upperBoundInclusive)
                {
                    // Flip endianness if we need to (returning nonce should be big endian).
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(nonceData);
                    }

                    // Return our nonce and mix hash.
                    return (nonceData, result.MixHash);
                }
            }

            return (null, null);
        }
        #endregion
    }
}
