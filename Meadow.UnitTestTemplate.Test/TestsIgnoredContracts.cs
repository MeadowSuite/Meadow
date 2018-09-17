using Meadow.JsonRpc.Types;
using Meadow.EVM.Data_Types;
using Meadow.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System.Threading.Tasks;
using Meadow.Contract;

namespace Meadow.UnitTestTemplate.Test
{

    [TestClass]
    public class TestsIgnoredContracts : ContractTest
    {

        protected override Task BeforeEach()
        {
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task MockContractFunction()
        {
            var contract = await MockContract.New(RpcClient);
            string testStr = "test string 1234";
            var resultStr = await contract.echoString(testStr).Call();
            Assert.AreEqual(testStr, resultStr);
        }

        [TestMethod]
        public async Task IgnoreContractFunction()
        {
            var contract = await IgnoreContract.New(RpcClient);
            string testStr = "test string 1234";
            var resultStr = await contract.echoString(testStr).Call();
            Assert.AreEqual(testStr, resultStr);
        }
    }
}

