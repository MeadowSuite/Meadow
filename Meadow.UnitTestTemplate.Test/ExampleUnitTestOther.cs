using Meadow.JsonRpc.Types;
using Meadow.EVM.Data_Types;
using Meadow.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System.Threading.Tasks;
using Meadow.Contract;
using System.Globalization;

namespace Meadow.UnitTestTemplate.Test
{
    [TestClass]
    public class ExampleUnitTestOther : ContractTest
    {
        BasicContractOther _contract;

        protected override async Task BeforeEach()
        {
            // Deploy our test contract
            _contract = await BasicContractOther.New($"TestName", true, 34, RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);
        }

        [TestMethod]
        public async Task FallbackPayable()
        {
            var result = await _contract.FallbackFunction.TransactionReceipt(new TransactionParams { Value = 10000 });
        }

        [TestMethod]
        public async Task AccountAddressEcho1()
        {
            var res = await _contract.echoAddress(Accounts[1]).Call();
            Assert.AreEqual(Accounts[1], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho2()
        {
            var res = await _contract.echoAddress(Accounts[2]).Call();
            Assert.AreEqual(Accounts[2], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho3()
        {
            var res = await _contract.echoAddress(Accounts[3]).Call();
            Assert.AreEqual(Accounts[3], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho4()
        {
            var res = await _contract.echoAddress(Accounts[4]).Call();
            Assert.AreEqual(Accounts[4], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho5()
        {
            var res = await _contract.echoAddress(Accounts[5]).Call();
            Assert.AreEqual(Accounts[5], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho6()
        {
            var res = await _contract.echoAddress(Accounts[6]).Call();
            Assert.AreEqual(Accounts[6], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho7()
        {
            var res = await _contract.echoAddress(Accounts[7]).Call();
            Assert.AreEqual(Accounts[7], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho8()
        {
            var res = await _contract.echoAddress(Accounts[8]).Call();
            Assert.AreEqual(Accounts[8], res);
        }

        [TestMethod]
        public async Task AccountAddressEcho9()
        {
            var res = await _contract.echoAddress(Accounts[9]).Call();
            Assert.AreEqual(Accounts[9], res);
        }

        [TestMethod]
        public async Task EmitEventSeveral()
        {
            for (ulong i = 1; i < 5; i++)
            {
                var eventResult = await _contract.emitTheEvent().FirstEventLog<BasicContract.TestEvent>();
                Assert.AreEqual((ulong)i, eventResult._id);
            }
        }

        [TestMethod]
        public async Task EmitEvent2()
        {
            await _contract.emitTheEvent().FirstEventLog<BasicContract.TestEvent>();
        }

        [TestMethod]
        public async Task EmitEvent3()
        {
            await _contract.emitTheEvent().FirstEventLog<BasicContract.TestEvent>();
        }

        [TestMethod]
        public async Task EmitEvent4()
        {
            await _contract.emitTheEvent().FirstEventLog<BasicContract.TestEvent>();
        }
    }
}