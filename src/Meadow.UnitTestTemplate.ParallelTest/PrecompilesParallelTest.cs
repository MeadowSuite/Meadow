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
    public class PrecompilesParallelTests : ContractTest
    {
        PrecompilesContract _contract;

        protected override async Task BeforeEach()
        {
            // Deploy our test contract
            _contract = await PrecompilesContract.New(RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);
        }

        [TestMethod]
        public async Task EcRecoverTest()
        {
            var ecRecoverTest = await _contract.testECRecover(
                new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f },
                0x1c,
                BigIntegerConverter.GetBytes(BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683", CultureInfo.InvariantCulture)))
                .Call();

            Assert.AreEqual("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", ecRecoverTest.GetHexString(hexPrefix: true));
        }

        [TestMethod]
        public async Task Sha256Test()
        {
            var sha256HashTest = await _contract.testSha256("hello world").Call(); // should be 0xb94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
            Assert.AreEqual("0xb94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", sha256HashTest.ToHexString(hexPrefix: true));
        }

        [TestMethod]
        public async Task Ripemd160Test()
        {
            var ripemd160HashTest = await _contract.testRipemd160("hello world").Call(); // should be 0x98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f
            Assert.AreEqual("0x98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f", ripemd160HashTest.ToHexString(hexPrefix: true));
        }

        [TestMethod]
        public async Task IdentityTest()
        {
            // Create a list of input strings to test echoing with the identity precompile.
            string[] inputStrings =
            {
                "",
                "okayTestTest!",
                "okayTest\x85TEST\x00TEST"
            };
            
            // Loop for each input to test echoing
            foreach (string inputString in inputStrings)
            {
                // Encode our string as bytes, and try echoing it back.
                byte[] outputBytes = await _contract.testIdentity(Encoding.UTF8.GetBytes(inputString)).Call();

                // Obtain our result as a string
                string outputString = Encoding.UTF8.GetString(outputBytes);

                // Assert our string is equal
                Assert.AreEqual(inputString, outputString);
            }
        }

        [TestMethod]
        public async Task ModExpTest()
        {
            // Test the modexp precompile.
            var modExpTestBytes = await _contract.testModExp(
                BigIntegerConverter.GetBytes(BigInteger.Parse("1212121323543453245345678346345737475734753745737774573475377734577", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("3", CultureInfo.InvariantCulture)),
                BigIntegerConverter.GetBytes(BigInteger.Parse("4345328123928357434573234217343477", CultureInfo.InvariantCulture)))
                .Call();

            // Convert the result into an integer.
            var modExpTest = BigIntegerConverter.GetBigInteger(modExpTestBytes, false, modExpTestBytes.Length);
            Assert.AreEqual("856753145937825219130387866259147", modExpTest.ToString(CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public async Task MulModTest()
        {
            // Test cases of the form (x, y, z, res) such that mulmod(x, y, z) = res
            // Importantly, some are constructed to require the non-truncation behaviour of mulmod
            (BigInteger, BigInteger, BigInteger, BigInteger)[] testData =
            {
                (2, 7, 3, 2),
                (4, 8, 11, 10),
                (2, 1000, 10, 0),
                (BigInteger.Pow(2, 10), BigInteger.Pow(2, 15), BigInteger.Pow(2, 32), BigInteger.Pow(2, 25)),
                (BigInteger.Pow(2, 255), 2, 1, 0),
                (BigInteger.Pow(2, 255), 2, 150, 136),
                (BigInteger.Pow(2, 255), 3, 5, 4),
                (BigInteger.Pow(2, 256) - 1, BigInteger.Pow(2, 256) - 1, 2019, 1254)
            };
            foreach (var (x, y, z, res) in testData)
            {
                UInt256 obs = await _contract.testMulMod((UInt256)x, (UInt256)y, (UInt256)z).Call();
                Assert.AreEqual((UInt256)res, obs);
            }
        }

        [TestMethod]
        public async Task AddModTest()
        {
            // Test cases of the form (x, y, z, res) such that addmod(x, y, z) = res
            // Importantly, some are constructed to require the non-truncation behaviour of addmod
            (BigInteger, BigInteger, BigInteger, BigInteger)[] testData =
            {
                (10, 5, 10, 5),
                (50, 29, 7, 2),
                (10000, 25000, 7, 0),
                (BigInteger.Pow(2, 255), BigInteger.Pow(2, 255), 1, 0),
                (BigInteger.Pow(2, 255), BigInteger.Pow(2, 255), 150, 136),
                (BigInteger.Pow(2, 256) - 1, BigInteger.Pow(2, 10), 2019, 1951),
                (BigInteger.Pow(2, 256) - 1, BigInteger.Pow(2, 256) - 1, 2019, 1854),
            };
            foreach (var (x, y, z, res) in testData)
            {
                UInt256 obs = await _contract.testAddMod((UInt256)x, (UInt256)y, (UInt256)z).Call();
                Assert.AreEqual((UInt256)res, obs);
            }
        }

        [TestMethod]
        public async Task ZkSnarksTest()
        {
            // Deploy the contract
            ZkSnarkTest snarkTest = await ZkSnarkTest.New(RpcClient, new TransactionParams { From = Accounts[0], Gas = 4712388 }, Accounts[0]);

            // Test adding/multiplying
            var testAddResult = await snarkTest.f().Call();
            Assert.IsTrue(testAddResult);

            // Test simple negation + add == zero.
            var testNegAddResult = await snarkTest.g().Call();
            Assert.IsTrue(testNegAddResult);

            // Test simple pairing example
            var testSimplePair = await snarkTest.pair().Call();
            Assert.IsTrue(testSimplePair);

            // Test verify complex pairing
            var testPairingResult = await snarkTest.verifyTx().Call();
            Assert.IsTrue(testPairingResult);
        }
    }
}
