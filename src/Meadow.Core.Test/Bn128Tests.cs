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
            Fp2 x = new Fp2(new BigInteger[2] { 1, 0 });
            Fp2 f = new Fp2(new BigInteger[2] { 1, 2 });
            Fp2 fpx = new Fp2(new BigInteger[2] { 2, 2 });

            Assert.Equal(fpx, x + f);
            Assert.Equal(Fp2.One, f / f);
            Assert.Equal((Fp2.One + x) / f, (Fp2.One / f) + (x / f));
            Assert.Equal((Fp2.One + x) * f, (Fp2.One * f) + (x * f));
        }

        [Fact]
        public void TestFp12()
        {
            Fp12 x = new Fp12(new BigInteger[12] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            Fp12 f = new Fp12(new BigInteger[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            Fp12 fpx = new Fp12(new BigInteger[12] { 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

            Assert.Equal(fpx, x + f);
            Assert.Equal(Fp12.One, f / f);
            Assert.Equal((Fp12.One + x) / f, (Fp12.One / f) + (x / f));
            Assert.Equal((Fp12.One + x) * f, (Fp12.One * f) + (x * f));
        }
    }
}
