using Meadow.Core.AbiEncoding;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.Contract.Test
{
   public class EthFuncTest
    {
        [Fact]
        public void FunctionDataDecode_MixedParamTypes()
        {
            var encoded = "0x00000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000100ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffd4920000000000000000000000000000000000000000000000000000000000000140000000000000000000000000000000000000000000000000000000000000006300000000000000000000000000000000000000000000000000000000000000090000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffffffffffffffff00000000000000000000000000000000000000000000000000000000000000096d7920737472696e670000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000300000000000000000000000098e4625b2d7424c403b46366150ab28df406340800000000000000000000000040515114eea1497d753659dff85910f838c6b234000000000000000000000000df0270a6bff43e7a3fd92372dfb549292d683d22";
            var ethFunc = EthFunc.Create<bool, string, long, Address[], byte, ulong[]>(
                null, null,
                "bool", DecoderFactory.Decode,
                "string", DecoderFactory.Decode,
                "int56", DecoderFactory.Decode,
                "address[]", DecoderFactory.GetArrayDecoder(EncoderFactory.LoadEncoder("address", default(Address))),
                "uint8", DecoderFactory.Decode,
                "uint64[3]", DecoderFactory.GetArrayDecoder(EncoderFactory.LoadEncoder("uint64", default(ulong))));

            var p1 = true;
            var p2 = "my string";
            var p3 = (long)-11118;
            var p4 = new Address[] { "0x98E4625b2d7424C403B46366150AB28Df4063408", "0x40515114eEa1497D753659DFF85910F838c6B234", "0xDf0270A6BFf43e7A3Fd92372DfB549292D683D22" };
            var p5 = (byte)99;
            var p6 = new ulong[] { 9, 0, ulong.MaxValue };

            var (r1, r2, r3, r4, r5, r6) = ethFunc.ParseReturnData(encoded.HexToBytes());

            Assert.Equal(p1, r1);
            Assert.Equal(p2, r2);
            Assert.Equal(p3, r3);
            Assert.Equal(p4, r4);
            Assert.Equal(p5, r5);
            Assert.Equal(p6, r6);
        }


        [Fact]
        public void FunctionDataEncode_MultipleStringParams()
        {
            var funcSig = "echoMultipleDynamic(string,string,string)";
            var strP1 = "first string";
            var strP2 = "asdf";
            var strP3 = "utf8; 4 bytes: 𠾴; 3 bytes: ⏰ works!";
            var callData = EncoderUtil.GetFunctionCallBytes(
                funcSig,
                EncoderFactory.LoadEncoder("string", strP1),
                EncoderFactory.LoadEncoder("string", strP2),
                EncoderFactory.LoadEncoder("string", strP3));
            var expectedEncode = "0x14d6b8fa000000000000000000000000000000000000000000000000000000000000006000000000000000000000000000000000000000000000000000000000000000a000000000000000000000000000000000000000000000000000000000000000e0000000000000000000000000000000000000000000000000000000000000000c666972737420737472696e670000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000461736466000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000028757466383b20342062797465733a20f0a0beb43b20332062797465733a20e28fb020776f726b7321000000000000000000000000000000000000000000000000";
            Assert.Equal(expectedEncode, callData.ToHexString(hexPrefix: true));
        }

        [Fact]
        public void FunctionDataEncode_MixedParamTypes()
        {
            var p1 = true;
            var p2 = "my string";
            var p3 = (long)-11118;
            var p4 = new Address[] { "0x98E4625b2d7424C403B46366150AB28Df4063408", "0x40515114eEa1497D753659DFF85910F838c6B234", "0xDf0270A6BFf43e7A3Fd92372DfB549292D683D22" };
            var p5 = (byte)99;
            var p6 = new ulong[] { 9, 0, ulong.MaxValue };
            var callData = EncoderUtil.GetFunctionCallBytes(
                "boat(bool,string,int56,address[],uint8,uint64[3])",
                EncoderFactory.LoadEncoder("bool", p1),
                EncoderFactory.LoadEncoder("string", p2),
                EncoderFactory.LoadEncoder("int56", p3),
                EncoderFactory.LoadEncoder("address[]", p4, EncoderFactory.LoadEncoder("address", default(Address))),
                EncoderFactory.LoadEncoder("uint8", p5),
                EncoderFactory.LoadEncoder("uint64[3]", p6, EncoderFactory.LoadEncoder("uint64", default(ulong))));

            var expected = "0x7a4a328f00000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000100ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffd4920000000000000000000000000000000000000000000000000000000000000140000000000000000000000000000000000000000000000000000000000000006300000000000000000000000000000000000000000000000000000000000000090000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffffffffffffffff00000000000000000000000000000000000000000000000000000000000000096d7920737472696e670000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000300000000000000000000000098e4625b2d7424c403b46366150ab28df406340800000000000000000000000040515114eea1497d753659dff85910f838c6b234000000000000000000000000df0270a6bff43e7a3fd92372dfb549292d683d22";
            var result = callData.ToHexString(hexPrefix: true);
            Assert.Equal(expected, result);
        }


    }
}
