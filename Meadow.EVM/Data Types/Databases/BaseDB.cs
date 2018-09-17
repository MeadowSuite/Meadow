using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Trees.Comparer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Data_Types.Databases
{
    public class BaseDB
    {
        #region Fields
        protected Dictionary<Memory<byte>, byte[]> _internalLookup;
        #endregion

        #region Constructors
        public BaseDB()
        {
            // Create a new lookup
            _internalLookup = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());
        }

        public BaseDB(Dictionary<Memory<byte>, byte[]> lookup)
        {
            // Create a new lookup with all elements in the provided lookup.
            _internalLookup = new Dictionary<Memory<byte>, byte[]>(lookup, new MemoryComparer<byte>());
        }
        #endregion

        #region Functions
        public virtual bool Contains(byte[] key)
        {
            // Check if the lookup contains the key.
            return _internalLookup.ContainsKey(key);
        }

        public bool Contains(string key)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(key);

            // Remove the value
            return Contains(actualKey);
        }

        public bool Contains(string prefix, byte[] key)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(prefix).Concat(key);

            // Remove the value
            return Contains(actualKey);
        }

        public virtual bool TryGet(byte[] key, out byte[] val)
        {
            // Obtain the value from the dictionary.
            return _internalLookup.TryGetValue(key, out val);
        }

        public bool TryGet(string key, out byte[] val)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(key);

            // Obtain the data.
            return TryGet(actualKey, out val);
        }

        public bool TryGet(string prefix, byte[] key, out byte[] val)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(prefix).Concat(key);

            // Obtain the data.
            return TryGet(actualKey, out val);
        }


        public virtual void Set(byte[] key, byte[] data)
        {
            // Set the given value in the dictionary.
            _internalLookup[key] = data;
        }

        public void Set(string key, byte[] data)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(key);

            // Set the value
            Set(actualKey, data);
        }

        public void Set(string prefix, byte[] key, byte[] data)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(prefix).Concat(key);

            // Set the value
            Set(actualKey, data);
        }

        public virtual void Remove(byte[] key)
        {
            // Removes a given item from the lookup by key.
            _internalLookup.Remove(key);
        }

        public void Remove(string key)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(key);

            // Remove the value
            Remove(actualKey);
        }

        public void Remove(string prefix, byte[] key)
        {
            // Obtain the actual key.
            byte[] actualKey = UTF8Encoding.UTF8.GetBytes(prefix).Concat(key);

            // Remove the value
            Remove(actualKey);
        }
        #endregion
    }
}
