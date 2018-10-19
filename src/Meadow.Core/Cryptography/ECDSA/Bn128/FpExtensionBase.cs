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
        public ICollection<BigInteger> Coefficients { get; }
        public abstract ICollection<BigInteger> ModulusCoefficients { get; }
        public int Degree
        {
            get
            {
                return ModulusCoefficients.Count;
            }
        }
        public abstract T Zero { get; }
        public abstract T One { get; }
        #endregion

        #region Constructors
        public FpExtensionBase(ICollection<BigInteger> coefficients)
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
            // Create an extended list which can contain results from both coefficient collections.
            List<BigInteger> b = new List<BigInteger>(new BigInteger[(Degree * 2) - 1]);

            // Compute each product linearly for every index in our list.
            for (int i = 0; i < Coefficients.Count; i++)
            {
                for (int j = 0; j < other.Coefficients.Count; j++)
                {
                    b[i + j] += Coefficients.ElementAt(i) * other.Coefficients.ElementAt(j);
                }
            }

            for (int exp = Degree - 2; exp >= 0; exp--)
            {
                BigInteger top = b[b.Count - 1];
                b.RemoveAt(b.Count - 1);
                for (int i = 0; i < ModulusCoefficients.Count; i++)
                {
                    b[exp + i] -= top * ModulusCoefficients.ElementAt(i);
                }
            }

            // Perform modular division to wrap our coefficients around p.
            return New(b.Select(x => x.Mod(Bn128Curve.P)));
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

        private int GetDegree(BigInteger[] p)
        {
            int d = p.Length - 1;
            while (p[d] == 0 && d != 0)
            {
                d--;
            }

            return d;
        }

        private BigInteger[] DividePolynomialRounded(BigInteger[] a, BigInteger[] b)
        {
            var dega = GetDegree(a);
            var degb = GetDegree(b);
            var temp = (BigInteger[])a.Clone();
            var o = new BigInteger[a.Length];

            for (int i = dega - degb; i >= 0; i--)
            {
                o[i] = o[i] + (temp[degb + i] * b[degb].ModInverse(Bn128Curve.P));
                for (int j = 0; j < degb + 1; j++)
                {
                    temp[i + j] -= o[j];
                }
            }

            // Resize our result array and wrap it around p.
            Array.Resize(ref o, GetDegree(o) + 1);
            for (int i = 0; i < o.Length; i++)
            {
                o[i] = o[i].Mod(Bn128Curve.P);
            }

            return o;
        }

        public T Inverse()
        {
            // Define our initial variables and give them their initial assignments.
            BigInteger[] newt = new BigInteger[Degree + 1];
            newt[0] = 1;

            BigInteger[] t = new BigInteger[Degree + 1];

            BigInteger[] newr = new BigInteger[Coefficients.Count + 1];
            Coefficients.CopyTo(newr, 0);

            BigInteger[] r = new BigInteger[ModulusCoefficients.Count + 1];
            ModulusCoefficients.CopyTo(r, 0);
            r[r.Length - 1] = 1;

            // Loop while there are elements which are non-zero.
            while (GetDegree(newr) != 0)
            {
                BigInteger[] quotient = DividePolynomialRounded(r, newr);
                Array.Resize(ref quotient, Degree + 1);

                BigInteger[] tempt = (BigInteger[])t.Clone();
                BigInteger[] tempr = (BigInteger[])r.Clone();
                for (int i = 0; i < Degree + 1; i++)
                {
                    for (int j = 0; j < Degree + 1 - i; j++)
                    {
                        tempt[i + j] -= newt[i] * quotient[j];
                        tempr[i + j] -= newr[i] * quotient[j];
                    }
                }

                // Perform modulo on tempt
                for (int i = 0; i < tempt.Length; i++)
                {
                    tempt[i] = tempt[i].Mod(Bn128Curve.P);
                }

                // Perform modulo on tempr
                for (int i = 0; i < tempr.Length; i++)
                {
                    tempr[i] = tempr[i].Mod(Bn128Curve.P);
                }

                // Swap state for the next iteration.
                (newt, newr, t, r) = (tempt, tempr, newt, newr);
            }

            // Resize the array to the degree size accordingly, divide and return.
            Array.Resize(ref newt, Degree);
            return New(newt).Divide(newr[0]);
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

        public static T operator +(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Add((T)right);
        public static T operator +(FpExtensionBase<T> left, BigInteger right) => left.Add(right);
        public static T operator +(BigInteger left, FpExtensionBase<T> right) => right.Add(left);

        public static T operator -(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Subtract((T)right);
        public static T operator -(FpExtensionBase<T> left, BigInteger right) => left.Subtract(right);

        public static T operator *(FpExtensionBase<T> left, FpExtensionBase<T> right) => left.Multiply((T)right);
        public static T operator *(FpExtensionBase<T> left, BigInteger right) => left.Multiply(right);
        public static T operator *(BigInteger left, FpExtensionBase<T> right) => right.Multiply(left);

        public static T operator /(FpExtensionBase<T> dividend, FpExtensionBase<T> divisor) => dividend.Divide((T)divisor);
        public static T operator /(FpExtensionBase<T> dividend, BigInteger divisor) => dividend.Divide(divisor);

        public static T operator -(FpExtensionBase<T> number) => number.Negate();

        #endregion
    }
}
