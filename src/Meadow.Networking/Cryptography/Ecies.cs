using Meadow.Core.AccountDerivation;
using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Meadow.Networking
{
    /// <summary>
    /// Implements the Elliptic Curve Integrated Encryption Scheme ("ECIES"), an asymmetrical encryption scheme.
    /// </summary>
    public abstract class Ecies
    {
        #region Fields
        private static SHA256 _sha256 = SHA256.Create();
        #endregion

        #region Functions
        public static byte[] Encrypt(EthereumEcdsa receiverPublicKey, byte[] data, byte[] sharedMacData)
        {
            // Split our data into its individual components.
            var test = Secp256k1Curve.DomainParameters.Curve.FieldSize;

            // Generate a random private key
            EthereumEcdsa privateKey = EthereumEcdsa.Generate(new SystemRandomAccountDerivation());

            // Generate the elliptic curve diffie hellman ("ECDH") shared key
            byte[] ecdhKey = privateKey.ComputeECDHKey(receiverPublicKey);

            // Perform NIST SP 800-56 Concatenation Key Derivation Function ("KDF")
            return null;
        }

        public static byte[] Decrypt(EthereumEcdsa privateKey, Memory<byte> encryptedData, byte[] sharedMacData)
        {
            // Verify our provided key type
            if (privateKey.KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("ECIES could not decrypt data because the provided key was not a private key.");
            }

            return null;
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
            byte[] allHashes = new byte[hashRounds * hashSize];
            Span<byte> allHashesMemory = allHashes;
            int allHashesOffset = 0;

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
                    hashResult.CopyTo(allHashesMemory.Slice(allHashesOffset, hashResult.Length));
                }
                else if (hashType == KDFHashAlgorithm.Keccak256)
                {
                    // Calculate the Keccak256 hash for this round.
                    KeccakHash.ComputeHash(dataToHash, allHashesMemory.Slice(allHashesOffset, KeccakHash.HASH_SIZE), KeccakHash.HASH_SIZE);
                }
                else
                {
                    throw new NotImplementedException();
                }

                // Advance our offset
                allHashesOffset += hashSize;

                // If our offset is passed our required key length, we can stop early
                if (allHashesOffset >= desiredKeyLength)
                {
                    break;
                }
            }

            // Slice off only the desired data
            return allHashesMemory.Slice(0, desiredKeyLength).ToArray();
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
