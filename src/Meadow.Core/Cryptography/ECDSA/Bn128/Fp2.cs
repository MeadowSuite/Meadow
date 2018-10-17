using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.ECDSA.Bn128
{
    public class Fp2 : FpExtensionBase<Fp2>
    {
        #region Fields
        private static IReadOnlyCollection<BigInteger> _modulusCoefficients = new BigInteger[] { 1, 0 };
        public static readonly Fp2 Zero = new Fp2(new BigInteger[2]);
        public static readonly Fp2 One = new Fp2(new BigInteger[2] { 1, 0 });
        #endregion

        #region Properties
        public override IReadOnlyCollection<BigInteger> ModulusCoefficients => _modulusCoefficients;
        #endregion

        #region Constructor
        public Fp2(IReadOnlyCollection<BigInteger> coefficients) : base(coefficients)
        {

        }
        #endregion

        #region Functions
        protected override Fp2 New(IEnumerable<BigInteger> coefficients)
        {
            return new Fp2(coefficients.ToArray());
        }
        #endregion
    }
}
