using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    /// <summary>
    /// Provides information about the Barreto-Naehrig curve Bn128.
    /// </summary>
    public abstract class Bn128Curve
    {
        #region Fields
        private static Fp12 _twistPoint;
        #endregion

        #region Properties
        /// <summary>
        /// The elliptic curve order of the bn128 curve. This is a number which when multiplied to any other point, yields the point at infinity.
        /// </summary>
        public static BigInteger N { get; }
        /// <summary>
        /// The prime used for our field operations. Referred to as p in F_p. Used as the modulo divisor to wrap values around the field.
        /// </summary>
        public static BigInteger P { get; }

        /// <summary>
        /// Generator for curve over FQ
        /// </summary>
        public static FpVector3<Fp> G1 { get; }
        /// <summary>
        /// Generator for curve over FQ2
        /// </summary>
        public static FpVector3<Fp2> G2 { get; }
        public static Fp B { get; }
        public static Fp2 B2 { get; }
        public static Fp12 B12 { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Our default static constructor, sets static read only variables.
        /// </summary>
        static Bn128Curve()
        {
            N = BigInteger.Parse("21888242871839275222246405745257275088548364400416034343698204186575808495617", CultureInfo.InvariantCulture);
            P = BigInteger.Parse("21888242871839275222246405745257275088696311157297823662689037894645226208583", CultureInfo.InvariantCulture);
            _twistPoint = new Fp12(0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            G1 = new FpVector3<Fp>(Fp.OneValue, new Fp(2), Fp.OneValue);
            G2 = new FpVector3<Fp2>(
                new Fp2(
                    BigInteger.Parse("10857046999023057135944570762232829481370756359578518086990519993285655852781", CultureInfo.InvariantCulture),
                    BigInteger.Parse("11559732032986387107991004021392285783925812861821192530917403151452391805634", CultureInfo.InvariantCulture)),
                new Fp2(
                    BigInteger.Parse("8495653923123431417604973247489272438418190587263600148770280649306958101930", CultureInfo.InvariantCulture),
                    BigInteger.Parse("4082367875863433681332203403145435568316851327593401208105741076214120093531", CultureInfo.InvariantCulture)),
                Fp2.OneValue);
            B = new Fp(3);
            B2 = new Fp2(3, 0) / new Fp2(9, 1);
            B12 = new Fp12(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
        #endregion

        #region Functions
        public static FpVector3<Fp12> Twist(FpVector3<Fp2> point)
        {
            // If the point isn't properly initialize
            if (point == null)
            {
                return null;
            }

            // Place the items at the start and half way point
            BigInteger[] xcoefficients = new BigInteger[12] { point.X.Coefficients.ElementAt(0) - (point.X.Coefficients.ElementAt(1) * 9), 0, 0, 0, 0, 0, point.X.Coefficients.ElementAt(1), 0, 0, 0, 0, 0 };
            BigInteger[] ycoefficients = new BigInteger[12] { point.Y.Coefficients.ElementAt(0) - (point.Y.Coefficients.ElementAt(1) * 9), 0, 0, 0, 0, 0, point.Y.Coefficients.ElementAt(1), 0, 0, 0, 0, 0 };
            BigInteger[] zcoefficients = new BigInteger[12] { point.Z.Coefficients.ElementAt(0) - (point.Z.Coefficients.ElementAt(1) * 9), 0, 0, 0, 0, 0, point.Z.Coefficients.ElementAt(1), 0, 0, 0, 0, 0 };

            // Instantiate fp12s from the extended fp2
            Fp12 newx = new Fp12(xcoefficients);
            Fp12 newy = new Fp12(ycoefficients);
            Fp12 newz = new Fp12(zcoefficients);

            // Return a twisted point from fp2 to fp12.
            return new FpVector3<Fp12>(newx * _twistPoint.Pow(2), newy * _twistPoint.Pow(3), newz);
        }
        #endregion
    }
}
