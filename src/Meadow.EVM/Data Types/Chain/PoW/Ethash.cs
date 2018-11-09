using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using System;
using System.Collections.Concurrent;
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
    /// Ethereum hash implementation used for hashing blocks and proof of work. Increased difficulty and use of memory/storage to avoid Application Specific Integrated Circuit ("ASIC") hashing advantages.
    /// </summary>
    // Code coverage disabled while tests are disabled for performance reasons.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class Ethash
    {
        // Source: https://github.com/ethereum/wiki/wiki/Ethash
        // Source: https://ethereum.github.io/yellowpaper/paper.pdf#appendix.J

        #region Constants
        /// <summary>
        /// A prime with which we perform xor operations.
        /// </summary>
        public const uint FNV_PRIME = 0x01000193;

        /// <summary>
        /// The size of a standard word in bytes for this hashing algorithm.
        /// </summary>
        public const int WORD_SIZE = 4;
        /// <summary>
        /// The initial size in bytes of the dataset when the chain is conceived.
        /// </summary>
        public const int DATASET_BYTES_INIT = 1073741824; // 2^30
        /// <summary>
        /// The amount of growth in bytes one can expect to see between epochs for the dataset.
        /// </summary>
        public const int DATASET_BYTES_GROWTH = 8388608; // 2^23
        /// <summary>
        /// The initial size of the generated cache when the chain is conceived. 
        /// </summary>
        public const int CACHE_BYTES_INIT = 16777216; // 2^24
        /// <summary>
        /// The amount of growth in bytes one can expect to see between epochs for the cache.
        /// </summary>
        public const int CACHE_BYTES_GROWTH = 131072; // 2^17
        /// <summary>
        /// The count of blocks between the start of a given time period/epoch and the next.
        /// </summary>
        public const int EPOCH_LENGTH = 30000;
        /// <summary>
        /// The length of the mix data, in bytes.
        /// </summary>
        public const int MIX_SIZE = 128;
        /// <summary>
        /// The length of the hash produced, in bytes.
        /// </summary>
        public const int HASH_SIZE = 64;
        /// <summary>
        /// The number of parents for each dataset entry.
        /// </summary>
        public const int DATASET_PARENTS = 256;
        /// <summary>
        /// The number of rounds to take when generating the cache.
        /// </summary>
        public const int CACHE_ROUNDS = 3;
        /// <summary>
        /// The number of accesses in the hashimoto loop.
        /// </summary>
        public const int ACCESSES = 64;
        /// <summary>
        /// The amount of entries we cache.
        /// </summary>
        public const int CACHED_CACHE_ENTRIES = 3;
        #endregion

        #region Fields
        /// <summary>
        /// Lookup for epoch number to cache.
        /// </summary>
        private static ConcurrentDictionary<int, Memory<byte>> _cachecache;
        #endregion

        #region Constructors
        static Ethash()
        {
            // Create our cached cache lookup.
            _cachecache = new ConcurrentDictionary<int, Memory<byte>>();
        }
        #endregion

        #region Functions
        // Helper Functions
        #region Helpers
        /// <summary>
        /// Simple hash function used to hash two unsigned integers.
        /// </summary>
        /// <param name="v1">The first unsigned integer to hash.</param>
        /// <param name="v2">The second unsigned integer to hash.</param>
        /// <returns>Returns an unsigned integer hash code of the two provided unsigned integers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Fnv_hash(uint v1, uint v2)
        {
            return ((v1 * FNV_PRIME) ^ v2);
        }

        private static Span<byte> Keccak256(Span<byte> message)
        {
            return KeccakHash.ComputeHash(message);
        }

        private static void Keccak256(Span<byte> message, Span<byte> output)
        {
            KeccakHash.ComputeHash(message, output);
        }

        private static Span<byte> Keccak512(Span<byte> message)
        {
            byte[] output = new byte[0x40];
            KeccakHash.ComputeHash(message, output);
            return output;
        }

        private static void Keccak512(Span<byte> message, Span<byte> output)
        {
            KeccakHash.ComputeHash(message, output);
        }

        private static BigInteger Sqrt(BigInteger n)
        {
            // Base case: If our number is 0, sqrt is 0.
            if (n == 0)
            {
                return 0;
            }

            // Verify our number isn't negative.
            else if (n < 0)
            {
                throw new ArithmeticException("BigInteger Sqrt function did not expect a number less than zero.");
            }

            int bits = Convert.ToInt32(Math.Ceiling(BigInteger.Log(n, 2)));
            BigInteger root = BigInteger.One << (bits / 2);

            // Loop continuously until we hit our break condition
            while (true)
            {
                // Verify our bounds.
                BigInteger lowerBound = root * root;
                BigInteger upperBound = (root + 1) * (root + 1);
                if (n < lowerBound || n >= upperBound)
                {
                    break;
                }

                root += n / root;
                root /= 2;
            }

            return root;
        }

        private static bool IsPrime(BigInteger value)
        {
            // Starting from 2 (since we want 1 to avoid this and pass instantly), we go through all values until the square root to try and find a divisor.
            BigInteger sqrtValue = Sqrt(value);
            for (int i = 2; i < sqrtValue; i++)
            {
                if (value % i == 0)
                {
                    return false;
                }
            }

            // We couldn't find a valid divisor, this must be a prime number.
            return true;
        }
        #endregion

        // Cache / Data Set Functions
        /// <summary>
        /// Obtains the size of the cache for a given block number. 
        /// </summary>
        /// <param name="blockNumber">The block number for which we require the cache.</param>
        /// <returns>Returns the size of the cache at a given block number in time.</returns>
        public static int GetCacheSize(BigInteger blockNumber)
        {
            // We obtain our cache size (our initial size + growth, where we can get growth by taking growth rate * time, where time is the amount of epochs past, hence block number divided by epoch).
            BigInteger size = CACHE_BYTES_INIT + (CACHE_BYTES_GROWTH * (blockNumber / EPOCH_LENGTH));

            // Subtract one hash length
            size -= HASH_SIZE;

            // While our amount of hash entries (total size / hash size) isn't prime, we keep removing two hashes in size.
            while (!IsPrime(size / HASH_SIZE))
            {
                size -= 2 * HASH_SIZE;
            }

            // Return the size.
            return (int)size;
        }

        /// <summary>
        /// Obtains the size of the dataset for a given block number. 
        /// </summary>
        /// <param name="blockNumber">The block number for which we require the dataset.</param>
        /// <returns>Returns the size of the dataset at a given block number in time.</returns>
        public static BigInteger GetDataSetSize(BigInteger blockNumber)
        {
            // We obtain our data set size (our initial size + growth, where we can get growth by taking growth rate * time, where time is the amount of epochs past, hence block number divided by epoch).
            BigInteger size = DATASET_BYTES_INIT + (DATASET_BYTES_GROWTH * (blockNumber / EPOCH_LENGTH));

            // Subtract one hash in space
            size -= MIX_SIZE;

            // While our amount of hash entries (total size / hash size) isn't prime, we keep removing two hashes in size.
            while (!IsPrime(size / MIX_SIZE))
            {
                size -= 2 * MIX_SIZE;
            }

            // Return the size.
            return size;
        }


        /// <summary>
        /// Generates the cache for a given block number.
        /// </summary>
        /// <param name="blockNumber">The block number for which we require the cache.</param>
        /// <returns>Returns the cache generated for the given block number in time.</returns>
        public static Memory<byte> MakeCache(BigInteger blockNumber)
        {
            // Determine what epoch period we are in.
            int epochNumber = (int)(blockNumber / EPOCH_LENGTH);

            // If we have the cache cached, we return it.
            if (_cachecache.ContainsKey(epochNumber))
            {
                return _cachecache[epochNumber];
            }

            // The seed starts as a hash size array of zeroes, and keeps hashing itself until it has the epoch numbers hash.
            Memory<byte> seed = new byte[KeccakHash.HASH_SIZE];
            for (int i = 0; i < epochNumber; i++)
            {
                Keccak256(seed.Span, seed.Span);
            }

            // Obtain the hash count for the cache for this block number.
            int cacheHashCount = GetCacheSize(blockNumber) / HASH_SIZE;

            // Obtain the cache item
            Memory<byte> cache = GetCache(seed, cacheHashCount);

            // Enforce our cache count
            if (_cachecache.Count == CACHED_CACHE_ENTRIES)
            {
                // Obtain the minimum epoch from our keys.
                int minimumEpoch = -1;
                foreach (int curEpoch in _cachecache.Keys)
                {
                    if (curEpoch < minimumEpoch)
                    {
                        minimumEpoch = curEpoch;
                    }
                }

                // Remove it from our cache
                if (minimumEpoch >= 0)
                {
                    _cachecache.TryRemove(minimumEpoch, out _);
                }
            }

            // Set it in our cache cache.
            if (_cachecache.Count < CACHED_CACHE_ENTRIES)
            {
                _cachecache[epochNumber] = cache;
            }

            // Return our cache.
            return cache;
        }

        /// <summary>
        /// Generates the cache given a seed and hash count.
        /// </summary>
        /// <param name="seed">The seed which will determine how our cache is initially formed.</param>
        /// <param name="cacheHashCount">The hash count which we desire for our cache.</param>
        /// <returns>Returns the cache generated for the given block number in time.</returns>
        private static Memory<byte> GetCache(Memory<byte> seed, int cacheHashCount)
        {
            // We create our cache, a list of hashes.
            Memory<byte> cache = new byte[cacheHashCount * HASH_SIZE];

            // We populate our cache, starting with the seed, and all other items are hashes of the previous items.
            Keccak512(seed.Span, cache.Slice(0, HASH_SIZE).Span);
            for (int i = 1; i < cacheHashCount; i++)
            {
                Keccak512(cache.Slice((i - 1) * HASH_SIZE, HASH_SIZE).Span, cache.Slice(i * HASH_SIZE, HASH_SIZE).Span);
            }

            // Loop for every round we want to make on our cache generation
            for (int x = 0; x < CACHE_ROUNDS; x++)
            {
                for (int i = 0; i < cacheHashCount; i++)
                {
                    // Obtain the hash for the current hash index in the cache.
                    Span<uint> currentHash = MemoryMarshal.Cast<byte, uint>(cache.Slice(i * HASH_SIZE, HASH_SIZE).Span);

                    // Use the first uint from the hash as an index to another hash we will use to xor the iterated over data.
                    uint index = currentHash[0] % (uint)cacheHashCount;
                    Span<uint> indexedHash = MemoryMarshal.Cast<byte, uint>(cache.Slice((int)index * HASH_SIZE, HASH_SIZE).Span);

                    // Obtain the hash before this one (wrapped around, ex: if currentHash is index 0, previousHash will be the last item)
                    Span<uint> previousHash = MemoryMarshal.Cast<byte, uint>(cache.Slice(((cacheHashCount - 1 + i) % cacheHashCount) * HASH_SIZE, HASH_SIZE).Span);

                    // The iterated hash is xor'd by our indexed hash.
                    for (int j = 0; j < previousHash.Length; j++)
                    {
                        currentHash[j] = previousHash[j] ^ indexedHash[j];
                    }

                    // Set our current hash as a hash of this modified previous hash
                    Span<byte> currentHashBytes = MemoryMarshal.AsBytes(currentHash).Slice(0, HASH_SIZE);
                    Keccak512(currentHashBytes, currentHashBytes);
                }
            }

            // Return it as an array.
            return cache;
        }

        /// <summary>
        /// Generates the data set for the Ethash algorithm.
        /// </summary>
        /// <param name="cache">The cache to use for the data set generation.</param>
        /// <param name="index">The index of our item in the set which we wish to generate.</param>
        /// <param name="outputBuffer">The resulting buffer where our dataset hash item will be stored.</param>
        public static void CalculateDatasetItem(Memory<byte> cache, uint index, Span<byte> outputBuffer)
        {
            // Setup initial variables
            uint cacheHashCount = (uint)(cache.Length / HASH_SIZE);
            uint hashWordCount = HASH_SIZE / WORD_SIZE;

            // Obtain mix data from our index (wrapped around) (cloned since we'll operate on this data)
            cache.Slice((int)(index % cacheHashCount) * HASH_SIZE, HASH_SIZE).Span.CopyTo(outputBuffer);
            Span<uint> mix = MemoryMarshal.Cast<byte, uint>(outputBuffer);

            // Xor our first uint with this index
            mix[0] ^= index;

            // Set the mix as a hash of itself.
            Keccak512(outputBuffer, outputBuffer);

            // Loop for each parent of this mix.
            for (uint i = 0; i < DATASET_PARENTS; i++)
            {
                // Obtain our parent.
                uint parentIndex = Fnv_hash(index ^ i, mix[(int)(i % hashWordCount)]) % cacheHashCount;

                // Get the parent of the mix
                Span<uint> parentMix = MemoryMarshal.Cast<byte, uint>(cache.Slice((int)parentIndex * HASH_SIZE, HASH_SIZE).Span);

                // Xor each mix word with its adjacent parent mix word.
                for (int j = 0; j < mix.Length; j++)
                {
                    mix[j] = Fnv_hash(mix[j], parentMix[j]);
                }
            }

            // Finally we hash our mix to output it.
            Keccak512(outputBuffer, outputBuffer);
        }

        /// <summary>
        /// Generates the data set for the Ethash algorithm.
        /// </summary>
        /// <param name="cache">The cache to use for the data set generation.</param>
        /// <param name="size">The size of the data set which we wish to generate.</param>
        /// <returns>Returns the calculated data set.</returns>
        public static byte[] CalculateDataset(Memory<byte> cache, BigInteger size)
        {
            // Allocate our data set
            int hashCount = (int)(size / HASH_SIZE);

            byte[] bufferArray = new byte[(long)size];
            Memory<byte> buffer = bufferArray;

            using (buffer.Pin())
            {
                Memory<byte> bufferPointer = buffer;

#if DEBUG
                // Populate all items
                int completed = 0;
#endif

                Parallel.For(0, hashCount, i =>
                {
                    CalculateDatasetItem(cache, (uint)i, bufferPointer.Slice(i * HASH_SIZE, HASH_SIZE).Span);
#if DEBUG
                    int ran = Interlocked.Increment(ref completed);
                    if (ran % 100000 == 0)
                    {
                        Console.WriteLine(Math.Round(((double)ran / hashCount) * 100, 2) + "%");
                    }
#endif
                });

                // Return the result
                return bufferArray;
            }
        }
        
        /// <summary>
        /// Ethereum's hash implementation to be used to generate mix hashes for a block given certain block information. In mining, the mix hash is provided, and the nonce is unknown.
        /// </summary>
        /// <param name="headerHash">Hash of a block header we are obtaining a mix hash for.</param>
        /// <param name="nonce">The byte data of the 64-bit nonce, given in little endian format.</param>
        /// <param name="size">The size of the data set.</param>
        /// <param name="datasetLookup">The dataset lookup function to use.</param>
        /// <returns>Returns the mix hash and resulting bound variable.</returns>
        private static (byte[] MixHash, byte[] Result) Hashimoto(byte[] headerHash, byte[] nonce, BigInteger size, Action<uint, byte[]> datasetLookup)
        {
            // Calculate our hash count for our given size
            int hashCount = (int)(size / HASH_SIZE);

            // Calculate our mix sizes
            int mixWordCount = MIX_SIZE / WORD_SIZE;
            int mixHashCount = MIX_SIZE / HASH_SIZE;

            // Generate a hash from our header and nonce
            Span<byte> seed = Keccak512(headerHash.Concat(nonce));
            Span<uint> seedWords = MemoryMarshal.Cast<byte, uint>(seed);

            // Initial population of mix
            Span<byte> mix = new Span<byte>(new byte[MIX_SIZE]);
            Span<uint> mixWords = MemoryMarshal.Cast<byte, uint>(mix);

            // To begin, our seed tiles the whole mix.
            for (int i = 0; i < mixHashCount; i++)
            {
                seed.CopyTo(mix.Slice(i * HASH_SIZE));
            }

            byte[] buffer = new byte[HASH_SIZE];

            // Loop for every access/pass in our loop
            for (uint i = 0; i < ACCESSES; i++)
            {
                // Obtain an index to start grabbing our data set items from.
                uint index = Fnv_hash(i ^ seedWords[0], mixWords[(int)(i % mixWordCount)]) % (uint)((hashCount / mixHashCount) * mixHashCount);

                // Copy enough data set items to equal our mix hashes size.
                for (uint x = 0; x < mixHashCount; x++)
                {
                    // Obtain our next consecutive data set item from the set.
                    datasetLookup(index + x, buffer);
                    Span<uint> dataSetItem = MemoryMarshal.Cast<byte, uint>(buffer);
                    for (int y = 0; y < dataSetItem.Length; y++)
                    {
                        // Hash the mix word with the data set item word.
                        int mixOffset = (int)((x * dataSetItem.Length) + y);
                        mixWords[mixOffset] = Fnv_hash(mixWords[mixOffset], dataSetItem[y]);
                    }
                }
            }

            // Our final hash is yet to be computed.
            Span<uint> cmix = MemoryMarshal.Cast<byte, uint>(new byte[MIX_SIZE / 4]);
            for (int i = 0; i < cmix.Length; i += 4)
            {
                cmix[i] = Fnv_hash(Fnv_hash(Fnv_hash(mix[i], mix[i + 1]), mix[i + 2]), mix[i + 3]);
            }

            // Return our result
            byte[] mixDigest = MemoryMarshal.AsBytes(cmix).ToArray(); // cmix
            byte[] result = Keccak256(seed.ToArray().Concat(mixDigest)).ToArray(); // sha3_256(seed + cmix)
            return (mixDigest, result);
        }

        /// <summary>
        /// Ethereum's hash implementation used to generate hashes for block mining. This implementation generates the dataset on the fly, thus it is lighter on space but slower. Best used for verification.
        /// </summary>
        /// <param name="cache">The cache to used to generate the dataset for this hashing operation.</param>
        /// <param name="blockNumber">The block number from the block to hash, so the dataset can be generated for hashing this block.</param>
        /// <param name="header">The header hash from the block to hash.</param>
        /// <param name="nonce">The nonce from the block to hash.</param>
        /// <returns>Returns the mix hash and the result.</returns>
        public static (byte[] MixHash, byte[] Result) HashimotoLight(Memory<byte> cache, BigInteger blockNumber, byte[] header, byte[] nonce)
        {
            // In the light implementation, we don't store the data set, rather we generate data set items when we need them.
            return Hashimoto(header, nonce, (uint)GetDataSetSize(blockNumber), (uint x, byte[] buffer) => 
            {
                CalculateDatasetItem(cache, x, buffer);
            });
        }

        /// <summary>
        /// Ethereum's hash implementation used to generate hashes for block mining. This implementation has a pre-generated dataset, thus it uses much more memory/space, but is faster to hash, best used when mining to avoid recomputing dataset.
        /// </summary>
        /// <param name="dataSet">The dataset to use for this hashing operation.</param>
        /// <param name="header">The header hash from the block to hash.</param>
        /// <param name="nonce">The nonce from the block to hash.</param>
        /// <returns>Returns the mix hash and the result.</returns>
        public static (byte[] MixHash, byte[] Result) HashimotoFull(byte[][] dataSet, byte[] header, byte[] nonce)
        {
            // In the full version we store the whole data set. This is useful for mining or etc, since you won't need to regenerate the same data repetitively like the light client.
            return Hashimoto(header, nonce, (uint)dataSet.Length * HASH_SIZE, (uint x, byte[] buffer) =>
            {
                dataSet[x].CopyTo(buffer.AsSpan());
            });
        }
        #endregion
    }
}
