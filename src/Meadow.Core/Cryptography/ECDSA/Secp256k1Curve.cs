using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.Ecdsa
{
    /// <summary>
    /// Provides relevant information to the ECDSA provider about the Secp256k1 curve.
    /// </summary>
    public abstract class Secp256k1Curve
    {
        #region Fields
        private static BigInteger _halfN;
        private static Org.BouncyCastle.Math.BigInteger _b_halfN;
        #endregion

        #region Properties
        /// <summary>
        /// The elliptic curve order of the secp256k1 curve. This is a number which when multiplied to any other point, yields the point at infinity.
        /// </summary>
        public static BigInteger N { get; }
        /// <summary>
        /// The elliptic curve base point of the secp256k1 curve.
        /// </summary>
        public static BigInteger G { get; }
        /// <summary>
        /// Our elliptic curve parameters.
        /// </summary>
        public static X9ECParameters Parameters { get; }
        /// <summary>
        /// The domain parameters for this curve.
        /// </summary>
        public static ECDomainParameters DomainParameters { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Our default static constructor, sets static read only variables.
        /// </summary>
        static Secp256k1Curve()
        {
            Parameters = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("secp256k1");
            DomainParameters = new ECDomainParameters(Parameters.Curve, Parameters.G, Parameters.N, Parameters.H);
            N = Parameters.N.ToNumericsBigInteger();
            _b_halfN = Parameters.N.Divide(Org.BouncyCastle.Math.BigInteger.Two);
            _halfN = _b_halfN.ToNumericsBigInteger();
        }
        #endregion

        #region Functions
        public static BigInteger EnforceLowS(BigInteger s)
        {
            // If it's large we set it as N - S.
            if (s.CompareTo(_halfN) > 0)
            {
                return N - s;
            }

            // Otherwise we simply return it.
            return s;
        }

        public static Org.BouncyCastle.Math.BigInteger EnforceLowS(Org.BouncyCastle.Math.BigInteger s)
        {
            // If it's large we set it as N - S.
            if (s.CompareTo(_b_halfN) > 0)
            {
                return Parameters.N.Subtract(s);
            }

            // Otherwise we simply return it.
            return s;
        }

        public static bool CheckLowS(BigInteger s)
        {
            // Check that s is low.
            return s.CompareTo(_halfN) < 0;
        }

        public static bool CheckLowS(Org.BouncyCastle.Math.BigInteger s)
        {
            // Check that s is low.
            return s.CompareTo(_b_halfN) < 0;
        }
        #endregion
    }
}
