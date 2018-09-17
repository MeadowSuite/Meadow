using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Trees.Comparer;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Instructions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class LookupTests
    {
        [Fact]
        public void PerfTest()
        {
            Dictionary<byte[], int> lookup1 = new Dictionary<byte[], int>(new ArrayComparer<byte[]>());
            Dictionary<Memory<byte>, int> lookup2 = new Dictionary<Memory<byte>, int>(new MemoryComparer<byte>());

            int rounds = 50_000;
            byte[][] keys = new byte[rounds][];

            for (var i = 0; i < rounds; i++)
            {
                keys[i] = KeccakHash.ComputeHashBytes(BitConverter.GetBytes(i));
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < rounds; i++)
            {
                lookup1[keys[i]] = i;
            }

            foreach (var key in lookup1.Keys)
            {
                var item = lookup1[key];
            }

            sw.Stop();
            var took1 = sw.Elapsed.TotalMilliseconds;


            sw.Restart();
            for (var i = 0; i < rounds; i++)
            {
                lookup2[keys[i]] = i;
            }

            foreach (var key in lookup2.Keys)
            {
                var item = lookup2[key];
            }

            sw.Stop();
            var took2 = sw.Elapsed.TotalMilliseconds;
        }

        [Fact]
        public void ArrayComparerManualTest()
        {
            byte[] same1 = new byte[4] { 0, 7, 0, 7 };
            byte[] same2 = new byte[4] { 0, 7, 0, 0 };
            same2[3] = 7;
            Dictionary<Memory<byte>, string> lookup = new Dictionary<Memory<byte>, string>(new MemoryComparer<byte>());
            lookup[same1] = "1";
            lookup[same2] = "2";

            same2[3] = 8;
            lookup[same2] = "333";

            Assert.Equal("2", lookup[same1]);
            Assert.Equal("333", lookup[same2]);
            Assert.Equal(2, lookup.Count);
        }

        [Fact]
        public void ArrayComparerCollisionTest()
        {
            // Define how many rounds we'll test
            const int ROUNDS = 100;

            // Generate a random array to start.
            Random random = new Random();
            byte[] arrayA = new byte[EVMDefinitions.WORD_SIZE];
            random.NextBytes(arrayA);

            // Create a lookup, and make all our entries to test. Also create a duplicate array (different instance, same underlying data) to verify everything matches.
            Dictionary<Memory<byte>, string> lookup = new Dictionary<Memory<byte>, string>(new MemoryComparer<byte>());
            for (int i = 0; i < ROUNDS; i++)
            {
                // Generate a new array of data that is almost guarenteed to be unique from previous entries.
                arrayA = KeccakHash.ComputeHashBytes(arrayA);

                // Create a copy of the array.
                byte[] arrayB = arrayA.Slice(0);

                // Set our data in lookup. The second line should overwrite the value of the first.
                lookup[arrayA] = "0";
                lookup[arrayB] = "1";
            }

            // Assert every key we used set as 1 (generated same hashcodes for same data)
            foreach (var key in lookup.Keys)
            {
                Assert.Equal("1", lookup[key]);
            }

            // Verify we have the correct amount of entries (generated different hashcodes for different data).
            Assert.Equal(ROUNDS, lookup.Count);
        }

        [Fact]
        public void AddressDictionaryTest()
        {
            // Make a dictionary to test address lookups in.
            Dictionary<Address, int> testDictionary = new Dictionary<Address, int>();
            testDictionary[new Address(0)] = 7;
            Assert.True(testDictionary.ContainsKey(new Address("0x0")));
        }
    }
}
