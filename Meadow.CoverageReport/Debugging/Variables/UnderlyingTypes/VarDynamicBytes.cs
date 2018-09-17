using Meadow.Core.Cryptography;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarDynamicBytes : VarRefBase
    {
        #region Constructors
        public VarDynamicBytes(AstElementaryTypeName type, VarLocation location) : base(type)
        {
            // Initialize our bounds.
            InitializeBounds(1, UInt256.SIZE, location);
        }
        #endregion

        #region Functions
        public override object ParseDereferencedFromMemory(Memory<byte> memory, int offset)
        {
            // Dynamic memory is a WORD denoting length, followed by the length in bytes of data.

            // Read a length bytes (size of word) to dereference the value.
            Memory<byte> lengthData = memory.Slice(offset, UInt256.SIZE);
            BigInteger length = BigIntegerConverter.GetBigInteger(lengthData.Span, false, UInt256.SIZE);

            // Read our data
            return memory.Slice(offset + UInt256.SIZE, (int)length);
        }

        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation)
        {
            // Obtain our storage value for our given storage location.
            Memory<byte> storageData = storageManager.ReadStorageSlot(storageLocation.SlotKey, storageLocation.DataOffset, SizeBytes);
            BigInteger storageValue = BigIntegerConverter.GetBigInteger(storageData.Span, false, SizeBytes);

            // The lowest bit of our value signifies if it was stored in multiple slots, or if it fit in a single slot.
            bool requiresMultipleSlots = (storageValue & 1) != 0;
            if (requiresMultipleSlots)
            {
                // The length is shifted one bit to the left as a result of our flag encoded at bit 0. 
                // So we shift to obtain length.
                int length = (int)(storageValue >> 1);

                // Calculate our slot count.
                int slotCount = (int)Math.Ceiling((double)length / UInt256.SIZE);

                // Define our result
                Memory<byte> result = new byte[length];

                // Calculate the slot key for our array data (dynamic array's data slot keys are 
                // the array's slot key hashed, with subsequent slot keys being + 1 to the previous)
                Memory<byte> arrayDataSlotKey = KeccakHash.ComputeHashBytes(storageLocation.SlotKey.Span);

                // Define our slot location to iterate over.
                StorageLocation slotLocation = new StorageLocation(arrayDataSlotKey, 0);

                // Loop for every byte we wish to copy.
                for (int i = 0; i < length;)
                {
                    // Obtain the slot
                    Memory<byte> arrayDataSlotValue = storageManager.ReadStorageSlot(slotLocation.SlotKey);

                    // Calculate the remainder of our bytes
                    int remainder = length - i;

                    // Determine the remainder in this slot.
                    int remainderInSlot = Math.Min(remainder, UInt256.SIZE);

                    // Copy our data into our result.
                    arrayDataSlotValue.Slice(0, remainderInSlot).CopyTo(result.Slice(i));

                    // Increment our slot key
                    slotLocation.SlotKeyInteger++;

                    // Increment our byte index.
                    i += remainderInSlot;
                }

                // Return our result
                return result;
            }
            else
            {
                // We did not require multiple storage slots, so it is embedded in this slot.
                // But the count for data size remains at the end of this storage slot, so we
                // first obtain the data size from that byte.
                int length = ((int)(storageValue & 0xFF)) >> 1;

                // Slice off the desired data from our storage slot data and return it.
                return storageData.Slice(0, length);
            }
        }
        #endregion
    }
}
