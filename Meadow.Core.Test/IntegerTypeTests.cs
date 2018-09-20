using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class IntegerTypeTests
    {
        [Fact]
        public void CastUpDownIntegerSizes()
        {
            // Scale down an unsigned integer.
            UInt40 u40 = UInt40.MaxValue;
            UInt24 u24 = (UInt24)u40;
            Assert.Equal(UInt24.MaxValue, u24);

            // Scale up an unsigned integer, should retain value.
            u40 = u24;
            Assert.Equal(UInt24.MaxValue, u40);

            // Scale down a signed integer (should cause scaled down to be negative as sign bit is set).
            Int32 i32 = 0x00800000; // positive (sign bit is not set)
            Int24 i24 = (Int24)i32; // negative (sign bit is now set)
            Assert.Equal(Int24.MinValue, i24);

            // Scale up a signed integer, should retain value (not go back to positive).
            i32 = i24;
            Assert.Equal((int)Int24.MinValue, i32);

            // Scale down max integer (unsigned)
            UInt248 u248 = UInt248.MaxValue;
            UInt72 u72 = (UInt72)u248;
            Assert.Equal(UInt72.MaxValue, u72);
            UInt64 u64 = (UInt64)u72;
            Assert.Equal(UInt64.MaxValue, u64);
            UInt56 u56 = (UInt56)u72;
            Assert.Equal(UInt56.MaxValue, u56);
            UInt32 u32 = (UInt32)u56;
            Assert.Equal(UInt32.MaxValue, u32);
            UInt16 u16 = (UInt16)u56;
            Assert.Equal(UInt16.MaxValue, u16);
            byte u8 = (byte)u56;
            Assert.Equal(byte.MaxValue, u8);

            // Scale down max integer (signed)
            Int248 i248 = -1;
            Int72 i72 = (Int72)i248;
            Assert.Equal(-1, i72);
            Int64 i64 = (Int64)i72;
            Assert.Equal(-1, i64);
            Int56 i56 = (Int56)i72;
            Assert.Equal(-1, i56);
            i32 = (Int32)i56;
            Assert.Equal(-1, i32);
            Int16 i16 = (Int16)i56;
            Assert.Equal(-1, i16);
            sbyte i8 = (sbyte)i56;
            Assert.Equal(-1, i8);
        }

        [Fact]
        public void CastDifferentTypeIntegers()
        {
            // Take an unsigned integer that represents a negative signed integer
            UInt40 u40 = (UInt40)0x8000000000;
            UInt48 u48 = u40;
            Int40 i40 = (Int40)u40;
            Int48 i48 = (Int48)u48;

            // Assert our values
            Assert.Equal(0x8000000000UL, u40);
            Assert.Equal(0x8000000000UL, u48);
            Assert.Equal(-549755813888, i40);
            Assert.Equal(0x8000000000, i48);
            Assert.Equal(u40, i48);
            Assert.Equal<Int64>(u48, i48);

            // Verify our unsigned and signed values equal with our operator.
            Assert.True(u48 == i48);

            // Define a signed + unsigned type and upscale to assert equality.
            UInt80 u80 = (UInt80)0x8000000000;
            Int80 i80 = (Int80)u80;
            Assert.Equal<Int88>(u80, i80);
        }

        [Fact]
        public void CastSameTypeIntegers()
        {
            // Scale down an unsigned integer.
            UInt40 u40 = UInt40.MaxValue;
            UInt24 u24 = (UInt24)u40;
            Assert.Equal(UInt24.MaxValue, u24);

            // Scale up an unsigned integer, should retain value.
            u40 = u24;
            Assert.Equal(UInt24.MaxValue, u40);

            // Scale down a signed integer (should cause scaled down to be negative as sign bit is set).
            Int32 i32 = 0x00800000; // positive (sign bit is not set)
            Int24 i24 = (Int24)i32; // negative (sign bit is now set)
            Assert.Equal(Int24.MinValue, i24);

            // Scale up a signed integer, should retain value (not go back to positive).
            i32 = i24;
            Assert.Equal((int)Int24.MinValue, i32);
        }

        [Fact]
        public void CastFloat()
        {
            float num = 34;
            UInt248 result = (UInt248)num;
            Assert.Equal("34", result.ToString());

            Int248 result2 = (Int248)num;
            Assert.Equal("34", result2.ToString());

            num = (float)result;
            Assert.Equal("34", result.ToString());
        }

        [Fact]
        public void CastIntegersImplicitly()
        {
            UInt248 u248 = 0;
            Int248 i248 = 0;

            // Test literals of all sizes.
            u248 = 0x7f;
            i248 = 0x7f;
            u248 = 0x7fff;
            i248 = 0x7fff;
            u248 = 0x7fffffff;
            i248 = 0x7fffffff;
            u248 = 0x7fffffffffffffff;
            i248 = 0x7fffffffffffffff;

            i248 = -128;
            i248 = -32768;
            i248 = -2147483648;
            i248 = -9223372036854775808;

           
            // These should all fail.
            //u248 = -128;
            //u248 = -32768;
            //u248 = -2147483648;
            //u248 = -9223372036854775808;

            // Test implicit casting
            u248 = byte.MaxValue;
            i248 = byte.MaxValue;
            u248 = ushort.MaxValue;
            i248 = ushort.MaxValue;
            u248 = uint.MaxValue;
            i248 = uint.MaxValue;
            u248 = ulong.MaxValue;
            i248 = ulong.MaxValue;

            //u248 = sbyte.MaxValue; // This should fail
            i248 = sbyte.MaxValue;
            //u248 = short.MaxValue; // This should fail
            i248 = short.MaxValue;
            u248 = int.MaxValue;
            i248 = int.MaxValue;
            u248 = long.MaxValue;
            i248 = long.MaxValue;

            // These should fail
            //i248 = u248;
            //u248 = i248;

            Int240 i240 = 0;
            UInt240 u240 = 0;

            // These should fail
            //i240 = i248;
            //i240 = u248;
            //u240 = i248;
            //u240 = u248;

            // Upscale integer sizes.
            i248 = i240;
            i248 = u240;
            //u248 = i240; // This should fail
            u248 = u240;

            Int24 i24 = 0x7fffff;
            UInt24 u24 = 0;
            Int56 i56 = 0;
            UInt56 u56 = 0;
            Int40 i40 = 0;
            UInt40 u40 = 0;
        }

        [Fact]
        public void ComparisonOperators()
        {
            // We test comparison operators on differing sized integers, left hand side (LHS) and RHS.

            // Unsigned
            UInt248 u248 = 0;
            UInt240 u240 = 0;
            if (u248 < u240) { }
            if (u248 <= u240) { }
            if (u248 > u240) { }
            if (u248 >= u240) { }
            if (u248 == u240) { }
            if (u248 != u240) { }

            if (u240 < u248) { }
            if (u240 <= u248) { }
            if (u240 > u248) { }
            if (u240 >= u248) { }
            if (u240 == u248) { }
            if (u240 != u248) { }

            // Signed
            Int248 i248 = 0;
            Int240 i240 = 0;
            if (i248 < i240) { }
            if (i248 <= i240) { }
            if (i248 > i240) { }
            if (i248 >= i240) { }
            if (i248 == i240) { }
            if (i248 != i240) { }

            if (i240 < i248) { }
            if (i240 <= i248) { }
            if (i240 > i248) { }
            if (i240 >= i248) { }
            if (i240 == i248) { }
            if (i240 != i248) { }

            // Unsigned/Signed (signed being bigger)
            if (i248 < u240) { }
            if (i248 <= u240) { }
            if (i248 > u240) { }
            if (i248 >= u240) { }
            if (i248 == u240) { }
            if (i248 != u240) { }

            if (u240 < i248) { }
            if (u240 <= i248) { }
            if (u240 > i248) { }
            if (u240 >= i248) { }
            if (u240 == i248) { }
            if (u240 != i248) { }

            // Unsigned/signed (signed being smaller) (these should all fail)
            //if (u248 < i240) { }
            //if (u248 <= i240) { }
            //if (u248 > i240) { }
            //if (u248 >= i240) { }
            //if (u248 == i240) { }
            //if (u248 != i240) { }

            //if (i240 < u248) { }
            //if (i240 <= u248) { }
            //if (i240 > u248) { }
            //if (i240 >= u248) { }
            //if (i240 == u248) { }
            //if (i240 != u248) { }

            UInt232 x = 0x7fffffff;
            Assert.Equal<UInt240>(0x7fffffff, x);
        }

        [Fact(Skip ="TODO: Add Int256/UInt256 after switching to new integer class.")]
        public void SizeOfTests()
        {
            // Test signed types.

            // <byte is internal MS object>
            // <Int16 is internal MS object>
            Assert.Equal(3, Marshal.SizeOf((Int24)0));
            // <Int32 is internal MS object>
            Assert.Equal(5, Marshal.SizeOf((Int40)0));
            Assert.Equal(6, Marshal.SizeOf((Int48)0));
            Assert.Equal(7, Marshal.SizeOf((Int56)0));
            // <Int64 is internal MS object>
            Assert.Equal(9, Marshal.SizeOf((Int72)0));
            Assert.Equal(10, Marshal.SizeOf((Int80)0));
            Assert.Equal(11, Marshal.SizeOf((Int88)0));
            Assert.Equal(12, Marshal.SizeOf((Int96)0));
            Assert.Equal(13, Marshal.SizeOf((Int104)0));
            Assert.Equal(14, Marshal.SizeOf((Int112)0));
            Assert.Equal(15, Marshal.SizeOf((Int120)0));
            Assert.Equal(16, Marshal.SizeOf((Int128)0));
            Assert.Equal(17, Marshal.SizeOf((Int136)0));
            Assert.Equal(18, Marshal.SizeOf((Int144)0));
            Assert.Equal(19, Marshal.SizeOf((Int152)0));
            Assert.Equal(20, Marshal.SizeOf((Int160)0));
            Assert.Equal(21, Marshal.SizeOf((Int168)0));
            Assert.Equal(22, Marshal.SizeOf((Int176)0));
            Assert.Equal(23, Marshal.SizeOf((Int184)0));
            Assert.Equal(24, Marshal.SizeOf((Int192)0));
            Assert.Equal(25, Marshal.SizeOf((Int200)0));
            Assert.Equal(26, Marshal.SizeOf((Int208)0));
            Assert.Equal(27, Marshal.SizeOf((Int216)0));
            Assert.Equal(28, Marshal.SizeOf((Int224)0));
            Assert.Equal(29, Marshal.SizeOf((Int232)0));
            Assert.Equal(30, Marshal.SizeOf((Int240)0));
            Assert.Equal(31, Marshal.SizeOf((Int248)0));
            // TODO: UNCOMMENT THIS AFTER WE REMOVE OLD UINT256 CODE AND GENERATE 256-BIT INTEGERS:
            // Assert.Equal(32, Marshal.SizeOf((Int256)0));

            // Test unsigned types.

            // <byte is internal MS object>
            // <UInt16 is internal MS object>
            Assert.Equal(3, Marshal.SizeOf((UInt24)0));
            // <UInt32 is internal MS object>
            Assert.Equal(5, Marshal.SizeOf((UInt40)0));
            Assert.Equal(6, Marshal.SizeOf((UInt48)0));
            Assert.Equal(7, Marshal.SizeOf((UInt56)0));
            // <UInt64 is internal MS object>
            Assert.Equal(9, Marshal.SizeOf((UInt72)0));
            Assert.Equal(10, Marshal.SizeOf((UInt80)0));
            Assert.Equal(11, Marshal.SizeOf((UInt88)0));
            Assert.Equal(12, Marshal.SizeOf((UInt96)0));
            Assert.Equal(13, Marshal.SizeOf((UInt104)0));
            Assert.Equal(14, Marshal.SizeOf((UInt112)0));
            Assert.Equal(15, Marshal.SizeOf((UInt120)0));
            Assert.Equal(16, Marshal.SizeOf((UInt128)0));
            Assert.Equal(17, Marshal.SizeOf((UInt136)0));
            Assert.Equal(18, Marshal.SizeOf((UInt144)0));
            Assert.Equal(19, Marshal.SizeOf((UInt152)0));
            Assert.Equal(20, Marshal.SizeOf((UInt160)0));
            Assert.Equal(21, Marshal.SizeOf((UInt168)0));
            Assert.Equal(22, Marshal.SizeOf((UInt176)0));
            Assert.Equal(23, Marshal.SizeOf((UInt184)0));
            Assert.Equal(24, Marshal.SizeOf((UInt192)0));
            Assert.Equal(25, Marshal.SizeOf((UInt200)0));
            Assert.Equal(26, Marshal.SizeOf((UInt208)0));
            Assert.Equal(27, Marshal.SizeOf((UInt216)0));
            Assert.Equal(28, Marshal.SizeOf((UInt224)0));
            Assert.Equal(29, Marshal.SizeOf((UInt232)0));
            Assert.Equal(30, Marshal.SizeOf((UInt240)0));
            Assert.Equal(31, Marshal.SizeOf((UInt248)0));
            Assert.Equal(32, Marshal.SizeOf((UInt256)0));
        }

        [Fact]
        public void ComparativeTests()
        {
            // Same size (signed)
            Int24 i24 = 7;
            Int24 i24_2 = 8;
            Assert.True(i24 < i24_2);
            Assert.True(i24 <= i24_2);
            Assert.False(i24 > i24_2);
            Assert.False(i24 >= i24_2);
            Assert.False(i24 == i24_2);
            Assert.True(i24 != i24_2);

            // Same size (unsigned)
            UInt24 u24 = 7;
            UInt24 u24_2 = 8;
            Assert.True(u24 < u24_2);
            Assert.True(u24 <= u24_2);
            Assert.False(u24 > u24_2);
            Assert.False(u24 >= u24_2);
            Assert.False(u24 == u24_2);
            Assert.True(u24 != u24_2);

            // Different size, same type, compatible value (signed)
            Int72 i72 = 8;
            Assert.True(i24 < i72);
            Assert.True(i24 <= i72);
            Assert.False(i24 > i72);
            Assert.False(i24 >= i72);
            Assert.False(i24 == i72);
            Assert.True(i24 != i72);

            // Different size, same type, compatible value (unsigned)
            UInt72 u72 = 8;
            Assert.True(u24 < u72);
            Assert.True(u24 <= u72);
            Assert.False(u24 > u72);
            Assert.False(u24 >= u72);
            Assert.False(u24 == u72);
            Assert.True(u24 != u72);

            // Different size, same type, overflow value (signed)

            // For this test we simply want to verify data isn't truncated
            // and we are scaling upwards to do the proper comparison.
            i72 = 0x80000007;
            Assert.True(i24 < i72);
            Assert.True(i24 <= i72);
            Assert.False(i24 > i72);
            Assert.False(i24 >= i72);
            Assert.False(i24 == i72);
            Assert.True(i24 != i72);

            // Different size, same type, overflow value (unsigned)

            // For this test we simply want to verify data isn't truncated
            // and we are scaling upwards to do the proper comparison.
            u72 = 0xffffffff000007;
            Assert.True(u24 < u72);
            Assert.True(u24 <= u72);
            Assert.False(u24 > u72);
            Assert.False(u24 >= u72);
            Assert.False(u24 == u72);
            Assert.True(u24 != u72);

            // Mask our value to 24 bits (signed) (now it should equal i24)
            i72 = (Int24)i72;
            Assert.False(i24 < i72);
            Assert.True(i24 <= i72);
            Assert.False(i24 > i72);
            Assert.True(i24 >= i72);
            Assert.True(i24 == i72);
            Assert.False(i24 != i72);

            // Mask our value to 24 bits (unsigned) (now it should equal u24)
            u72 = (UInt24)u72;
            Assert.False(u24 < u72);
            Assert.True(u24 <= u72);
            Assert.False(u24 > u72);
            Assert.True(u24 >= u72);
            Assert.True(u24 == u72);
            Assert.False(u24 != u72);
        }

        [Fact]
        public void UInt64Cast()
        {
            ulong num = UInt64.MaxValue;
            UInt248 bigNum = num;
            var same = bigNum == num;
            Assert.True(same);
            Assert.Equal(num, (ulong)bigNum);
        }

        [Fact]
        public void ByteCast()
        {
            byte num = 0xF5;
            UInt248 bigNum = num;
            var same = bigNum == num;
            Assert.True(same);
            Assert.Equal(num, (byte)bigNum);
        }

        [Fact]
        public void WeiTest()
        {
            var weiInt = BigInteger.Pow(10, 18);
            var wei = EthUtil.ONE_ETHER_IN_WEI;
            Assert.Equal(weiInt.ToString(CultureInfo.InvariantCulture), wei.ToString());
            var exp = Math.Round(BigInteger.Log10((BigInteger)wei));
            Assert.Equal(18, exp);
        }

        [Fact]
        public void FloatingValid()
        {
            double num = 10e18;
            UInt248 result = (UInt248)num;
            Assert.Equal("10000000000000000000", result.ToString());
        }

        [Fact]
        public void FloatingZero()
        {
            float num = 0;
            UInt248 result = (UInt248)num;
            Assert.Equal("0", result.ToString());
        }

        [Fact]
        public void FloatingOversized()
        {
            double num = 2e257;
            UInt248 result;
            Assert.Throws<OverflowException>(() => result = (UInt248)num);
        }


        [Fact]
        public void FloatingNaN()
        {
            float num = float.NaN;
            UInt248 result;
            Assert.Throws<NotFiniteNumberException>(() => result = (UInt248)num);
        }

        [Fact]
        public void FloatingInfinity()
        {
            float num = float.PositiveInfinity;
            UInt248 result;
            Assert.Throws<NotFiniteNumberException>(() => result = (UInt248)num);
        }

        [Fact]
        public void FloatingNegInfinity()
        {
            float num = float.NegativeInfinity;
            UInt248 result;
            Assert.Throws<NotFiniteNumberException>(() => result = (UInt248)num);
        }

        [Fact]
        public void FloatingFractional()
        {
            float num = 3.4f;
            UInt248 result;
            Assert.Throws<ArithmeticException>(() => result = (UInt248)num);
        }

        [Fact]
        public void FloatingNegative()
        {
            float num = -3;
            UInt248 result;
            Assert.Throws<OverflowException>(() => result = (UInt248)num);
        }

        [Fact]
        public void DecimalNegative()
        {
            decimal num = -1;
            UInt248 result;
            Assert.Throws<OverflowException>(() => result = (UInt248)num);
        }

        [Fact]
        public void DecimalFractional()
        {
            decimal num = 1.0000001M;
            UInt248 result;
            Assert.Throws<ArithmeticException>(() => result = (UInt248)num);
        }

        [Fact]
        public void DecimalMaxValue()
        {
            decimal num = decimal.MaxValue;
            UInt248 result = (UInt248)num;
            Assert.Equal("79228162514264337593543950335", result.ToString());
        }

        [Fact]
        public void DecimalValid()
        {
            decimal num = 10e18M;
            UInt248 result = (UInt248)num;
            Assert.Equal("10000000000000000000", result.ToString());
        }

        [Fact]
        public void DecimalZero()
        {
            decimal num = 0;
            UInt248 result = (UInt248)num;
            Assert.Equal("0", result.ToString());
        }

        [Fact]
        public void UInt248HexConversions()
        {
            UInt248 num = 123456789;
            var hex = num.ToHexString();
            UInt248 roundTrip = UInt248.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = 0;
            hex = num.ToHexString();
            roundTrip = UInt248.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = UInt248.MaxValue;
            hex = num.ToHexString();
            roundTrip = UInt248.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = 486854645767;
            hex = num.ToHexString(hexPrefix: false);
            roundTrip = UInt248.FromHexString(hex);
            Assert.Equal(num, roundTrip);
        }

        [Theory]
        [InlineData("0x0", "0")]
        [InlineData("0", "0")]
        [InlineData("0X0", "0")]
        [InlineData("0xf", "15")]
        [InlineData("0xff", "255")]
        [InlineData("0x8685ab734df053d736164f5c07e22335ba7dda29", "767985697838764003447443487293009458167325514281")]
        [InlineData("6da13a032165564a050786978b9aaa", "569230421728738090059415783358569130")]
        public void UInt248FromHexString(string hexString, string expectedResult)
        {
            var num = UInt248.FromHexString(hexString);
            var numString = num.ToString();
            Assert.Equal(expectedResult, numString);

            // test loose casting
            var num2 = (UInt248)hexString;
            var numString2 = num2.ToString();
            Assert.Equal(expectedResult, numString2);
        }

        [Theory]
        [InlineData(" 23456", "23456")]
        [InlineData(" 23456 ", "23456")]
        [InlineData("23_456", "23456")]
        [InlineData("23 456", "23456")]
        [InlineData("23e5", "2300000")]
        [InlineData("23E5", "2300000")]
        [InlineData("23e+5", "2300000")]
        [InlineData("23E+5", "2300000")]
        [InlineData("2.3e5", "230000")]
        [InlineData("2.3E5", "230000")]
        [InlineData("2.3e+5", "230000")]
        [InlineData("2.3E+5", "230000")]
        [InlineData("4.56e+42", "4560000000000000000000000000000000000000000")]
        [InlineData("456,767,878", "456767878")]
        [InlineData("4560000000000000000000000000000000000000000", "4560000000000000000000000000000000000000000")]
        [InlineData("767985697838764003447443487293009458167325514281", "767985697838764003447443487293009458167325514281")]
        [InlineData("569230421728738090059415783358569130", "569230421728738090059415783358569130")]
        public void UInt248FromIntegerString(string integerString, string expectedResult)
        {
            var num = UInt248.FromString(integerString);
            var numString = num.ToString();
            Assert.Equal(expectedResult, numString);

            // test loose casting
            var num2 = (UInt248)integerString;
            var numString2 = num2.ToString();
            Assert.Equal(expectedResult, numString2);
        }

        [Theory]
        [InlineData("2345.6")]
        [InlineData("-23456")]
        [InlineData("0x23456")]
        [InlineData("0X23456")]
        [InlineData("2.3EE+5")]
        [InlineData("2ee5")]
        [InlineData("-23E+5")]
        [InlineData("23E-5")]
        [InlineData("80520320241300052306206929746664718734528646206379514131377825831023239961331000523062069297466647187345286462063")]
        public void UInt248FromIntegerStringInvalid(string integerString)
        {
            Assert.Throws<ArgumentException>(() => UInt248.FromString(integerString));
        }
    }
}
