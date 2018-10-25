using Meadow.Networking.Cryptography.Aes;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Meadow.Core.Utils;
using Xunit;

namespace Meadow.Networking.Test
{
    public class AesCtrTests
    {
        [Theory]
        [InlineData("okayAESstrTest")]
        [InlineData("test2")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.")]
        public void Aes128CtrEncryptDecrypt(string testString)
        {
            // Generate a random key.
            byte[] key = new byte[16]; // 128 bit
            RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(key);

            // Obtain the data as bytes.
            byte[] testData = Encoding.UTF8.GetBytes(testString);

            // Encrypt then decrypt the data.
            byte[] encrypted = AesCtr.Encrypt(key, testData);
            byte[] decrypted = AesCtr.Decrypt(key, encrypted);

            // Get the decrypted result as a string
            string result = Encoding.UTF8.GetString(decrypted);

            // Verify the string equals the original string.
            Assert.Equal(testString, result);
        }


        [Theory]
        [InlineData("okayAESstrTest", "b6cdb3e3d3ab41bffb750e2785ba")]
        [InlineData("test2", "adc3a1eea0")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "b8c8b6baf3807db8e7622862f6ee8474c0ebb5bff2b33e8815c77fc44478a844a71916f77d63a39bd34cfa3122c35169c1da811b284d0b3eb4b1cfa9ea298dfa98571b365b0377")]
        public void Aes128CtrEncrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] plainData = Encoding.UTF8.GetBytes(plainString);

            // Encrypt the provided data.
            byte[] resultData = AesCtr.Encrypt(key, plainData, null);
            string resultHexString = resultData.ToHexString(false);

            // Assert the data is the same length
            Assert.Equal(plainData.Length, resultData.Length);

            // Verify the string equals the original string.
            Assert.Equal(encryptedHexString, resultHexString, true);
        }

        [Theory]
        [InlineData("okayAESstrTest", "b6cdb3e3d3ab41bffb750e2785ba")]
        [InlineData("test2", "adc3a1eea0")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "b8c8b6baf3807db8e7622862f6ee8474c0ebb5bff2b33e8815c77fc44478a844a71916f77d63a39bd34cfa3122c35169c1da811b284d0b3eb4b1cfa9ea298dfa98571b365b0377")]
        public void Aes128CtrDecrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] encryptedData = encryptedHexString.HexToBytes();

            // Decrypt the provided data.
            byte[] resultData = AesCtr.Decrypt(key, encryptedData, null);
            string resultString = Encoding.UTF8.GetString(resultData);

            // Assert the data is the same length
            Assert.Equal(plainString.Length, resultString.Length);

            // Verify the string equals the original string.
            Assert.Equal(plainString, resultString);
        }

        [Theory]
        [InlineData("okayAESstrTest")]
        [InlineData("test2")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.")]
        public void Aes192CtrEncryptDecrypt(string testString)
        {
            // Generate a random key.
            byte[] key = new byte[24]; // 192 bit
            RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(key);

            // Obtain the data as bytes.
            byte[] testData = Encoding.UTF8.GetBytes(testString);

            // Encrypt then decrypt the data.
            byte[] encrypted = AesCtr.Encrypt(key, testData);
            byte[] decrypted = AesCtr.Decrypt(key, encrypted);

            // Get the decrypted result as a string
            string result = Encoding.UTF8.GetString(decrypted);

            // Verify the string equals the original string.
            Assert.Equal(testString, result);
        }


        [Theory]
        [InlineData("okayAESstrTest", "910c44cb05f50b0a994e7b7c6d1c")]
        [InlineData("test2", "8a0256c676")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "9f09419225de370d85595d391e48e221672e9473175b04aed31cc9e00fd21837ce2daf1f74c7d00ca601874597c76fd3a1521bb81f4ba840c94414ec76ad065dba6b385d8e3d17")]
        public void Aes192CtrEncrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a802017392337b6dcbffad4bbbcd".HexToBytes();

            // Obtain the data as bytes.
            byte[] plainData = Encoding.UTF8.GetBytes(plainString);

            // Encrypt the provided data.
            byte[] resultData = AesCtr.Encrypt(key, plainData, null);
            string resultHexString = resultData.ToHexString(false);

            // Assert the data is the same length
            Assert.Equal(plainData.Length, resultData.Length);

            // Verify the string equals the original string.
            Assert.Equal(encryptedHexString, resultHexString, true);
        }

        [Theory]
        [InlineData("okayAESstrTest", "910c44cb05f50b0a994e7b7c6d1c")]
        [InlineData("test2", "8a0256c676")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "9f09419225de370d85595d391e48e221672e9473175b04aed31cc9e00fd21837ce2daf1f74c7d00ca601874597c76fd3a1521bb81f4ba840c94414ec76ad065dba6b385d8e3d17")]
        public void Aes192CtrDecrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a802017392337b6dcbffad4bbbcd".HexToBytes();

            // Obtain the data as bytes.
            byte[] encryptedData = encryptedHexString.HexToBytes();

            // Decrypt the provided data.
            byte[] resultData = AesCtr.Decrypt(key, encryptedData, null);
            string resultString = Encoding.UTF8.GetString(resultData);

            // Assert the data is the same length
            Assert.Equal(plainString.Length, resultString.Length);

            // Verify the string equals the original string.
            Assert.Equal(plainString, resultString);
        }

        [Theory]
        [InlineData("okayAESstrTest")]
        [InlineData("test2")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.")]
        public void Aes256CtrEncryptDecrypt(string testString)
        {
            // Generate a random key.
            byte[] key = new byte[32]; // 256 bit
            RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(key);

            // Obtain the data as bytes.
            byte[] testData = Encoding.UTF8.GetBytes(testString);

            // Encrypt then decrypt the data.
            byte[] encrypted = AesCtr.Encrypt(key, testData);
            byte[] decrypted = AesCtr.Decrypt(key, encrypted);

            // Get the decrypted result as a string
            string result = Encoding.UTF8.GetString(decrypted);

            // Verify the string equals the original string.
            Assert.Equal(testString, result);
        }


        [Theory]
        [InlineData("okayAESstrTest", "30335e267ead387259db28851ace")]
        [InlineData("test2", "2b3d4c2b0d")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "3e365b7f5e86047545cc0ec0699aabec5898515a97f726b2cb45fd6ba07b4169ef18eb735b39f2f9b6ad17d894a3f2b9461b9296a98753f7278987c830d94e7c24420871e38ec5")]
        public void Aes256CtrEncrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a802017392337b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] plainData = Encoding.UTF8.GetBytes(plainString);

            // Encrypt the provided data.
            byte[] resultData = AesCtr.Encrypt(key, plainData, null);
            string resultHexString = resultData.ToHexString(false);

            // Assert the data is the same length
            Assert.Equal(plainData.Length, resultData.Length);

            // Verify the string equals the original string.
            Assert.Equal(encryptedHexString, resultHexString, true);
        }

        [Theory]
        [InlineData("okayAESstrTest", "30335e267ead387259db28851ace")]
        [InlineData("test2", "2b3d4c2b0d")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "3e365b7f5e86047545cc0ec0699aabec5898515a97f726b2cb45fd6ba07b4169ef18eb735b39f2f9b6ad17d894a3f2b9461b9296a98753f7278987c830d94e7c24420871e38ec5")]
        public void Aes256CtrDecrypt(string plainString, string encryptedHexString)
        {
            // Obtain the key.
            byte[] key = "7b6dcbffad4bbbcd25e2a802017392337b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] encryptedData = encryptedHexString.HexToBytes();

            // Decrypt the provided data.
            byte[] resultData = AesCtr.Decrypt(key, encryptedData, null);
            string resultString = Encoding.UTF8.GetString(resultData);

            // Assert the data is the same length
            Assert.Equal(plainString.Length, resultString.Length);

            // Verify the string equals the original string.
            Assert.Equal(plainString, resultString);
        }

        /*
        * Test vectors for the following test obtained from:
        * https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf
        * */
        [Theory]
        [InlineData("2b7e151628aed2a6abf7158809cf4f3c", "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff", "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710", "874d6191b620e3261bef6864990db6ce9806f66b7970fdff8617187bb9fffdff5ae4df3edbd5d35e5b4f09020db03eab1e031dda2fbe03d1792170a0f3009cee")] // AES-128-CTR
        [InlineData("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b", "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff", "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710", "1abc932417521ca24f2b0459fe7e6e0b090339ec0aa6faefd5ccc2c6f4ce8e941e36b26bd1ebc670d1bd1d665620abf74f78a7f6d29809585a97daec58c6b050")] // AES-192-CTR
        [InlineData("603deb1015ca71be2b73aef0857d77811f352c073b6108d72d9810a30914dff4", "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff", "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710", "601ec313775789a5b7a7f504bbf3d228f443e3ca4d62b59aca84e990cacaf5c52b0930daa23de94ce87017ba2d84988ddfc9c58db67aada613c2dd08457941a6")] // AES-256-CTR
        public void AesCtrTestWithNISTVectors(string keyHexString, string counterHexString, string plainTextHexString, string cipherTextHexString)
        {
            // Obtain the components as bytes.
            byte[] key = keyHexString.HexToBytes();
            byte[] counter = counterHexString.HexToBytes();
            byte[] plainTextData = plainTextHexString.HexToBytes();

            // Encrypt the provided data.
            byte[] encryptedData = AesCtr.Encrypt(key, plainTextData, counter);
            string encryptedDataHexString = encryptedData.ToHexString(false);

            // Assert this equals our ciphered text
            Assert.Equal(cipherTextHexString, encryptedDataHexString, true);

            // Decrypt the data back to it's original format
            byte[] decryptedData = AesCtr.Decrypt(key, encryptedData, counter);
            string decryptedDataHexString = decryptedData.ToHexString(false);

            // Assert this equals our plain text
            Assert.Equal(plainTextHexString, decryptedDataHexString, true);
        }
    }
}
