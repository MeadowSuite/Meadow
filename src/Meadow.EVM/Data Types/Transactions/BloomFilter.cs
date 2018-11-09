using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Transactions
{
    /// <summary>
    /// Bloom Filters are used to determine inclusiveness in sets. In Ethereum, this hashes log information such as address and topics and sets unique bits to represent their inclusiveness.
    /// False positives are possible (indicating something exists when it does not) because all set bits are OR'd when multiple items are filtered, but false negatives will not occur 
    /// (indicating something is not in the set when it actually is). Ethereum's implementation of the bloom filter is 2048 bits in size.
    /// </summary>
    public abstract class BloomFilter
    {
        #region Constants
        /// <summary>
        /// For every value added, this is the amount of 16-bit chunks we obtain to represent bit index in our bloom filter. Should never exceed 32 (keccak hash size).
        /// </summary>
        private const int BLOOM_CHUNK_COUNT = 3;
        #endregion

        #region Functions
        /// <summary>
        /// Generates a bloom filter for a given item with a given byte count.
        /// </summary>
        /// <param name="item">The item to generate a bloom filter for.</param>
        /// <param name="byteCount">The amount of bytes that the item is made up of.</param>
        /// <returns>Returns the bloom filter generated for this item.</returns>
        public static BigInteger Generate(BigInteger item, int byteCount = EVMDefinitions.WORD_SIZE)
        {
            // Compute our hash for this item.
            byte[] hash = KeccakHash.ComputeHashBytes(BigIntegerConverter.GetBytes(item, byteCount));

            // Create our resulting bloom filter
            BigInteger bloomFilter = 0;

            // Out of the hash, for every 16-bit bit chunk we should cycle through, we use the 11-bits of the 16-bit integer as a bit index to set in our filter.
            for (int i = 0; i < BLOOM_CHUNK_COUNT * 2; i += 2)
            {
                int bitIndex = ((hash[i] << 8) | (hash[i + 1])) & 0x7FF; // mask into an 11-bit integer.
                bloomFilter |= ((BigInteger)1 << bitIndex); // set the bit at that index
            }

            return bloomFilter;
        }

        /// <summary>
        /// Generates a bloom filter for a given items with the given byte count.
        /// </summary>
        /// <param name="items">The items to generate a bloom filter for.</param>
        /// <param name="byteCount">The amount of bytes that the items are individually made up of.</param>
        /// <returns>Returns the bloom filter generated for these items.</returns>
        public static BigInteger Generate(IEnumerable<BigInteger> items, int byteCount = EVMDefinitions.WORD_SIZE)
        {
            // Obtain our enumerator
            IEnumerator<BigInteger> itemEnumerator = items.GetEnumerator();

            // Create our resulting bloom filter
            BigInteger bloomFilter = 0;

            // Loop through every item in the list, and set all the bits from the bloom filters generated from them.
            while (itemEnumerator.MoveNext())
            {
                bloomFilter |= Generate(itemEnumerator.Current, byteCount);
            }

            return bloomFilter;
        }

        /// <summary>
        /// Checks for the possible inclusion of a given item in a given bloom filter.
        /// </summary>
        /// <param name="bloomFilter">The bloom filter to check for possible inclusiveness of our item in.</param>
        /// <param name="item">The item to check possible inclusion for in our bloom filter.</param>
        /// <returns>Returns true if there is a possibility the item is in the set which the bloom filter was made from.</returns>
        public static bool Check(BigInteger bloomFilter, BigInteger item)
        {
            // Mask out only the item bits, and make sure they're all set (if all bits are set, it could possibly exist in set).
            return (bloomFilter & item) == item;
        }
        #endregion
    }
}
