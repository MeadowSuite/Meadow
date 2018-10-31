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
        public void TestFp()
        {
            Assert.Equal(new Fp(4), new Fp(2) * new Fp(2));
            Assert.Equal(new Fp(11) / new Fp(7), (new Fp(2) / new Fp(7)) + (new Fp(9) / new Fp(7)));
            Assert.Equal(new Fp(11) * new Fp(7), (new Fp(2) * new Fp(7)) + (new Fp(9) * new Fp(7)));
            Assert.Equal(new Fp(9), new Fp(9).Pow(Bn128Curve.P));
        }

        [Fact]
        public void TestFp2()
        {
            Fp2 x = new Fp2(1, 0);
            Fp2 f = new Fp2(1, 2);
            Fp2 fpx = new Fp2(2, 2);

            Assert.Equal(fpx, x + f);
            Assert.Equal(Fp2.OneValue, f / f);
            Assert.Equal((Fp2.OneValue + x) / f, (Fp2.OneValue / f) + (x / f));
            Assert.Equal((Fp2.OneValue + x) * f, (Fp2.OneValue * f) + (x * f));
        }

        [Fact]
        public void TestFp12()
        {
            Fp12 x = new Fp12(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            Fp12 f = new Fp12(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            Fp12 fpx = new Fp12(2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            Assert.Equal(fpx, x + f);
            Assert.Equal(Fp12.OneValue, f / f);
            Assert.Equal((Fp12.OneValue + x) / f, (Fp12.OneValue / f) + (x / f));
            Assert.Equal((Fp12.OneValue + x) * f, (Fp12.OneValue * f) + (x * f));
        }

        [Fact]
        public void TestPairing()
        {
            var standardResult = Bn128Pairing.Pair(Bn128Curve.G2, Bn128Curve.G1);
            var negativeG1Result = Bn128Pairing.Pair(Bn128Curve.G2, Bn128Curve.G1.Negate());
            Assert.Equal(Fp12.OneValue, standardResult * negativeG1Result);

            var negativeG2Result = Bn128Pairing.Pair(Bn128Curve.G2.Negate(), Bn128Curve.G1);
            Assert.Equal(Fp12.OneValue, standardResult * negativeG1Result);

            Assert.Equal(Fp12.OneValue, standardResult.Pow(Bn128Curve.N));

            var twoTimesG1Result = Bn128Pairing.Pair(Bn128Curve.G2, Bn128Curve.G1.Multiply(2));
            Assert.Equal(standardResult * standardResult, twoTimesG1Result);

            Assert.NotEqual(standardResult, twoTimesG1Result);
            Assert.NotEqual(standardResult, negativeG2Result);
            Assert.NotEqual(twoTimesG1Result, negativeG2Result);

            var twoTimesG2Result = Bn128Pairing.Pair(Bn128Curve.G2.Multiply(2), Bn128Curve.G1);
            Assert.Equal(standardResult * standardResult, twoTimesG2Result);

            var final1 = Bn128Pairing.Pair(Bn128Curve.G2.Multiply(27), Bn128Curve.G1.Multiply(37));
            var final2 = Bn128Pairing.Pair(Bn128Curve.G2, Bn128Curve.G1.Multiply(999));
            Assert.Equal(final1, final2);
        }
    }
}
