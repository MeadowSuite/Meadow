using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

/*
 * Portions of code ported from: https://github.com/ethereum/py_ecc
 */

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    public abstract class FpExtensionBase<T> : IField<T> where T : FpExtensionBase<T>
    {
        #region Properties
        public IReadOnlyCollection<BigInteger> Coefficients { get; }
        public abstract IReadOnlyCollection<BigInteger> ModulusCoefficients { get; }
        public int Degree
        {
            get
            {
                return ModulusCoefficients.Count;
            }
        }
        #endregion

        #region Constructors
        public FpExtensionBase(IReadOnlyCollection<BigInteger> coefficients)
        {
            // Verify we have an equal amount of coefficients and modulus coefficients.
            if (coefficients == null || ModulusCoefficients == null)
            {
                throw new ArgumentNullException("Coefficients array provided to F_p extension cannot be null.");
            }
            else if (coefficients.Count != ModulusCoefficients.Count)
            {
                throw new ArgumentNullException("Coefficient arrays provided to F_p extension must have equal lengths.");
            }

            // Set our coefficients.
            Coefficients = coefficients;
        }
        #endregion

        #region Functions
        protected abstract T New(IEnumerable<BigInteger> coefficients);

        public T Add(T other)
        {
            return New(Coefficients.Zip(other.Coefficients, (x, y) => (x + y).Mod(Bn128Curve.P)));
        }

        public T Add(BigInteger other)
        {
            return New(Coefficients.Select(x => (x + other).Mod(Bn128Curve.P)));
        }

        public T Subtract(T other)
        {
            return New(Coefficients.Zip(other.Coefficients, (x, y) => (x - y).Mod(Bn128Curve.P)));
        }

        public T Subtract(BigInteger other)
        {
            return New(Coefficients.Select(x => (x - other).Mod(Bn128Curve.P)));
        }

        public T Multiply(T other)
        {
            throw new NotImplementedException();
        }

        public T Multiply(BigInteger other)
        {
            return New(Coefficients.Select(x => (x * other).Mod(Bn128Curve.P)));
        }

        public T Divide(T other)
        {
            return Multiply(other.Inverse());
        }

        public T Divide(BigInteger other)
        {
            return New(Coefficients.Select(x => (x * other.ModInverse(Bn128Curve.P)).Mod(Bn128Curve.P)));
        }

        public T Negate()
        {
            // Negate all coefficients.
            return New(Coefficients.Select(x => (-x).Mod(Bn128Curve.P)));
        }

        public T Inverse()
        {
            throw new NotImplementedException();
        }

        public T Pow(BigInteger exponent)
        {
            // Create a new field of the same size with the first coefficient set only.
            var oData = new BigInteger[Degree];
            oData[0] = 1;
            T o = New(oData);
            T t = (T)this;

            // Loop for each bit in our exponent, until there are none left to process.
            // Essentially what is happening here is the same Pow() method as the singular
            // Fp field element code, but it is happening for each coefficient due to our
            // implemented multiply operation.
            while (exponent > 0)
            {
                // If our current bit is set in the exponent, we multiply o by t
                if ((exponent & 1) != 0)
                {
                    o = o.Multiply(t);
                }

                // Shift to remove the processed bit
                exponent >>= 1;

                // Square t
                t = t.Multiply(t);
            }

            // Return our result.
            return o;
        }

        public override bool Equals(object obj)
        {
            return obj is FpExtensionBase<T> other ? Coefficients.SequenceEqual(other.Coefficients) : false;
        }

        public override int GetHashCode()
        {
            return Coefficients.GetHashCode();
        }
        #endregion

        #region Operators
        public static bool operator !=(FpExtensionBase<T> left, FpExtensionBase<T> right) => !(left == right);
        public static bool operator ==(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Coefficients.SequenceEqual(right.Coefficients);
        public static FpExtensionBase<T> operator +(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Add((T)right);
        public static FpExtensionBase<T> operator -(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Subtract((T)right);
        public static FpExtensionBase<T> operator *(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Multiply((T)right);
        public static FpExtensionBase<T> operator /(FpExtensionBase<T> dividend, FpExtensionBase<T> divisor) => dividend.Divide((T)divisor);
        public static FpExtensionBase<T> operator -(FpExtensionBase<T> number) => number.Negate();
        #endregion
    }
}
