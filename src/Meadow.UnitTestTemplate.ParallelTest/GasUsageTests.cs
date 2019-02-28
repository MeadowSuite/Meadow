using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.JsonRpc.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate.ParallelTest
{
    [ParallelTestClass]
    public class GasUsageTests : ContractTest
    {
        PrecompilesContract _precompiles;
        BasicContract _basicContract;

        protected override async Task BeforeEach()
        {
            // Deploy our test contract
            _basicContract = await BasicContract.New($"TestName", true, 34, RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);
            _precompiles = await PrecompilesContract.New(RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);

        }

        [TestMethod]
        public async Task EcRecoverTest()
        {
            var gas = await _precompiles.testECRecover(
                new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f },
                0x1c,
                BigIntegerConverter.GetBytes(BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683", CultureInfo.InvariantCulture)))
                .EstimateGas();

            Assert.AreEqual(32481, gas);
        }

        [TestMethod]
        public async Task Sha256Test()
        {
            var gas = await _precompiles.testSha256("hello world").EstimateGas();
            Assert.AreEqual(24085, gas);
        }

        [TestMethod]
        public async Task Ripemd160Test()
        {
            var gas = await _precompiles.testRipemd160("hello world").EstimateGas();
            Assert.AreEqual(24715, gas);
        }

        [DataTestMethod]
        public async Task IdentityTest()
        {
            // Create a list of input strings to test echoing with the identity precompile.
            string[] inputStrings =
            {
                "",
                "okayTestTest!",
                "okayTest\x85TEST\x00TEST"
            };

            UInt256[] resultGasUsages =
            {
                23109, 24457, 24814
            };

            // Loop for each input to test echoing
            for (int i = 0; i < inputStrings.Length; i++)
            {
                // Encode our string as bytes, and try echoing it back. Obtain gas usage.
                var gas = await _precompiles.testIdentity(Encoding.UTF8.GetBytes(inputStrings[i])).EstimateGas();

                // Assert our gas usage
                Assert.AreEqual(resultGasUsages[i], gas);
            }
        }

        [TestMethod]
        public async Task ModExpTest()
        {
            // Test the modexp precompile.
            var gas = await _precompiles.testModExp(
                BigIntegerConverter.GetBytes(BigInteger.Parse("1212121323543453245345678346345737475734753745737774573475377734577", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("3", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("4345328123928357434573234217343477", CultureInfo.InvariantCulture)))
                .EstimateGas();

            Assert.AreEqual(29944, gas);
        }


        [TestMethod]
        public async Task VerifyInt()
        {
            var gas = await _basicContract.verifyInt(778899).EstimateGas();
            Assert.AreEqual(22280, gas);
        }

        [TestMethod]
        public async Task CallAndTransact()
        {
            var gas1 = await _basicContract.incrementValCounter().EstimateGas();
            Assert.AreEqual(42017, gas1);

            var gas2 = await _basicContract.getValCounter().EstimateGas();
            Assert.AreEqual(22202, gas2);
        }

        [TestMethod]
        public async Task FallbackNonpayable()
        {
            var gas = await _basicContract.FallbackFunction.EstimateGas();
            Assert.AreEqual(21064, gas);
        }

        [TestMethod]
        public async Task AccountAddressEcho()
        {
            // The amount of gas used will deviate depending on leading zeros, so we test without any.
            Address address = new Address("0x7777777777777777777777777777777777777777");
            var gas = await _basicContract.echoAddress(address).EstimateGas();
            Assert.AreEqual(23269, gas);
        }

        [TestMethod]
        public async Task EmitEvent()
        {
            var gas = await _basicContract.emitTheEvent().EstimateGas();
            Assert.AreEqual(64709, gas);
        }

        [TestMethod]
        public async Task ZkSnarksTest()
        {
            // Deploy the contract
            ZkSnarkTest snarkTest = await ZkSnarkTest.New(RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);

            // Test adding/multiplying
            var testAddResult = await snarkTest.f().EstimateGas();
            Assert.AreEqual(65114, testAddResult);

            // Test simple negation + add == zero.
            var testNegAddResult = await snarkTest.g().EstimateGas();
            Assert.AreEqual(24219, testNegAddResult);

            // Test simple pairing example
            var testSimplePair = await snarkTest.pair().EstimateGas();
            Assert.AreEqual(595435, testSimplePair);

            // Test pairing
            var testPairingResult = await snarkTest.verifyTx().EstimateGas();
            Assert.AreEqual(1927502, testPairingResult);
        }
    }
}
