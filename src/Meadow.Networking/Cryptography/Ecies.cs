using Meadow.Core.AccountDerivation;
using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using Meadow.Networking.Cryptography.Aes;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Meadow.Networking.Cryptography
{
    /// <summary>
    /// Implements the Elliptic Curve Integrated Encryption Scheme ("ECIES"), an asymmetrical encryption scheme.
    /// </summary>
    public abstract class Ecies
    {
        #region Constants
        private const byte ECIES_HEADER_BYTE = 0x04;
        #endregion

        #region Fields
        private static SHA256 _sha256 = SHA256.Create();
        private static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        #endregion

        #region Functions
        public static byte[] Encrypt(EthereumEcdsa remotePublicKey, byte[] data, byte[] sharedMacData = null)
        {
            // If we have no shared mac data, we set it as a blank array
            sharedMacData = sharedMacData ?? Array.Empty<byte>();

            // Generate a random private key
            EthereumEcdsa senderPrivateKey = EthereumEcdsa.Generate(new SystemRandomAccountDerivation());

            // Generate the elliptic curve diffie hellman ("ECDH") shared key
            byte[] ecdhKey = senderPrivateKey.ComputeECDHKey(remotePublicKey);

            // Perform NIST SP 800-56 Concatenation Key Derivation Function ("KDF")
            Memory<byte> keyData = DeriveKeyKDF(ecdhKey, 32);

            // Split the AES encryption key and MAC from the derived key data.
            var aesKey = keyData.Slice(0, 16).ToArray();
            byte[] hmacSha256Key = keyData.Slice(16, 16).ToArray();
            hmacSha256Key = _sha256.ComputeHash(hmacSha256Key);

            // We generate a counter for our aes-128-ctr operation.
            byte[] counter = new byte[AesCtr.BLOCK_SIZE];
            _randomNumberGenerator.GetBytes(counter);

            // Encrypt the data accordingly.
            byte[] encryptedData = AesCtr.Encrypt(aesKey, data, counter);

            // Obtain the sender's public key to compile our message.
            byte[] localPublicKey = senderPrivateKey.ToPublicKeyArray(false, true);

            // We'll want to put this data into the message in the following order (where || is concatenation):
            // ECIES_HEADER_BYTE (1 byte) || sender's public key (64 bytes) || counter (16 bytes) || encrypted data (arbitrary length) || tag (32 bytes)
            // This gives us a total size of 113 + data.Length
            byte[] result = new byte[113 + encryptedData.Length];

            // Define a pointer and copy in our data as suggested.
            int offset = 0;
            result[offset++] = ECIES_HEADER_BYTE;
            Array.Copy(localPublicKey, 0, result, offset, localPublicKey.Length);
            offset += localPublicKey.Length;
            Array.Copy(counter, 0, result, offset, counter.Length);
            offset += counter.Length;
            Array.Copy(encryptedData, 0, result, offset, encryptedData.Length);
            offset += encryptedData.Length;

            // We still have to copy the tag, which is a HMACSHA256 of our counter + encrypted data + shared mac.

            // We copy the data into a buffer for this hash computation since counter + encrypted data are already aligned.
            byte[] tagPreimage = new byte[counter.Length + encryptedData.Length + sharedMacData.Length];
            Array.Copy(result, 65, tagPreimage, 0, counter.Length + encryptedData.Length);
            Array.Copy(sharedMacData, 0, tagPreimage, counter.Length + encryptedData.Length, sharedMacData.Length);

            // Obtain a HMACSHA256 provider
            HMACSHA256 hmacSha256 = new HMACSHA256(hmacSha256Key);

            // Compute a hash of our counter + encrypted data + shared mac data.
            byte[] tag = hmacSha256.ComputeHash(tagPreimage);

            // Copy the tag into our result buffer.
            Array.Copy(tag, 0, result, offset, tag.Length);
            offset += tag.Length;

            // Return the resulting data.
            return result;
        }

        public static byte[] Decrypt(EthereumEcdsa localPrivateKey, Memory<byte> message, Memory<byte> sharedMacData)
        {
            // Verify our provided key type
            if (localPrivateKey.KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("ECIES could not decrypt data because the provided key was not a private key.");
            }

            // Verify the size of our message (layout specified in Encrypt() describes this value)
            if (message.Length <= 113)
            {
                throw new ArgumentException("ECIES could not decrypt data because the provided data did not contain enough information.");
            }

            // Verify the first byte of our data
            int offset = 0;
            if (message.Span[offset++] != ECIES_HEADER_BYTE)
            {
                throw new ArgumentException("ECIES could not decrypt data because the provided data had an invalid header.");
            }

            // Extract the sender's public key from the data.
            Memory<byte> remotePublicKeyData = message.Slice(offset, 64);
            EthereumEcdsa remotePublicKey = EthereumEcdsa.Create(remotePublicKeyData, EthereumEcdsaKeyType.Public);
            offset += remotePublicKeyData.Length;
            Memory<byte> counter = message.Slice(offset, AesCtr.BLOCK_SIZE);
            offset += counter.Length;
            Memory<byte> encryptedData = message.Slice(offset, message.Length - offset - 32);
            offset += encryptedData.Length;
            byte[] tag = message.Slice(offset, message.Length - offset).ToArray();
            offset += tag.Length;

            // Generate the elliptic curve diffie hellman ("ECDH") shared key
            byte[] ecdhKey = localPrivateKey.ComputeECDHKey(remotePublicKey);

            // Perform NIST SP 800-56 Concatenation Key Derivation Function ("KDF")
            Memory<byte> keyData = DeriveKeyKDF(ecdhKey, 32);

            // Split the AES encryption key and MAC from the derived key data.
            var aesKey = keyData.Slice(0, 16).ToArray();
            byte[] hmacSha256Key = keyData.Slice(16, 16).ToArray();
            hmacSha256Key = _sha256.ComputeHash(hmacSha256Key);

            // Next we'll want to verify our tag (HMACSHA256 hash of counter + encrypted data + shared mac data).

            // We copy the data into a buffer for this hash computation since counter + encrypted data are already aligned.
            byte[] tagPreimage = new byte[counter.Length + encryptedData.Length + sharedMacData.Length];
            Memory<byte> tagPreimageMemory = tagPreimage;
            counter.CopyTo(tagPreimageMemory);
            encryptedData.CopyTo(tagPreimageMemory.Slice(counter.Length, encryptedData.Length));
            sharedMacData.CopyTo(tagPreimageMemory.Slice(counter.Length + encryptedData.Length));

            // Obtain a HMACSHA256 provider
            HMACSHA256 hmacSha256 = new HMACSHA256(hmacSha256Key);

            // Compute a hash of our counter + encrypted data + shared mac data.
            byte[] validTag = hmacSha256.ComputeHash(tagPreimage);

            // Verify our tag is valid
            if (!tag.SequenceEqual(validTag))
            {
                throw new ArgumentException("ECIES could not decrypt data because the hash/tag on the counter/encrypted data/shared mac data was invalid.");
            }

            // Decrypt the data accordingly.
            byte[] decryptedData = AesCtr.Decrypt(aesKey, encryptedData.ToArray(), counter.ToArray());

            // Return the decrypted data.
            return decryptedData;
        }

        /// <summary>
        /// Performs the NIST SP 800-56 Concatenation Key Derivation Function ("KDF") to derive a key of the specified desired length from a base key of arbitrary length.
        /// </summary>
        /// <param name="key">The base key to derive another key from.</param>
        /// <param name="desiredKeyLength">The desired key length of the resulting derived key.</param>
        /// <param name="hashType">The type of hash algorithm to use in the key derivation process.</param>
        /// <returns>Returns the key derived from the provided base key and hash algorithm.</returns>
        private static byte[] DeriveKeyKDF(byte[] key, int desiredKeyLength, KDFHashAlgorithm hashType = KDFHashAlgorithm.SHA256)
        {
            // References:
            // https://csrc.nist.gov/CSRC/media/Publications/sp/800-56a/archive/2006-05-03/documents/sp800-56-draft-jul2005.pdf

            // Define our block size and hash size
            int hashSize;
            if (hashType == KDFHashAlgorithm.SHA256)
            {
                hashSize = 32;
            }
            else if (hashType == KDFHashAlgorithm.Keccak256)
            {
                hashSize = KeccakHash.HASH_SIZE;
            }
            else
            {
                throw new NotImplementedException();
            }

            // Determine the amount of hashes required to generate a key of the desired length (ceiling by adding one less bit than needed to round up 1)
            int hashRounds = (desiredKeyLength + (hashSize - 1)) / hashSize;

            // Create a memory space to store all hashes for each round. The final key will slice from the start of this for all bytes it needs.
            byte[] aggregateHashData = new byte[hashRounds * hashSize];
            Span<byte> aggregateHashMemory = aggregateHashData;
            int aggregateHashOffset = 0;

            // Loop for each hash round to compute.
            for (int i = 0; i <= hashRounds; i++)
            {
                // Get the iteration count (starting from 1)
                byte[] counterData = BitConverter.GetBytes(i + 1);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(counterData);
                }

                // Get the data to hash in a single buffer
                byte[] dataToHash = counterData.Concat(key);

                // Determine what provider to use to hash the buffer.
                if (hashType == KDFHashAlgorithm.SHA256)
                {
                    // Calculate the SHA256 hash for this round.
                    byte[] hashResult = _sha256.ComputeHash(dataToHash);

                    // Copy it into our all hashes buffer.
                    hashResult.CopyTo(aggregateHashMemory.Slice(aggregateHashOffset, hashSize));
                }
                else if (hashType == KDFHashAlgorithm.Keccak256)
                {
                    // Calculate the Keccak256 hash for this round.
                    KeccakHash.ComputeHash(dataToHash, aggregateHashMemory.Slice(aggregateHashOffset, hashSize), KeccakHash.HASH_SIZE);
                }
                else
                {
                    throw new NotImplementedException();
                }

                // Advance our offset
                aggregateHashOffset += hashSize;

                // If our offset is passed our required key length, we can stop early
                if (aggregateHashOffset >= desiredKeyLength)
                {
                    break;
                }
            }

            // Slice off only the desired data
            return aggregateHashMemory.Slice(0, desiredKeyLength).ToArray();
        }
        #endregion

        #region Enums
        private enum KDFHashAlgorithm
        {
            Keccak256,
            SHA256
        }
        #endregion
    }
}
