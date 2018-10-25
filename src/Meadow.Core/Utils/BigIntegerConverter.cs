using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Utils
{ 
    public abstract class BigIntegerConverter
    {
        #region Constants
        public const int WORD_SIZE = 32;
        #endregion

        #region Fields
        private static BigInteger? _uint256_max_value;
        private static BigInteger? _int256_max_value;
        private static BigInteger? _int256_min_value;
        #endregion

        #region Properties
        /// <summary>
        /// Represents the minimum value a 256-bit unsigned integer could have.
        /// </summary>
        public static BigInteger UINT256_MIN_VALUE
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Represents the maximum value a 256-bit unsigned integer could have.
        /// </summary>
        public static BigInteger UINT256_MAX_VALUE
        {
            get
            {
                if (_uint256_max_value == null)
                {
                    _uint256_max_value = 1;
                    _uint256_max_value <<= 256;
                    _uint256_max_value -= 1;
                }

                return (BigInteger)_uint256_max_value;
            }
        }

        /// <summary>
        /// Represents the minimum value a 256-bit signed integer could have.
        /// </summary>
        public static BigInteger INT256_MAX_VALUE
        {
            get
            {
                if (_int256_max_value == null)
                {
                    _int256_max_value = 1;
                    _int256_max_value <<= 255;
                    _int256_max_value -= 1;
                }

                return (BigInteger)_int256_max_value;
            }
        }

        /// <summary>
        /// Represents the maximum value a 256-bit signed integer could have.
        /// </summary>
        public static BigInteger INT256_MIN_VALUE
        {
            get
            {
                if (_int256_min_value == null)
                {
                    _int256_min_value = -1 - INT256_MAX_VALUE;
                }

                return (BigInteger)_int256_min_value;
            }
        }
        #endregion

        /// <summary>
        /// Obtains the bytes that represent the BigInteger as if it was a big endian 256-bit integer.
        /// </summary>
        /// <param name="bigInteger">The BigInteger to obtain the byte representation of.</param>
        /// <returns>Returns the bytes that represent BigInteger as if it was a 256-bit integer.</returns>
        public static byte[] GetBytes(BigInteger bigInteger, int byteCount = WORD_SIZE)
        {
            // Obtain the bytes which represent this BigInteger.
            byte[] result = bigInteger.ToByteArray();

            // Store the original size of the data, then resize it to the size of a word.
            int originalSize = result.Length;
            Array.Resize(ref result, byteCount);

            // BigInteger uses the most significant bit as sign and optimizes to return values like -1 as 0xFF instead of as 0xFFFF or larger (since there is no bound size, and negative values have all leading bits set)
            // Instead if we wanted to represent 256 (0xFF), we would add a leading zero byte so the sign bit comes from it, and will be zero (positive) (0x00FF), this way, BigInteger knows to represent this as a positive value.
            // Because we resized the array already, it would have added leading zero bytes which works for positive numbers, but if it's negative, all extended bits should be set, so we check for that case.

            // If the integer is negative, any extended bits should all be set.
            if (bigInteger.Sign < 0)
            {
                for (int i = originalSize; i < result.Length; i++)
                {
                    result[i] = 0xFF;
                }
            }

            // Flip the array so it is in big endian form.
            Array.Reverse(result);

            return result;
        }

        /// <summary>
        /// Obtains the bytes that represent the BigInteger as if it was a big endian 256-bit integer, except removes leading zero bytes.
        /// </summary>
        /// <param name="bigInteger">The BigInteger to obtain the byte representation of.</param>
        /// <returns>Returns the bytes that represent BigInteger as if it was a 256-bit integer, except with leading zero bytes removed..</returns>
        public static byte[] GetBytesWithoutLeadingZeros(BigInteger bigInteger, int byteCount = WORD_SIZE)
        {
            // Obtain the bytes which represent this BigInteger.
            byte[] result = GetBytes(bigInteger, byteCount);

            // Count the number of leading zeros
            int startIndex = result.Length;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != 0)
                {
                    startIndex = i;
                    break;
                }
            }

            // If the start index is 0, we return the array as is
            if (startIndex == 0)
            {
                return result;
            }

            // Return our sliced array
            byte[] resultSliced = new byte[result.Length - startIndex];
            Array.Copy(result, startIndex, resultSliced, 0, resultSliced.Length);
            return resultSliced;
        }

        /// <summary>
        /// Obtains a BigInteger representation for the provided data as if the data represents a big endian 256-bit integer.
        /// </summary>
        /// <param name="bytes">The big endian ordered bytes that constitute a 256-bit integer</param>
        /// <param name="signed">Determines whether we want to interpret the data signed or unsigned</param>
        /// <returns>Returns the BigInteger representation of the provided data</returns>
        public static BigInteger GetBigInteger(Span<byte> bytes, bool signed = false, int byteCount = WORD_SIZE)
        {
            // If the data provided is null, it is equal to zero.
            if (bytes == null || bytes.Length == 0)
            {
                return 0;
            }

            // We'll want to either extend or shrink the amount of bytes provided to a 256-bit buffer.
            byte[] data = new byte[byteCount];
            int count = Math.Min(data.Length, bytes.Length);
            int sourceIndex = Math.Max(0, bytes.Length - count);
            int targetIndex = Math.Max(0, data.Length - count);
            bytes.Slice(sourceIndex, count).CopyTo(new Span<byte>(data).Slice(targetIndex, count));

            // We'll operate on the data in little endian
            Array.Reverse(data);

            // If our sign bit is set and we want an unsigned BigInteger, we'll want to add a leading zero byte to take over the sign (this is little endian so we append to the end)
            if (!signed)
            {
                if ((data[data.Length - 1] >> 7) == 1)
                {
                    Array.Resize(ref data, data.Length + 1);
                }
            }

            // Convert back to big endian if necessary.
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            // Parse and return our BigInteger representation from this data.
            return new BigInteger(data);
        }
    }
}
