using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class UIntTests
    {
        [Fact]
        public void UInt32Cast()
        {
            uint num = 2147483640;
            UInt256 bigNum = num;
            var same = bigNum == num;
            Assert.True(same);
            Assert.Equal(num, (uint)bigNum);
        }

        [Fact]
        public void UInt64Cast()
        {
            ulong num = UInt64.MaxValue;
            UInt256 bigNum = num;
            var same = bigNum == num;
            Assert.True(same);
            Assert.Equal(num, (ulong)bigNum);
        }

        [Fact]
        public void ByteCast()
        {
            byte num = 0xF5;
            UInt256 bigNum = num;
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
            UInt256 result = num;
            Assert.Equal("10000000000000000000", result.ToString());
        }

        [Fact]
        public void FloatingZero()
        {
            float num = 0;
            UInt256 result = num;
            Assert.Equal("0", result.ToString());
        }

        [Fact]
        public void FloatingOversized()
        { 
            double num = 2e257;
            UInt256 result;
            Assert.Throws<OverflowException>(() => result = num);
        }

        [Fact]
        public void FloatingSmall()
        {
            float num = 34;
            UInt256 result = num;
            Assert.Equal("34", result.ToString());
        }

        [Fact]
        public void FloatingNaN()
        {
            float num = float.NaN;
            UInt256 result;
            Assert.Throws<NotFiniteNumberException>(() => result = num);
        }

        [Fact]
        public void FloatingInfinity()
        {
            float num = float.PositiveInfinity;
            UInt256 result;
            Assert.Throws<NotFiniteNumberException>(() => result = num);
        }

        [Fact]
        public void FloatingNegInfinity()
        {
            float num = float.NegativeInfinity;
            UInt256 result;
            Assert.Throws<NotFiniteNumberException>(() => result = num);
        }

        [Fact]
        public void FloatingFractional()
        {
            float num = 3.4f;
            UInt256 result;
            Assert.Throws<ArithmeticException>(() => result = num);
        }

        [Fact]
        public void FloatingNegative()
        {
            float num = -3;
            UInt256 result;
            Assert.Throws<OverflowException>(() => result = num);
        }

        [Fact]
        public void DecimalNegative()
        {
            decimal num = -1;
            UInt256 result;
            Assert.Throws<OverflowException>(() => result = num);
        }

        [Fact]
        public void DecimalFractional()
        {
            decimal num = 1.0000001M;
            UInt256 result;
            Assert.Throws<ArithmeticException>(() => result = num);
        }

        [Fact]
        public void DecimalMaxValue()
        {
            decimal num = decimal.MaxValue;
            UInt256 result = num;
            Assert.Equal("79228162514264337593543950335", result.ToString());
        }

        [Fact]
        public void DecimalValid()
        {
            decimal num = 10e18M;
            UInt256 result = num;
            Assert.Equal("10000000000000000000", result.ToString());
        }

        [Fact]
        public void DecimalZero()
        {
            decimal num = 0;
            UInt256 result = num;
            Assert.Equal("0", result.ToString());
        }

        [Fact]
        public void UInt256HexConversions()
        {
            UInt256 num = 123456789;
            var hex = num.ToHexString();
            UInt256 roundTrip = UInt256.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = 0;
            hex = num.ToHexString();
            roundTrip = UInt256.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = UInt256.MaxValue;
            hex = num.ToHexString();
            roundTrip = UInt256.FromHexString(hex);
            Assert.Equal(num, roundTrip);

            num = 486854645767;
            hex = num.ToHexString(hexPrefix: false);
            roundTrip = UInt256.FromHexString(hex);
            Assert.Equal(num, roundTrip);
        }

        [Theory]
        [InlineData("0x0", "0")]
        [InlineData("0", "0")]
        [InlineData("0X0", "0")]
        [InlineData("0xf", "15")]
        [InlineData("0xff", "255")]
        [InlineData("0x8685ab734df053d736164f5c07e22335ba7dda29", "767985697838764003447443487293009458167325514281")]
        [InlineData("0xcded53d631ce4a38a1f90d59e5f2f9c023cd28c64aa66488e9462cc4a64a032f", "93143455333542622135039847066834837440973860438506650885173108183573526610735")]
        [InlineData("c11bae21c952aeb885d801a230a16587063b3265a25634168f9fdefeccd002e7", "87345286462063795141313778258310232399613318052032024130005230620692974666471")]
        [InlineData("6da13a032165564a050786978b9aaa", "569230421728738090059415783358569130")]
        public void UInt256FromHexString(string hexString, string expectedResult)
        {
            var num = UInt256.FromHexString(hexString);
            var numString = num.ToString();
            Assert.Equal(expectedResult, numString);

            // test loose casting
            var num2 = (UInt256)hexString;
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
        [InlineData("93143455333542622135039847066834837440973860438506650885173108183573526610735", "93143455333542622135039847066834837440973860438506650885173108183573526610735")]
        [InlineData("87345286462063795141313778258310232399613318052032024130005230620692974666471", "87345286462063795141313778258310232399613318052032024130005230620692974666471")]
        [InlineData("569230421728738090059415783358569130", "569230421728738090059415783358569130")]
        public void UInt256FromIntegerString(string integerString, string expectedResult)
        {
            var num = UInt256.FromString(integerString);
            var numString = num.ToString();
            Assert.Equal(expectedResult, numString);

            // test loose casting
            var num2 = (UInt256)integerString;
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
        public void UInt256FromIntegerStringInvalid(string integerString)
        {
            Assert.Throws<ArgumentException>(() => UInt256.FromString(integerString));
        }
    }
}
