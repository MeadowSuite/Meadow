using Meadow.Contract;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types;
using Meadow.JsonRpc.Types;
using Meadow.UnitTestTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;

namespace Meadow.DebugExampleTests
{
    [TestClass]
    public class NestedContractTests : ContractTest
    {
        NestedContract _contract;

        protected override async Task BeforeEach()
        {
            // Deploy our test contract
            _contract = await NestedContract.New(RpcClient);
        }

        [TestMethod]
        public async Task IncrementNumber()
        {
            await _contract.incrementNumber(123).Call();
        }

    }
}