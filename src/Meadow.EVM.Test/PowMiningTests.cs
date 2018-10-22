using Meadow.EVM.Data_Types.Chain.PoW;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class PowMiningTests
    {
        private void AssertSHA1(string expectedHash, Span<byte> data, bool matches = true)
        {
            System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] dest = new byte[sha1.HashSize / 8];
            Assert.True(sha1.TryComputeHash(data, dest, out _));
            string hashString = dest.ToHexString();

            if (matches)
            {
                Assert.Equal(expectedHash, hashString, true);
            }
            else
            {
                Assert.NotEqual<string>(expectedHash, hashString, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        [Fact]
        public void TestCacheGenerationSmall()
        {
            // Generate a cache for our future block.
            BigInteger blockNum = 2;
            Memory<byte> cache = Ethash.MakeCache(blockNum);

            // Flatten the array and hash it.
            AssertSHA1("780ff0a0259531c918d50c78bb52c291587c4ccd", cache.Span);
        }

        [Fact(Skip = "Too intensive for build server")]
        public void TestCacheGeneration()
        {
            // Generate a cache for our future block.
            BigInteger blockNum = 5650000;
            Memory<byte> cache = Ethash.MakeCache(blockNum);

            // Flatten the array and hash it.
            AssertSHA1("CBFD542DF1457676C766997504074B7FB126C05C", cache.Span);
        }

        [Fact]
        public void TestPartialDataSet()
        {
            // This is based off a cut-up version of the full set generator. Thus if that is updated, this should be too! Look at the Full Data Set Test for more info about how full data sets are generated.

            // Create our hash object and our cache
            BigInteger blockNum = 0;
            Memory<byte> cache = Ethash.MakeCache(blockNum);

            // Allocate our data set (with only 10 hashes)
            uint hashCount = 0x10;
            int hashSize = 0x40;
            Memory<byte> result = new byte[hashCount * hashSize];

            // Populate all items
            for (var i = 0; i < result.Length / hashSize; i++)
            {
                Ethash.CalculateDatasetItem(cache, (uint)i, result.Slice(i * hashSize, hashSize).Span);
            }

            // Return the result
            AssertSHA1("72E61249707E414862063C5B8763AC9C337ACB09", result.Span);
        }

        [Fact(Skip = "Too intensive for build server")] //(TODO: Check this test periodically, but we don't include it since it runs for a long time).
        public void TestFullDataSet()
        {
            // Create our hash object and our cache
            BigInteger blockNum = 0;
            Memory<byte> cache = Ethash.MakeCache(blockNum);

            // Create our data set and flatten it to check the hash.
            byte[] dataSet = Ethash.CalculateDataset(cache, Ethash.GetDataSetSize(blockNum));
            AssertSHA1("78cadf0b9653a3eaa8ba98a64d5fcb6b9450df49", dataSet);
        }
    }
}
