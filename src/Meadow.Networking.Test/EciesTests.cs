using Meadow.Core.Cryptography.Ecdsa;
using System;
using System.Collections.Generic;
using System.Text;
using Meadow.Core.AccountDerivation;
using Meadow.Core.Utils;
using Xunit;
using Meadow.Networking.Cryptography;
using Meadow.Networking.Cryptography.Aes;

namespace Meadow.Networking.Test
{
    public class EciesTests
    {
        /*
         * NOTE: ECIES Encrypt uses a random private key to encrypt, so the encrypted data will differ each time, thus it cannot efficiently be individually tested.
         * ECIES decrypt however can be tested, as the random private key's public key was included in the encrypted data and can be decrypted to the original data as intended.
         * */

        [Fact]
        public void EciesEncryptDecryptTest()
        {
            string[] testDataSets = new string[] { "okayAESstrTest", "test2", "and another \x00 test. but this one is a sentence with \x11 weird characters." };

            for (int i = 0; i < testDataSets.Length; i++)
            {
                // Generate a keypair
                EthereumEcdsa keypair = EthereumEcdsa.Generate();

                byte[] testData = Encoding.UTF8.GetBytes(testDataSets[i]);

                byte[] encrypted = Ecies.Encrypt(keypair, testData, null);
                byte[] decrypted = Ecies.Decrypt(keypair, encrypted, null);

                string result = Encoding.UTF8.GetString(decrypted);

                Assert.Equal(testDataSets[i], result);
            }
        }

        [Theory]
        [InlineData("65884e73d44c1e6f339960a7143cca6318bfe19b029267c8a4a4e6a0e9330f56", "7468697349734154657374537472696E67", "04468d123428fbee4d87635984e2337acd9a0d839cfaa62fc51b3bc4f0acf3a193eb0952c09b5a85c26b748653c118457402f39fdedcd242565f9f0ad2885727efc30b6f121c4f7701d8be16db04a87c2d6789e80d9632127eda638237007fb1d0e1ceb5c86e7ad74c4c4f0e4630468cfc9f7f97709c1d78fcdeeb63a85d8b9e6464")]
        [InlineData("26fcd113018abede12fe0cd4495e7c482cc2ae5e5adc7d796343367207386e3a", "746869736973416E6F7468657254657374537472696E67", "04c0d8d1a38f90dfd95241a588f4b2ebd50248a8645fa405907a23eb535ae343c5d60ea11d9c51d1a377e7dea591b300a84df6a1a2a8bdd76884463ea36dc20b98d0cf5cc50dc560d4cdf23c443500d2bb1651cf1bc7f8aeda89dd4af41d9f374f9f2aa757021c4728b5e99d24828c730822090930a5ed058c7a27952c0b35a2f6dd3a62713bcf28")]
        [InlineData("7ca3a3c603c89f69dcd1b1d4273d32309a244a62d50170cab5758ec1d30ad69a", "74686973497341007370656369616C436861725374722221110102", "04688bf887c557ed71c303e1ba9e83a02837a75db5ef2bbf6a7a0fc8b069f51af53fb00d2216ac2a02b3abf258cff26d9e22dcc4b12fefc60d7d22d6e029b3ac317df9a8255d412e8aa51f6369e9b111691b5948e52d7bd52d9b5d357c3887298e4673b6595ad98d4175dda559c446b036ee9425e7b79c8cb64c06d5f1f749a9345b7fe66d757eff3d425d7a")]
        [InlineData("1789cbcf61b1b4cc4961ccbe0d3e0304bd01a5370b7ed3f206b730a257900da8", "6E6F775468697349734C6F6E676572537472696E67576869636853686F756C64496E6372656D656E74546865436F756E746572496E41455331323843545221", "04d41b617134ea424f1c885b80889b8d5220f90b5331f72c67313e916801635c5fa73172a20bc53596f791a294bf174b7f62b5b335f4975f6f595ab2b1126080bf6cf97a3deb61249a74a6770118dffda30d8828ddacde500b0492b89639be93b8b0f46bed6efc6797d46f280217d67f69437668ba3d6b689b4752f11b905caec6edb6c10ee1fae90aac805df73c4597781c1d619e6d895627e48486aa20fa99b530eaf6ba8037c22c6aa6c203cec17b")]
        public void EciesDecryptStaticTest(string receiverPrivateKey, string expectedResult, string encryptedData)
        {
            // Generate a keypair
            EthereumEcdsa keypair = EthereumEcdsa.Create(receiverPrivateKey.HexToBytes(), EthereumEcdsaKeyType.Private);

            // Decrypt the encrypted data with the given key.
            byte[] decrypted = Ecies.Decrypt(keypair, encryptedData.HexToBytes(), null);

            // Verify the decrypted result was as expected.
            Assert.Equal(expectedResult.HexToBytes(), decrypted);
        }
    }
}
