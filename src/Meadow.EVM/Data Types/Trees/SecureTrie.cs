using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Databases;
using Meadow.EVM.Data_Types.Trees.Comparer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Data_Types.Trees
{
    /// <summary>
    /// Variation of the normally modified Merkle Patricia Tree which instead uses a keccak256 hash of it's keys as keys (to avoid DoS attacks using controlled lookups/stores of using similar key nibbles, we hash the key such that the path is less predictable).
    /// </summary>
    public class SecureTrie : Trie
    {
        #region Constructors
        public SecureTrie(BaseDB database = null) : base(database) { }
        public SecureTrie(BaseDB database, byte[] rootHash) : base(database, rootHash) { }
        #endregion

        #region Functions
        /// <summary>
        /// Given a key, obtains the corresponding value from our trie.
        /// </summary>
        /// <param name="key">The key to grab the corresponding value for.</param>
        /// <returns>Returns the value which corresponds to the provided key.</returns>
        public override byte[] Get(byte[] key)
        {
            // Use the hash of our provided key as a key.
            return base.Get(KeccakHash.ComputeHashBytes(key));
        }

        /// <summary>
        /// Given a key, sets the corresponding value in our trie.
        /// </summary>
        /// <param name="key">The key for which to set the corresponding value for.</param>
        /// <param name="value">The value to store in the trie for the provided key.</param>
        public override void Set(Memory<byte> key, byte[] value)
        {
            // Use the hash of our provided key as a key.
            Memory<byte> hash = new byte[KeccakHash.HASH_SIZE];
            KeccakHash.ComputeHash(key.Span, hash.Span);
            base.Set(hash, value);

            // Set our hash->key lookup in the database
            Database.Set(hash.ToArray(), key.ToArray());
        }

        /// <summary>
        /// Given a key, deletes the key-value entry from the trie.
        /// </summary>
        /// <param name="key">The key for which we'd like to remove the key-value entry.</param>
        public override void Remove(Memory<byte> key)
        {
            // Use the hash of our provided key as a key.
            Memory<byte> hash = new byte[KeccakHash.HASH_SIZE];
            KeccakHash.ComputeHash(key.Span, hash.Span);
            base.Remove(hash);
        }

        /// <summary>
        /// Obtains a dictionary representing all key-value pairs in our trie.
        /// </summary>
        /// <returns>Returns a dictionary which represents all key-value pairs in our trie.</returns>
        public override Dictionary<Memory<byte>, byte[]> ToDictionary()
        {
            // Obtain our dictionary from our base trie. This will be a hash->value lookup.
            var dictionary = base.ToDictionary();

            // We'll need to create a dictionary which represents key->value by resolving hash->key in our database.
            Dictionary<Memory<byte>, byte[]> result = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());
            foreach (var hash in dictionary.Keys)
            {
                // Obtain our key
                bool succeeded = Database.TryGet(hash.ToArray(), out var key);
                if (!succeeded)
                {
                    throw new Exception("Failed to obtain key from key hash in SecureTrie.");
                }

                // Set our value
                result[key] = dictionary[hash];
            }

            // Return the resulting dictionary
            return result;
        }
        #endregion
    }
}
