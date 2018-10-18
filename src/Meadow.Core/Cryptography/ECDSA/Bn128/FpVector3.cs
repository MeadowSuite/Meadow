using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

/*
 * Portions of code ported from: https://github.com/ethereum/py_ecc
 */

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    public struct FpVector3<T> : ICloneable where T : IField<T>
    {
        #region Fields
        public T X;
        public T Y;
        public T Z;
        #endregion

        #region Properties
        public bool Initialized
        {
            get
            {
                return !(X == null || Y == null || Z == null);
            }
        }
        #endregion

        #region Constructor
        public FpVector3(T x, T y, T z)
        {
            // Set our components
            X = x;
            Y = y;
            Z = z;
        }
        #endregion

        #region Function
        public bool IsInfinity()
        {
            return Z.Equals(Z.Zero);
        }

        public bool IsOnCurveCheck(Fp b)
        {
            // If this point is at infinity, we mark it as on the curve.
            if (IsInfinity())
            {
                return true;
            }

            return Y.Pow(2).Multiply(Z).Subtract(X.Pow(3)).Equals(Z.Pow(3).Multiply(b.N));
        }

        public (T X, T Y) Normalize()
        {
            return (X.Divide(Z), Y.Divide(Z));
        }

        public FpVector3<T> Double()
        {
            T w = X.Multiply(3).Multiply(X);
            T s = Y.Multiply(Z);
            T b = X.Multiply(Y).Multiply(s);
            T h = w.Multiply(w).Subtract(b.Multiply(8));
            T s_sq = s.Multiply(s);

            T newx = h.Multiply(2).Multiply(s);
            T newy = w.Multiply(b.Multiply(4).Subtract(h)).Subtract(Y.Multiply(8).Multiply(Y).Multiply(s_sq));
            T newz = s.Multiply(8).Multiply(s_sq);
            return new FpVector3<T>(newx, newy, newz);
        }

        public FpVector3<T> Add(FpVector3<T> p2)
        {
            // Verify our z coordinates aren't zero
            if (Z.Equals(X.Zero))
            {
                return this;
            }
            else if (p2.Z.Equals(X.Zero))
            {
                return p2;
            }

            T u1 = p2.Y.Multiply(Z);
            T u2 = Y.Multiply(p2.Z);
            T v1 = p2.X.Multiply(Z);
            T v2 = X.Multiply(p2.Z);
            if (v1.Equals(v2) && u1.Equals(u2))
            {
                return Double();
            }
            else if (v1.Equals(v2))
            {
                return new FpVector3<T>(X.One, X.One, X.Zero);
            }

            T u = u1.Subtract(u2);
            T v = v1.Subtract(v2);
            T v_sq = v.Multiply(v);
            T v_sq_v2 = v_sq.Multiply(v2);
            T v_cu = v_sq.Multiply(v);

            T w = Z.Multiply(p2.Z);
            T a = u.Multiply(u).Multiply(w).Subtract(v_cu).Subtract(v_sq_v2.Multiply(2));

            T newx = v.Multiply(a);
            T newy = u.Multiply(v_sq_v2.Subtract(a)).Subtract(v_cu.Multiply(u2));
            T newz = v_cu.Multiply(w);
            return new FpVector3<T>(newx, newy, newz);
        }

        public FpVector3<T> Multiply(BigInteger n)
        {
            if (n == 0)
            {
                return new FpVector3<T>(X.One, X.One, X.Zero);
            }
            else if (n == 1)
            {
                return this;
            }
            else if (n % 2 == 0)
            {
                return Double().Multiply(n / 2);
            }
            else
            {
                return Double().Multiply(n / 2).Add(this);
            }
            // Return a new vector with negated y.
            return new FpVector3<T>(X, Y.Negate(), Z);
        }

        public FpVector3<T> Negate()
        {
            // Return a new vector with negated y.
            return new FpVector3<T>(X, Y.Negate(), Z);
        }

        public object Clone()
        {
            return new FpVector3<T>(X, Y, Z);
        }
        #endregion
    }
}
