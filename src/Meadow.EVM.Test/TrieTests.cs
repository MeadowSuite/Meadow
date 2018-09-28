using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.Data_Types.Trees.Comparer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class TrieTests
    {
        private void TestDataSet(Dictionary<string, string> data, Trie trie1 = null)
        {
            // Create a trie
            Trie trie = trie1 != null ? trie1 : new Trie();

            // Populate our trie.
            foreach (string key in data.Keys)
            {
                trie.Set(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data[key]));
            }

            // Verify all items exist as they should.
            foreach (string key in data.Keys)
            {
                Assert.Equal(data[key], Encoding.UTF8.GetString(trie.Get(Encoding.UTF8.GetBytes(key))));
            }

            // Next we'll try re-setting our values.
            string prefixReset = "OKAYOKAYOKAYwhat";
            int index = 0;
            foreach (string key in data.Keys)
            {
                string value = prefixReset + index.ToString(CultureInfo.InvariantCulture);
                trie.Set(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
                index++;
            }

            // Verify our re-set values
            index = 0;
            foreach (string key in data.Keys)
            {
                string value = prefixReset + index.ToString(CultureInfo.InvariantCulture);
                Assert.Equal(value, Encoding.UTF8.GetString(trie.Get(Encoding.UTF8.GetBytes(key))));
                index++;
            }

            // Okay now we revert to our original values
            foreach (string key in data.Keys)
            {
                trie.Set(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data[key]));
            }

            // Verify all items exist as they should (again)
            foreach (string key in data.Keys)
            {
                Assert.Equal(data[key], Encoding.UTF8.GetString(trie.Get(Encoding.UTF8.GetBytes(key))));
            }

            // Remove every other key.
            bool removeToggle = false;
            foreach (string key in data.Keys)
            {
                // If our toggle is set, remove the item.
                if (removeToggle)
                {
                    trie.Remove(Encoding.UTF8.GetBytes(key));
                }

                // Toggle again.
                removeToggle = !removeToggle;
            }

            // Verify every other key was removed.
            removeToggle = false;
            foreach (string key in data.Keys)
            {
                // Assert our inclusion status
                Assert.Equal(!removeToggle, trie.Contains(Encoding.UTF8.GetBytes(key)));

                // Toggle again.
                removeToggle = !removeToggle;
            }
        }

        void TestToDictionary(Trie trie = null)
        {
            // Create a test set of data.
            Dictionary<Memory<byte>, byte[]> testSet = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Create an RNG
            Random random = new Random();

            // Create a trie if ours is null
            if (trie == null)
            {
                trie = new Trie();
            }

            // Test data count (between 20-30 key-value pairs).
            int testDataCount = random.Next(20, 30);
            for (int i = 0; i < testDataCount; i++)
            {
                // Create a key/value of random length each.
                int keySize = random.Next(16, 40);
                int valueSize = random.Next(16, 40);
                byte[] key = new byte[keySize];
                random.NextBytes(key);
                byte[] value = new byte[valueSize];
                random.NextBytes(value);

                // Set it in our dictionary
                testSet[key] = value;

                // Set it our trie
                trie.Set(key, value);
            }

            // Get our resulting dictionary
            var result = trie.ToDictionary();

            // Verify the size
            Assert.Equal(testSet.Count, result.Count);

            // Loop for every item
            foreach (var key in testSet.Keys)
            {
                // Check if our key is contained in our result
                bool contained = result.ContainsKey(key);
                Assert.True(contained);

                // Obtain our values
                byte[] testSetValue = testSet[key];
                byte[] resultValue = testSet[key];

                // Verify length
                Assert.Equal(testSetValue.Length, resultValue.Length);

                // Verify all the bytes
                for (int i = 0; i < testSetValue.Length; i++)
                {
                    Assert.Equal(testSetValue[i], resultValue[i]);
                }
            }
        }

        [Fact]
        public void TrieToDictionaryTest()
        {
            TestToDictionary(new Trie());
        }

        [Fact]
        public void SecureTrieToDictionaryTest()
        {
            TestToDictionary(new SecureTrie());
        }

        [Fact]
        public void TrieTest()
        {
            Dictionary<string, string> testSet = new Dictionary<string, string>();
            testSet["okay"] = "yeah";
            testSet["bleh"] = "gfdgdfgfdggfd";
            for (int i = 0; i < 100; i++)
            {
                testSet["bleh" + i.ToString(CultureInfo.InvariantCulture)] = $"gfdgd{i}fgfdggfd" + i.ToString(CultureInfo.InvariantCulture);
            }

            TestDataSet(testSet);
        }

        [Fact]
        public void SecureTrieTest()
        {
            Dictionary<string, string> testSet = new Dictionary<string, string>();
            testSet["okay"] = "yeah";
            testSet["bleh"] = "gfdgdfgfdggfd";
            for (int i = 0; i < 100; i++)
            {
                testSet["bleh" + i.ToString(CultureInfo.InvariantCulture)] = "gfdgdfgfdggfd" + i.ToString(CultureInfo.InvariantCulture);
            }

            TestDataSet(testSet, new SecureTrie());
        }
    }
}
