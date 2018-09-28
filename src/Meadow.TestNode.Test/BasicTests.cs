using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types;
using Meadow.EVM.Data_Types;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Globalization;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Meadow.TestNode.Test
{
    public class BasicTests : IDisposable
    {
        #region Properties
        /// <summary>
        /// The shared data to use across tests within this class.
        /// </summary>
        readonly TestChainFixture _fixture;

        /// <summary>
        /// The client that is shared across tests within this class.
        /// </summary>
        IJsonRpcClient Client => _fixture.Client;

        #endregion

        public BasicTests()
        {
            _fixture = new TestChainFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task SnapshotRevertWithCoverageTest()
        {
            await Client.SetCoverageEnabled(true);

            var accounts = await Client.Accounts();
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            var snapshotID = await Client.Snapshot();
            var initialValCounter = await contract.getValCounter().Call();
            await contract.incrementValCounter();
            var valCounter2 = await contract.getValCounter().Call();
            await Client.Revert(snapshotID);
            var finalValCounter = await contract.getValCounter().Call();
            Assert.Equal(0, initialValCounter);
            Assert.Equal(2, valCounter2);
            Assert.Equal(0, finalValCounter);

            var snapshotID2 = await Client.Snapshot();
            var contract2 = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            await contract2.incrementValCounter();
            await Client.Revert(snapshotID2);

            var coverage = await Client.GetCoverageMap(contract.ContractAddress);
        }

        #region Tests
        [Fact]
        public async Task TestPrecompiles()
        {
            // Grab a list of accounts
            var accounts = await Client.Accounts();

            // Deploy our test contract
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);

            // 1) This ECRecover test should yield 0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd. This test data is taken from SigningTests.cs
            var ecRecoverTest = await contract.testECRecover(
                new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f },
                0x1c,
                BigIntegerConverter.GetBytes(BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683", CultureInfo.InvariantCulture)))
                .Call();

            Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", ecRecoverTest);

            // Precompile hash tests

            // 2) SHA-256
            var sha256HashTest = await contract.sha256str("hello world").Call(); // should be 0xb94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
            Assert.Equal("0xb94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", sha256HashTest.ToHexString(hexPrefix: true));

            // 3) RIPEMD160
            var ripemd160HashTest = await contract.ripemd160str("hello world").Call(); // should be 0x98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f
            Assert.Equal("0x98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f", ripemd160HashTest.ToHexString(hexPrefix: true));

            // 4) IDENTITY + 5) MODEXP (this function uses both)
            var modExpTestBytes = await contract.testModExp(
                BigIntegerConverter.GetBytes(BigInteger.Parse("1212121323543453245345678346345737475734753745737774573475377734577", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("3", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("4345328123928357434573234217343477", CultureInfo.InvariantCulture)))
            .Call();
            var modExpTest = BigIntegerConverter.GetBigInteger(modExpTestBytes, false, modExpTestBytes.Length); // should be 0x856753145937825219130387866259147
            Assert.Equal(BigInteger.Parse("856753145937825219130387866259147", CultureInfo.InvariantCulture), modExpTest);

            // TODO: 6) Checking a pairing equation on bn128 curve
            // TODO: 7) Addition of bn128 curve
            // TODO: 8) Scalar multiplication of bn128 curve.

        }

        #endregion
    }
}
