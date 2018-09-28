using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.Core.RlpEncoding
{
    /// <summary>
    /// Represents the base class for an RLP serializable item. This class should not be instantiated directly as it does not implement any data type and only this classes derivatives are serialized.
    /// </summary>
    public class RLPItem
    {
        #region Properties
        /// <summary>
        /// Indicates this item is a byte array.
        /// </summary>
        public bool IsByteArray
        {
            get
            {
                return this is RLPByteArray;
            }
        }

        /// <summary>
        /// Indicates this item is a list.
        /// </summary>
        public bool IsList
        {
            get
            {
                return this is RLPList;
            }
        }
        #endregion

        #region Operators

        public static implicit operator RLPItem(BigInteger num)
        {
            if (num == 0)
            {
                return new RLPByteArray(null);
            }

            if (num < 0)
            {
                throw new Exception("RLP encoding of negative integers is ambiguous and not supported.");
            }

            var arr = num.ToByteArray();

            // Swap BigInteger's little-endian bytes to big-endian
            Array.Reverse(arr);

            // Strip leading zeros
            int offset = 0;
            while (offset < arr.Length && arr[offset] == 0)
            {
                offset++;
            }

            return new RLPByteArray(new Memory<byte>(arr, offset, arr.Length - offset));
        }

        public static implicit operator RLPItem(short num) => (BigInteger)num;

        public static implicit operator RLPItem(ushort num) => (BigInteger)num;

        public static implicit operator RLPItem(int num) => (BigInteger)num;

        public static implicit operator RLPItem(uint num) => (BigInteger)num;

        public static implicit operator RLPItem(long num) => (BigInteger)num;

        public static implicit operator RLPItem(ulong num) => (BigInteger)num;

        public static implicit operator RLPItem(decimal num) => (BigInteger)num;

        public static implicit operator RLPItem(byte data)
        {
            return new RLPByteArray(new byte[] { data });
        }

        public static implicit operator RLPItem(byte[] data)
        {
            return new RLPByteArray(data);
        }

        public static implicit operator RLPItem(Memory<byte> data)
        {
            return new RLPByteArray(data);
        }

        public static implicit operator RLPItem(string data)
        {
            return new RLPByteArray(StringUtil.UTF8.GetBytes(data));
        }

        public static implicit operator RLPItem(RLPItem[] itemArray)
        {
            return new RLPList(itemArray);
        }

        public static implicit operator RLPItem(List<RLPItem> itemArray)
        {
            return new RLPList(itemArray);
        }

        public static implicit operator byte(RLPItem data)
        {
            return ((RLPByteArray)data).Data.Span[0];
        }

        public static implicit operator byte[](RLPItem data)
        {
            return ((RLPByteArray)data).Data.ToArray();
        }

        public static implicit operator Memory<byte>(RLPItem data)
        {
            return ((RLPByteArray)data).Data;
        }

        public static implicit operator string(RLPItem data)
        {
            RLPByteArray rlpBytes = ((RLPByteArray)data);
            if (rlpBytes.Data.Length == 0)
            {
                return null;
            }
            else
            {
                return StringUtil.UTF8.GetString(rlpBytes.Data.ToArray());
            }
        }
        #endregion
    }
}
