using Meadow.Core.AbiEncoding;
using Meadow.Core.AbiEncoding.Encoders;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Meadow.Core.EthTypes;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using Xunit.Abstractions;

namespace Meadow.Core.Test
{
    public class NonGenericAbiEncodingNumeric
    {
        public static IEnumerable<object[]> GetNumericDataUnsigned(dynamic num)
        {
            yield return new object[] { (byte)num };
            yield return new object[] { (ushort)num };
            yield return new object[] { (uint)num };
            yield return new object[] { (ulong)num };

            yield return new object[] { (float)num };
            yield return new object[] { (double)num };
            yield return new object[] { (decimal)num };

            yield return new object[] { (UInt256)num };
        }

        public static IEnumerable<object[]> GetNumericDataSigned(dynamic num)
        {
            yield return new object[] { (sbyte)num };
            yield return new object[] { (short)num };
            yield return new object[] { (int)num };
            yield return new object[] { (long)num };

            yield return new object[] { (float)num };
            yield return new object[] { (double)num };
            yield return new object[] { (decimal)num };

            yield return new object[] { (BigInteger)num };
        }

        static readonly string[] SolidityUnsignedTypes = { "uint8", "uint16", "uint24", "uint32", "uint40", "uint48", "uint56", "uint64", "uint72", "uint80", "uint88", "uint96", "uint104", "uint112", "uint120", "uint128", "uint136", "uint144", "uint152", "uint160", "uint168", "uint176", "uint184", "uint192", "uint200", "uint208", "uint216", "uint224", "uint232", "uint240", "uint248", "uint256" };
        static readonly string[] SoliditySignedTypes = { "int8", "int16", "int24", "int32", "int40", "int48", "int56", "int64", "int72", "int80", "int88", "int96", "int104", "int112", "int120", "int128", "int136", "int144", "int152", "int160", "int168", "int176", "int184", "int192", "int200", "int208", "int216", "int224", "int232", "int240", "int248", "int256" };

        private readonly ITestOutputHelper _output;

        public NonGenericAbiEncodingNumeric(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetNumericDataUnsigned), parameters: 123)]
        public void EncodeUnsigned(object num)
        {
            foreach (var solType in SolidityUnsignedTypes.Concat(SoliditySignedTypes))
            {
                TestConvert(solType, num);
            }
        }

        [Theory]
        [MemberData(nameof(GetNumericDataSigned), parameters: -23)]
        public void EncodeSigned(object num)
        {
            foreach (var solType in SoliditySignedTypes)
            {
                TestConvert(solType, num);
            }
        }

        void TestConvert(string solType, object num)
        {
            var result = SolidityUtil.AbiEncode((solType, num));
            var decoded = SolidityUtil.AbiDecode(solType, result);
            var expectedResult = ToInvariantString(num);
            var decodedResult = ToInvariantString(decoded);
            Assert.Equal(expectedResult, decodedResult);
            _output.WriteLine($"{solType}, {num.GetType()}, {expectedResult}, {decodedResult}");
        }

        public static string ToInvariantString(object obj)
        {
            return TypeDescriptor.GetConverter(typeof(string)).ConvertToInvariantString(obj);
        }
    }

}
