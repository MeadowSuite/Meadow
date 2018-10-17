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

        /// <summary>
        /// Computes the modular multiplicative inverse for a given integer.
        /// </summary>
        /// <param name="a">The integer which we should obtain the modular multiplicative inverse for.</param>
        /// <param name="n">The upper bound/wrapping point of the ring.</param>
        /// <returns>Returns the multiplicative inverse for the given integer.</returns>
        public static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            // References:
            // https://en.wikipedia.org/wiki/Modular_multiplicative_inverse
            // https://en.wikipedia.org/wiki/Finite_field_arithmetic#Multiplicative_inverse
            // https://en.wikipedia.org/wiki/Extended_Euclidean_algorithm#Modular_integers

            // If a == 0, our result is 0
            if (a == 0)
            {
                return 0;
            }

            // Define our initial variables and give them their initial assignments.
            BigInteger newt = 1;
            BigInteger t = 0;
            BigInteger newr = a % n;
            BigInteger r = n;

            // Loop until we have handled all fractions/quotients.
            while (newr != 0)
            {
                // Calculate our quotient for this round.
                BigInteger quotient = r / newr;

                // Update our t/r values by subtracting our fraction.
                (t, newt) = (newt, t - (quotient * newt));
                (r, newr) = (newr, r - (quotient * newr));
            }

            // If our r > 1, we cannot inverse this number
            if (r > 1)
            {
                throw new ArgumentException("Could not obtain the modular multiplicative inverse because argument is not invertable.");
            }

            // If our t value is negative, we correct it by offseting it from our upper bound.
            if (t < 0)
            {
                t += n;
            }

            // Return our result
            return t % n;
        }

        /// <summary>
        /// Performs true modulo division (as C#'s % operator does not perform modulo divison, but remainder instead).
        /// </summary>
        /// <param name="a">The number to perform modulo division on.</param>
        /// <param name="n">The divisor for the modulo operation.</param>
        /// <returns>Returns the result of modulo divison on a with the divisor of n.</returns>
        public static BigInteger Mod(this BigInteger a, BigInteger n)
        {
            // Perform a remainder operation on our number
            BigInteger result = a % n;

            // If it's negative, add the upper bound to it to make it within bounds.
            if (result < 0)
            {
                result += n;
            }

            // Return the result.
            return result;
        }
        #endregion
    }
}