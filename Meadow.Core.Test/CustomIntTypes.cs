using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    // Custom int types are currently unused
    /*
    public class CustomIntTypes
    {
        [Fact]
        public void TestUInt24()
        {
            var orig = 12345;
            UInt24 num = orig;
            Assert.Equal(num.ToString(), orig.ToString());
        }

        [Fact]
        public void TestUInt24Max()
        {
            var orig = 16777216;
            Assert.Equal(UInt24.MaxValue.ToString(), orig.ToString());
        }

        [Fact]
        public void TestUInt24Underflow()
        {
            var orig = -12345;
            UInt24 num;
            Assert.Throws<OverflowException>(() => num = orig);
        }

        [Fact]
        public void TestUInt24Overflow()
        {
            var orig = 16777217;
            UInt24 num;
            Assert.Throws<OverflowException>(() => num = orig);
        }

        [Fact]
        public void UInt24Mul()
        {
            UInt24 numA = 5;
            UInt24 numB = 6;
            UInt24 res = numA * numB;
            Assert.Equal("30", res.ToString());
        }

        [Fact]
        public void UInt24Compare()
        {
            UInt24 numA = 10;
            UInt24 numB = 11;
            UInt24 numC = 10;
            Assert.True(numA < numB);
            Assert.True(numB > numA);
            Assert.True(numA.CompareTo(numC) == 0);
        }

        [Fact]
        public void UInt24Shift()
        {
            int n = 12345;
            n <<= 5;

            UInt24 c = 12345;
            c <<= 5;

            Assert.Equal(n.ToString(), c.ToString());

            int n1 = 12345;
            n1 >>= 5;

            UInt24 c1 = 12345;
            c1 >>= 5;

            Assert.Equal(n1.ToString(), c1.ToString());
        }

        [Fact]
        public void UInt24BitOr()
        {
            int n = 12345;
            n |= 5;

            UInt24 c = 12345;
            c |= 5;

            Assert.Equal(n.ToString(), c.ToString());
        }

        [Fact]
        public void UInt24BitAnd()
        {
            int n = 12345;
            n &= 5;

            UInt24 c = 12345;
            c &= 5;

            Assert.Equal(n.ToString(), c.ToString());
        }

    }

    */
}
