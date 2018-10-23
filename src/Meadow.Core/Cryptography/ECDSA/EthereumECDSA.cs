using Meadow.Core.AccountDerivation;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Cryptography.Ecdsa
{
    public abstract class EthereumEcdsa
    {
        /// <summary>
        /// True to use the native secp256k1 lib, false to use the managed BouncyCastle lib.
        /// </summary>
        public static bool UseNativeLib = true;

        /// <summary>
        /// Set to true for public and private key data to be included in secp256k1 exceptions.
        /// Off by default since exceptions typically end up in logs and shared without security concerns.
        /// Useful for test/debug situations.
        /// </summary>
        public static bool IncludeKeyDataInExceptions = false;

        protected static ArgumentException _notPrivateKeyException = new ArgumentException("Public keys cannot be used to sign data. You must provide a private key instead.");
        public const int PRIVATE_KEY_SIZE = 32;
        public const int PUBLIC_KEY_SIZE = 64;
        public const int SIGNATURE_RS_SIZE = 64;
        public const int ECDH_SHARED_SECRET_SIZE = 32;

        /// <summary>
        /// The type of key that is available in this ECDSA instance.
        /// </summary>
        public EthereumEcdsaKeyType KeyType { get; protected set; }

        /// <summary>
        /// Obtains the binary data representation of our public key.
        /// </summary>
        /// <returns>Returns a binary data representation of the public key.</returns>
        public abstract byte[] ToPublicKeyArray(bool compressed = false, bool slicedPrefix = true);

        /// <summary>
        /// Obtains the binary data representation of our private key.
        /// </summary>
        /// <returns>Returns a binary data representation of the private key.</returns>
        public abstract byte[] ToPrivateKeyArray();


        /// <summary>
        /// Verifies a hash was signed correctly given the r and s signature components.
        /// </summary>
        /// <param name="hash">The hash which was signed.</param>
        /// <param name="r">The ECDSA signature component r.</param>
        /// <param name="s">The ECDSA signature component s.</param>
        /// <returns>Returns a boolean indicating whether the data was properly signed.</returns>
        public abstract bool VerifyData(Span<byte> hash, BigInteger r, BigInteger s);

        /// <summary>
        /// Signs given data and returns the r and s components of the ECDSA signature, along with a recovery ID to recover the public key given the original signed message and the returned components.
        /// </summary>
        /// <param name="hash">The hash to be signed.</param>
        /// <returns>Returns r and s components of an ECDSA signature, along with a recovery ID to recover the signers public key given the original signed message and r, s.</returns>
        public abstract (byte RecoveryID, BigInteger r, BigInteger s) SignData(Span<byte> hash);

        /// <summary>
        /// Computes a shared secret among two keys using Elliptic Curve Diffie-Hellman ("ECDH"). Assumes this instance is of the private key, and requires a public key as input.
        /// </summary>
        /// <param name="publicKey">The public key to compute a shared secret for, using this current private key.</param>
        /// <returns>Returns a computed shared secret using this private key with the provided public key. Throws an exception if this instance is not a private key and the provided argument is not a public key.</returns>
        public abstract byte[] ComputeSharedSecret(EthereumEcdsa publicKey);

        /// <summary>
        /// Initializes an ECDSA instance given a key and the type of key which it is.
        /// </summary>
        /// <param name="key">The key data for either a public or private key.</param>
        /// <param name="keyType">The type of key this provided key is.</param>
        public static EthereumEcdsa Create(Memory<byte> key, EthereumEcdsaKeyType keyType)
        {
            if (UseNativeLib)
            {
                return new EthereumEcdsaNative(key, keyType);
            }
            else
            {
                return new EthereumEcdsaBouncyCastle(key, keyType);
            }
        }

        /// <summary>
        /// Creates an ECDSA instance with a freshly generated keypair.
        /// </summary>
        /// <returns>Returns the ECDSA instance which has the generated keypair.</returns>
        public static EthereumEcdsa Generate(IAccountDerivation accountFactory = null)
        {
            // If the account factory is null, we use a random factory
            if (accountFactory == null)
            {
                accountFactory = new SystemRandomAccountDerivation();
            }

            // Determine which library to use
            if (UseNativeLib)
            {
                return EthereumEcdsaNative.Generate(accountFactory);
            }
            else
            {
                return EthereumEcdsaBouncyCastle.Generate(accountFactory);
            }
        }

        /// <summary>
        /// Creates an ECDSA instance with a freshly generated keypair.
        /// </summary>
        /// <returns>Returns the ECDSA instance which has the generated keypair.</returns>
        public static IEnumerable<EthereumEcdsa> Generate(int count, IAccountDerivation accountFactory)
        {
            if (UseNativeLib)
            {
                return EthereumEcdsaNative.Generate(count, accountFactory);
            }
            else
            {
                return EthereumEcdsaBouncyCastle.Generate(count, accountFactory);
            }
        }

        /// <summary>
        /// Creates an ECDSA instance by recovering a public key given a hash, recovery ID, and r and s components of the resulting signature of the hash. Throws an exception if recovery is not possible.
        /// </summary>
        /// <param name="hash">The hash of the data which was signed.</param>
        /// <param name="recoveryId">The recovery ID of ECDSA during signing.</param>
        /// <param name="ecdsa_r">The r component of the ECDSA signature for the provided hash.</param>
        /// <param name="ecdsa_s">The s component of the ECDSA signature for the provided hash.</param>
        /// <returns>Returns the quotient/public key which was used to sign this hash.</returns>
        public static EthereumEcdsa Recover(Span<byte> hash, byte recoveryId, BigInteger ecdsa_r, BigInteger ecdsa_s)
        {
            if (UseNativeLib)
            {
                return EthereumEcdsaNative.Recover(hash, recoveryId, ecdsa_r, ecdsa_s);
            }
            else
            {
                return EthereumEcdsaBouncyCastle.Recover(hash, recoveryId, ecdsa_r, ecdsa_s);
            }
        }


        /// <summary>
        /// Obtains a public key Keccak hash, often used in Ethereum to identify a sender/signer of a transaction.
        /// </summary>
        /// <returns>Returns the Keccak hash of the public key.</returns>
        public byte[] GetPublicKeyHash()
        {
            // Obtain our Q.
            byte[] q = ToPublicKeyArray();

            // Get the hash of the public key without prefix
            return KeccakHash.ComputeHashBytes(q);
        }

        /// <summary>
        /// Encodes the recovery ID (and an optional chain ID) into a v parameter.
        /// </summary>
        /// <param name="chainID">The optional chain ID to encode into v.</param>
        /// <param name="recoveryID">The recovery ID to encode into v.</param>
        /// <returns>Returns the v parameter with encoded recovery ID and chain ID.</returns>
        public static byte GetVFromRecoveryID(uint? chainID, byte recoveryID)
        {
            // Dependent on fork, chain ID may be embedded.
            if (chainID != null)
            {
                return (byte)((chainID * 2) + 35 + recoveryID);
            }
            else
            {
                return (byte)(recoveryID + 27);
            }
        }


        /// <summary>
        /// Decodes the recovery ID from the v parameter.
        /// </summary>
        /// <param name="v">The v which has the recovery ID embedded in it which we wish to extract.</param>
        /// <returns>Returns the recovery ID embedded in v.</returns>
        public static byte GetRecoveryIDFromV(byte v)
        {
            // Recovery ID is in V, which could also have ChainID and other things embedded, so we need special cases to extract it.
            // Geth also used 0, 1 before fixing, while Ethereum typically uses 27 or 28 if not past spurious dragon. If it is, it embeds it past 35.
            // We will transform all variants into 0, 1 variants. (There is support for [0,3] but 2,3 are not used.
            if (v >= 35)
            {
                // If it's above 35, we assume it has a chain ID embedded. Chain ID is multiplied by 2, so if it's odd, we know the recover ID should be 1, otherwise 0.
                return (byte)(1 - (v % 2));
            }
            else if (v >= 27)
            {
                // If it's above 27, we assume it's 27/28 so we just assume odd numbers are recovery ID 0 and even are 1 (since the first index 0 (even) started at 27 (odd))
                return (byte)((v - 27) % 2);
            }
            else
            {
                // Otherwise we'll just modulus divide by 2 to get a 0, 1 and hope it's right (this will also handle the Geth case of it using 0, 1).
                return (byte)(v % 2);
            }
        }

    }
}
