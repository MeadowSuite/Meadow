using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.RlpEncoding
{
    /// <summary>
    /// Used to serialize and deserialize data in Recursive Length Prefix ("RLP") encoding.
    /// </summary>
    public abstract class RLP
    {
        #region Functions
        public static byte[] Encode(params RLPItem[] rlpValues)
        {
            return Encode(new RLPList(rlpValues));
        }

        public static byte[] Encode(RLPItem rlpValue)
        {
            // Encode the value according to it's type.
            if (rlpValue.IsByteArray)
            {
                return EncodeByteArray((RLPByteArray)rlpValue);
            }
            else if (rlpValue.IsList)
            {
                return EncodeList((RLPList)rlpValue);
            }

            throw new ArgumentException("RLP encoding passed an invalid RLPItem");
        }

        public static RLPItem Decode(byte[] data)
        {
            // Decode from the start
            int result = 0;
            return DecodeAt(data, 0, out result);
        }

        public static RLPItem DecodeAt(byte[] data, int start, out int end)
        {
            // Decode the value according to it's type
            byte id = data[start];
            if (id < 0xc0)
            {
                return DecodeByteArray(data, start, out end);
            }
            else
            {
                return DecodeList(data, start, out end);
            }
        }

        private static byte[] EncodeLength(int length)
        {
            // Find out how many bytes we should encode.
            int byteCount = 0;
            int len = length;
            for (int i = 0; i < 4; i++)
            {
                // If there are no more bits set, stop.
                if (len >> (i * 8) == 0)
                {
                    break;
                }

                // Otherwise we add to our byte count.
                byteCount++;
            }

            // Get the bytes out of the data.
            byte[] data = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                data[i] = (byte)(length >> (((byteCount - 1) - i) * 8));
            }

            return data;
        }

        private static int DecodeLength(byte[] data)
        {
            return DecodeLength(data, 0, data.Length);
        }

        private static int DecodeLength(byte[] data, int start, int count)
        {
            // Get the length from our data.
            int length = 0;
            for (int i = 0; i < count; i++)
            {
                length |= data[start + i] << (((count - 1) - i) * 8);
            }

            return length;
        }

        private static byte[] EncodeByteArray(RLPByteArray rlpBytes)
        {
            // If it's null
            if (rlpBytes.Data.Length == 0)
            {
                return new byte[] { 0x80 };
            }

            // If it's a single byte less than/equal to 128, the encoding is it's own value.
            if (rlpBytes.Data.Length == 1 && rlpBytes.Data.Span[0] <= 0x7f)
            {
                return rlpBytes.Data.ToArray();
            }

            // If it's between 0 and 55 bytes, we add a prefix that denotes length.
            if (rlpBytes.Data.Length < 55)
            {
                return new byte[] { (byte)(0x80 + rlpBytes.Data.Length) }.Concat(rlpBytes.Data.ToArray());
            }

            // If it's more, we'll want to get the length of the data (as a possibly large integer)
            int length = rlpBytes.Data.Length;
            byte[] lengthData = EncodeLength(length);

            // Return an array with our data
            return new[] { (byte)(0xb7 + lengthData.Length) }.Concat(lengthData, rlpBytes.Data.ToArray());
        }

        private static RLPByteArray DecodeByteArray(byte[] data, int start, out int end)
        {
            // If the first byte is less than or equal to 0x7f, set it directly.
            if (data[start] <= 0x7f)
            {
                end = start + 1;
                return new RLPByteArray(new byte[] { data[start] });
            }

            // If it's less than 0xb7, then it's an array of length 0-55
            int length = 0;
            if (data[start] < 0xb7)
            {
                length = data[start] - 0x80;
                end = start + 1 + length;
                if (length == 0)
                {
                    return new RLPByteArray(null);
                }
                else
                {
                    return new RLPByteArray(data.Slice(start + 1, end));
                }
            }

            // Obtain our length of our length.
            int lengthLength = data[start] - 0xb7;

            // Obtain our length
            length = DecodeLength(data, start + 1, lengthLength);

            // Read our actual data
            end = start + 1 + lengthLength + length;
            return new RLPByteArray(data.Slice(start + 1 + lengthLength, end));
        }

        private static byte[] EncodeList(RLPList rlpList)
        {
            // Obtain all the encoded items and total length.
            int length = 0;
            byte[][] data = new byte[rlpList.Items.Count][];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Encode(rlpList.Items[i]);
                length += data[i].Length;
            }

            // If it's between 0 and 55 bytes, we add a prefix that denotes length.
            if (length < 55)
            {
                return new byte[] { (byte)(0xC0 + length) }.Concat(data);
            }

            // If it's more, we'll want to get the length of the data (as a possibly large integer)
            byte[] lengthData = EncodeLength(length);

            // Return an array with our data
            return new[] { (byte)(0xf7 + lengthData.Length) }.Concat(lengthData).Concat(data);
        }

        private static RLPList DecodeList(byte[] data, int start, out int end)
        {
            // Create a new list representation.
            RLPList rlpList = new RLPList();

            int current = 0;

            // If it's less than 0xf7 than it's a list constituting 0-55 bytes total.
            if (data[start] < 0xf7)
            {
                // Obtain the length of our data.
                int length = data[start] - 0xc0;

                // Calculate the end of our RLP item.
                end = start + 1 + length;

                // Keep adding items until we reach the end.
                current = start + 1;
                while (current < end)
                {
                    rlpList.Items.Add(DecodeAt(data, current, out current));
                }
            }
            else
            {
                // Obtain the length of the length
                int lengthLength = data[start] - 0xf7;

                // Obtain our length
                int length = DecodeLength(data, start + 1, lengthLength);

                // Calculate the end of our RLP item.
                end = start + 1 + lengthLength + length;

                // Keep adding items until we reach the end.
                current = start + 1 + lengthLength;
                while (current < end)
                {
                    rlpList.Items.Add(DecodeAt(data, current, out current));
                }
            }

            // Verify our decoding added at our predicted end.
            if (current != end)
            {
                throw new ArgumentException("RLP deserialization encountered an error where travering an encoded list does not add up to the stored length.");
            }

            return rlpList;
        }

        // Type handling
        public static RLPByteArray FromInteger(BigInteger bigInteger, int byteCount = 32, bool removeLeadingZeros = false)
        {
            if (!removeLeadingZeros)
            {
                return new RLPByteArray(BigIntegerConverter.GetBytes(bigInteger, byteCount));
            }
            else
            {
                return new RLPByteArray(BigIntegerConverter.GetBytesWithoutLeadingZeros(bigInteger, byteCount));
            }
        }

        public static BigInteger ToInteger(RLPByteArray rlpByteArray, int byteCount = 32, bool signed = false)
        {
            // If our data is null or empty, the result is 0.
            if (rlpByteArray.Data.Length == 0)
            {
                return 0;
            }

            // Obtain our integer
            return BigIntegerConverter.GetBigInteger(rlpByteArray.Data.Span, signed, byteCount);
        }
        #endregion
    }
}
