using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    /// <summary>
    /// Represents a field element wrapped around at point P from our bn128 curve.
    /// </summary>
    public struct Fp : IField<Fp>
    {
        #region Fields
        public static readonly Fp Zero = new Fp(0);
        public static readonly Fp One = new Fp(1);

        public readonly BigInteger N;
        #endregion

        #region Constructors
        public Fp(BigInteger n)
        {
            // Set our internal field element.
            N = n.Mod(Bn128Curve.P);
        }

        public Fp(Fp fp)
        {
            // Set our internal field element. It is already wrapped around accordingly so we quickly set its value.
            N = fp.N;
        }
        #endregion

        #region Functions
        public Fp Add(Fp other)
        {
            return Add(other.N);
        }

        public Fp Add(BigInteger other)
        {
            return new Fp((N + other).Mod(Bn128Curve.P));
        }

        public Fp Subtract(Fp other)
        {
            return Subtract(other.N);
        }

        public Fp Subtract(BigInteger other)
        {
            // Perform modulo division on our result
            BigInteger result = (N - other).Mod(Bn128Curve.P);
            return new Fp(result);
        }

        public Fp Multiply(Fp other)
        {
            return Multiply(other.N);
        }

        public Fp Multiply(BigInteger other)
        {
            return new Fp((N * other).Mod(Bn128Curve.P));
        }

        public Fp Divide(Fp other)
        {
            return Divide(other.N);
        }

        public Fp Divide(BigInteger other)
        {
            return new Fp((N * other.ModInverse(Bn128Curve.P)).Mod(Bn128Curve.P));
        }

        public Fp Negate()
        {
            // Perform modulo division on our result
            BigInteger result = (-N).Mod(Bn128Curve.P);
            return new Fp(result);
        }

        public Fp Inverse()
        {
            return new Fp(N.ModInverse(Bn128Curve.P));
        }

        public Fp Pow(Fp exponent)
        {
            return Pow(exponent.N);
        }

        public Fp Pow(BigInteger exponent)
        {
            // If our exponent is 0, return 1
            if (exponent == 0)
            {
                return new Fp(1);
            }
            else if (exponent == 1)
            {
                // If the exponent is 1, return the number itself.
                return new Fp(N);
            }
            else if (exponent % 2 == 0)
            {
                return Multiply(N).Pow(exponent / 2);
            }
            else
            {
                return Multiply(N).Pow(exponent / 2).Multiply(N);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Fp other ? N == other.N : false;
        }

        public override int GetHashCode()
        {
            return N.GetHashCode();
        }
        #endregion

        #region Operators
        public static bool operator !=(Fp left, Fp right) => !(left == right);
        public static bool operator ==(Fp left, Fp right) => left.N == right.N;
        public static Fp operator +(Fp left, Fp right) => left.Add(right);
        public static Fp operator -(Fp left, Fp right) => left.Subtract(right);
        public static Fp operator *(Fp left, Fp right) => left.Multiply(right);
        public static Fp operator /(Fp dividend, Fp divisor) => dividend.Divide(divisor);
        public static Fp operator -(Fp number) => number.Negate();
        #endregion
    }
}
