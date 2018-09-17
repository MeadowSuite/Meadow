using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using Meadow.Core.AbiEncoding;
using Meadow.Core.AccountDerivation;
using Meadow.Core.Cryptography.Ecdsa;

namespace Meadow.Core.Test
{
    public class SolidityUtilTests
    {
        [Theory]
        [InlineData("0xd9eba16ed0ecae432b71fe008c98cc872bb4cc214d3220a36f365326cf807d68", "hello world")]
        [InlineData("0xca02fbdcb08dd9deb739cdbba9244b1d8875799fcccc01182d2e3fbbc8094be9", "中文")]
        [InlineData("0x42bb67263d41d78772dc8d327b390012f7cdbafe309d1f7051a39df770a35c1d", "訊息摘要演算法第五版（英語：Message-Digest Algorithm 5，縮寫為MD5），是當前電腦領域用於確保資訊傳輸完整一致而廣泛使用的雜湊演算法之一（又譯雜湊演算法、摘要演算法等），主流程式語言普遍已有MD5的實作。")]
        public void HashPersonalMessage(string expectedResult, string inputMsg)
        {
            var result = SolidityUtil.HashPersonalMessage(inputMsg).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }


        [Theory]
        [InlineData("0x8529a0dce7952a07207a6bad9405f92482061a7fd29b70077262acb8e3fad25c2ab329a589457e672cf937ca3282d5a6857e9b4c9fa4a3e4d06f0b08ebe949001c", "uint256", 12345678)]
        [InlineData("0xea2862afcc46d3c28661a482d6c8d5c06d1011f4a074b56e38b70e70e709086c733b6965488c3737423ac0fb45365f601a9bef0e1f8990318d8044167688983b1b", "uint256", 12345678, "bool", true)]
        public void ECSign(string expectedResult, params object[] values)
        {
            (AbiTypeInfo AbiType, object Value)[] abiVals = new (AbiTypeInfo, object)[values.Length / 2];
            for (var i = 0; i < values.Length; i += 2)
            {
                abiVals[i / 2] = ((string)values[i], values[i + 1]);
            }

            var privateKey = "0x150a5bb56e818a0a59cb327a0d92a799a06c1b850b04d4f355d712ec89a31b75";
            var packed = SolidityUtil.PackAndHash(abiVals);
            var sig = SolidityUtil.ECSign(packed, privateKey);
            var result = SolidityUtil.SignatureToRpcFormat(sig).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ECRecover()
        {
            string message = "hello world";
            byte[] messageHash = SolidityUtil.HashPersonalMessage(message);

            var keypair = EthereumEcdsa.Generate(new SystemRandomAccountDerivation());
            byte[] privateKey = keypair.ToPrivateKeyArray();

            var signature = SolidityUtil.ECSign(messageHash, privateKey);
            var signatureSerialized = SolidityUtil.SignatureToRpcFormat(signature);

            var publicKey = SolidityUtil.ECRecover(messageHash, signatureSerialized);
            var keyAddress = SolidityUtil.PublicKeyToAddress(publicKey);

            var expectedAddress = SolidityUtil.PublicKeyToAddress(keypair.ToPublicKeyArray());

            Assert.Equal(expectedAddress.ToString(), keyAddress.ToString());
        }

        [Theory]
        [InlineData("0x3e27a893dc40ef8a7f0841d96639de2f58a132be5ae466d40087a2cfa83b7179", "uint256", 234564535, "bytes", "0xfff23243", "bool", true, "int256", -10)]
        [InlineData("0x661136a4267dba9ccdf6bfddb7c00e714de936674c4bdb065a531cf1cb15c7fc", "string", "Hello!%")]
        [InlineData("0x61c831beab28d67d1bb40b5ae1a11e2757fa842f031a2d0bc94a7867bc5d26c2", "uint256", "234")]
        [InlineData("0x61c831beab28d67d1bb40b5ae1a11e2757fa842f031a2d0bc94a7867bc5d26c2", "uint256", 0xea)]
        [InlineData("0x4e8ebbefa452077428f93c9520d3edd60594ff452a29ac7d2ccc11d47f3ab95b", "bytes", "0x407d73d8a49eeb85d32cf465507dd71d507100c1")]
        [InlineData("0x4e8ebbefa452077428f93c9520d3edd60594ff452a29ac7d2ccc11d47f3ab95b", "address", "0x407d73d8a49eeb85d32cf465507dd71d507100c1")]
        [InlineData("0x3c69a194aaf415ba5d6afca734660d0a3d45acdc05d54cd1ca89a8988e7625b4", "bytes32", "0x407D73d8a49eeb85D32Cf465507dd71d507100c1")]
        [InlineData("0xa13b31627c1ed7aaded5aecec71baf02fe123797fffd45e662eac8e06fbe4955", "string", "Hello!%", "int8", -23, "address", "0x85F43D8a49eeB85d32Cf465507DD71d507100C1d")]
        public void HashAndPack(string expectedResult, params object[] values)
        {
            (AbiTypeInfo AbiType, object Value)[] abiVals = new (AbiTypeInfo, object)[values.Length / 2];
            for (var i = 0; i < values.Length; i += 2)
            {
                abiVals[i / 2] = ((string)values[i], values[i + 1]);
            }

            var result = SolidityUtil.PackAndHash(abiVals).ToHexString(hexPrefix: true);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SignatureRpcFormatting()
        {
            var keypair = EthereumEcdsa.Generate(new SystemRandomAccountDerivation());
            var msgHash = SolidityUtil.HashPersonalMessage("test message");
            var signature = SolidityUtil.ECSign(msgHash, keypair.ToPrivateKeyArray());
            var signatureHex = SolidityUtil.SignatureToRpcFormat(signature).ToHexString(hexPrefix: true);
            var convertedSignature = SolidityUtil.SignatureFromRpcFormat(signatureHex.HexToBytes());
            Assert.Equal(signature.R, convertedSignature.R);
            Assert.Equal(signature.S, convertedSignature.S);
            Assert.Equal(signature.V, convertedSignature.V);
        }
        

        [Fact]
        public void PadLeft_ByteArray()
        {
            byte[] arr = new byte[] { 1, 2, 3, 4, 5 };
            var newArr = SolidityUtil.PadLeft(arr, 32).ToHexString(hexPrefix: true);
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000102030405", newArr);
        }

        [Fact]
        public void PadLeft_ByteSpan()
        {
            Span<byte> arr = new byte[] { 1, 2, 3, 4, 5 };
            var newArr = SolidityUtil.PadLeft(arr, 32).ToHexString(hexPrefix: true);
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000102030405", newArr);
        }

        [Fact]
        public void PadLeft_HexString()
        {
            string arr = "0x0102030405";
            var newArr = SolidityUtil.PadLeft(arr, 32);
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000102030405", newArr);
        }

        [Fact]
        public void PadLeft_HexString_Full()
        {
            string arr = "0x1234000000000000000000000000000000000000000000000000000102030405";
            var newArr = SolidityUtil.PadLeft(arr, 32);
            Assert.Equal("0x1234000000000000000000000000000000000000000000000000000102030405", newArr);
        }
    }
}
