using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

/*
 * Portions of code ported from: https://github.com/ethereum/py_ecc
 */

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    public abstract class Bn128Pairing
    {
        #region Properties
        public static BigInteger AteLoopCount { get; }
        public static int LogAteLoopCount { get; }
        public static int[] PseudoBinaryEncoding { get; } 
        #endregion

        #region Constructor
        /// <summary>
        /// Our default static constructor, sets static read only variables.
        /// </summary>
        static Bn128Pairing()
        {
            AteLoopCount = BigInteger.Parse("29793968203157093288", CultureInfo.InvariantCulture);
            LogAteLoopCount = 63;
            PseudoBinaryEncoding = new int[] 
            {
                0, 0, 0, 1, 0, 1, 0, -1, 0, 0, 1, -1, 0, 0, 1, 0,
                0, 1, 1, 0, -1, 0, 0, 1, 0, -1, 0, 0, 0, 0, 1, 1,
                1, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 1,
                1, 0, 0, -1, 0, 0, 0, 1, 1, 0, -1, 0, 0, 1, 0, 1,
                1,
            };
        }
        #endregion

        #region Functions
        private static (Fp12 numerator, Fp12 denominator) LineFunction(FpVector3<Fp12> p1, FpVector3<Fp12> p2, FpVector3<Fp12> t)
        {
            // As mentioned in py_ecc: the projective coordinates given are (x / z, y / z).
            // For slope of a line m, we usually have delta_y / delta_x. In this case it would be
            // m = ((p2.y / p2.z) - (p1.y / p1.z)) / ((p2.x / p2.z) - (p1.x / p1.z))
            // So to eliminate these fractions, we multiply both numerator and denominator by p1.z * p2.z,
            // which yields the values below. This only affects scale but keeps the same m result.
            Fp12 slopeNumerator = (p2.Y * p1.Z) - (p1.Y * p2.Z);
            Fp12 slopeDenominator = (p2.X * p1.Z) - (p1.X * p2.Z);

            // Determine how to compute our data.
            bool denominatorIsZero = slopeDenominator == Fp12.ZeroValue;
            bool numeratorIsZero = slopeNumerator == Fp12.ZeroValue;
            if (denominatorIsZero && !numeratorIsZero)
            {
                // Slope is undefined.
                return ((t.X * p1.Z) - (p1.X * t.Z), p1.Z * t.Z);
            }
            else if (numeratorIsZero)
            {
                slopeNumerator = 3 * p1.X * p1.X;
                slopeDenominator = 2 * p1.Y * p1.Z;
            }

            Fp12 resultNumerator = (slopeNumerator * ((t.X * p1.Z) - (p1.X * t.Z))) - (slopeDenominator * ((t.Y * p1.Z) - (p1.Y * t.Z)));
            Fp12 resultDenominator = slopeDenominator * t.Z * p1.Z;
            return (resultNumerator, resultDenominator);
        }

        private static Fp12 MillerLoop(FpVector3<Fp12> q, FpVector3<Fp12> p, bool finalExponentiate = true)
        {
            if (q == null || p == null)
            {
                return Fp12.OneValue;
            }

            FpVector3<Fp12> nq = q.Negate();

            FpVector3<Fp12> r = (FpVector3<Fp12>)q.Clone();
            Fp12 fNumerator = Fp12.OneValue;
            Fp12 fDenominator = Fp12.OneValue;
            for (int i = LogAteLoopCount; i >= 0; i--)
            {
                (Fp12 num, Fp12 denom) = LineFunction(r, r, p);
                fNumerator = fNumerator * fNumerator * num;
                fDenominator = fDenominator * fDenominator * denom;
                r = r.Double();

                int v = PseudoBinaryEncoding[i];
                if (v == 1)
                {
                    (num, denom) = LineFunction(r, q, p);
                    fNumerator *= num;
                    fDenominator *= denom;
                    r = r.Add(q);
                }
                else if (v == -1)
                { 
                    (num, denom) = LineFunction(r, nq, p);
                    fNumerator *= num;
                    fDenominator *= denom;
                    r = r.Add(nq);
                }
            }

            FpVector3<Fp12> q1 = new FpVector3<Fp12>(q.X.Pow(Bn128Curve.P), q.Y.Pow(Bn128Curve.P), q.Z.Pow(Bn128Curve.P));
            FpVector3<Fp12> nq2 = new FpVector3<Fp12>(q1.X.Pow(Bn128Curve.P), -q1.Y.Pow(Bn128Curve.P), q1.Z.Pow(Bn128Curve.P));

            (Fp12 num1, Fp12 denom1) = LineFunction(r, q1, p);
            r = r.Add(q1);
            (Fp12 num2, Fp12 denom2) = LineFunction(r, nq2, p);
            Fp12 f = (fNumerator * num1 * num2) / (fDenominator * denom1 * denom2);

            return finalExponentiate ? FinalExponentiate(f) : f;
        }

        public static Fp12 Pair(FpVector3<Fp2> q, FpVector3<Fp> p, bool finalExponentiate = true)
        {
            // Check z's for zero.
            if (p.Z == Fp.ZeroValue || q.Z == Fp2.ZeroValue)
            {
                return Fp12.OneValue;
            }

            // Run the miller loop on twist(q) and fq12(p)
            return MillerLoop(Bn128Curve.Twist(q), CastFpPointToFp12Point(p), finalExponentiate);
        }

        public static Fp12 FinalExponentiate(Fp12 p)
        {
            return p.Pow((BigInteger.Pow(Bn128Curve.P, 12) - 1) / Bn128Curve.N);
        }

        private static FpVector3<Fp12> CastFpPointToFp12Point(FpVector3<Fp> point)
        {
            // If our point isn't initialized, return null.
            if (point == null)
            {
                return null;
            }

            // Create our data for our fq
            BigInteger[] xData = new BigInteger[12];
            xData[0] = point.X.N;
            BigInteger[] yData = new BigInteger[12];
            yData[0] = point.Y.N;
            BigInteger[] zData = new BigInteger[12];
            zData[0] = point.Z.N;

            // Return our Fp12 vector.
            return new FpVector3<Fp12>(new Fp12(xData), new Fp12(yData), new Fp12(zData));
        }
        #endregion
    }
}
