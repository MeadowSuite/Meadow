using System;
using Meadow.JsonRpc.Types;
using Meadow.EVM.Data_Types;
using Meadow.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System.Threading.Tasks;
using Meadow.Contract;
using Meadow.JsonRpc;
using Meadow.CoverageReport.Debugging;
using Meadow.Core.EthTypes;

namespace Meadow.UnitTestTemplate.Test
{
    [TestClass]
    public class ContractDeploymentFailures : ContractTest
    {
        [TestMethod]
        public async Task FailDeployment()
        {
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                await MissingConstructorChild.New(RpcClient);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (ContractExecutionException ex)
            {
                StringAssert.Contains(ex.Message, "Contract deployment ended up deploying a contract which is zero bytes in size.", ex.Message);
                return;
            }

            throw new Exception("Should have thrown");
        }

        [TestMethod]
        public async Task ContractTooLargeException()
        {
            try
            {
                await OversizedContract.New(RpcClient);
            }
            catch (ContractExecutionException ex)
            {
                StringAssert.Contains(ex.Message, "exceeds the maximum contract size");
                return;
            }

            throw new Exception("Should have failed deployment");
        }

        [TestMethod]
        public async Task ContractSizeCheckDisabled()
        {
            OversizedContract contract = null;
            await DisableContractSizeLimit(async () =>
            {
                contract = await OversizedContract.New(RpcClient, new TransactionParams { Gas = 8700000 });
            });
            var (num, tx) = await contract.echoNumber(123).CallAndTransact();
            Assert.AreEqual(123, num);
        }

        [TestMethod]
        [SkipCoverage]
        public async Task EmptyTransactionData()
        {
            try
            {
                await RpcClient.SendTransaction(new TransactionParams
                {
                    From = Accounts[0],
                    Data = new byte[1000]
                });
            }
            catch
            {
                return;
            }

            throw new Exception("Should have failed");
        }

        [TestMethod]
        public async Task ExpectDeploymentRevert()
        {
            await ExceptionContract.New(_throwOnConstructor: true, RpcClient).ExpectRevert();
        }

        [TestMethod]
        public async Task ExpectDeploymentRevert_Fail()
        {
            await Assert.ThrowsExceptionAsync<Exception>(() => ExceptionContract.New(_throwOnConstructor: false, RpcClient).ExpectRevert());
        }
    }
}

