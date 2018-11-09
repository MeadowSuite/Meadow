using Meadow.Core.AccountDerivation;
using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.Data_Types.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.EVM.Test
{
    public class SigningTests
    {
        /// <summary>
        /// Tests the BigInteger conversion between System.Numerics and Org.BouncyCastle.Math.
        /// </summary>
        [Fact]
        public void BouncyCastleBigIntegerTests()
        {
            // Run 100 rounds of conversion from bouncy castle to system.numerics
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                byte[] data = new byte[random.Next(1, 0x20 + 1)];
                random.NextBytes(data);
                BigInteger bigInteger = BigIntegerConverter.GetBigInteger(data);
                Org.BouncyCastle.Math.BigInteger bcInteger = bigInteger.ToBouncyCastleBigInteger();
                BigInteger bigInteger2 = bcInteger.ToNumericsBigInteger();
                Assert.Equal<BigInteger>(bigInteger, bigInteger2);
            }
        }

        [Fact]
        public void TransactionRLPAndVerify()
        {
            // Verifies our RLP decoding works, and we can recover the public key/sender from the transaction.
            byte[] rlpEncodedTransaction = "f86a15850430e23400830186a094a593094cebb06bf34df7311845c2a34996b5232485e8d4a510008026a0a003ddf704feb0c62aba5459ad0af698eab974b0fe9c3685426bde2f31669252a05625a814b54f7f994faf5747eae956dbf5c85e4649022b22b9512a74c41e92f4".HexToBytes();
            Transaction transaction = new Transaction(RLP.Decode(rlpEncodedTransaction));
            Assert.Equal("0x82bd8ead9cfbf50d35f9c3ab75f994a59e6c3317", transaction.GetSenderAddress().ToString());

            // Verifies our rlp reencoded exactly as intended.
            byte[] rlpReencodedTransaction = RLP.Encode(transaction.Serialize());
            Assert.True(rlpEncodedTransaction.ValuesEqual(rlpReencodedTransaction));
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionGenerateSignAndVerify(bool useBouncyCastle)
        {

            // Generate a keypair
            EthereumEcdsa ecdsa;
            if (useBouncyCastle)
            {
                ecdsa = EthereumEcdsaBouncyCastle.Generate(new SystemRandomAccountDerivation());
            }
            else
            {
                ecdsa = EthereumEcdsaNative.Generate(new SystemRandomAccountDerivation());
            }

            // Obtain our sender address for our keypair.
            Address actualSender = new Address(ecdsa.GetPublicKeyHash());

            // Define our chain IDs to test, including null to test pre-spurious dragon.
            EthereumChainID?[] chainIDs = new EthereumChainID?[] { null, EthereumChainID.Ethereum_MainNet };

            // Define our transaction to test
            // Create a new transaction.
            Transaction transaction = new Transaction(21, 18000000000, 100000, new Address("0xa593094cebb06bf34df7311845c2a34996b52324"), 1000000000000, null);

            // Loop for each chain ID to test.
            foreach (EthereumChainID? chainID in chainIDs)
            {
                // Sign our data
                transaction.Sign(ecdsa, chainID);

                // Obtain our sender from our signed transaction, verify it matches our actual sender.
                Address transactionSender = transaction.GetSenderAddress();
                Assert.Equal(actualSender.ToString(), transactionSender.ToString());
            }

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SignAndVerify(bool useBouncyCastle)
        {
            // Generate ECDSA keypair, compute a hash, sign it, then recover the public key from the signature and verify it matches.
            EthereumEcdsa provider;
            if (useBouncyCastle)
            {
                provider = EthereumEcdsaBouncyCastle.Generate(new SystemRandomAccountDerivation());
            }
            else
            {
                provider = EthereumEcdsaNative.Generate(new SystemRandomAccountDerivation());
            }

            byte[] hash = KeccakHash.ComputeHashBytes(new byte[] { 11, 22, 33, 44 });
            (byte RecoveryID, BigInteger r, BigInteger s) signature = provider.SignData(hash);
            EthereumEcdsa recovered = EthereumEcdsa.Recover(hash, signature.RecoveryID, signature.r, signature.s);
            Assert.True(provider.GetPublicKeyHash().ValuesEqual(recovered.GetPublicKeyHash()));
        }

        [Theory]
        [InlineData(true, "103aaccf80ad53c11ce2d1654e733a70835b852bfa4528a6214f11a9b9c6e55c", "7d2386471f6caf4327e08fe8767d5b3e3ae014a32ec2f1bd4f7ca3dcac7c00448f613f0ae0c2b340a06a2183586d4b36c0b33a19dba3cad5e9dd81278e1e5a9b", "d0ab6bbdc1e1bc5c189d843a0ed4ae18bb76b1afbe4c2b6ffed66992402f8f90")]
        [InlineData(false, "103aaccf80ad53c11ce2d1654e733a70835b852bfa4528a6214f11a9b9c6e55c", "7d2386471f6caf4327e08fe8767d5b3e3ae014a32ec2f1bd4f7ca3dcac7c00448f613f0ae0c2b340a06a2183586d4b36c0b33a19dba3cad5e9dd81278e1e5a9b", "d0ab6bbdc1e1bc5c189d843a0ed4ae18bb76b1afbe4c2b6ffed66992402f8f90")]
        [InlineData(true, "00e9088ce6d8df1357233e1cde9ad58a910a26605bd1921570977d6708b96e37b5", "2d85837598dbdb4cb0d803157d0219880098ffe9735802107d89aeecda4518e44b07e3a41e37daa79cd44d8cdbb8bb10deca2481264aee3472ab044cc61e17a0", "542c718db53e6b8af98f8903e2f6afa39da3b892d9bc9f152f87f8f3d9c046fb")]
        [InlineData(false, "00e9088ce6d8df1357233e1cde9ad58a910a26605bd1921570977d6708b96e37b5", "2d85837598dbdb4cb0d803157d0219880098ffe9735802107d89aeecda4518e44b07e3a41e37daa79cd44d8cdbb8bb10deca2481264aee3472ab044cc61e17a0", "542c718db53e6b8af98f8903e2f6afa39da3b892d9bc9f152f87f8f3d9c046fb")]
        public void ComputeECDHKeyTest(bool useBouncyCastle, string privateKeyStr, string publicKeyStr, string expectedSecretStr)
        {
            // Generate ECDSA keypair
            EthereumEcdsa privateKey = null;
            EthereumEcdsa publicKey = null;
            if (useBouncyCastle)
            {
                privateKey = new EthereumEcdsaBouncyCastle(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
                publicKey = new EthereumEcdsaBouncyCastle(publicKeyStr.HexToBytes(), EthereumEcdsaKeyType.Public);
            }
            else
            {
                privateKey = new EthereumEcdsaNative(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
                publicKey = new EthereumEcdsaNative(publicKeyStr.HexToBytes(), EthereumEcdsaKeyType.Public);
            }

            // Compute a shared key.
            byte[] data = privateKey.ComputeECDHKey(publicKey);

            Assert.Equal(expectedSecretStr, data.ToHexString(false));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ProvidedKeySigning(bool useBouncyCastle)
        {
            // Use a provided private key to sign two different transactions and verify the sender should be the same.
            string privateKeyStr = "e815acba8fcf085a0b4141060c13b8017a08da37f2eb1d6a5416adbb621560ef";

            EthereumEcdsa provider;
            if (useBouncyCastle)
            {
                provider = new EthereumEcdsaBouncyCastle(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
            }
            else
            {
                provider = new EthereumEcdsaNative(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
            }

            // Signing two different transactions with or without a chain ID should yield the same sender.
            Transaction transaction1 = new Transaction(25, 77044660770, 100100, new Address("0xffff094cebb06bf34df7311845c2a34996b52324"), 2000055000000, null);
            Transaction transaction2 = new Transaction(21, 18000000000, 100000, new Address("0xa593094cebb06bf34df7311845c2a34996b52324"), 1000000000000, null);
            transaction1.Sign(provider);
            transaction2.Sign(provider, EthereumChainID.Ethereum_MainNet);

            Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", transaction1.GetSenderAddress().ToString());
            Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", transaction2.GetSenderAddress().ToString());
            Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", new Address(provider.GetPublicKeyHash()).ToString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PublicKeyFormatTests(bool useBouncyCastle)
        {
            // Test compressed, uncompressed, ethereum format public keys and verify they all equal the same resulting key.
            string privateKeyStr = "e815acba8fcf085a0b4141060c13b8017a08da37f2eb1d6a5416adbb621560ef";

            EthereumEcdsa provider;
            if (useBouncyCastle)
            {
                provider = new EthereumEcdsaBouncyCastle(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
            }
            else
            {
                provider = new EthereumEcdsaNative(privateKeyStr.HexToBytes(), EthereumEcdsaKeyType.Private);
            }

            // Obtain our public key in different formats.
            byte[] publicKeyCompressed = provider.ToPublicKeyArray(true, false);
            byte[] publicKeyUncompressed = provider.ToPublicKeyArray(false, false);
            byte[] publicKeyEthereum = provider.ToPublicKeyArray(false, true);

            // Put them into a singular array to test.
            byte[][] publicKeys = new byte[][] { publicKeyCompressed, publicKeyUncompressed, publicKeyEthereum };

            // Loop for each format to test.
            for (int i = 0; i < publicKeys.Length; i++)
            {
                // Parse the public key that is indexed.
                EthereumEcdsa publicKeyProvider;
                if (useBouncyCastle)
                {
                    publicKeyProvider = new EthereumEcdsaBouncyCastle(publicKeys[i], EthereumEcdsaKeyType.Public);
                }
                else
                {
                    publicKeyProvider = new EthereumEcdsaNative(publicKeys[i], EthereumEcdsaKeyType.Public);
                }

                // Verify the public key hashes match
                Assert.Equal(provider.GetPublicKeyHash().ToHexString(), publicKeyProvider.GetPublicKeyHash().ToHexString());
            }
        }
    }
}
