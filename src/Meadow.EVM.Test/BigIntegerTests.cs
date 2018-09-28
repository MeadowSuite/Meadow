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
    public class BigIntegerTests
    {
        /// <summary>
        /// Verifies that when converting a negative BigInteger to a byte array of a requested size, the leading bytes have all bits set.
        /// </summary>
        [Fact]
        public void LeadingBytesNegative()
        {
            BigInteger bigInt = -1;
            byte[] result = BigIntegerConverter.GetBytes(bigInt);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(0xFF, result[i]);
            }

            Assert.Equal(0x20, result.Length);
        }

        /// <summary>
        /// Verifies that overflow past a certain size is capped properly when calling the cap overflow function.
        /// </summary>
        [Fact]
        public void OverflowCap()
        {
            // Cap a 256-bit integer (32 bytes)
            BigInteger bigInt = EVMDefinitions.UINT256_MAX_VALUE + 2;
            bigInt = bigInt.CapOverflow(); // default value accounts for this.
            Assert.Equal(1, bigInt);

            // Cap a 32-bit integer (4 bytes)
            bigInt = (BigInteger)uint.MaxValue + 7;
            bigInt = bigInt.CapOverflow(4); // 32-bits = 4 bytes
            Assert.Equal(6, bigInt);
        }

        /// <summary>
        /// Verifies that when converting a positive BigInteger to a byte array of a requested size, the leading bytes have all bits not set.
        /// </summary>
        [Fact]
        public void LeadingBytesPositive()
        {
            BigInteger bigInt = 1;
            byte[] result = BigIntegerConverter.GetBytes(bigInt);
            for (int i = 0; i < result.Length; i++)
            {
                if (i == result.Length - 1)
                {
                    Assert.Equal(0x01, result[i]);
                }
                else
                {
                    Assert.Equal(0x00, result[i]);
                }
            }

            Assert.Equal(0x20, result.Length);
        }

        /// <summary>
        /// Verifies that when converting a BigInteger to a byte array of a requested size, the core value embedded in the byte array still matches the original method we extended/overloaded.
        /// </summary>
        [Fact]
        public void ValuesUnchanged()
        {
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                byte[] data = new byte[random.Next(1, 0x20 + 1)];
                random.NextBytes(data);
                BigInteger bigInteger = BigIntegerConverter.GetBigInteger(data);
                byte[] parsed = BigIntegerConverter.GetBytes(bigInteger);
                for (int x = 0; x < Math.Min(data.Length, parsed.Length); x++)
                {
                    Assert.Equal(data[data.Length - x - 1], parsed[parsed.Length - x - 1]);
                }
            }
        }

        /// <summary>
        /// Verifies our computed maximums for INT256 and UINT256 on a byte level
        /// </summary>
        [Fact]
        public void BoundsComputedCorrectly()
        {
            // Largest unsigned integer should be 0x21 bytes, (the data + one leading zero byte to indicate sign is positive)
            BigInteger largestUnsigned = EVMDefinitions.UINT256_MAX_VALUE;
            byte[] data = largestUnsigned.ToByteArray();
            Assert.Equal(0x21, data.Length);
            Assert.Equal(1, EVMDefinitions.UINT256_MAX_VALUE.Sign);
            for (int i = 0; i < data.Length; i++)
            {
                if (i != data.Length - 1)
                {
                    Assert.Equal(0xFF, data[i]);
                }
                else
                {
                    Assert.Equal(0x00, data[i]);
                }
            }

            // Larged signed integer should be 0x20 bytes
            BigInteger largestSigned = EVMDefinitions.INT256_MAX_VALUE;
            data = largestSigned.ToByteArray();
            Assert.Equal(0x20, data.Length);
            Assert.Equal(1, EVMDefinitions.INT256_MAX_VALUE.Sign);
            for (int i = 0; i < data.Length; i++)
            {
                if (i != data.Length - 1)
                {
                    Assert.Equal(0xFF, data[i]);
                }
                else
                {
                    Assert.Equal(0x7F, data[i]);
                }
            }

            // Smallest signed integer should be 0x20 bytes
            BigInteger smallestSigned = EVMDefinitions.INT256_MIN_VALUE;
            data = smallestSigned.ToByteArray();
            Assert.Equal(0x20, data.Length);
            Assert.Equal(-1, EVMDefinitions.INT256_MIN_VALUE.Sign);
            for (int i = 0; i < data.Length; i++)
            {
                if (i != data.Length - 1)
                {
                    Assert.Equal(0, data[i]);
                }
                else
                {
                    Assert.Equal(0x80, data[i]);
                }
            }
        }
    }
}
