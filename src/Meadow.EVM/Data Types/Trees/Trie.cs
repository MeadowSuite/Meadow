using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Databases;
using Meadow.EVM.Data_Types.Trees.Comparer;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;

namespace Meadow.EVM.Data_Types.Trees
{
    /*
     * Source: https://github.com/ethereum/wiki/wiki/Patricia-Tree
     * */

    /// <summary>
    /// Represents a modified Merkle Patricia Tree used as a cryptographically authenticated key-value store, efficiently backed by a storage database for larger node data.
    /// </summary>
    public class Trie
    {
        #region Static Properties
        /// <summary>
        /// Defines a hash of a blank node
        /// </summary>
        public static byte[] BLANK_NODE_HASH
        {
            get
            {
                // A blank node is an empty byte array, so we the RLP encoding of that.
                return KeccakHash.ComputeHashBytes(RLP.Encode(BLANK_NODE));
            }
        }

        /// <summary>
        /// Defines a blank node.
        /// </summary>
        private static byte[] BLANK_NODE
        {
            get
            {
                return Array.Empty<byte>();
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// The key used to access the root node.
        /// </summary>
        private byte[] RootNodeHash { get; set; }
        private RLPList RootNode { get; set; }
        public BaseDB Database { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Loads a trie using a provided storage database and the hash of the root node which this trie should have.
        /// </summary>
        /// <param name="database">The storage database to store longer encoded trie nodes.</param>
        /// <param name="rootNodeHash">The hash of the root node to obtain and load.</param>
        public Trie(BaseDB database = null, byte[] rootNodeHash = null)
        {
            // Initialize our key lookup.
            Database = database ?? new BaseDB();

            // Load our trie root using the given root node hash or the blank one if none is provided.
            LoadRootNodeFromHash(rootNodeHash ?? BLANK_NODE_HASH);
        }
        #endregion

        #region Functions
        // Main functions
        /// <summary>
        /// Checks if a given key is in our trie.
        /// </summary>
        /// <param name="key">The key to check is contained in the trie.</param>
        /// <returns>Returns a boolean indicating if the key is in our trie.</returns>
        public bool Contains(byte[] key)
        {
            // If we can obtain a non-null key, then it exists.
            return Get(key) != null;
        }

        /// <summary>
        /// Given a key, gets the corresponding value from our trie. Returns null if none exists.
        /// </summary>
        /// <param name="key">The key to grab the corresponding value for.</param>
        /// <returns>Returns the value which corresponds to the provided key.</returns>
        public virtual byte[] Get(byte[] key)
        {
            // Recursively search downward through the node path using our key to find the value.
            return NodeGetValue(RootNode, key, 0);
        }

        /// <summary>
        /// Given a key, sets the corresponding value in our trie.
        /// </summary>
        /// <param name="key">The key for which to set the corresponding value for.</param>
        /// <param name="value">The value to store in the trie for the provided key.</param>
        public virtual void Set(Memory<byte> key, byte[] value) 
        {
            // Set the value and obtain the resulting root node, set it, and rehash it.
            RootNode = NodeUpdateValue(RootNode, key, 0, value);
            RehashRootNode();
        }

        /// <summary>
        /// Given a key, deletes the key-value entry from the trie.
        /// </summary>
        /// <param name="key">The key for which we'd like to remove the key-value entry.</param>
        public virtual void Remove(Memory<byte> key)
        {
            // Remove the value and obtain the resulting root node, set it, and rehash it.
            RootNode = NodeRemoveValue(RootNode, key);
            RehashRootNode();
        }

        /// <summary>
        /// Obtains a dictionary representing all key-value pairs in our trie.
        /// </summary>
        /// <returns>Returns a dictionary which represents all key-value pairs in our trie.</returns>
        public virtual Dictionary<Memory<byte>, byte[]> ToDictionary()
        {
            // Create a new dictionary
            Dictionary<Memory<byte>, byte[]> result = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

            // Obtain all pairs
            NodeGetAllPairs(RootNode, Array.Empty<byte>(), result);

            // Return our result
            return result;
        }

        // Root node
        /// <summary>
        /// Obtains the root node hash for the trie.
        /// </summary>
        /// <returns>The root node hash for the trie.</returns>
        public byte[] GetRootNodeHash()
        {
            // Return our root node hash.
            return RootNodeHash;
        }

        /// <summary>
        /// Returns a boolean indicating whether the provided node hash constitutes a blank node hash.
        /// </summary>
        /// <param name="nodeHash">The node hash to check is a blank node hash.</param>
        /// <returns>Returns a boolean indicating whether the provided node hash constitutes a blank node hash.</returns>
        private bool IsBlankNodeHash(byte[] nodeHash)
        {
            // If the hash is null, or matches the blank node hash, return true.
            return (nodeHash == null || nodeHash.Length == 0 || nodeHash.ValuesEqual(BLANK_NODE_HASH));
        }

        /// <summary>
        /// Loads the trie root from the database by looking up the provided trie root node hash.
        /// </summary>
        /// <param name="rootNodeHash">The trie root node hash used to look up and load the trie root from the database.</param>
        public void LoadRootNodeFromHash(byte[] rootNodeHash)
        {
            // If our provided root node is a blank one, set the root node and hash accordingly.
            if (IsBlankNodeHash(rootNodeHash))
            {
                RootNode = null;
                RootNodeHash = BLANK_NODE_HASH;
            }
            else
            {
                RootNode = DecodeNode(rootNodeHash);
                RootNodeHash = rootNodeHash;
            }
        }

        /// <summary>
        /// Recalculates the root node hash and sets it in the database accordingly.
        /// </summary>
        private void RehashRootNode()
        {
            // If our root node is null, we set the hash as the blank node hash, and exit.
            if (RootNode == null)
            {
                RootNodeHash = BLANK_NODE_HASH;
                return;
            }

            // Otherwise we get our value, and calculate our key (the hash of the value)
            byte[] value = RLP.Encode(RootNode);
            byte[] key = KeccakHash.ComputeHashBytes(value);

            // Update our root hash.
            RootNodeHash = key;

            // Set our value in our database.
            Database.Set(key, value);
        }

        // Helper functions to process individual nibbles in a byte array.
        /// <summary>
        /// Converts an array of bytes into an array of nibbles, thus the resulting array is twice as long and maximum values are 0xf.
        /// </summary>
        /// <param name="data">The array of bytes to convert into an array of nibbles.</param>
        /// <returns>Returns an array of nibbles.</returns>
        private byte[] ByteArrayToNibbleArray(Memory<byte> data, long nibbleStartIndex = 0)
        {
            // Our nibble array should be twice as long, minus our nibble start index.
            byte[] nibbles = new byte[(data.Length * 2) - nibbleStartIndex];

            // We'll loop for every byte
            for (int i = 0; i < nibbles.Length; i++)
            {
                // If it's an even nibble index, then it's a high nibble, otherwise a low nibble.
                long byteIndex = nibbleStartIndex + i;
                nibbles[i] = GetNibble(data, nibbleStartIndex + i);
            }

            // Return our nibble list.
            return nibbles;
        }

        /// <summary>
        /// Converts an array of nibbles into a byte array by pairing alternating nibbles as high/low nibbles in each byte.
        /// </summary>
        /// <param name="nibbles">The array of nibbles to convert into a byte array by pairing high and low nibbles.</param>
        /// <returns>Returns a byte array which represents the provided nibbles.</returns>
        private byte[] NibbleArrayToByteArray(byte[] nibbles)
        {
            // Our resulting byte array will be half the length of our nibble array.
            byte[] data = new byte[nibbles.Length / 2];

            // Loop for every byte
            for (int i = 0; i < data.Length; i++)
            {
                // Set each byte by ORing the left shifted high nibble, and the low nibble.
                data[i] = (byte)((nibbles[(i * 2)] << 4) | (nibbles[(i * 2) + 1]));
            }

            // Return our resulting byte array.
            return data;
        }

        /// <summary>
        /// Obtains a nibble from a byte array given a nibble index, where 0 is the high nibble of the first byte in the array, and the last item is the low nibble of the last byte.
        /// </summary>
        /// <param name="data">The byte array to obtain the nibble from.</param>
        /// <param name="index">The index of the nibble to obtain, where 0 is the high nibble of the first byte in the array, and the last/highest index is the low nibble of the last byte.</param>
        /// <returns>Returns the nibble from the byte array at the given nibble index.</returns>
        private byte GetNibble(Memory<byte> data, long index)
        {
            return (byte)((data.Span[(int)(index / 2)] >> (index % 2 == 0 ? 4 : 0)) & 0x0F);
        }

        // Node types/prefixes/unpacking
        /// <summary>
        /// Given a direct trie node, obtains the type of node.
        /// </summary>
        /// <param name="node">The node to obtain the type of.</param>
        /// <returns>Returns the type of node for the provided node.</returns>
        private TrieNodeType GetNodeType(RLPList node)
        {
            // If this node is null or has no items, we say it's a blank type.
            if (node == null || node.Items.Count == 0)
            {
                return TrieNodeType.Blank;
            }

            // If it has two items then it is either a leaf or an extension node. We can determine this based off of the use of a terminator.
            if (node.Items.Count == 2)
            {
                // Check our first nibble to obtain prefix, then return our type accordingly. This is faster than unpacking the whole nibble set.
                TrieNodePrefix prefix = (TrieNodePrefix)GetNibble(node.Items[0], 0);
                if (prefix == TrieNodePrefix.ExtensionNodeEven || prefix == TrieNodePrefix.ExtensionNodeOdd)
                {
                    return TrieNodeType.Extension;
                }
                else
                {
                    return TrieNodeType.Leaf;
                }
            }

            // Otherwise if it has 17 items, it's a branch node.
            else if (node.Items.Count == 17)
            {
                return TrieNodeType.Branch;
            }

            // Otherwise this node is not formatted properly.
            throw new ArgumentException("Failed to interpret node type from node prefix in trie.");
        }

        /// <summary>
        /// Given a trie node type, and a pair of nibbles, packs them into a prefix/shared-nibbles pair (extension) or prefix/key-remainder pair (leaf) respectively.
        /// </summary>
        /// <param name="type">The trie node type to pack into the first item of our node.</param>
        /// <param name="nibbles">The nibble array to pack into the first item of our node.</param>
        /// <returns>Returns a packed byte array which represents the new data for the first item of our node.</returns>
        private byte[] PackPrefixedNode(TrieNodeType type, byte[] nibbles)
        {
            // Determine the base prefix for the data (we set our first bit (odd/even) next).
            TrieNodePrefix prefix = TrieNodePrefix.ExtensionNodeEven;
            if (type == TrieNodeType.Leaf)
            {
                prefix = TrieNodePrefix.LeafNodeEven;
            }

            // We set the first bit based off of if it's even or odd length.
            bool oddLength = (nibbles.Length % 2) != 0;
            if (oddLength)
            {
                prefix = (TrieNodePrefix)((int)prefix | 1);
            }

            // If it's odd, just adding our prefix will make it byte-aligned. Otherwise we add a zero after the prefix.
            if (oddLength)
            {
                nibbles = new byte[] { (byte)prefix }.Concat(nibbles);
            }
            else
            {
                nibbles = new byte[] { (byte)prefix, 0 }.Concat(nibbles);
            }

            return NibbleArrayToByteArray(nibbles);
        }

        /// <summary>
        /// Given a packed byte array which represents data for the first item of a leaf or extension node, unpacks the data accordingly.
        /// </summary>
        /// <param name="data">The packed data for the node's first item.</param>
        /// <returns>Returns the unpacked data from the provided leaf or extension node's first item. This includes the type of node it is, and the nibble set that constitutes the shared nibbles (extension) or key remainder (leaf).</returns>
        private (TrieNodeType type, byte[] nibbles) UnpackPrefixedNode(byte[] data)
        {
            // Obtain our list of nibbles.
            byte[] nibbles = ByteArrayToNibbleArray(data);

            // Check our prefix and handle our data accordingly.
            TrieNodePrefix prefix = (TrieNodePrefix)nibbles[0];
            if (prefix == TrieNodePrefix.ExtensionNodeEven)
            {
                return (TrieNodeType.Extension, nibbles.Slice(2));
            }
            else if (prefix == TrieNodePrefix.ExtensionNodeOdd)
            {
                return (TrieNodeType.Extension, nibbles.Slice(1));
            }
            else if (prefix == TrieNodePrefix.LeafNodeEven)
            {
                return (TrieNodeType.Leaf, nibbles.Slice(2));
            }
            else if (prefix == TrieNodePrefix.LeafNodeOdd)
            {
                return (TrieNodeType.Leaf, nibbles.Slice(1));
            }

            // Throw our error otherwise.
            throw new ArgumentException("Attempted to unpack invalid trie node type!");
        }

        // RLP encoding/decoding trie nodes
        /// <summary>
        /// Given a trie node, encodes it into an RLP item such that if it is 32 bytes or less encoded, it is directly included, otherwise it will be a 32 byte reference to the data.
        /// </summary>
        /// <param name="node">The node to encode into an RLP item for storage in the trie.</param>
        /// <returns>Returns the RLP encoded trie node as it should be represented in our trie efficienctly.</returns>
        private RLPItem EncodeNode(RLPList node)
        {
            // If it's a blank node, we return blank.
            if (node == null || node.Items.Count == 0)
            {
                return BLANK_NODE;
            }

            // Obtain our node type
            TrieNodeType type = GetNodeType(node);
            byte[] encoded = RLP.Encode(node);

            // If our RLP encoded node is less than 32 bytes, we'll include the node directly as an RLP list.
            if (encoded.Length < 32)
            {
                return node;
            }

            // Otherwise if the data is 32 bytes or longer, we encode it as an RLP byte array with the hash to the node in the database.

            // Get our hash key
            byte[] hashKey = KeccakHash.ComputeHashBytes(encoded);

            // Set in our database.
            Database.Set(hashKey, encoded);

            // Return as an RLP byte array featuring the lookup hash/key for the encoded node.
            return hashKey;
        }

        /// <summary>
        /// Given a trie node directly or by reference, obtains the direct node to operate on.
        /// </summary>
        /// <param name="nodeOrReference">The trie node or trie node reference used to obtain the actual trie node.</param>
        /// <returns>Returns the representing trie node for this value.</returns>
        private RLPList DecodeNode(RLPItem nodeOrReference)
        {
            // If it's an RLP list, it's a direct representation of the node.
            if (nodeOrReference.IsList)
            {
                return (RLPList)nodeOrReference;
            }
            else
            {
                // If it's a RLP byte array/32 byte hash, it's the key used to look up the node (a reference)
                byte[] nodeHash = nodeOrReference;

                // If this matches our blank node hash, return our blank node.
                if (IsBlankNodeHash(nodeHash))
                {
                    return null;
                }

                // Otherwise we decode our node from RLP after fetching it from our database.
                if (!Database.TryGet(nodeHash, out var nodeData))
                {
                    throw new Exception($"Could not fetch node from database with node hash: {nodeHash.ToHexString(hexPrefix: true)}");
                }

                return (RLPList)RLP.Decode(nodeData);
            }
        }

        // Underlying trie implementation / helpers
        /// <summary>
        /// Checks if two given nodes are equal in structure.
        /// </summary>
        /// <param name="first">The first node to check structural equality of.</param>
        /// <param name="second">The second node to check structural equality of.</param>
        /// <returns>Returns a boolean indicating if the provided nodes are equal in structure and underlying values.</returns>
        private bool NodeEquals(RLPList first, RLPList second)
        {
            // If both are null, they're equal. If only one is, they aren't.
            if (first == null && second == null)
            {
                return true;
            }
            else if (first == null || second == null)
            {
                return false;
            }

            // Otherwise we treat it as a real node, encode, and compare.
            byte[] encodedFirst = RLP.Encode(first);
            byte[] encodedSecond = RLP.Encode(second);
            return encodedFirst.ValuesEqual(encodedSecond);
        }

        /// <summary>
        /// Given a node, duplicates it such that it is structurally the same, yet is a different object instance.
        /// </summary>
        /// <param name="node">The node to duplicate.</param>
        /// <returns>Returns a duplicate of the provided node.</returns>
        private RLPList NodeDuplicate(RLPList node)
        {
            // If the node is null, we return null.
            if (node == null)
            {
                return null;
            }

            // Otherwise we serialize and deserialize it to clone it.
            return (RLPList)RLP.Decode(RLP.Encode(node));
        }

        /// <summary>
        /// Given an trie node, traverses down all paths to obtain all key-value pairs in the trie.
        /// </summary>
        /// <param name="node">The trie node to recursively enumerate key-value pairs from.</param>
        /// <param name="currentNibbles">The current key nibbles that have been traversed up to this point.</param>
        /// <param name="result">The resulting dictionary created by obtaining all pairs from this node recursively downward.</param>
        private void NodeGetAllPairs(RLPList node, byte[] currentNibbles, Dictionary<Memory<byte>, byte[]> result)
        {
            // Obtain our node type
            TrieNodeType nodeType = GetNodeType(node);

            // Switch on node type
            switch (nodeType)
            {
                // If it's blank, return the blank node
                case TrieNodeType.Blank:
                    return;

                case TrieNodeType.Branch:
                    // Check we have data set
                    Memory<byte> valueData = ((RLPByteArray)node.Items[16]).Data;
                    if (valueData.Length > 0)
                    {
                        // Set our key-value pair at this node in our results
                        result[NibbleArrayToByteArray(currentNibbles)] = valueData.ToArray();
                    }

                    // Loop for every branch in this branch node (every sub item is a branch except for the last one,
                    // which is the value if no more nibbles are needed to index it).
                    for (byte branchIndex = 0; branchIndex < node.Items.Count - 1; branchIndex++)
                    {
                        // Obtain our node at the given index and obtain that item
                        RLPList branchNode = DecodeNode(node.Items[branchIndex]);

                        // Keep running down the branch recursively.
                        NodeGetAllPairs(branchNode, currentNibbles.Concat(new byte[] { branchIndex }), result);
                    }

                    return;

                case TrieNodeType.Extension:
                    // If it's an extension, we'll unpack the whole nibble set of our first item (prefix + key remainder)
                    byte[] sharedNibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // The second item in this node will represent the "next node", so we recursively obtain from that, advancing our key position as well.
                    RLPList nextNode = DecodeNode(node.Items[1]);
                    NodeGetAllPairs(nextNode, currentNibbles.Concat(sharedNibbles), result);
                    return;

                case TrieNodeType.Leaf:
                    // If it's a leaf, we'll unpack the whole nibble set of our first item (prefix + key remainder)
                    sharedNibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // Otherwise we passed verification, so we return the "value" that is stored in this leaf.
                    result[NibbleArrayToByteArray(currentNibbles.Concat(sharedNibbles))] = ((RLPByteArray)node.Items[1]).Data.ToArray();
                    return;
            }

            // Throw an exception if we somehow end up here.
            throw new ArgumentException("Could not obtain key-value pairs, invalid node type detected!");
        }

        /// <summary>
        /// Given an trie node, traverses down the appropriate node path to obtain the value for a given key and the current nibble index in our search.
        /// </summary>
        /// <param name="node">The current trie node to traverse through for our value, given our key and current nibble index.</param>
        /// <param name="key">The key for the value which we wish to obtain.</param>
        /// <param name="currentNibbleIndex">The index of the current nibble in our key which we are at in our indexing process.</param>
        /// <returns>Returns the value stored in the trie for the provided key, or null if it does not exist.</returns>
        private byte[] NodeGetValue(RLPList node, Memory<byte> key, int currentNibbleIndex = 0)
        {
            // Obtain our node type
            TrieNodeType nodeType = GetNodeType(node);

            // Switch on node type
            switch (nodeType)
            {
                // If it's blank, return the blank node
                case TrieNodeType.Blank:
                    return null;

                case TrieNodeType.Branch:
                    // If it's a branch and the key to search for has ended, then we've found our result in this node.
                    if (key.Length * 2 == currentNibbleIndex)
                    {
                        // Return the "value" portion of our node, the 17th item.
                        return ((RLPByteArray)node.Items[16]).Data.ToArray();
                    }

                    // Obtain our next nibble which constitutes branch index.
                    byte branchIndex = GetNibble(key, currentNibbleIndex);

                    // Obtain our node at the given index and obtain that item
                    RLPList branchNode = DecodeNode(node.Items[branchIndex]);

                    // Keep running down the branch recursively.
                    return NodeGetValue(branchNode, key, currentNibbleIndex + 1);

                case TrieNodeType.Extension:
                    // If it's an extension, we'll unpack the whole nibble set of our first item (prefix + key remainder)
                    byte[] nibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // If there's less of the key than nibbles in this extension, it's not here.
                    if ((key.Length * 2) - currentNibbleIndex < nibbles.Length)
                    {
                        return null;
                    }

                    // Any "shared nibbles" we find in this nibble set should match the current length of our key.
                    for (int i = 0; i < nibbles.Length; i++)
                    {
                        if (nibbles[i] != GetNibble(key, currentNibbleIndex + i))
                        {
                            return null;
                        }
                    }

                    // The second item in this node will represent the "next node", so we recursively obtain from that, advancing our key position as well.
                    RLPList nextNode = DecodeNode(node.Items[1]);
                    return NodeGetValue(nextNode, key, currentNibbleIndex + nibbles.Length);

                case TrieNodeType.Leaf:
                    // If it's a leaf, we'll unpack the whole nibble set of our first item (prefix + key remainder)
                    nibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // And we'll want to verify the remainder of our key, first we verify length.
                    if ((key.Length * 2) - currentNibbleIndex != nibbles.Length)
                    {
                        return null;
                    }

                    // Then we make sure the rest of the key sequence matches
                    for (int i = 0; i < nibbles.Length; i++)
                    {
                        if (nibbles[i] != GetNibble(key, currentNibbleIndex + i))
                        {
                            return null;
                        }
                    }

                    // Otherwise we passed verification, so we return the "value" that is stored in this leaf.
                    return node.Items[1];
            }

            // Throw an exception if we somehow end up here.
            throw new ArgumentException("Could not obtain node, invalid node type!");
        }

        /// <summary>
        /// Given a trie node, traverses down the appropriate node path, updating the value for a given key and the current nibble index for our key in the indexing process.
        /// </summary>
        /// <param name="node">The current trie node to traverse down to update our value, given our key and current nibble index.</param>
        /// <param name="key">The key for the value which we wish to update.</param>
        /// <param name="currentNibbleIndex">The index of the current nibble in our key which we are at in our indexing process.</param>
        /// <param name="value">The value which we wish to set for the provided key in our trie.</param>
        /// <returns>Returns a node with the given update which is to be used as the replacement for the provided node.</returns>
        private RLPList NodeUpdateValue(RLPList node, Memory<byte> key, int currentNibbleIndex, byte[] value)
        {
            // Obtain the node type
            TrieNodeType nodeType = GetNodeType(node);

            // Switch on our node type
            switch (nodeType)
            {
                case TrieNodeType.Blank:
                    // Obtain the remainder of our key as a nibble sequence.
                    byte[] keyNibbles = ByteArrayToNibbleArray(key, currentNibbleIndex);
                    // Since this node is blank, we turn it into a leaf with the key remainder in it.
                    return new RLPList(PackPrefixedNode(TrieNodeType.Leaf, keyNibbles), value);

                case TrieNodeType.Branch:
                    // If we reached the end of key, this node is the destination to set the value in.
                    if (currentNibbleIndex == key.Length * 2)
                    {
                        // The key ended, so we set the value here.
                        node.Items[16] = value;
                        return node;
                    }
                    else
                    {
                        // Obtain our branch index and node
                        int branchIndex = GetNibble(key, currentNibbleIndex);
                        RLPList branchNode = DecodeNode(node.Items[branchIndex]);

                        // Update the value down this path, and obtain the new branch node.
                        RLPList newBranchNode = NodeUpdateValue(branchNode, key, currentNibbleIndex + 1, value);

                        // Set the branch node
                        node.Items[branchIndex] = EncodeNode(newBranchNode);
                        return node;
                    }

                case TrieNodeType.Leaf:
                case TrieNodeType.Extension:

                    // Obtain the key nibbles from the node.
                    byte[] nodeNibbles = UnpackPrefixedNode(node.Items[0]).nibbles;
                    keyNibbles = ByteArrayToNibbleArray(key, currentNibbleIndex);

                    // Next we'll want to count all of the shared nibbles among the node nibbles and our key. We'll count all common nibbles.
                    int sharedNibbles = 0;
                    int nibbleCount = Math.Min(nodeNibbles.Length, keyNibbles.Length);
                    for (int i = 0; i < nibbleCount; i++)
                    {
                        // If any nibble doesn't match, we can stop counting.
                        if (keyNibbles[i] != nodeNibbles[i])
                        {
                            break;
                        }

                        // Otherwise keep counting all shared nibbles.
                        sharedNibbles++;
                    }

                    // We'll create our node here to handle specific cases.
                    RLPList newNode = null;

                    // If our node and key nibbles are all shared
                    if (sharedNibbles == keyNibbles.Length && nodeNibbles.Length == keyNibbles.Length)
                    {
                        // And if our node type was a leaf, then we simply return a leaf with the value we provided (instead of any existing value)
                        if (nodeType == TrieNodeType.Leaf)
                        {
                            return new RLPList(node.Items[0], value);
                        }

                        // Otherwise this is an extension, so we decode our next node and update that.
                        RLPList nextNode = DecodeNode(node.Items[1]);
                        newNode = NodeUpdateValue(nextNode, key, currentNibbleIndex + sharedNibbles, value);
                    }

                    // If all of our node nibbles are done, yet we still have key nibbles
                    else if (sharedNibbles == nodeNibbles.Length)
                    {
                        if (nodeType == TrieNodeType.Leaf)
                        {
                            // If our node type is a leaf and we still have key nibbles, our new node type will be a branch node.
                            newNode = new RLPList();
                            for (int i = 0; i < 16; i++)
                            {
                                newNode.Items.Add(BLANK_NODE);
                            }

                            // We add the leaf's value as the branch's value.
                            newNode.Items.Add(node.Items[1]);

                            // We create a new leaf that this branch node will connect to with the remainder of our key, and store our value there.
                            RLPList newLeafNode = new RLPList(PackPrefixedNode(TrieNodeType.Leaf, keyNibbles.Slice(sharedNibbles + 1)), value);

                            // Set our branch to our new leaf node
                            int branchIndex = keyNibbles[sharedNibbles];
                            newNode.Items[branchIndex] = EncodeNode(newLeafNode);
                        }
                        else
                        {
                            // This is an extension, we simply update the next node
                            RLPList nextNode = DecodeNode(node.Items[1]);
                            newNode = NodeUpdateValue(nextNode, key, currentNibbleIndex + sharedNibbles, value);
                        }
                    }
                    else
                    {
                        // Both the node and the key have nibbles they didn't agree on.
                        newNode = new RLPList();
                        for (int i = 0; i < 17; i++)
                        {
                            newNode.Items.Add(BLANK_NODE);
                        }

                        // Obtain our branch index for our node's nibble
                        int branchNodeIndex = nodeNibbles[sharedNibbles];

                        // If this is an extension and there's one node nibble left.
                        if (nodeType == TrieNodeType.Extension && nodeNibbles.Length - sharedNibbles == 1)
                        { 
                            // We set this branch to the extension's next node.
                            newNode.Items[branchNodeIndex] = node.Items[1];
                        }
                        else
                        {
                            // Whatever type of node this was before, we put it into our branch slot and advance it a key/strip a key nibble for the new subnode.
                            RLPList subNodeReplacement = new RLPList(PackPrefixedNode(nodeType, nodeNibbles.Slice(sharedNibbles + 1)), node.Items[1]);
                            newNode.Items[branchNodeIndex] = EncodeNode(subNodeReplacement);
                        }

                        // If our key nibbles are empty, we can set our value in this branch.
                        if (keyNibbles.Length - sharedNibbles == 0)
                        {
                            newNode.Items[16] = value;
                        }
                        else
                        {
                            // Otherwise we take our next nibble from our key to decide the branch index for our other path.
                            int branchKeyIndex = keyNibbles[sharedNibbles];

                            // Create our new leaf node to store the remainder of our key and value.
                            RLPList keyLeafNode = new RLPList(PackPrefixedNode(TrieNodeType.Leaf, keyNibbles.Slice(sharedNibbles + 1)), value);
                            newNode.Items[branchKeyIndex] = EncodeNode(keyLeafNode);
                        }
                    }

                    // If we had any shared nibbles between our node's nibbles and our current key's nibbles, we create an extension for that accordingly and link it our created node below it.
                    if (sharedNibbles > 0)
                    {
                        return new RLPList(PackPrefixedNode(TrieNodeType.Extension, nodeNibbles.Slice(0, sharedNibbles)), EncodeNode(newNode));
                    }
                    else
                    {
                        // Otherwise we just return the created node directly.
                        return newNode;
                    }
            }

            // If we somehow end up here, throw an exception.
            throw new ArgumentException("Unexpected node type while updating trie nodes.");
        }

        /// <summary>
        /// Given a trie node, traverses down the appropriate node path, removing the key/value for a given key and the current nibble index for our key in the indexing process.
        /// </summary>
        /// <param name="node">The current trie node to traverse down to remove our key/value, given our key and current nibble index.</param>
        /// <param name="key">The key for the key/value which we wish to remove.</param>
        /// <param name="currentNibbleIndex">The index of the current nibble in our key which we are at in our indexing process.</param>
        /// <returns>Returns a node with the given removal which is to be used as the replacement for the provided node.</returns>
        private RLPList NodeRemoveValue(RLPList node, Memory<byte> key, int currentNibbleIndex = 0)
        {
            // Obtain the node type
            TrieNodeType nodeType = GetNodeType(node);

            // Switch on our node type
            switch (nodeType)
            {
                // If it's a blank node, we return the blank node.
                case TrieNodeType.Blank:
                    return null;

                case TrieNodeType.Branch:

                    // If we reached the end of key, we remove the value from here (since our value for this key resides here), and we cleanup/reformat this branch.
                    if (currentNibbleIndex == key.Length * 2)
                    {
                        node.Items[16] = EncodeNode(null);
                        return CleanupBranch(node);
                    }

                    // Otherwise we obtain delete down the branch path, so we obtain the branch node.
                    byte branchNodeIndex = GetNibble(key, currentNibbleIndex);
                    RLPList branchNode = DecodeNode(node.Items[branchNodeIndex]);

                    // Delete by going down the branch node, and obtain the resulting branch node.
                    RLPList newBranchNode = NodeRemoveValue(NodeDuplicate(branchNode), key, currentNibbleIndex + 1);

                    // If the new node is the same as the old, we can return our node as it is. This node only changes if the path below changed.
                    if (NodeEquals(branchNode, newBranchNode))
                    {
                        return node;
                    }

                    // Otherwise we set our updated now in our branch index.
                    node.Items[branchNodeIndex] = EncodeNode(newBranchNode);

                    // If our new node is null, then should check if this node might need to be fixed up, otherwise we can return the node as it is.
                    if (newBranchNode == null)
                    {
                        return CleanupBranch(node);
                    }
                    else
                    {
                        return node;
                    }

                case TrieNodeType.Leaf:

                    // Obtain our key remainder nibbles from our leaf.
                    byte[] nibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // If the remainder of our key doesnt match the key remainder on the leaf, return the node as it is, it isn't the leaf we were searching for.
                    if ((key.Length * 2) - currentNibbleIndex != nibbles.Length)
                    {
                        return node;
                    }

                    // Any key remainder nibbles we find in this nibble set should match the remainder of our key.
                    for (int i = 0; i < nibbles.Length; i++)
                    {
                        if (nibbles[i] != GetNibble(key, currentNibbleIndex + i))
                        {
                            return node;
                        }
                    }

                    // We can now confirm the key has matched, hence this is the leaf we're searching for, and we return a blank node to replace it.
                    return null;

                case TrieNodeType.Extension:
                    // Obtain our key shared nibbles from our extension.
                    nibbles = UnpackPrefixedNode(node.Items[0]).nibbles;

                    // If the remainder of our key is smaller than the shared nibbles, return the node as it is, the key doesn't exist down this path.
                    if ((key.Length * 2) - currentNibbleIndex < nibbles.Length)
                    {
                        return node;
                    }

                    // Any shared nibbles we find in this nibble set should match the remainder of our key.
                    for (int i = 0; i < nibbles.Length; i++)
                    {
                        if (nibbles[i] != GetNibble(key, currentNibbleIndex + i))
                        {
                            return node;
                        }
                    }

                    // Obtain our decoded next node.
                    RLPList nextNode = DecodeNode(node.Items[1]);
                    RLPList newNextNode = NodeRemoveValue(NodeDuplicate(nextNode), key, currentNibbleIndex + nibbles.Length);

                    // If the next node didn't change at all, we can simply keep this node as it is.
                    if (NodeEquals(nextNode, newNextNode))
                    {
                        return node;
                    }

                    // If our new next node is null, then an extension shouldn't exist here, so we return null, changes should propogate changes upwards.
                    if (newNextNode == null)
                    {
                        return null;
                    }

                    // Get our new next node type
                    TrieNodeType newNodeType = GetNodeType(newNextNode);

                   // If the new node type is a branch..
                    if (newNodeType == TrieNodeType.Branch)
                    {
                        // We return a fixed up extension, with our new encoded next node.
                        return new RLPList(PackPrefixedNode(TrieNodeType.Extension, nibbles), EncodeNode(newNextNode));
                    }
                    else
                    {
                        // Obtain the subnodes prefix/nibbles
                        var subNodePrefixNibbles = UnpackPrefixedNode(newNextNode.Items[0]);

                        // If the new node type is an extension or leaf, then we instead join it with this extension.
                        return new RLPList(PackPrefixedNode(subNodePrefixNibbles.type, nibbles.Concat(subNodePrefixNibbles.nibbles)), newNextNode.Items[1]);
                    }
            }

            // If we somehow end up here, throw an exception.
            throw new ArgumentException("Unexpected node type while removing trie nodes.");
        }

        /// <summary>
        /// Given a branch node that has had a branch path removed, reviews the node to see if it should be another type, and returns a replacement node for the provided node.
        /// </summary>
        /// <param name="node">The branch node to clean up and replace with an updated/reformatted node.</param>
        /// <returns>Returns an updated/reformatted node which accounts for a deleted branch path, possibly homogenizing the downward path, such that it should be a leaf instead of a branch, etc.</returns>
        private RLPList CleanupBranch(RLPList node)
        {
            // If the node is null or does not have 17 items, then it is not an extension and we do not change it.
            if (node == null || node.Items.Count != 17)
            {
                return node;
            }

            // After deletion, we need to clean up adjacent branches which may be unnecessary and could possibly be shortened.
            int nonBlankCount = 0;
            int lastNonBlankIndex = -1;
            for (int i = 0; i < node.Items.Count; i++)
            {
                if (DecodeNode(node.Items[i]) != null)
                {
                    nonBlankCount++;
                    lastNonBlankIndex = i;
                }
            }

            // If we have more than one subnode referenced, this branch node is necessary.
            if (nonBlankCount > 1)
            {
                return node;
            }

            // If only the value item is non blank, then we can convert this to a leaf with a blank key remainder, and the same value slot.
            if (lastNonBlankIndex == 16)
            {
                return new RLPList(PackPrefixedNode(TrieNodeType.Leaf, Array.Empty<byte>()), node.Items[16]);
            }

            // Otherwise it was a branch that was changed, so we obtain the branch subnode
            RLPList subNode = DecodeNode(node.Items[lastNonBlankIndex]);
            TrieNodeType subNodeType = GetNodeType(subNode);

            // If our sub node type is also a branch, we convert this into an extension since its a straight path with shared nibbles. We convert the branch nibble to an extension shared nibble.
            if (subNodeType == TrieNodeType.Branch)
            {
                // Pack our prefix nibble set (using branch nibble as a shared nibble) and return our extension node which links to our branch subnode.
                return new RLPList(PackPrefixedNode(TrieNodeType.Extension, new byte[] { (byte)lastNonBlankIndex }), EncodeNode(subNode));
            }

            // If it's a leaf or extension (a node with key and value), the subnode takes the place of this one, and we prefix our subnode's key nibbles with our branch key nibble.
            else if (subNodeType == TrieNodeType.Leaf || subNodeType == TrieNodeType.Extension)
            {
                // Obtain our unpacked prefix/nibble set.
                var subNodePrefixUnpacked = UnpackPrefixedNode(subNode.Items[0]);

                // Add our branch nibble, and pack it back into the packed prefix node
                subNodePrefixUnpacked.nibbles = new byte[] { (byte)lastNonBlankIndex }.Concat(subNodePrefixUnpacked.nibbles);

                // Create our modified leaf/extension with it's longer "key" portion and the same "value" and return it
                return new RLPList(PackPrefixedNode(subNodePrefixUnpacked.type, subNodePrefixUnpacked.nibbles), subNode.Items[1]);
            }
            else
            {
                // Otherwise if we somehow end up here, throw an error.
                throw new ArgumentException("Could not clean up branch node because it has unexpected conditions!");
            }
        }
        #endregion

        #region Enums
        /// <summary>
        /// The prefix value for a leaf or extension trie node which indicates if it is a leaf or extension (second bit), along with if the provided underlying nibble set for the node's first item is odd or even length.
        /// </summary>
        public enum TrieNodePrefix : uint
        {
            ExtensionNodeEven = 0,
            ExtensionNodeOdd = 1,
            LeafNodeEven = 2,
            LeafNodeOdd = 3
        }

        /// <summary>
        /// Indicates the type of trie node one can encounter.
        /// </summary>
        public enum TrieNodeType : uint
        {
            Blank,
            Leaf,
            Extension,
            Branch
        }
        #endregion
    }

}
