using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    /// <summary>
    /// Provides information about the Barreto-Naehrig curve Bn128.
    /// </summary>
    public abstract class Bn128Curve
    {
        #region Properties
        /// <summary>
        /// The elliptic curve order of the bn128 curve. This is a number which when multiplied to any other point, yields the point at infinity.
        /// </summary>
        public static BigInteger N { get; }
        /// <summary>
        /// The prime used for our field operations. Referred to as p in F_p. Used as the modulo divisor to wrap values around the field.
        /// </summary>
        public static BigInteger P { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Our default static constructor, sets static read only variables.
        /// </summary>
        static Bn128Curve()
        {
            N = BigInteger.Parse("21888242871839275222246405745257275088548364400416034343698204186575808495617", CultureInfo.InvariantCulture);
            P = BigInteger.Parse("21888242871839275222246405745257275088696311157297823662689037894645226208583", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Functions

        #endregion
    }
}
