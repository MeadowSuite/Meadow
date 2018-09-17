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