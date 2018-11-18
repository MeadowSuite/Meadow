using Meadow.Contract;
using Meadow.Core.Utils;
using Meadow.CoverageReport.Debugging;
using Meadow.EVM.Data_Types;
using Meadow.JsonRpc.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate.Test
{
    [TestClass]
    public class ExceptionTracing : ContractTest
    {
        ExceptionContract _contract;

        protected override async Task BeforeEach()
        {
            // Deploy our test contract
            _contract = await ExceptionContract.New(_throwOnConstructor: false, RpcClient);
        }

        [TestMethod]
        public async Task DeploymentException()
        {
            ContractExecutionException exec = null;
            try
            {
                await ExceptionContract.New(_throwOnConstructor: true, RpcClient);
            }
            catch (ContractExecutionException ex)
            {
                exec = ex;
            }

            var expected = @"
INVALID instruction hit!
-> at 'assert(!_throwOnConstructor);' in 'ExceptionContract' constructor in file 'ExceptionContract.sol' : line 8";

            // Normalize our expected output.
            expected = StringUtil.NormalizeNewLines(expected).Trim();

            // Normalize our actual output
            var actual = StringUtil.NormalizeNewLines(exec.Message).Trim();

            // Verify our output.
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task CallRevertException()
        {
            await _contract.doRevert().Call();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task CallAssertException()
        {
            await _contract.doAssert().Call();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task CallRequireException()
        {
            await _contract.doRequire().Call();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task CallThrowException()
        {
            await _contract.doThrow().Call();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task CallOutOfBoundArrayAccess()
        {
            await _contract.outOfBoundArrayAccess().Call();
        }

        [TestMethod]
        public async Task CallValidateStackTrace()
        {
            ContractExecutionException exec = null;
            try
            {
                await _contract.entryFunction().Call();
            }
            catch (ContractExecutionException ex)
            {
                exec = ex;
            }

            var expected = @"INVALID instruction hit!
-> at 'assert(thingDidWork);' in method 'doAssert' in file 'ExceptionContract.sol' : line 27
-> at 'doAssert();' in method 'lastFunc' in file 'ExceptionContract.sol' : line 47
-> at 'lastFunc();' in method 'anotherFunc' in file 'ExceptionContract.sol' : line 43
-> at 'anotherFunc();' in method 'nextFunction' in file 'ExceptionContract.sol' : line 39
-> at 'nextFunction();' in method 'entryFunction' in file 'ExceptionContract.sol' : line 35";

            Assert.AreEqual(
                StringUtil.NormalizeNewLines(expected).Trim(),
                StringUtil.NormalizeNewLines(exec.Message).Trim());

        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task TransactionRevertException()
        {
            await _contract.doRevert();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task TransactionAssertException()
        {
            await _contract.doAssert();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task TransactionRequireException()
        {
            await _contract.doRequire();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task TransactionThrowException()
        {
            await _contract.doThrow();
        }

        [TestMethod]
        [ExpectedException(typeof(ContractExecutionException))]
        public async Task TransactionOutOfBoundArrayAccess()
        {
            await _contract.outOfBoundArrayAccess();
        }

        [TestMethod]
        public async Task TransactionValidateStackTrace()
        {
            ContractExecutionException exec = null;
            try
            {
                await _contract.entryFunction();
            }
            catch (ContractExecutionException ex)
            {
                exec = ex;
            }

            var expected = @"INVALID instruction hit!
-> at 'assert(thingDidWork);' in method 'doAssert' in file 'ExceptionContract.sol' : line 27
-> at 'doAssert();' in method 'lastFunc' in file 'ExceptionContract.sol' : line 47
-> at 'lastFunc();' in method 'anotherFunc' in file 'ExceptionContract.sol' : line 43
-> at 'anotherFunc();' in method 'nextFunction' in file 'ExceptionContract.sol' : line 39
-> at 'nextFunction();' in method 'entryFunction' in file 'ExceptionContract.sol' : line 35";

            Assert.AreEqual(
                StringUtil.NormalizeNewLines(expected), 
                StringUtil.NormalizeNewLines(exec.Message));

        }

    }
}
