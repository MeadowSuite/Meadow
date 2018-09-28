using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Trees;
using Meadow.EVM.Data_Types.Trees.Comparer;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.EVM.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Accounts
{
    public class Account : IRLPSerializable, ICloneable
    {
        #region Properties
        /// <summary>
        /// The nonce ensures that each transaction can only be processed once
        /// </summary>
        public BigInteger Nonce { get; set; }
        /// <summary>
        /// The current ether balance for the account.
        /// </summary>
        public BigInteger Balance { get; set; }
        /// <summary>
        /// The storage trie root hash used to obtain storage from the storage trie.
        /// </summary>
        public byte[] StorageRoot { get; set; }
        /// <summary>
        /// A hash of the code section for this account, used to access it from the database.
        /// </summary>
        public byte[] CodeHash { get; set; }

        /// <summary>
        /// The configuration we are using in the state that spawned this account.
        /// </summary>
        public Configuration.Configuration Configuration { get; private set; }
        public Trie StorageTrie { get; private set; }
        public Dictionary<Memory<byte>, byte[]> StorageCache { get; set; }
        /// <summary>
        /// Not an actual account property in Ethereum, but used by the Ethereum state to determine if an account has been modified.
        /// </summary>
        public bool IsDirty { get; set; }
        /// <summary>
        /// Not an actual account property in Ethereum, but used by the Ethereum state to track if an account is new or not.
        /// </summary>
        public bool IsNew { get; set; }
        /// <summary>
        /// Not an actual account property in Ethereum, but used by the Ethereum state to track if an account has been deleted since the last commit.
        /// </summary>
        public bool IsDeleted { get; set; }
        public bool IsBlank
        {
            get
            {
                return Nonce == Configuration.InitialAccountNonce && Balance == 0 && CodeHash.ValuesEqual(KeccakHash.BLANK_HASH);
            }
        }
        #endregion

        #region Constructor
        public Account(Configuration.Configuration configuration)
        {
            // Set our configuration
            Configuration = configuration;

            // Set our properties
            Nonce = Configuration.InitialAccountNonce;
            Balance = 0;
            StorageRoot = Trie.BLANK_NODE_HASH; // The hash of a blank RLP node (our initial value).
            CodeHash = KeccakHash.BLANK_HASH; // The hash of a zero length byte array (our initial value).

            // Initialize our storage change cache
            StorageCache = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Load our trie given our storage root
            StorageTrie = new Trie(Configuration.Database, StorageRoot);

            // Mark our account as a new account.
            IsNew = true;
        }

        public Account(Configuration.Configuration configuration, BigInteger nonce, BigInteger balance, byte[] storageRoot, byte[] codeHash)
        {
            // Set our configuration
            Configuration = configuration;

            // Set all of our properties.
            Nonce = nonce;
            Balance = balance;
            StorageRoot = storageRoot;
            CodeHash = codeHash;

            // Initialize our storage change cache
            StorageCache = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Load our trie given our storage root
            StorageTrie = new Trie(Configuration.Database, StorageRoot);
        }

        public Account(Configuration.Configuration configuration, byte[] rlpData)
        {
            // Set our configuration
            Configuration = configuration;

            // Deserialize our decoded RLP data.
            Deserialize(RLP.Decode(rlpData));
        }
        #endregion

        #region Functions
        public object Clone()
        {
            // Create a memberwise clone.
            Account clone = new Account(Configuration, Nonce, Balance, StorageRoot, CodeHash);

            // Copy other properties
            clone.IsDirty = IsDirty;
            clone.IsNew = IsNew;
            clone.IsDeleted = IsDeleted;

            // Copy our storage cache entries
            foreach (var key in StorageCache.Keys)
            {
                clone.StorageCache[key] = StorageCache[key];
            }

            // Return the clone.
            return clone;
        }

        public byte[] ReadStorage(byte[] key)
        {
            // If we already have cached storage data, return it. Otherwise we'll need to cache some.
            if (!StorageCache.TryGetValue(key, out var val))
            {
                // We obtain the value from our storage trie, cache it, and decode it, as it's RLP encoded.
                byte[] value = StorageTrie.Get(key);
                if (value == null)
                {
                    val = null;
                }
                else
                {
                    val = RLP.Decode(value);
                }

                StorageCache[key] = val;
            }

            // Return our storage key.
            return val;
        }

        public void WriteStorage(byte[] key, byte[] value)
        {
            // If our value has zero length, we set it to null
            if (value != null && value.Length == 0)
            {
                value = null;
            }

            // We add our changes to our cache
            StorageCache[key] = value;
        }

        public void CommitStorageChanges()
        {
            // For each storage cache item, we want to flush those changes to the main trie/database.
            foreach (var key in StorageCache.Keys)
            {
                // If the value is null, it means the value is non existent anymore, so we remove the key value pair
                if (StorageCache[key] == null)
                {
                    StorageTrie.Remove(key);
                }
                else
                {
                    // Otherwise we set the new value.
                    StorageTrie.Set(key, RLP.Encode(StorageCache[key]));
                }
            }

            // Now we clear our cache.
            StorageCache.Clear();

            // And update our account's storage root hash.
            StorageRoot = StorageTrie.GetRootNodeHash();
        }
        #endregion

        #region RLP Serialization
        /// <summary>
        /// Serializes the account into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP account.</returns>
        public RLPItem Serialize()
        {
            // We create a new RLP list that constitute this account.
            RLPList rlpAccount = new RLPList();

            // Add our nonce, balance, storage and code hash.
            rlpAccount.Items.Add(RLP.FromInteger(Nonce, EVMDefinitions.WORD_SIZE, true));
            rlpAccount.Items.Add(RLP.FromInteger(Balance, EVMDefinitions.WORD_SIZE, true));
            rlpAccount.Items.Add(StorageRoot);
            rlpAccount.Items.Add(CodeHash);

            // Return our rlp log item.
            return rlpAccount;
        }

        /// <summary>
        /// Deserializes the given RLP serialized account and sets all values accordingly.
        /// </summary>
        /// <param name="item">The RLP item to deserialize and obtain values from.</param>
        public void Deserialize(RLPItem item)
        {
            // Verify this is a list
            if (!item.IsList)
            {
                throw new ArgumentException();
            }

            // Verify it has 4 items.
            RLPList rlpAccount = (RLPList)item;
            if (rlpAccount.Items.Count != 4)
            {
                throw new ArgumentException();
            }

            // Verify the types of all items
            if (!rlpAccount.Items[0].IsByteArray ||
                !rlpAccount.Items[1].IsByteArray ||
                !rlpAccount.Items[2].IsByteArray ||
                !rlpAccount.Items[3].IsByteArray)
            {
                throw new ArgumentException();
            }

            // Set our nonce, balance, storage, and code hash.
            Nonce = RLP.ToInteger((RLPByteArray)rlpAccount.Items[0]);
            Balance = RLP.ToInteger((RLPByteArray)rlpAccount.Items[1]);
            StorageRoot = rlpAccount.Items[2];
            CodeHash = rlpAccount.Items[3];

            // Verify the length of our storage root and code hash.
            if (StorageRoot.Length != KeccakHash.HASH_SIZE || CodeHash.Length != KeccakHash.HASH_SIZE)
            {
                throw new ArgumentException();
            }

            // Initialize our storage change cache
            StorageCache = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Load our trie given our storage root
            StorageTrie = new Trie(Configuration.Database, StorageRoot);
        }
        #endregion
    }
}
