using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Utils
{

    public static class BigIntegerExtensions
    {
        #region Functions
        /// <summary>
        /// Takes a big integer and returns a new instance whose data is interpreted as if it was unsigned.
        /// </summary>
        /// <param name="bigInteger">The signed BigInteger to convert to an unsigned BigInteger.</param>
        /// <returns>Returns a BigInteger instance that is an unsigned representation of the one provided (in integer value).</returns>
        public static BigInteger ToUInt256(this BigInteger bigInteger)
        {
            // If it's less than the signed max value and it's positive, we can treat it as an unsigned integer.
            if (bigInteger <= BigIntegerConverter.INT256_MAX_VALUE && bigInteger.Sign >= 0)
            {
                return bigInteger;
            }

            return BigIntegerConverter.GetBigInteger(BigIntegerConverter.GetBytes(bigInteger), false);
        }

        /// <summary>
        /// Takes a big integer and returns a new instance whose data is interpreted as if it was unsigned.
        /// </summary>
        /// <param name="bigInteger">The signed BigInteger to convert to an unsigned BigInteger.</param>
        /// <returns>Returns a BigInteger instance that is an unsigned representation of the one provided (in integer value).</returns>
        public static BigInteger ToInt256(this BigInteger bigInteger)
        {
            // If it's already negative, or it's smaller than the signed maximum, then it's indifferent from an unsigned integer.
            if (bigInteger.Sign <= 0 || bigInteger <= BigIntegerConverter.INT256_MAX_VALUE)
            {
                return bigInteger;
            }

            return BigIntegerConverter.GetBigInteger(BigIntegerConverter.GetBytes(bigInteger), true);
        }

        /// <summary>
        /// Ensures that if the integer flowed past the default Ethereum Virtual Machine word size (256-bit), we remove the additional overflow.
        /// </summary>
        /// <param name="bigInteger">The integer who's overflow we'll want to remove.</param>
        /// <returns>Returns a big integer value that is capped by overflow past the given amount of bytes in size.</returns>
        public static BigInteger CapOverflow(this BigInteger bigInteger, int byteCount = BigIntegerConverter.WORD_SIZE, bool signed = false)
        {
            return BigIntegerConverter.GetBigInteger(BigIntegerConverter.GetBytes(bigInteger, byteCount), signed, byteCount);
        }
        #endregion
    }
}