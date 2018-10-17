using Meadow.Core.Cryptography.ECDSA.Bn128;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    /*
     * Tests ported from: https://github.com/ethereum/py_ecc
     * */
    public class Bn128Tests
    {
        [Fact]
        public void TestFP()
        {
            Assert.Equal(new Fp(4), new Fp(2) * new Fp(2));
            Assert.Equal(new Fp(11) / new Fp(7), (new Fp(2) / new Fp(7)) + (new Fp(9) / new Fp(7)));
            Assert.Equal(new Fp(11) * new Fp(7), (new Fp(2) * new Fp(7)) + (new Fp(9) * new Fp(7)));
            Assert.Equal(new Fp(9), new Fp(9).Pow(Bn128Curve.P));
        }

        [Fact]
        public void TestFP12()
        {
            var xData = new BigInteger[12];
            xData[0] = 1;

            var fData = new BigInteger[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var fpxData = new BigInteger[12] { 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            Fp12 x = new Fp12(xData);
            Fp12 f = new Fp12(fData);
            Fp12 fpx = new Fp12(fpxData);

            Assert.Equal(fpx, x + f);
            Assert.Equal(Fp12.One, f / f);
            Assert.Equal((Fp12.One + x) / f, (Fp12.One / f) + (x / f));
            Assert.Equal((Fp12.One + x) * f, (Fp12.One * f) + (x * f));
        }
    }
}
