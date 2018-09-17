using Meadow.Core.AbiEncoding;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class AbiPacked
    {
        [Theory]
        [InlineData(SolidityType.UInt256, 12345, "0x0000000000000000000000000000000000000000000000000000000000003039")]
        [InlineData(SolidityType.UInt256, "43e11", "0x000000000000000000000000000000000000000000000000000003e92bf8f800")]
        [InlineData(SolidityType.UInt256, "0x43e11", "0x0000000000000000000000000000000000000000000000000000000000043e11")]
        [InlineData(SolidityType.UInt256, "12345", "0x0000000000000000000000000000000000000000000000000000000000003039")]
        [InlineData(SolidityType.UInt8, 253, "0xfd")]
        [InlineData(SolidityType.UInt24, 12345, "0x003039")]
        [InlineData(SolidityType.UInt32, 12345, "0x00003039")]
        [InlineData(SolidityType.UInt64, 253, "0x00000000000000fd")]
        [InlineData(SolidityType.UInt64, "0x43e11", "0x0000000000043e11")]
        [InlineData(SolidityType.UInt72, 253, "0x0000000000000000fd")]
        public void Int_Unsigned(SolidityType solidityType, object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((solidityType, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(SolidityType.Int256, -123456, "0xfffffffffffffffffffffffffffffffffffffffffffffffffffffffffffe1dc0")]
        [InlineData(SolidityType.Int256, "0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
        [InlineData(SolidityType.Int256, 123456, "0x000000000000000000000000000000000000000000000000000000000001e240")]
        [InlineData(SolidityType.Int240, -123456, "0xfffffffffffffffffffffffffffffffffffffffffffffffffffffffe1dc0")]
        [InlineData(SolidityType.Int240, 123456, "0x00000000000000000000000000000000000000000000000000000001e240")]
        [InlineData(SolidityType.Int256, 12345, "0x0000000000000000000000000000000000000000000000000000000000003039")]
        [InlineData(SolidityType.Int256, "43e11", "0x000000000000000000000000000000000000000000000000000003e92bf8f800")]
        [InlineData(SolidityType.Int256, "0x43e11", "0x0000000000000000000000000000000000000000000000000000000000043e11")]
        [InlineData(SolidityType.Int64, "0x43e11", "0x0000000000043e11")]
        [InlineData(SolidityType.Int256, "12345", "0x0000000000000000000000000000000000000000000000000000000000003039")]
        [InlineData(SolidityType.Int8, 253, "0xfd")]
        [InlineData(SolidityType.Int24, 12345, "0x003039")]
        [InlineData(SolidityType.Int32, 12345, "0x00003039")]
        [InlineData(SolidityType.Int64, 253, "0x00000000000000fd")]
        [InlineData(SolidityType.Int72, 253, "0x0000000000000000fd")]
        [InlineData(SolidityType.Int72, -253, "0xffffffffffffffff03")]
        [InlineData(SolidityType.Int72, "2361183241434822606847", "0x7fffffffffffffffff")]
        [InlineData(SolidityType.Int16, -4253, "0xef63")]
        [InlineData(SolidityType.Int24, -44253, "0xff5323")]
        [InlineData(SolidityType.Int32, "0x80000000", "0x80000000")]
        public void Int_Signed(SolidityType solidityType, object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((solidityType, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }


        [Theory]
        [InlineData(true, "0x01")]
        [InlineData(false, "0x00")]
        [InlineData("1", "0x01")]
        [InlineData("0", "0x00")]
        [InlineData(1, "0x01")]
        [InlineData(0, "0x00")]
        [InlineData((byte)1, "0x01")]
        [InlineData((byte)0, "0x00")]
        [InlineData((ulong)1, "0x01")]
        [InlineData((ulong)0, "0x00")]
        public void Bool(object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((SolidityType.Bool, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31367", "0x3c0c50f684c03e40bb5055b6e39651d11ed31367")]
        [InlineData("3c0c50f684c03e40bb5055b6e39651d11ed31367", "0x3c0c50f684c03e40bb5055b6e39651d11ed31367")]
        [InlineData("0", "0x0000000000000000000000000000000000000000")]
        [InlineData("0x0", "0x0000000000000000000000000000000000000000")]
        [InlineData("0x00", "0x0000000000000000000000000000000000000000")]
        public void Address(object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((SolidityType.Address, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("The quick brown fox jumps over the lazy dog", "0x54686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f67")]
        [InlineData("The quick brown fox jumps over the lazy dog.", "0x54686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f672e")]
        [InlineData("", "0x")]
        [InlineData(" ", "0x20")]
        [InlineData("中文", "0xe4b8ade69687")]
        [InlineData("aécio", "0x61c3a963696f")]
        [InlineData("𠜎", "0xf0a09c8e")]
        [InlineData("訊息摘要演算法第五版（英語：Message-Digest Algorithm 5，縮寫為MD5），是當前電腦領域用於確保資訊傳輸完整一致而廣泛使用的雜湊演算法之一", "0xe8a88ae681afe69198e8a681e6bc94e7ae97e6b395e7acace4ba94e78988efbc88e88bb1e8aa9eefbc9a4d6573736167652d44696765737420416c676f726974686d2035efbc8ce7b8aee5afabe782ba4d4435efbc89efbc8ce698afe795b6e5898de99bbbe885a6e9a098e59f9fe794a8e696bce7a2bae4bf9de8b387e8a88ae582b3e8bcb8e5ae8ce695b4e4b880e887b4e8808ce5bba3e6b39be4bdbfe794a8e79a84e99b9ce6b98ae6bc94e7ae97e6b395e4b98be4b880")]
        [InlineData("訊息摘要演算法第五版（英語：Message-Digest Algorithm 5，縮寫為MD5），是當前電腦領域用於確保資訊傳輸完整一致而廣泛使用的雜湊演算法之一（又譯雜湊演算法、摘要演算法等），主流程式語言普遍已有MD5的實作。", "0xe8a88ae681afe69198e8a681e6bc94e7ae97e6b395e7acace4ba94e78988efbc88e88bb1e8aa9eefbc9a4d6573736167652d44696765737420416c676f726974686d2035efbc8ce7b8aee5afabe782ba4d4435efbc89efbc8ce698afe795b6e5898de99bbbe885a6e9a098e59f9fe794a8e696bce7a2bae4bf9de8b387e8a88ae582b3e8bcb8e5ae8ce695b4e4b880e887b4e8808ce5bba3e6b39be4bdbfe794a8e79a84e99b9ce6b98ae6bc94e7ae97e6b395e4b98be4b880efbc88e58f88e8adafe99b9ce6b98ae6bc94e7ae97e6b395e38081e69198e8a681e6bc94e7ae97e6b395e7ad89efbc89efbc8ce4b8bbe6b581e7a88be5bc8fe8aa9ee8a880e699aee9818de5b7b2e69c894d4435e79a84e5afa6e4bd9ce38082")]
        [InlineData("𠜎𠜱", "0xf0a09c8ef0a09cb1")]
        [InlineData("𠾴𠾼𠿪", "0xf0a0beb4f0a0bebcf0a0bfaa")]
        public void String(object input, string expectedResult)
        {
            var result1 = SolidityUtil.AbiPack((SolidityType.String, input));
            var result2 = SolidityUtil.AbiPack(("string", input));
            Assert.Equal(expectedResult, result1.ToHexString(hexPrefix: true));
            Assert.Equal(expectedResult, result2.ToHexString(hexPrefix: true));
        }

        [Theory]
        [InlineData((ulong)7, "0x07")]
        [InlineData(0x2345576543733, "0x02345576543733")]
        [InlineData(0x76543733, "0x76543733")]
        [InlineData(0x6543733, "0x06543733")]
        [InlineData(0x3733, "0x3733")]
        [InlineData("0x07", "0x07")]
        [InlineData("0xffff", "0xffff")]
        [InlineData("0xffffff", "0xffffff")]
        [InlineData("0x0000ff", "0x0000ff")]
        [InlineData("0x3c0c50f6", "0x3c0c50f6")]
        [InlineData("0x3c0c50f684", "0x3c0c50f684")]
        [InlineData("0x3c0c50f684c0", "0x3c0c50f684c0")]
        [InlineData("0x3c0c50f684c03e", "0x3c0c50f684c03e")]
        [InlineData("0x3c0c50f684c03e40", "0x3c0c50f684c03e40")]
        [InlineData("0x3c0c50f684c03e40bb", "0x3c0c50f684c03e40bb")]
        [InlineData("0x3c0c50f684c03e40bb50", "0x3c0c50f684c03e40bb50")]
        [InlineData("0x3c0c50f684c03e40bb5055", "0x3c0c50f684c03e40bb5055")]
        [InlineData("0x3c0c50f684c03e40bb5055b6", "0x3c0c50f684c03e40bb5055b6")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e3", "0x3c0c50f684c03e40bb5055b6e3")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e396", "0x3c0c50f684c03e40bb5055b6e396")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651", "0x3c0c50f684c03e40bb5055b6e39651")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d1", "0x3c0c50f684c03e40bb5055b6e39651d1")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11e", "0x3c0c50f684c03e40bb5055b6e39651d11e")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed3", "0x3c0c50f684c03e40bb5055b6e39651d11ed3")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed313", "0x3c0c50f684c03e40bb5055b6e39651d11ed313")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c0", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c0")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb50", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb50")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e3", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e3")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e396", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e396")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d1", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d1")]
        [InlineData("0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d11e", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d11e")]
        public void BytesArray(object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((SolidityType.Bytes, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(SolidityType.Bytes7, 0x2345576543733, "0x02345576543733")]
        [InlineData(SolidityType.Bytes4, 0x76543733, "0x76543733")]
        [InlineData(SolidityType.Bytes4, 0x6543733, "0x06543733")]
        [InlineData(SolidityType.Bytes8, 0x6543733, "0x0654373300000000")]
        [InlineData(SolidityType.Bytes2, 0x3733, "0x3733")]
        [InlineData(SolidityType.Bytes1, "0x07", "0x07")]
        [InlineData(SolidityType.Bytes1, "0x7", "0x07")]
        [InlineData(SolidityType.Bytes1, "7", "0x07")]
        [InlineData(SolidityType.Bytes1, "0", "0x00")]
        [InlineData(SolidityType.Bytes1, 7, "0x07")]
        [InlineData(SolidityType.Bytes1, (byte)7, "0x07")]
        [InlineData(SolidityType.Bytes1, (uint)7, "0x07")]
        [InlineData(SolidityType.Bytes1, (short)7, "0x07")]
        [InlineData(SolidityType.Bytes1, (ushort)7, "0x07")]
        [InlineData(SolidityType.Bytes1, (long)7, "0x07")]
        [InlineData(SolidityType.Bytes1, (ulong)7, "0x07")]
        [InlineData(SolidityType.Bytes2, "0x07", "0x0700")]
        [InlineData(SolidityType.Bytes2, "0xffff", "0xffff")]
        [InlineData(SolidityType.Bytes3, "0xffffff", "0xffffff")]
        [InlineData(SolidityType.Bytes3, "0x0000ff", "0x0000ff")]
        [InlineData(SolidityType.Bytes4, "0x3c0c50f6", "0x3c0c50f6")]
        [InlineData(SolidityType.Bytes5, "0x3c0c50f684", "0x3c0c50f684")]
        [InlineData(SolidityType.Bytes6, "0x3c0c50f684c0", "0x3c0c50f684c0")]
        [InlineData(SolidityType.Bytes7, "0x3c0c50f684c03e", "0x3c0c50f684c03e")]
        [InlineData(SolidityType.Bytes8, "0x3c0c50f684c03e40", "0x3c0c50f684c03e40")]
        [InlineData(SolidityType.Bytes9, "0x3c0c50f684c03e40bb", "0x3c0c50f684c03e40bb")]
        [InlineData(SolidityType.Bytes10, "0x3c0c50f684c03e40bb50", "0x3c0c50f684c03e40bb50")]
        [InlineData(SolidityType.Bytes11, "0x3c0c50f684c03e40bb5055", "0x3c0c50f684c03e40bb5055")]
        [InlineData(SolidityType.Bytes12, "0x3c0c50f684c03e40bb5055b6", "0x3c0c50f684c03e40bb5055b6")]
        [InlineData(SolidityType.Bytes13, "0x3c0c50f684c03e40bb5055b6e3", "0x3c0c50f684c03e40bb5055b6e3")]
        [InlineData(SolidityType.Bytes14, "0x3c0c50f684c03e40bb5055b6e396", "0x3c0c50f684c03e40bb5055b6e396")]
        [InlineData(SolidityType.Bytes15, "0x3c0c50f684c03e40bb5055b6e39651", "0x3c0c50f684c03e40bb5055b6e39651")]
        [InlineData(SolidityType.Bytes16, "0x3c0c50f684c03e40bb5055b6e39651d1", "0x3c0c50f684c03e40bb5055b6e39651d1")]
        [InlineData(SolidityType.Bytes17, "0x3c0c50f684c03e40bb5055b6e39651d11e", "0x3c0c50f684c03e40bb5055b6e39651d11e")]
        [InlineData(SolidityType.Bytes18, "0x3c0c50f684c03e40bb5055b6e39651d11ed3", "0x3c0c50f684c03e40bb5055b6e39651d11ed3")]
        [InlineData(SolidityType.Bytes19, "0x3c0c50f684c03e40bb5055b6e39651d11ed313", "0x3c0c50f684c03e40bb5055b6e39651d11ed313")]
        [InlineData(SolidityType.Bytes20, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364")]
        [InlineData(SolidityType.Bytes21, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c0", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c0")]
        [InlineData(SolidityType.Bytes22, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e")]
        [InlineData(SolidityType.Bytes23, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40")]
        [InlineData(SolidityType.Bytes24, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb")]
        [InlineData(SolidityType.Bytes25, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb50", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb50")]
        [InlineData(SolidityType.Bytes26, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055")]
        [InlineData(SolidityType.Bytes27, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6")]
        [InlineData(SolidityType.Bytes28, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e3", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e3")]
        [InlineData(SolidityType.Bytes29, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e396", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e396")]
        [InlineData(SolidityType.Bytes30, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651")]
        [InlineData(SolidityType.Bytes31, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d1", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d1")]
        [InlineData(SolidityType.Bytes32, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d11e", "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d11e")]
        [InlineData(SolidityType.Bytes32, "0x89", "0x8900000000000000000000000000000000000000000000000000000000000000")]
        public void BytesM(SolidityType solidityType, object input, string expectedResult)
        {
            var result = SolidityUtil.AbiPack((solidityType, input)).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(SolidityType.Bytes1, 787)]
        [InlineData(SolidityType.Bytes1, "0xffff")]
        [InlineData(SolidityType.Bytes2, "0xffffff")]
        [InlineData(SolidityType.Bytes32, "0x3c0c50f684c03e40bb5055b6e39651d11ed31364c03e40bb5055b6e39651d11eff")]
        public void BytesM_Invalid(SolidityType solidityType, object input)
        {
            Assert.ThrowsAny<Exception>(() => SolidityUtil.AbiPack((solidityType, input)));
        }

        [Fact]
        public void Array_Dynamic()
        {
            object[] inputs = { "0xf566", 65, 94854, 698874345 };
            var expectedOutput = "0x0000f566000000410001728629a7f9e9";

            var arraydynamic = SolidityUtil.AbiPack(("uint32[]", inputs));
            Assert.Equal(expectedOutput, arraydynamic.ToHexString(hexPrefix: true));
        }

        [Fact]
        public void Array_Static()
        {
            object[] inputs = { "0xf566", 65, 94854, 698874345 };
            var expectedOutput = "0x0000f566000000410001728629a7f9e9";

            var arrayStatic = SolidityUtil.AbiPack(("uint32[4]", inputs));
            Assert.Equal(expectedOutput, arrayStatic.ToHexString(hexPrefix: true));
        }

        [Fact]
        public void MultipleMixedTypes()
        {
            var result = SolidityUtil.AbiPack(
                ("uint32[4]", new[] { 11, 65, 94854, 698874345 }),
                (SolidityType.String, "hello world"),
                ("address[]", new[] { "0x199ac13e5a01742a56885d82ea6d3c0c514b7dc5", "0x6de105e80bc4a71685cbd423d51398db2f31b131" }),
                (SolidityType.Bool, true),
                ("bool[]", new object[] { true, "1", 1 }));

            var resultHex = result.ToHexString(hexPrefix: true);
            var expectedOutput = "0x0000000b000000410001728629a7f9e968656c6c6f20776f726c64199ac13e5a01742a56885d82ea6d3c0c514b7dc56de105e80bc4a71685cbd423d51398db2f31b13101010101";
            Assert.Equal(expectedOutput, resultHex);
        }

    }
}
