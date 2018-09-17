using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.EVM.Memory
{
    /// <summary>
    /// Represents the expandable memory segment in the Ethereum Virtual Machine.
    /// </summary>
    public class EVMMemory
    {
        #region Fields
        /// <summary>
        /// The internal buffer that represents the memory segment in an Ethereum Virtual Machine.
        /// </summary>
        private Stream _internalBufferStream;
        #endregion

        #region Properties
        /// <summary>
        /// The parent Ethereum Virtual Machine that this memory belongs to (and gas should be charged to).
        /// </summary>
        public MeadowEVM EVM { get; }
        /// <summary>
        /// Represents the current length of the stream.
        /// </summary>
        public long Length
        {
            get { return _internalBufferStream.Length; }
        }

        /// <summary>
        /// A rolling count of changes to memory, indicates if changes to memory have been made, signalling for it to be included in the next trace item if tracing.
        /// </summary>
        public ulong ChangeCount { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Our default constructor, initializes a new virtual memory section.
        /// </summary>
        /// <param name="evm">The parent EVM that this memory belongs to (and gas should be charged to).</param>
        public EVMMemory(MeadowEVM evm) : this(evm, Array.Empty<byte>()) { }
        /// <summary>
        /// Our constructor, takes a byte array to map virtual memory to.
        /// </summary>
        /// <param name="evm">The parent EVM that this memory belongs to (and gas should be charged to).</param>
        /// <param name="memoryData">The byte array to map virtual memory to.</param>
        public EVMMemory(MeadowEVM evm, byte[] memoryData)
        {
            // Set our evm and stream
            EVM = evm;
            _internalBufferStream = new MemoryStream();
            if (memoryData != null && memoryData.Length != 0)
            {
                _internalBufferStream.Write(memoryData, 0, memoryData.Length);
                _internalBufferStream.Position = 0;
            }

            ChangeCount = 0;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Reads a byte from virtual memory at the provided address.
        /// </summary>
        /// <param name="address">The address to read the byte from.</param>
        /// <returns>Returns the byte at the given address.</returns>
        public byte ReadByte(long address)
        {
            // If we don't have enough address space to read from, expand the stream.
            ExpandStream(address, 1);

            // Set our position and read our byte.
            _internalBufferStream.Position = address;
            int result = _internalBufferStream.ReadByte();

            // If we couldn't read the data, throw an exception.
            if (result == -1)
            {
                throw new EndOfStreamException("ReadByte read past end of EVM memory.");
            }

            return (byte)result;
        }

        /// <summary>
        /// Reads a byte array from virtual memory of the given size at the given address.
        /// </summary>
        /// <param name="address">The address to read bytes from.</param>
        /// <param name="size">The amount of bytes to read.</param>
        /// <returns>Returns the bytes read from the given address.</returns>
        public byte[] ReadBytes(long address, int size)
        {
            // Set our position
            _internalBufferStream.Position = address;

            // If we don't have enough address space to read from, expand the stream.
            ExpandStream(address, size);

            // Read our bytes
            byte[] data = new byte[size];
            int result = _internalBufferStream.Read(data, 0, data.Length);
            if (result != data.Length)
            {
                throw new EndOfStreamException("ReadBytes had bytes remaining at the end of the stream but could not read them.");
            }

            return data;
        }

        /// <summary>
        /// Reads a 256-bit integer from virtual memory at the given address
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <param name="signed"></param>
        /// <returns>Returns the 256-bit integer read from the given address.</returns>
        public BigInteger ReadBigInteger(long address, bool signed = false)
        {
            return BigIntegerConverter.GetBigInteger(ReadBytes(address, EVMDefinitions.WORD_SIZE), signed);
        }

        /// <summary>
        /// Writes a byte to virtual memory at the provided address.
        /// </summary>
        /// <param name="address">The address to write the byte to.</param>
        /// <param name="data">The byte to write to the given address.</param>
        public void Write(long address, byte data)
        {
            // If we don't have enough room left to write this data, expand the stream.
            ExpandStream(address, 1);

            // Set our position and write
            _internalBufferStream.Position = address;
            _internalBufferStream.WriteByte(data);

            // Update our change count
            ChangeCount++;
        }

        /// <summary>
        /// Writes a byte array to virtual memory at the provided address.
        /// </summary>
        /// <param name="address">The address to write bytes to.</param>
        /// <param name="data">The byte array to write to the given address.</param>
        public void Write(long address, byte[] data)
        {
            // If we don't have enough room left to write this data, expand the stream.
            ExpandStream(address, data.Length);

            // Set our position and write
            _internalBufferStream.Position = address;
            _internalBufferStream.Write(data, 0, data.Length);

            // Update our change count
            ChangeCount++;
        }

        /// <summary>
        /// Writes a 256-bit integer to virtual memory at the provided address.
        /// </summary>
        /// <param name="address">The address to write to.</param>
        /// <param name="bigInteger">The 256-bit integer to write.</param>
        public void Write(long address, BigInteger bigInteger)
        {
            Write(address, BigIntegerConverter.GetBytes(bigInteger));
        }

        /// <summary>
        /// Expands the memory stream if needed (and charges gas) to accomodate for an operation to occur at the given address with a given size.
        /// </summary>
        /// <param name="address">The address where data is presumed to be read or written.</param>
        /// <param name="size">The size of the data presumed to be read or written.</param>
        public void ExpandStream(BigInteger address, BigInteger size)
        {
            // If our address space doesn't extend to handle data in these bounds, expand memory.
            if (address + size > Length)
            {
                // Memory is allocated such that it is aligned to the size of a WORD.
                BigInteger currentWordCount = EVMDefinitions.GetWordCount(Length);
                BigInteger targetWordCount = EVMDefinitions.GetWordCount(address + size);

                // Calculate cost of gas for expanding our array.
                BigInteger currentMemoryCost = GasDefinitions.GetMemoryAllocationCost(EVM.Version, currentWordCount);
                BigInteger targetMemoryCost = GasDefinitions.GetMemoryAllocationCost(EVM.Version, targetWordCount);
                BigInteger costDelta = targetMemoryCost - currentMemoryCost;

                // Deduct the difference in cost for expanding our memory.
                EVM.GasState.Deduct(costDelta);

                // Set the size of our stream
                _internalBufferStream.SetLength((long)targetWordCount * EVMDefinitions.WORD_SIZE);

                // Update our change count
                ChangeCount++;
            }
        }

        /// <summary>
        /// Obtains all of the EVM memory as a byte array.
        /// </summary>
        /// <returns>Returns all EVM memory as a byte array.</returns>
        public byte[] ToArray()
        {
            // Set our position
            _internalBufferStream.Position = 0;

            // Read our bytes
            byte[] data = new byte[_internalBufferStream.Length];
            int result = _internalBufferStream.Read(data, 0, data.Length);
            if (result != data.Length)
            {
                throw new EndOfStreamException("ReadBytes had bytes remaining at the end of the stream but could not read them.");
            }

            // Return our data.
            return data;
        }
        #endregion
    }
}
