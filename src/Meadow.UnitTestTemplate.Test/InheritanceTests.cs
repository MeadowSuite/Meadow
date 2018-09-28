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
    public class InheritanceTests : ContractTest
    {
        InheritanceChild _contract;

        protected override async Task BeforeEach()
        {
            _contract = await InheritanceChild.New(RpcClient);
        }

        [TestMethod]
        public async Task FailModifier()
        {
            await _contract.testThing(5).ExpectRevertTransaction();
        }

        [TestMethod]
        public async Task SuperPass()
        {
            var num = await _contract.testThing(2).Call();
            Assert.AreEqual(3, num);
        }

        [TestMethod]
        public async Task SuperFail()
        {
            await _contract.testThing(0).ExpectRevertTransaction();
        }
    }
}

