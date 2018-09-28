using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using Meadow.EVM.Exceptions;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Memory
{
    /// <summary>
    /// Represents the stack in the Ethereum Virtual Machine.
    /// </summary>
    public class EVMStack
    {
        #region Constants
        /// <summary>
        /// Describes the maximum count of items on the stack before an exception is thrown.
        /// </summary>
        public const uint MAX_STACK_SIZE = 1024;
        #endregion

        #region Fields
        private List<byte[]> _internalStack;
        #endregion

        #region Properties
        public int Count
        {
            get
            {
                return _internalStack.Count;
            }
        }
        #endregion

        #region Constructors
        public EVMStack()
        {
            // Initialize our internal stack.
            _internalStack = new List<byte[]>();
        }
        #endregion

        #region Functions
        public BigInteger Pop(bool signed = false)
        {
            // Verify we have items on the stack.
            if (_internalStack.Count == 0)
            {
                throw new EVMException("Tried to pop a value off of the stack when the stack was empty. This should not have happened.");
            }

            // Pop a value off the internal stack, convert it, and return it.
            byte[] data = _internalStack[_internalStack.Count - 1];
            _internalStack.RemoveAt(_internalStack.Count - 1);
            return BigIntegerConverter.GetBigInteger(data, signed);
        }

        public void Push(BigInteger obj)
        {
            // Verify we aren't reaching our maximum stack size.
            if (Count >= MAX_STACK_SIZE)
            {
                throw new EVMException($"Stack has overflowed past the maximum size of {MAX_STACK_SIZE} entries.");
            }

            // Push the object to the top of the stack.
            _internalStack.Add(BigIntegerConverter.GetBytes(obj));
        }

        public void Push(BigInteger obj, uint byteCount)
        {
            // Obtain the bytes for this BigInteger.
            byte[] data = BigIntegerConverter.GetBytes(obj);

            // If the byte count is larger than the data, something is wrong.
            if (byteCount > data.Length)
            {
                throw new ArgumentException("Tried to push a value onto the stack that was larger than 256-bit. This should not have happened.");
            }

            // Determine how many bytes we're going to zero out to keep our desired bytes only.
            int relevantBytesStart = data.Length - (int)byteCount;
            for (int i = 0; i < relevantBytesStart; i++)
            {
                data[i] = 0x00;
            }

            // Push this data.
            _internalStack.Add(data);
        }

        public void Duplicate(uint index)
        {
            // TODO: Error handling if stack is too small.

            // Calculate the item index in the array
            int arrayIndex = (_internalStack.Count - 1) - (int)index;

            // We'll take the item at the given index (smaller denotes higher on the stack).
            byte[] obj = _internalStack[arrayIndex];

            // And add a duplicate to the top of the stack.
            _internalStack.Add(obj);
        }

        public void Swap(uint index)
        {
            // TODO: Error handling if stack is too small.

            // Calculate the item index in the array
            int arrayIndex = (_internalStack.Count - 1) - (int)index;

            // Grab both items
            byte[] obj1 = (byte[])_internalStack[arrayIndex];
            byte[] obj2 = (byte[])_internalStack[_internalStack.Count - 1];

            // Swap their positions
            _internalStack[_internalStack.Count - 1] = obj1;
            _internalStack[arrayIndex] = obj2;
        }

        public override string ToString()
        {
            // Create our stack string representation.
            string stackStr = "Stack: {";

            // Add each stack item in order.
            for (int i = _internalStack.Count - 1; i >= 0; i--)
            {
                stackStr += _internalStack[i].ToHexString(true);
                if (i != 0)
                {
                    stackStr += ", ";
                }
            }
            
            // Close the stack string
            stackStr += "}";
            
            // Return it.
            return stackStr;
        }

        /// <summary>
        /// Obtains an array representation of all stack items.
        /// </summary>
        /// <returns>Returns an array which represents the stack, where each item is a byte array in the stack.</returns>
        public byte[][] ToArray()
        {
            // Return our internal stack as an array.
            return _internalStack.ToArray();
        }
        #endregion
    }
}
