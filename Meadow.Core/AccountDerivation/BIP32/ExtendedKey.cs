using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Core.AccountDerivation.BIP32
{
    /// <summary>
    /// Represents a hierarchal deterministic elliptic curve key, as defined by BIP32.
    /// </summary>
    public class ExtendedKey
    {
        #region Constants
        private const int SEED_DEFAULT_SIZE = 1024;
        private const int CHAIN_CODE_SIZE = 32;
        private const int FINGERPRINT_SIZE = 4;
        #endregion

        #region Fields
        private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();
        private static readonly RIPEMD160Managed RIPEMD160 = new RIPEMD160Managed();
        private static readonly SHA256 SHA256 = SHA256.Create();
        private static readonly byte[] SeedHMACKey = Encoding.ASCII.GetBytes("Bitcoin seed");
        #endregion

        #region Properties
        /// <summary>
        /// The underlying elliptic curve key to use to construct hierarchally deterministic child keys.
        /// </summary>
        public EthereumEcdsa InternalKey { get; private set; }

        /// <summary>
        /// The type of key this extended key represents.
        /// </summary>
        public EthereumEcdsaKeyType KeyType
        {
            get
            {
                return InternalKey.KeyType;
            }
        }

        /// <summary>
        /// The depth from the root key (where 1 indicates this is an immediate child of the root key).
        /// </summary>
        public uint Depth { get; private set; }
        /// <summary>
        /// The index of this key in its parent's child collection. This is the same as a "directory" item in a key path.
        /// Can represent a hardened index or not.
        /// </summary>
        public uint ChildIndex { get; private set; }

        /// <summary>
        /// Represents the key path leading to a hardened derived key.
        /// </summary>
        public bool Hardened
        {
            get
            {
                // Check if our index is hardened.
                return KeyPath.CheckHardenedDirectoryIndex(ChildIndex);
            }
        }

        /// <summary>
        /// Data used to help derive child keys.
        /// </summary>
        public byte[] ChainCode { get; private set; }

        /// <summary>
        /// A fingerprint calculated by this keys parent. Null if this key is the master (top-level) key.
        /// Used to verify parenthood of keys.
        /// </summary>
        public byte[] Fingerprint { get; private set; }
        #endregion

        #region Constructors
        public ExtendedKey(byte[] seed = null)
        {
            // If the seed is null, we generate a new one of the default size.
            if (seed == null)
            {
                seed = new byte[SEED_DEFAULT_SIZE];
                RandomNumberGenerator.GetBytes(seed);
            }

            // Initialize our key from a seed.
            InitializeFromSeed(seed);
        }

        public ExtendedKey(EthereumEcdsa key, byte[] chainCode, uint depth = 0, uint childIndex = 0, byte[] fingerprint = null)
        {
            // Set our key and chain code.
            InternalKey = key ?? throw new ArgumentNullException("Given key cannot be null when initializing extended key.");
            ChainCode = chainCode ?? throw new ArgumentNullException("Given chain code cannot be null when initializing extended key.");

            // Verify the size of our chain code.
            if (ChainCode.Length != CHAIN_CODE_SIZE)
            {
                throw new ArgumentException($"Given chain code was not of the expected size. Expected: {CHAIN_CODE_SIZE}, Actual: {ChainCode.Length}.");
            }

            // Set our other values
            Depth = depth;
            ChildIndex = childIndex;
            Fingerprint = fingerprint;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Initializes the extended key from the provided seed.
        /// </summary>
        /// <param name="seed">The seed to initialize this extended key from.</param>
        private void InitializeFromSeed(byte[] seed)
        {
            // Create a new HMACSHA512 instance
            HMACSHA512 hmacSha512 = new HMACSHA512(SeedHMACKey);

            // Compute the hash on our seed.
            byte[] hash = hmacSha512.ComputeHash(seed);

            // Set the key as the first 32 bytes of "hash"
            byte[] keyData = new byte[EthereumEcdsa.PRIVATE_KEY_SIZE];
            Array.Copy(hash, 0, keyData, 0, keyData.Length);
            InternalKey = EthereumEcdsa.Create(keyData, EthereumEcdsaKeyType.Private);

            // Initialize the chain code
            ChainCode = new byte[CHAIN_CODE_SIZE];

            // We derive the chain code as the data immediately following key data.
            Array.Copy(hash, 32, ChainCode, 0, ChainCode.Length);
        }

        /// <summary>
        /// Obtains the public key for this current key instance (if private, derives public, if public, returns as is).
        /// </summary>
        /// <returns>Returns this key if it is a public key, otherwise obtains the public key from this key.</returns>
        public ExtendedKey GetExtendedPublicKey()
        {
            // If this is already a public key, return itself
            if (KeyType == EthereumEcdsaKeyType.Public)
            {
                return this;
            }

            // This is a private key, so we derive our public key information
            // at this level from this private key.
            return new ExtendedKey(
                EthereumEcdsa.Create(InternalKey.ToPublicKeyArray(), EthereumEcdsaKeyType.Public),
                ChainCode, 
                Depth,
                ChildIndex,
                Fingerprint);
        }

        /// <summary>
        /// Computes the hash from which chain code and next key data is derived for the specified child.
        /// </summary>
        /// <param name="childIndex">The child key/index to derive a key for.</param>
        /// <param name="prefixedKeyData">The key data to use in computing the child hash, expected to be prefixed.</param>
        /// <returns>Returns the hash for the specified child from which the child chain code and key data is derived.</returns>
        internal byte[] ComputeChildHash(uint childIndex, byte[] prefixedKeyData)
        {
            // Convert our child index to data, big endian order.
            byte[] childIndexData = BitConverter.GetBytes(childIndex);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(childIndexData);
            }

            // Create a new HMACSHA512 instance
            HMACSHA512 hmacSha512 = new HMACSHA512(ChainCode);

            // Compute the hash on our data and child index.
            byte[] hash = hmacSha512.ComputeHash(prefixedKeyData.Concat(childIndexData));

            // Return the computed hash
            return hash;
        }

        /// <summary>
        /// Computes a hash for the current key to determine what its child's hash should be, in order to verify a supposed parent matches a supposed child.
        /// </summary>
        /// <returns>Returns a hash used as an extended key fingerprint.</returns>
        private byte[] GetChildFingerprint()
        {
            // Obtain our public key (compressed).
            byte[] publicKeyCompressed = InternalKey.ToPublicKeyArray(true, false);

            // Compute RIPEMD160(SHA256(compressed public key)).
            Memory<byte> hash = RIPEMD160.ComputeHash(SHA256.ComputeHash(publicKeyCompressed));

            // Obtain our fingerprint from our hash.
            return hash.Slice(0, FINGERPRINT_SIZE).ToArray();
        }

        /// <summary>
        /// Computes the child key and child chain code for a given child key/index relative from this extended key.
        /// </summary>
        /// <param name="index">The child key/index to derive a key/chain code for.</param>
        /// <returns>Returns a key and chain code for a child relative from this extended key. Derives a key of the same type as this key.</returns>
        private (EthereumEcdsa childKey, byte[] childChainCode) GetChildKeyInternal(uint index)
        {
            // Declare a hash we will obtain of our key.
            byte[] hash = null;

            // If this is a hardened directory/key/index
            if (KeyPath.CheckHardenedDirectoryIndex(index))
            {
                // Verify we aren't trying to derive a hardened key from a public key (since private is required).
                if (KeyType == EthereumEcdsaKeyType.Public)
                {
                    // Throw an exception because hardened keys mean we need the private key, but this is the public key.
                    throw new ArgumentException("Hierarchically deterministic child key cannot be derived from a public key when the child key index is hardened. Hardened keys can only be derived when the private key is known.");
                }

                // Obtain the message to hash. (0x00 byte prefixing the private key to pad it to 33 bytes long).
                byte[] hashMessage = new byte[] { 0 }.Concat(InternalKey.ToPrivateKeyArray());

                // Next we hash our key data.
                hash = ComputeChildHash(index, hashMessage);
            }
            else
            {
                // Compute the hash on our public key and index.
                hash = ComputeChildHash(index, InternalKey.ToPublicKeyArray(true, false));
            }

            // Set the child key data as the first 32 bytes of "hash"
            byte[] childKeyData = new byte[EthereumEcdsa.PRIVATE_KEY_SIZE];
            Array.Copy(hash, 0, childKeyData, 0, childKeyData.Length);

            // Initialize the child chain code
            byte[] childChainCode = new byte[CHAIN_CODE_SIZE];

            // We derive the child chain code as the data immediately following key data.
            Array.Copy(hash, 32, childChainCode, 0, childChainCode.Length);

            // Convert the key data to an integer
            BigInteger childKeyInt = BigIntegerConverter.GetBigInteger(childKeyData, false, childChainCode.Length);

            // If the child key is above N
            if (childKeyInt >= Secp256k1Curve.N)
            {
                throw new ArgumentException("Calculated child key value cannot exceed or equal N on the secp256k1 curve. Hierarchically deterministic child key cannot derive here. Try again.");
            }

            // Define our resulting key to obtain
            EthereumEcdsa childKey = null;

            // Obtain our child key depending on type.;
            if (KeyType == EthereumEcdsaKeyType.Public)
            {
                // Obtain our public key and add it to G * childKey
                var q = Secp256k1Curve.Parameters.Curve.DecodePoint(InternalKey.ToPublicKeyArray(true, false));
                q = Secp256k1Curve.Parameters.G.Multiply(childKeyInt.ToBouncyCastleBigInteger()).Add(q);
                if (q.IsInfinity)
                {
                    throw new ArgumentException("Calculated child key value point is infinity. This is a very rare occurrence. Hierarchically deterministic child key cannot derive here.");
                }

                // Normalize our point.
                q = q.Normalize();

                
                var p = Secp256k1Curve.DomainParameters.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());
                var encoded = p.GetEncoded(compressed: true);
                // Derive our child data.
                childKey = EthereumEcdsa.Create(encoded, EthereumEcdsaKeyType.Public);
            }
            else
            {
                // Add our private key to our parsed new key, mod N, to derive our new key.
                BigInteger computedChildKeyInt = (BigIntegerConverter.GetBigInteger(InternalKey.ToPrivateKeyArray()) + childKeyInt) % Secp256k1Curve.N;

                // Verify our computed child key is non-zero
                if (computedChildKeyInt == 0)
                {
                    throw new ArgumentException("Calculated child private key is zero. This is a very rare occurrence. Hierarchically deterministic child key cannot derive here.");
                }

                // Obtain our new key from this
                byte[] computedChildKeyData = BigIntegerConverter.GetBytes(computedChildKeyInt, EthereumEcdsa.PRIVATE_KEY_SIZE);

                // Initialize our key
                childKey = EthereumEcdsa.Create(computedChildKeyData, EthereumEcdsaKeyType.Private);
            }

            // Return our obtained data.
            return (childKey, childChainCode);
        }

        /// <summary>
        /// Obtains a child extended key, relative from this key, at the given child key/index.
        /// </summary>
        /// <param name="index">The child key/index to obtain the extended key for, relative from this extended key.</param>
        /// <returns>Returns a extended key for the provided child key/index, relative from this extended key.</returns>
        public ExtendedKey GetChildKey(uint index)
        {
            // Obtain our key and chain code for our child.
            (EthereumEcdsa childKey, byte[] childChainCode) = GetChildKeyInternal(index);

            // Initialize a new extended private key using this current key at the given child key.
            ExtendedKey extendedChildKey = new ExtendedKey(childKey, childChainCode, Depth + 1, index, GetChildFingerprint());

            // Return the extended child key.
            return extendedChildKey;
        }

        /// <summary>
        /// Obtains a child extended key, relative from this key, at the given key path.
        /// </summary>
        /// <param name="keyPath">The path of the child extended key to derive, relative from this extended key.</param>
        /// <returns>Returns an extended key for a child relative from this key, at the provided key path.</returns>
        public ExtendedKey GetChildKey(KeyPath keyPath)
        {
            // Declare our current private key.
            ExtendedKey current = this;

            // Loop for each index in the key path.
            foreach (uint index in keyPath.Indices)
            {
                current = current.GetChildKey(index);
            }

            // Return our private key
            return current;
        }

        /// <summary>
        /// Determines if the provided key is a child key to this current key.
        /// </summary>
        /// <param name="childKey">The key to determine is a child key or not.</param>
        /// <returns>Returns true if the provided key is a child to this key.</returns>
        public bool IsChild(ExtendedKey childKey)
        {
            // Verify depth
            if (childKey.Depth != Depth + 1)
            {
                return false;
            }

            // If the child's fingerprint is null, it is not a child.
            if (childKey.Fingerprint == null)
            {
                return false;
            }

            // Verify the fingerprint equals
            byte[] expectedChildFingerprint = GetChildFingerprint();
            return expectedChildFingerprint.SequenceEqual(childKey.Fingerprint);
        }

        /// <summary>
        /// Determines if the provided key is a parent key to this current key.
        /// </summary>
        /// <param name="parentKey">The key to determine is a parent key or not.</param>
        /// <returns>Returns true if the provided key is the parent to this key.</returns>
        public bool IsParent(ExtendedKey parentKey)
        {
            // Determine if this is a child of the parent key.
            return parentKey.IsChild(this);
        }

        /// <summary>
        /// Obtains the parent private key if provided the parent public key. This key must be a private key.
        /// </summary>
        /// <param name="parentPublicKey">The public key of the parent of this extended key.</param>
        /// <returns>Returns the private key of the parent of this extended key.</returns>
        public ExtendedKey GetParentPrivateKey(ExtendedKey parentPublicKey)
        {
            // Verify this key is a private key
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                // This key is not a private key.
                throw new ArgumentNullException("Could not obtain parent private key. Can only obtain the parent private key of a private key. This key is a public key.");
            }
            else if (parentPublicKey == null)
            {
                // The public key is null.
                throw new ArgumentNullException("Could not obtain parent private key. Provided parent public key argument is null.");
            }
            else if (parentPublicKey.KeyType == EthereumEcdsaKeyType.Private)
            {
                // The public key was not a public key.
                throw new ArgumentException("Could not obtain parent private key. Provided parent public key argument is not a public key.");
            }
            else if (Hardened)
            {
                throw new ArgumentException("Could not obtain parent private key if this key is a hardened key.");
            }
            else if (Depth == 0)
            {
                throw new ArgumentException("Could not obtain parent private key for this key because this key is a top level key.");
            }
            else if (!parentPublicKey.IsChild(this))
            {
                // The provided parent public key is not a parent.
                throw new ArgumentException("Could not obtain parent private key for this key because the provided parent public key argument is not a parent to this key.");
            }

            // Obtain the hash used to derive this current key, from the parent.
            byte[] hash = parentPublicKey.ComputeChildHash(ChildIndex, parentPublicKey.InternalKey.ToPublicKeyArray(true, false));

            // Set the child key data as the first 32 bytes of "hash"
            byte[] childKeyData = new byte[EthereumEcdsa.PRIVATE_KEY_SIZE];
            Array.Copy(hash, 0, childKeyData, 0, childKeyData.Length);

            // Initialize the child chain code
            byte[] childChainCode = new byte[CHAIN_CODE_SIZE];

            // We derive the child chain code as the data immediately following key data.
            Array.Copy(hash, 32, childChainCode, 0, childChainCode.Length);

            // Verify the chain code is equal
            if (!ChainCode.SequenceEqual(childChainCode))
            {
                throw new ArgumentException("Derived chain code from the parent at this key's child index that did not match this key's chain code.");
            }

            // Convert the key data to an integer
            BigInteger childKeyInt = BigIntegerConverter.GetBigInteger(childKeyData, false, childChainCode.Length);

            // Convert this private key to an integer
            byte[] thisKeyData = InternalKey.ToPrivateKeyArray();
            BigInteger thisKeyInt = BigIntegerConverter.GetBigInteger(thisKeyData, false, thisKeyData.Length);

            // Compute our parent key
            BigInteger parentPrivateKeyInt = ((thisKeyInt - childKeyInt) + Secp256k1Curve.N) % Secp256k1Curve.N;

            // Obtain our new key from this
            byte[] computedParentKeyData = BigIntegerConverter.GetBytes(parentPrivateKeyInt, EthereumEcdsa.PRIVATE_KEY_SIZE);

            // Obtain the parent private key
            EthereumEcdsa parentPrivateKey = EthereumEcdsa.Create(computedParentKeyData, EthereumEcdsaKeyType.Private);

            // Create the parent extended private key
            return new ExtendedKey(parentPrivateKey, parentPublicKey.ChainCode, parentPublicKey.Depth, parentPublicKey.ChildIndex, parentPublicKey.Fingerprint);
        }
        #endregion
    }
}
