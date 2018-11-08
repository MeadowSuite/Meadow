using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Instructions;
using System;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class KeccakTests
    {
        private void AssertHash256(string expectedHash, byte[] data, bool matches = true)
        {
            byte[] hash = KeccakHash.ComputeHashBytes(data);
            string hashString = hash.ToHexString();

            if (matches)
            {
                Assert.Equal(expectedHash, hashString, true);
            }
            else
            {
                Assert.NotEqual<string>(expectedHash, hashString, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        private void AssertHash256(string expectedHash, string data, bool matches = true)
        {
            AssertHash256(expectedHash, System.Text.UTF8Encoding.UTF8.GetBytes(data), matches);
        }

        private void AssertHash512(string expectedHash, byte[] data, bool matches = true)
        {
  
            byte[] hash = new byte[64];
            KeccakHash.ComputeHash(data, hash);
            string hashString = hash.ToHexString();

            if (matches)
            {
                Assert.Equal(expectedHash, hashString, true);
            }
            else
            {
                Assert.NotEqual<string>(expectedHash, hashString, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        private void AssertHash512(string expectedHash, string data, bool matches = true)
        {
            AssertHash512(expectedHash, System.Text.UTF8Encoding.UTF8.GetBytes(data), matches);
        }

        /// <summary>
        /// Verifies that Keccak256 hashing functions are working accordingly.
        /// </summary>
        [Fact]
        public void TestKeccakHashing()
        {
            AssertHash256("4d741b6f1eb29cb2a9b9911c82f56fa8d73b04959d3d9d222895df6c0b28aa15", "The quick brown fox jumps over the lazy dog");
            AssertHash256("578951e24efd62a3d63a86f7cd19aaa53c898fe287d2552133220370240b572d", "The quick brown fox jumps over the lazy dog.");
            AssertHash256("70a2b6579047f0a977fcb5e9120a4e07067bea9abb6916fbc2d13ffb9a4e4eee", "中文");
            AssertHash256("c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470", ""); // should match
            AssertHash256("c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a471", "", false); // shouldn't match
        }

        [Fact]
        public void TestMinerKeccakHashes()
        {
            AssertHash512("e0f85cdb352f5b69346b5ddf7b2c0ef8af1eb71b7d0a4f52fb06c96c7bc1526cbcbe22d302e3f979a77f6237919ba5394a0f53021a030e55272cc2a1e738792f", "This is a quick test of the 512bit Keccak hashing in the PoW mining algorithm DAG.");
            AssertHash512("ab7192d2b11f51c7dd744e7b3441febf397ca07bf812cceae122ca4ded6387889064f8db9230f173f6d1ab6e24b6e50f065b039f799f5592360a6558eb52d760", "The quick brown fox jumps over the lazy dog.");
            AssertHash512("0eab42de4c3ceb9235fc91acffe746b29c29a8c366b7c60e4e67c466f36a4304c00fa9caf9d87976ba469bcbe06713b435f091ef2769fb160cdab33d3670680e", ""); // should match
            AssertHash512("0eab42de4c3ceb9235fc91acffe746b29c29a8c366b7c60e4e67c466f36a4304c00fa9caf9d87976ba469bcbe06713b435f091ef2769fb160cdab33d3670680f", "", false); // shouldn't match
        }
    }
}
