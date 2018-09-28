using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace Meadow.Core.Test
{
    public class HexConverterTests
    {

        [Fact]
        public void UInt8_1()
        {
            byte num = 123;
            var hex = "0x7b";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            byte numReturn = HexConverter.HexToInteger<byte>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int8_1()
        {
            sbyte num = -56;
            var hex = "0xc8";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            sbyte numReturn = HexConverter.HexToInteger<sbyte>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int32_1()
        {
            int num = -16098398;
            var hex = "0xff0a5ba2";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            int numReturn = HexConverter.HexToInteger<int>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int32_2()
        {
            int num = int.MaxValue;
            var hex = "0x7fffffff";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            int numReturn = HexConverter.HexToInteger<int>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int32_3()
        {
            int num = int.MinValue;
            var hex = "0x80000000";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            int numReturn = HexConverter.HexToInteger<int>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int32_4()
        {
            int num = 0;
            var hex = "0x";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            int numReturn = HexConverter.HexToInteger<int>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int32_5()
        {
            int num = -16098398;
            var hex = "0xff0a5ba2";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            var numReturn = HexConverter.HexToInteger<int>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void Int64_1()
        {
            long num = 4611686018427387904;
            var hex = "0x4000000000000000";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            long numReturn = HexConverter.HexToInteger<long>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void UInt64_1()
        {
            ulong num = 1844674407370955161;
            var hex = "0x1999999999999999";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            ulong numReturn = HexConverter.HexToInteger<ulong>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void UInt64_2()
        {
            ulong num = 12345;
            var hex = "0x3039";
            var toHex = HexConverter.GetHexFromInteger(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            ulong numReturn = HexConverter.HexToInteger<ulong>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void HexToIntegerGenerics()
        {
            uint actual = 127;
            uint parsed = HexConverter.HexToInteger<uint>("0x7f");
            Assert.Equal(actual, parsed);
        }

        [Fact]
        public void AddressGeneric()
        {
            var addrHex = "0xb60e8dd61c5d32be8058bb8eb970870f07233155";
            var addr = HexConverter.HexToValue<Address>(addrHex);
            var roundTrip = HexConverter.GetHex(addr, hexPrefix: true);
            Assert.Equal(addrHex, roundTrip);
        }

        [Fact]
        public void AddressDynamic()
        {
            var addrHex = "0xb60e8dd61c5d32be8058bb8eb970870f07233155";
            var addr = (Address)HexConverter.HexToObject(typeof(Address), addrHex);
            var roundTrip = HexConverter.GetHexFromObject(addr, hexPrefix: true);
            Assert.Equal(addrHex, roundTrip);
        }

        [Fact]
        public void UInt64Dynamic()
        {
            ulong num = 12345;
            var hex = "0x3039";
            var toHex = HexConverter.GetHexFromObject(num, hexPrefix: true);
            Assert.Equal(hex, toHex);
            ulong numReturn = HexConverter.HexToInteger<ulong>(toHex);
            Assert.Equal(num, numReturn);
        }

        [Fact]
        public void UInt256()
        { 
            UInt256 num = 1234565;
            var hex = HexConverter.GetHexFromInteger(num);
            Assert.Equal("12d685", hex);
            var roundTrip = HexConverter.HexToInteger<UInt256>(hex);
            Assert.Equal(num, roundTrip);
        }

        [Fact]
        public void Hash()
        {
            Hash hash = "0xb903239f8543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
            var hex = HexConverter.GetHex<Hash>(hash, hexPrefix: true);
            var roundTrip = HexConverter.HexToValue<Hash>(hex);
            Assert.Equal(hash.ToString(), roundTrip);
        }

        [Fact]
        public void HexConverterStringUndersized()
        {
            Hash hash = "0x000000008543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
            var hex = "0x8543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
            var roundTrip = HexConverter.HexToValue<Hash>(hex);
            Assert.Equal(hash.ToString(), roundTrip);
        }

        [Fact]
        public void HexConverterStringOversized()
        {
            Hash hash = "0xb903239f8543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
            var hex = "0x000000b903239f8543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
            var roundTrip = HexConverter.HexToValue<Hash>(hex);
            Assert.Equal(hash.ToString(), roundTrip);
        }
    }
}
