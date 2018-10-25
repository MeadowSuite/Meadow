using Meadow.Networking.Cryptography.Aes;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Meadow.Core.Utils;
using Xunit;

namespace Meadow.Networking.Test
{
    public class Aes128CtrTests
    {
        [Theory]
        [InlineData("okayAESstrTest")]
        [InlineData("test2")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.")]
        public void Aes128CtrEncryptDecrypt(string testString)
        {
            // Generate a random key.
            byte[] key = new byte[Aes128Ctr.KEY_SIZE];
            RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(key);

            // Obtain the data as bytes.
            byte[] testData = Encoding.UTF8.GetBytes(testString);

            // Encrypt then decrypt the data.
            byte[] encrypted = Aes128Ctr.Encrypt(key, testData);
            byte[] decrypted = Aes128Ctr.Decrypt(key, encrypted);

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
            // Generate a random key.
            byte[] key = "7b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] plainData = Encoding.UTF8.GetBytes(plainString);

            // Encrypt the provided data.
            byte[] resultData = Aes128Ctr.Encrypt(key, plainData, null);
            string resultHexString = resultData.ToHexString(false);

            // Assert the data is the same length
            Assert.Equal(plainData.Length, resultData.Length);

            // Verify the string equals the original string.
            Assert.Equal(encryptedHexString, resultHexString);
        }

        [Theory]
        [InlineData("okayAESstrTest", "b6cdb3e3d3ab41bffb750e2785ba")]
        [InlineData("test2", "adc3a1eea0")]
        [InlineData("and another \x00 test. but this one is a sentence with \x11 weird characters.", "b8c8b6baf3807db8e7622862f6ee8474c0ebb5bff2b33e8815c77fc44478a844a71916f77d63a39bd34cfa3122c35169c1da811b284d0b3eb4b1cfa9ea298dfa98571b365b0377")]
        public void Aes128CtrDecrypt(string plainString, string encryptedHexString)
        {
            // Generate a random key.
            byte[] key = "7b6dcbffad4bbbcd25e2a80201739233".HexToBytes();

            // Obtain the data as bytes.
            byte[] encryptedData = encryptedHexString.HexToBytes();

            // Encrypt the provided data.
            byte[] resultData = Aes128Ctr.Decrypt(key, encryptedData, null);
            string resultString = Encoding.UTF8.GetString(resultData);

            // Assert the data is the same length
            Assert.Equal(plainString.Length, resultString.Length);

            // Verify the string equals the original string.
            Assert.Equal(plainString, resultString);
        }
    }
}
