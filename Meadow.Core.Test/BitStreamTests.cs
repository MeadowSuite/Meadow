using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class BitStreamTests
    {
        [Fact]
        public void TestReadWriteByte()
        {
            // Create a new bit stream in memory.
            BitStream bitStream = new BitStream();
            bitStream.WriteByte(0xFF, 3);
            bitStream.WriteByte(7, 2);

            // Verify position after write.
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(5, bitStream.BitPosition);

            // Verify position after directly setting it.
            bitStream.Position = 0;
            bitStream.BitPosition = 0;
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(0, bitStream.BitPosition);

            // Read 5 bits, all should be set.
            byte value = bitStream.ReadByte(5);
            Assert.Equal(31, value);

            // Verify position after read.
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(5, bitStream.BitPosition);

            // Write split between two bytes (3 bits in first, 2 bits in another). (0xAA == 10101010b) (5 bits = 01010b = 0x0A)
            bitStream.WriteByte(0xAA, 5);

            // Verify position after crossing byte boundary writing
            Assert.Equal(1, bitStream.Position);
            Assert.Equal(2, bitStream.BitPosition);

            // Move backwards to the start of our 0xAA value.
            bitStream.BitPosition -= 5;

            // Read the value, it should be 0xAA.
            value = bitStream.ReadByte(5);
            Assert.Equal(0x0A, value);

            // Verify position after crossing byte boundary reading.
            Assert.Equal(1, bitStream.Position);
            Assert.Equal(2, bitStream.BitPosition);

            // Move backwards to the start of our stream.
            bitStream.Position = 0;
            bitStream.BitPosition = 0;

            // Read both bytes to verify.
            value = bitStream.ReadByte(8);
            Assert.Equal(0xFA, value);

            value = bitStream.ReadByte(8);
            Assert.Equal(0x80, value);

            // Close the bit stream.
            bitStream.Close();
        }

        [Fact]
        public void TestReadWriteBytes()
        {
            // Create a new bit stream in memory.
            BitStream bitStream = new BitStream();
            bitStream.WriteByte(0xFF, 3);
            bitStream.WriteByte(7, 2);

            // Verify position after write.
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(5, bitStream.BitPosition);

            // Verify position after directly setting it.
            bitStream.Position = 0;
            bitStream.BitPosition = 0;
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(0, bitStream.BitPosition);

            // Read 5 bits, all should be set.
            byte value = bitStream.ReadByte(5);
            Assert.Equal(31, value);

            // ---------------------------------
            // MULTI BYTE CODE STARTS BELOW
            // ---------------------------------

            // Write an array (8 bytes = 64 bit).
            byte[] allSet = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            bitStream.WriteBytes(allSet, 63);
            bitStream.WriteByte(5, 5);

            // Go back to the start of multi byte code
            bitStream.BitPosition -= 68;

            // Read 63 bits.
            byte[] resultAllSet = bitStream.ReadBytes(63, true);

            // Test all bytes except last (not byte-aligned bits).
            for (int i = 0; i < allSet.Length - 1; i++)
            {
                Assert.Equal(allSet[i], resultAllSet[i]);
            }

            // Test the last
            Assert.Equal(0xFE, resultAllSet[resultAllSet.Length - 1]);

            // Read our 5 bit integer.
            value = bitStream.ReadByte(5);

            // Verify the last integer's value.
            Assert.Equal(5, value);

            // Close the bit stream.
            bitStream.Close();
        }

        [Fact]
        public void TestReadWriteInts()
        {
            // Create a new bit stream in memory.
            BitStream bitStream = new BitStream();
            bitStream.Write((ulong)0xFF, 3);
            bitStream.Write((uint)7, 2);

            // Verify position after write.
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(5, bitStream.BitPosition);

            // Verify position after directly setting it.
            bitStream.Position = 0;
            bitStream.BitPosition = 0;
            Assert.Equal(0, bitStream.Position);
            Assert.Equal(0, bitStream.BitPosition);

            // Read 5 bits, all should be set.
            byte value = (byte)bitStream.ReadUInt64(5);
            Assert.Equal(31, value);

            // Write two values
            bitStream.Write((ulong)0xAAAAAAAA, 15);
            bitStream.Write((ulong)0xFFFFFFFF, 29);

            // Move back.
            bitStream.BitPosition -= 44;

            // Verify our two values
            Assert.Equal((ulong)0x2AAA, bitStream.ReadUInt64(15));
            Assert.Equal((ulong)0x1FFFFFFF, bitStream.ReadUInt64(29));

            // Move backwards.
            bitStream.BitPosition -= 44;

            // Verify our two values as a single one.
            Assert.Equal((ulong)0x5555FFFFFFF, bitStream.ReadUInt64(44));

            // Move backwards.
            bitStream.BitPosition -= 44;

            // Rewrite our value as a singular big one.
            bitStream.Write((ulong)0x5555FFFFFFF, 44);

            // Move backwards.
            bitStream.BitPosition -= 44;

            // Verify our two values
            Assert.Equal((ulong)0x2AAA, bitStream.ReadUInt64(15));
            Assert.Equal((ulong)0x1FFFFFFF, bitStream.ReadUInt64(29));

            // Close the bit stream.
            bitStream.Close();
        }
    }
}
