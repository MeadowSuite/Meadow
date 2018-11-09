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
using Meadow.Core.EthTypes;

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
        public async Task GetBalanceTest()
        {
            var accounts = await Client.Accounts();
            var balance = await Client.GetBalance(accounts[0], DefaultBlockParameter.Default);
            Assert.Equal(2e21, balance);
        }

        [Fact]
        public async Task GetCodeTest()
        {
            var accounts = await Client.Accounts();
            var code = await Client.GetCode(accounts[0], DefaultBlockParameter.Default);

            await Client.GetCode(accounts[0], 9999999);
        }

        [Fact]
        public async Task IncreaseTimeTest()
        {
            // We allow 5 seconds of deviation in this test for the times between blocks we create after advancing time.
            // This is because processing the block itself may take a second or two. For slow machines we take the safe route
            // and use a deviation of 5.
            const ulong allowedDeviation = 5;

            // Define the amount of time to advance.
            const ulong timeToAdvanceSeconds = 1500;

            // Mine a block.
            await _fixture.Client.Mine();

            // Obtain the time stamp on it.
            var time1 = (await Client.GetBlockByNumber(DefaultBlockParameter.Default, false)).Timestamp;

            // Increase the time by our desired time to advance.
            await _fixture.Client.IncreaseTime(timeToAdvanceSeconds);

            // Mine another block after our time was advanced.
            await _fixture.Client.Mine();

            // Obtain the time stamp on the second block.
            var time2 = (await Client.GetBlockByNumber(DefaultBlockParameter.Default, false)).Timestamp;

            // Determine the time delta between blocks.
            var timeDelta = time2 - time1;

            // Verify the time delta is greater or equal to the time we advanced.
            Assert.True(timeDelta >= timeToAdvanceSeconds);

            // Verify the time delta is less than the time we advanced plus some deviation time (for processing).
            Assert.True(timeDelta <= timeToAdvanceSeconds + allowedDeviation);
        }

        [Fact]
        public async Task GasPriceTest()
        {
            var price = await Client.GasPrice();
            Assert.Equal(0, price);
        }

        [Fact]
        public async Task CoinbaseTest()
        {
            var coinbase = await Client.Coinbase();
            Assert.Equal("0x7777777777777777777777777777777777777777", coinbase);
        }

        [Fact]
        public async Task TestChainID()
        {
            var chainID = await Client.ChainID();
            Assert.Equal(77UL, chainID);
        }

        [Fact]
        public async Task MineTest()
        {
            var blockNum1 = (await Client.GetBlockByNumber(DefaultBlockParameter.Default, false)).Number.Value;
            await _fixture.Client.Mine();
            var blockNum2 = (await Client.GetBlockByNumber(DefaultBlockParameter.Default, false)).Number.Value;
            Assert.Equal(1UL, blockNum2 - blockNum1);
        }

        [Fact]
        public async Task GetBlockTest()
        {
            var blockNum = await Client.BlockNumber();
            var block1 = await Client.GetBlockByNumber(blockNum, false);
            var block2 = await Client.GetBlockByHash(block1.Hash.Value, false);
            Assert.Equal(block1.Hash.Value, block2.Hash.Value);
        }

        [Fact]
        public async Task GetTransactionByHash()
        {
            var accounts = await Client.Accounts();
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            var txHash = await contract.getValCounter().SendTransaction();
            var tx = await Client.GetTransactionByHash(txHash);
            Assert.Equal(txHash, tx.Hash);
        }

        [Fact]
        public async Task GetTransactionByBlockHashAndIndexTest()
        {
            var accounts = await Client.Accounts();
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            var txHash = await contract.getValCounter().SendTransaction();
            var curBlock = await Client.GetBlockByNumber(DefaultBlockParameter.Default, true);
            var tx = await Client.GetTransactionByBlockHashAndIndex(curBlock.Hash.Value, 0);
            Assert.Equal(txHash, tx.Hash);
        }


        [Fact]
        public async Task GetBlockTransactionCountTest()
        {
            var accounts = await Client.Accounts();
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            var txHash = await contract.getValCounter().SendTransaction();

            var curBlock = await Client.GetBlockByNumber(DefaultBlockParameter.Default, true);

            var count1 = await Client.GetBlockTransactionCountByHash(curBlock.Hash.Value);
            Assert.Equal(1UL, count1);

            var count2 = await Client.GetBlockTransactionCountByNumber(curBlock.Number.Value);
            Assert.Equal(1UL, count2);
        }

        [Fact]
        public async Task GetTransactionCountTest()
        {
            var accounts = await Client.Accounts();
            var contract = await BasicContract.New($"TestName", true, 34, Client, new TransactionParams { From = accounts[0], Gas = 4712388 }, accounts[0]);
            var txHash = await contract.getValCounter().SendTransaction(new TransactionParams(from: accounts[2]));

            var curBlock = await Client.GetBlockByNumber(DefaultBlockParameter.Default, true);

            var count = await Client.GetTransactionCount(accounts[2], DefaultBlockParameter.Default);
            Assert.Equal(1UL, count);
        }

        [Fact]
        public async Task ClientVersionTest()
        {
            var version = await Client.ClientVersion();
            Assert.Contains("Meadow-TestServer", version, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SyncingTest()
        {
            var syncing = await Client.Syncing();
            Assert.False(syncing.IsSyncing);
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
            await Client.ClearCoverage(contract.ContractAddress);
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
