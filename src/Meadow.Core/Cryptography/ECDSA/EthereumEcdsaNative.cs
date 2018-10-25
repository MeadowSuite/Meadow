using Meadow.Core.AccountDerivation;
using Meadow.Core.Utils;
using Secp256k1Net;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Meadow.Core.Cryptography.Ecdsa
{
    /// <summary>
    /// ECDSA cryptographic provider that accomodates for Ethereum signing standards, using the native secp256k1 library.
    /// </summary>
    public class EthereumEcdsaNative : EthereumEcdsa
    {
        private static readonly RandomNumberGenerator _secureRng;

        /// <summary>
        /// Enables the usage of the unmanaged C library to handle cryptographic operations.
        /// </summary>
        public Memory<byte> UnmanagedKey { get; }


        /// <summary>
        /// Our default static constructor that initializes any constant and shared values.
        /// </summary>
        static EthereumEcdsaNative()
        {
            _secureRng = RandomNumberGenerator.Create();
        }

        /// <summary>
        /// Initializes an ECDSA instance given a key and the type of key which it is.
        /// </summary>
        /// <param name="key">The key data for either a public or private key.</param>
        /// <param name="keyType">The type of key this provided key is.</param>
        public EthereumEcdsaNative(Memory<byte> key, EthereumEcdsaKeyType keyType)
        {
            // Set our type of key.
            KeyType = keyType;

            // If this is a public key, we will convert our type to uncompressed, sliced prefix (ethereum format)
            if (keyType == EthereumEcdsaKeyType.Public)
            {
                key = ConvertPublicKeyFormat(key, false, true);
            }

            // Obtain our expected size, and if our key exceeds that, we cut off the head.
            int expectedSize = keyType == EthereumEcdsaKeyType.Public ? PUBLIC_KEY_SIZE : PRIVATE_KEY_SIZE;
            if (key.Length > expectedSize)
            {
                UnmanagedKey = key.Slice(key.Length - expectedSize);
            }
            else if (key.Length < expectedSize)
            {
                // If it's too small, we add to it.
                Memory<byte> newKey = new byte[expectedSize];
                key.CopyTo(newKey.Slice(newKey.Length - key.Length));

                // And we update our key.
                UnmanagedKey = newKey;
            }
            else
            {
                UnmanagedKey = key;
            }
        }
        
        static EthereumEcdsaNative Generate(uint accountIndex, Secp256k1 secp256k1, IAccountDerivation accountFactory)
        {
            var privateKey = accountFactory.GeneratePrivateKey(accountIndex);
            if (!secp256k1.SecretKeyVerify(privateKey))
            {
                var errMsg = "Unmanaged EC library failed to valid private key. ";
                if (IncludeKeyDataInExceptions)
                {
                    errMsg += $"Private key: {privateKey.ToHexString()}";
                }

                throw new Exception(errMsg);
            }

            var keyBigInt = BigIntegerConverter.GetBigInteger(privateKey, signed: false, byteCount: PRIVATE_KEY_SIZE);
            keyBigInt = Secp256k1Curve.EnforceLowS(keyBigInt);
            privateKey = BigIntegerConverter.GetBytes(keyBigInt, PRIVATE_KEY_SIZE);

            return new EthereumEcdsaNative(privateKey, EthereumEcdsaKeyType.Private);
        }

        public static new EthereumEcdsaNative Generate(IAccountDerivation accountFactory)
        {
            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                return Generate(0, secp256k1, accountFactory);
            }
        }

        public static new IEnumerable<EthereumEcdsaNative> Generate(int count, IAccountDerivation accountFactory)
        {
            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                for (uint i = 0; i < count; i++)
                {
                    yield return Generate(i, secp256k1, accountFactory);
                }
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
        public static new EthereumEcdsaNative Recover(Span<byte> hash, byte recoveryId, BigInteger ecdsa_r, BigInteger ecdsa_s)
        {
            // Source: http://www.secg.org/sec1-v2.pdf (Section 4.1.6 - Public Key Recovery Operation)

            // Recovery ID must be between 0 and 4 (0 and 1 is all that should be used, but we support multiple cases in case)
            if (recoveryId < 0 || recoveryId > 3)
            {
                throw new ArgumentException($"ECDSA public key recovery must have a v parameter between [0, 3]. Value provided is {recoveryId.ToString(CultureInfo.InvariantCulture)}");
            }

            // NOTES:
            // First bit of recoveryID being set means y is odd, otherwise it is even.
            // The second bit indicates which item of the two to choose.

            // If the hash is null, we'll assume it's a zero length byte array
            if (hash == null)
            {
                hash = Array.Empty<byte>();
            }


            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                // Allocate memory for the signature and create a serialized-format signature to deserialize into our native format (platform dependent, hence why we do this).
                Span<byte> publicKeyOutput = new byte[PUBLIC_KEY_SIZE];
                Span<byte> serializedSignature = BigIntegerConverter.GetBytes(ecdsa_r).Concat(BigIntegerConverter.GetBytes(ecdsa_s));
                Span<byte> deserializedSignature = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                if (!secp256k1.RecoverableSignatureParseCompact(deserializedSignature, serializedSignature, recoveryId))
                {
                    var errMsg = "Unmanaged EC library failed to parse serialized signature. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"CompactSignature: {serializedSignature.ToHexString()}, RecoveryID: {recoveryId}";
                    }

                    throw new Exception(errMsg);
                }
                
                // Recovery from our deserialized signature.
                if (!secp256k1.Recover(publicKeyOutput, deserializedSignature, hash))
                {
                    var errMsg = "Unmanaged EC library failed to recover public key. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"Signature: {deserializedSignature.ToHexString()}, Message: {hash.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Serialize the public key
                Span<byte> serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if (!secp256k1.PublicKeySerialize(serializedKey, publicKeyOutput))
                {
                    var errMsg = "Unmanaged EC library failed to serialize public key. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"PublicKey: {publicKeyOutput.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Slice off any prefix.
                serializedKey = serializedKey.Slice(serializedKey.Length - PUBLIC_KEY_SIZE);

                // Obtain our public key from this
                return new EthereumEcdsaNative(serializedKey.ToArray(), EthereumEcdsaKeyType.Public);
            }

        }


        /// <summary>
        /// Verifies a hash was signed correctly given the r and s signature components.
        /// </summary>
        /// <param name="hash">The hash which was signed.</param>
        /// <param name="r">The ECDSA signature component r.</param>
        /// <param name="s">The ECDSA signature component s.</param>
        /// <returns>Returns a boolean indicating whether the data was properly signed.</returns>
        public override bool VerifyData(Span<byte> hash, BigInteger r, BigInteger s)
        {
            // Determine how to handle our verification.

            // TODO: Implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Signs given data and returns the r and s components of the ECDSA signature, along with a recovery ID to recover the public key given the original signed message and the returned components.
        /// </summary>
        /// <param name="hash">The hash to be signed.</param>
        /// <returns>Returns r and s components of an ECDSA signature, along with a recovery ID to recover the signers public key given the original signed message and r, s.</returns>
        public override (byte RecoveryID, BigInteger r, BigInteger s) SignData(Span<byte> hash)
        {
            // Verify we have a private key.
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw _notPrivateKeyException;
            }

            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                // Allocate memory for the signature and call our sign function.
                Span<byte> signature = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                if (!secp256k1.SignRecoverable(signature, hash, UnmanagedKey.Span))
                {
                    var errMsg = "Unmanaged EC library failed to sign data. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"MessageHash: {hash.ToHexString()}, SecretKey: {UnmanagedKey.Span.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Now we serialize our signature
                Span<byte> serializedSignature = new byte[Secp256k1.SERIALIZED_SIGNATURE_SIZE];
                if (!secp256k1.RecoverableSignatureSerializeCompact(serializedSignature, out var recoveryId, signature))
                {
                    var errMsg = "Unmanaged EC library failed to serialize signature. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"Signature: {signature.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Obtain our components.
                Span<byte> r = serializedSignature.Slice(0, 32);
                Span<byte> s = serializedSignature.Slice(32, 32);

                // Return them.
                return ((byte)recoveryId, BigIntegerConverter.GetBigInteger(r), BigIntegerConverter.GetBigInteger(s));
            }
        }

        /// <summary>
        /// Implements a hash algorithm function for Elliptic Curve Diffie Hellman, to be used to generate a shared secret.
        /// This hash algorithm does not hash the resulting data, but simply returns the X parameter as is expected.
        /// </summary>
        /// <param name="output">Outputs the X parameter of the result.</param>
        /// <param name="x">The X parameter of the pre-hashed result of ECDH.</param>
        /// <param name="y">The X parameter of the pre-hashed result of ECDH.</param>
        /// <param name="data">Arbitrary data that is passed through.</param>
        /// <returns>Returns the X parameter of the newly constructed ECDH key.</returns>
        private int ECDHHashAlgorithmNoHashReturnX(Span<byte> output, Span<byte> x, Span<byte> y, IntPtr data)
        {
            // Copy x to our output as is.
            x.Slice(0, output.Length).CopyTo(output);
            return 1;
        }

        /// <summary>
        /// Computes a shared secret among two keys using Elliptic Curve Diffie-Hellman ("ECDH"). Assumes this instance is of the private key, and requires a public key as input.
        /// </summary>
        /// <param name="publicKey">The public key to compute a shared secret for, using this current private key.</param>
        /// <returns>Returns a computed shared secret using this private key with the provided public key. Throws an exception if this instance is not a private key and the provided argument is not a public key.</returns>
        public override byte[] ComputeSharedSecret(EthereumEcdsa publicKey)
        {
            // Verify the types of keys
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("Could not calculate ECDH shared secret because called upon key was not a private key.");
            }

            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                // Allocate memory for the signature and call our sign function.
                byte[] result = new byte[ECDH_SHARED_SECRET_SIZE];

                // Define the public key array
                Span<byte> parsedPublicKeyData = new byte[PUBLIC_KEY_SIZE];

                // Parse our public key from the serialized data.
                byte[] prefixedPublicKey = publicKey.ToPublicKeyArray(false, false);
                if (!secp256k1.PublicKeyParse(parsedPublicKeyData, prefixedPublicKey))
                {
                    throw new Exception("Unmanaged EC library failed to deserialize public key.");
                }

                // Calculate the shared secret
                byte[] privateKeyData = ToPrivateKeyArray();
                if (!secp256k1.Ecdh(result, parsedPublicKeyData, privateKeyData, ECDHHashAlgorithmNoHashReturnX, IntPtr.Zero))
                {
                    throw new Exception("Failed to compute shared secret.");
                }

                return result;
            }
        }

        /// <summary>
        /// Converts a given public key (compressed/uncompressed or ethereum format (uncompressed without prefix)) to the specified format.
        /// </summary>
        /// <param name="publicKey">The public key to convert the format of.</param>
        /// <param name="compressed">If true, outputs a compressed public key.</param>
        /// <param name="slicedPrefix">If true, slices off the prefix byte from the public key.</param>
        /// <returns>Returns the provided public key, converted to the format specified.</returns>
        private static byte[] ConvertPublicKeyFormat(Memory<byte> publicKey, bool compressed = false, bool slicedPrefix = true)
        {
            // Define the public key array
            Span<byte> parsedPublicKey = new byte[PUBLIC_KEY_SIZE];

            // Add our uncompressed prefix to our key.
            byte[] prefixedPublicKey = null;
            if (publicKey.Length == Secp256k1.PUBKEY_LENGTH)
            {
                // We add our uncompressed prefix.
                prefixedPublicKey = new byte[] { 0x04 }.Concat(publicKey.ToArray());
            }
            else if (publicKey.Length == Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH ||
                publicKey.Length == Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
            {
                // The provided key is already prefixed, so we set it as is.
                prefixedPublicKey = publicKey.ToArray();
            }
            else
            {
                throw new ArgumentException($"Unmanaged EC library failed to normalize public key because key length was invalid ({publicKey.Length}).");
            }

            // Declare our serialized public key
            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                // Parse our public key from the serialized data.
                if (!secp256k1.PublicKeyParse(parsedPublicKey, prefixedPublicKey))
                {
                    var errMsg = "Unmanaged EC library failed to deserialize public key. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"PrefixedPublicKey: {prefixedPublicKey.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Serialize the public key
                int serializedKeyLength = compressed ? Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH : Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH;
                Flags serializedKeyFlags = compressed ? Flags.SECP256K1_EC_COMPRESSED : Flags.SECP256K1_EC_UNCOMPRESSED;
                Span<byte> serializedKey = new byte[serializedKeyLength];
                if (!secp256k1.PublicKeySerialize(serializedKey, parsedPublicKey, serializedKeyFlags))
                {
                    var errMsg = "Unmanaged EC library failed to serialize public key. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"PublicKey: {parsedPublicKey.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Slice off any prefix.
                if (slicedPrefix)
                {
                    serializedKey = serializedKey.Slice(1);
                }

                // Return it
                return serializedKey.ToArray();
            }
        }

        /// <summary>
        /// Obtains the binary data representation of our public key.
        /// </summary>
        /// <returns>Returns a binary data representation of the public key.</returns>
        public override byte[] ToPublicKeyArray(bool compressed = false, bool slicedPrefix = true)
        {
            // Throw an error if trying to slice prefix off of compressed public key
            if (compressed && slicedPrefix)
            {
                throw new ArgumentException("Should not be slicing the prefix off of a compressed public key, as compressed keys solely include X and Y is derived using the prefix.");
            }

            // Define the public key array
            Span<byte> publicKey = new byte[PUBLIC_KEY_SIZE];

            // Declare our serialized public key
            using (AutoObjectPool<Secp256k1>.Get(out var secp256k1))
            {
                // If this is the public key, we can simply return this
                if (KeyType == EthereumEcdsaKeyType.Public)
                {
                    // Add our uncompressed prefix to our key.
                    byte[] uncompressedPrefixedPublicKey = new byte[] { 0x04 }.Concat(UnmanagedKey.ToArray());

                    // Parse our public key from the serialized data.
                    if (!secp256k1.PublicKeyParse(publicKey, uncompressedPrefixedPublicKey))
                    {
                        var errMsg = "Unmanaged EC library failed to deserialize public key. ";
                        if (IncludeKeyDataInExceptions)
                        {
                            errMsg += $"PublicKey: {UnmanagedKey.ToHexString()}";
                        }

                        throw new Exception(errMsg);
                    }
                }
                else
                {
                    // Obtain the public key from the private key.
                    if (!secp256k1.PublicKeyCreate(publicKey, UnmanagedKey.Span))
                    {
                        var errMsg = "Unmanaged EC library failed to obtain public key from private key. ";
                        if (IncludeKeyDataInExceptions)
                        {
                            errMsg += $"PrivateKey: {UnmanagedKey.ToHexString()}";
                        }

                        throw new Exception(errMsg);
                    }
                }

                // Serialize the public key
                int serializedKeyLength = compressed ? Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH : Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH;
                Flags serializedKeyFlags = compressed ? Flags.SECP256K1_EC_COMPRESSED : Flags.SECP256K1_EC_UNCOMPRESSED;
                Span<byte> serializedKey = new byte[serializedKeyLength];
                if (!secp256k1.PublicKeySerialize(serializedKey, publicKey, serializedKeyFlags))
                {
                    var errMsg = "Unmanaged EC library failed to serialize public key. ";
                    if (IncludeKeyDataInExceptions)
                    {
                        errMsg += $"PublicKey: {publicKey.ToHexString()}";
                    }

                    throw new Exception(errMsg);
                }

                // Slice off any prefix.
                if (slicedPrefix)
                {
                    serializedKey = serializedKey.Slice(1);
                }

                // Return it
                return serializedKey.ToArray();
            }
        }

        /// <summary>
        /// Obtains a hex string representation of our public key.
        /// </summary>
        /// <returns>Returns a hex string representation of the public key.</returns>
        public string ToPublicKeyString()
        {
            // Obtain our bytes for Q.
            byte[] q = ToPublicKeyArray();

            // Return the hex string.
            return q.ToHexString();
        }

        /// <summary>
        /// Obtains the binary data representation of our private key.
        /// </summary>
        /// <returns>Returns a binary data representation of the private key.</returns>
        public override byte[] ToPrivateKeyArray()
        {
            // Verify we have a private key.
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw _notPrivateKeyException;
            }

            return UnmanagedKey.ToArray();

        }

        /// <summary>
        /// Obtains a hex string representation of our private key.
        /// </summary>
        /// <returns>Returns a hex string representation of the private key.</returns>
        public string ToPrivateKeyString()
        {
            // Get the hex string for the private key
            return ToPrivateKeyArray().ToHexString();
        }
    }
}
