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
    public class NestedBranchesTests : ContractTest
    {
        NestedBranches _contract;

        protected override async Task BeforeEach()
        {
            _contract = await NestedBranches.New(RpcClient);
        }

        [TestMethod]
        public async Task NestedStructBranches()
        {
            Assert.IsTrue(await _contract.checkStructTree(555).Call());
            Assert.IsFalse(await _contract.checkStructTree(111).Call());
        }
        
        [TestMethod]
        public async Task SimpleIfStatement()
        {
            Assert.IsTrue(await _contract.simpleIfStatement(true).Call());
            Assert.IsFalse(await _contract.simpleIfStatement(false).Call());
        }
    }
}

