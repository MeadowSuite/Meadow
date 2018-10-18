using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate.Test
{
    [TestClass]
    public class ArrayEncodingUnitTests : ContractTest
    {
        ArrayEncodingTests _contract;

        protected override async Task BeforeEach()
        {
            _contract = await ArrayEncodingTests.New(RpcClient);
        }


        [TestMethod]
        public async Task MultiDim()
        {
            // Create our expected 3 dimensional array.
            var expected3D = ArrayExtensions.CreateJaggedArray<UInt256[][][]>(2, 6, 3);
            uint x = 0;
            for (uint i = 0; i < 2; i++)
            {
                for (uint j = 0; j < 6; j++)
                {
                    for (uint k = 0; k < 3; k++)
                    {
                        expected3D[i][j][k] = x++;
                    }
                }
            }

            var t1 = await _contract.arrayStaticMultiDim3DEcho(expected3D).Call();

            // Create our expected 2 dimensional array.
            var expected2D = ArrayExtensions.CreateJaggedArray<UInt256[][]>(6, 3);
            x = 0;
            for (uint j = 0; j < 6; j++)
            {
                for (uint k = 0; k < 3; k++)
                {
                    expected2D[j][k] = x++;
                }
            }

            var t2 = await _contract.arrayStaticMultiDim2DEcho(expected2D).Call();

            // Encode and decode our 3D array to test re-encoding.
            var encoded = SolidityUtil.AbiEncode("uint256[2][6][3]", expected3D);
            var encodedHex = encoded.ToHexString();
            var recode = SolidityUtil.AbiDecode<UInt256[][][]>("uint256[2][6][3]", encoded);

            Assert.AreEqual(expected3D[0][0][0], recode[0][0][0]);
            Assert.AreEqual(expected3D[1][5][2], recode[1][5][2]);
            Assert.AreEqual(expected3D[0][2][1], recode[0][2][1]);

            // Call our method that returns a static 3D array programmed in a contract.
            var rawData3D = await _contract.arrayStaticMultiDim3D().CallRaw();
            var hex = rawData3D.ToHexString();
            var decoded3D = SolidityUtil.AbiDecode<UInt256[][][]>("uint256[2][6][3]", rawData3D);
            Assert.AreEqual(expected3D[0][0][0], decoded3D[0][0][0]);
            Assert.AreEqual(expected3D[1][5][2], decoded3D[1][5][2]);
            Assert.AreEqual(expected3D[0][2][1], decoded3D[0][2][1]);
            Assert.AreEqual(hex, encodedHex);

            // Call our method that returns a static 2D array programmed in a contract.
            var rawData2D = await _contract.arrayStaticMultiDim2D().CallRaw();
            var decoded2D = SolidityUtil.AbiDecode<UInt256[][]>("uint256[6][3]", rawData2D);
            Assert.AreEqual(expected2D[0][0], decoded2D[0][0]);
            Assert.AreEqual(expected2D[3][1], decoded2D[3][1]);
            Assert.AreEqual(expected2D[5][2], decoded2D[5][2]);
        }


        [TestMethod]
        [Ignore]
        public async Task ArrayOfBytes32()
        {
            List<byte[]> arr1 = new List<byte[]>
            {
                HexUtil.HexToBytes("bd40a10d9d184c3e84998e4ee8b29209"),
                HexUtil.HexToBytes("17dcf4ae6b024ce0afc99bd9b4ca7b69"),
                HexUtil.HexToBytes("5325195d6e3a40e2a1836a4da4478c88")
            };

            List<byte[]> arr2 = new List<byte[]>
            {
                HexUtil.HexToBytes("99bd9b4ca7b695325195d6e3a40e2a18"),
                HexUtil.HexToBytes("8b2920917dcf4ae6b024ce0afc99bd9b"),
                HexUtil.HexToBytes("920917dcf4ae6b024ce0afc99bd9b4ca")
            };

            var a_static = await _contract.takeBytes32ArrayStatic(arr1).FirstEventLog<ArrayEncodingTests.Bytes32Event>();
            var a_dyn = await _contract.takeBytes32ArrayDynamic(arr1).FirstEventLog<ArrayEncodingTests.Bytes32Event>();

            var b_static = await _contract.takeBytes32ArrayStatic2(arr1, arr2).FirstEventLog<ArrayEncodingTests.Bytes32Event>();
            var b_dyn = await _contract.takeBytes32ArrayDynamic2(arr1, arr2).FirstEventLog<ArrayEncodingTests.Bytes32Event>();

            var c_static = await _contract.takeBytes32ArrayStatic3(arr1, arr2).FirstEventLog<ArrayEncodingTests.Bytes32EventArrayStatic>();
            var c_dynamic = await _contract.takeBytes32ArrayDynamic3(arr1, arr2).FirstEventLog<ArrayEncodingTests.Bytes32EventArrayDynamic>();

            // BROKE: exception decoding array
            var d_static = await _contract.getBytes32ArrayStatic().Call();

            // BROKE: the last bytes32[] in the return tuple overwrites the previous in decoding
            var d_dyn = await _contract.getBytes32ArrayDynamic().Call();

            Assert.Inconclusive("Solc bug causes bad return values for tuples of arrays");
        }

        [TestMethod]
        [Ignore]
        public async Task ArrayOfUInt256()
        {
            List<UInt256> arr1 = new List<UInt256> { 1, 2, 3 };
            List<UInt256> arr2 = new List<UInt256> { 4, 5, 6 };

            var a_static = await _contract.takeUIntArrayStatic(arr1).FirstEventLog<ArrayEncodingTests.UIntEvent>();
            var a_dyn = await _contract.takeUIntArrayDynamic(arr1).FirstEventLog<ArrayEncodingTests.UIntEvent>();

            var b_static = await _contract.takeUIntArrayStatic2(arr1, arr2).FirstEventLog<ArrayEncodingTests.UIntEvent>();
            var b_dyn = await _contract.takeUIntArrayDynamic2(arr1, arr2).FirstEventLog<ArrayEncodingTests.UIntEvent>();

            var c_static = await _contract.takeUIntArrayStatic3(arr1, arr2).FirstEventLog<ArrayEncodingTests.UIntEventArrayStatic>();
            var c_dynamic = await _contract.takeUIntArrayDynamic3(arr1, arr2).FirstEventLog<ArrayEncodingTests.UIntEventArrayDynamic>();

            var d_static = await _contract.getUIntArrayStatic(arr1, arr2).Call();
            var d_dyn = await _contract.getUIntArrayDynamic(arr1, arr2).Call();

            // BROKE: incorrect results
            var e_static = await _contract.getUIntArrayStatic().Call();

            // BROKE: incorrect results
            var e_dyn = await _contract.getUIntArrayDynamic().Call();

            // These are different.. why?
            var compare1 = c_static.Data.ToHexString();
            var compare2 = (await _contract.getUIntArrayStatic().CallRaw()).ToHexString();

            Assert.Inconclusive("Solc bug causes bad return values for tuples of arrays");

        }

    }
}