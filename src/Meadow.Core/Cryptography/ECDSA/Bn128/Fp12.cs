using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    public class Fp12 : FpExtensionBase<Fp12>
    {
        #region Fields
        private static ICollection<BigInteger> _modulusCoefficients = new BigInteger[] { 82, 0, 0, 0, 0, 0, -18, 0, 0, 0, 0, 0 };
        public static readonly Fp12 Zero = new Fp12(new BigInteger[12]);
        public static readonly Fp12 One = new Fp12(new BigInteger[12] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        #endregion

        #region Properties
        public override ICollection<BigInteger> ModulusCoefficients => _modulusCoefficients;
        #endregion

        #region Constructor
        public Fp12(ICollection<BigInteger> coefficients) : base(coefficients)
        {

        }
        #endregion

        #region Functions
        protected override Fp12 New(IEnumerable<BigInteger> coefficients)
        {
            return new Fp12(coefficients.ToArray());
        }
        #endregion
    }
}
