using Meadow.Core.Cryptography;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarArray : VarRefBase
    {
        #region Properties
        public AstArrayTypeName ArrayTypeName { get; }
        public VarBase ElementObject { get; }
        public int? ArraySize { get; }
        #endregion

        #region Constructors
        public VarArray(AstArrayTypeName type, VarLocation location) : base(type)
        {
            // Obtain our array size
            ArraySize = VarParser.ParseArrayTypeComponents(BaseType).arraySize;

            // Set our type name
            ArrayTypeName = type;

            // Set our element parser with the given array element/base type.
            ElementObject = VarParser.GetVariableObject(ArrayTypeName.BaseType, location);

            // Define our bounds variables
            int storageSlotCount = 1;
            if (ArraySize.HasValue)
            {
                // If an element doesn't fit in a single slot, we keep the storage format as is, and slot count = element count * element slot count.
                // If an element fits in a single slot, we try to compact multiple into a single slot.
                if (ElementObject.SizeBytes >= UInt256.SIZE)
                {
                    storageSlotCount = ArraySize.Value * ElementObject.StorageEntryCount;
                }
                else
                {
                    // Determine how many elements we can fit in a slot.
                    int elementsPerSlot = UInt256.SIZE / ElementObject.SizeBytes;

                    // Figure out how many slots we'll actually need to store the element count.
                    storageSlotCount = (int)Math.Ceiling(ArraySize.Value / (double)elementsPerSlot);
                }
            }

            // Initialize our bounds.
            InitializeBounds(storageSlotCount, UInt256.SIZE, location);
        }
        #endregion

        #region Functions
        public override object ParseDereferencedFromMemory(Memory<byte> memory, int offset)
        {
            // Define our length.
            int length = 0;

            // If our array has a constant size, set it. Otherwise it's dynamic and lives in memory.
            int elementOffset = offset;
            if (ArraySize.HasValue)
            {
                length = ArraySize.Value;
            }
            else
            {
                // Obtain our length data from memory.
                Memory<byte> lengthData = memory.Slice(offset, UInt256.SIZE);

                // Obtain our length integer from the read memory.
                length = (int)BigIntegerConverter.GetBigInteger(lengthData.Span, false, UInt256.SIZE);

                // Advance our element offset since our offset position referred to the length ifrst
                elementOffset += UInt256.SIZE;
            }

            // Create our resulting object array
            object[] arrayElements = new object[length];

            // Obtain every element of our array
            for (int i = 0; i < arrayElements.Length; i++)
            {
                // Parse our array element
                arrayElements[i] = ElementObject.ParseFromMemory(memory, elementOffset);

                // Advance our offset
                elementOffset += UInt256.SIZE;
            }

            // Return our array elements.
            return arrayElements;
        }

        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation, IJsonRpcClient rpcClient = null)
        {
            // Obtain our storage value for our given storage location.
            Memory<byte> storageData = storageManager.ReadStorageSlot(storageLocation.SlotKey, storageLocation.DataOffset, SizeBytes);
            BigInteger storageValue = BigIntegerConverter.GetBigInteger(storageData.Span, false, SizeBytes);

            // Define our element slot location to iterate over.
            StorageLocation elementLocation = new StorageLocation(storageLocation.SlotKey, 0);

            // If this is a dynamic sized type, our element's storage key will be the defining 
            // array's key hashed, and all consecutive element items will have consecutive storage keys.

            // In any case, we'll want to grab our array length, which is either statically defined, or
            // is defined in the storage data obtained earlier from the given location.
            int length = 0;
            if (ArraySize.HasValue)
            {
                length = ArraySize.Value;
            }
            else
            {
                elementLocation.SlotKey = KeccakHash.ComputeHashBytes(elementLocation.SlotKey.Span);
                length = (int)storageValue;
            }

            // Create our resulting object array
            object[] arrayElements = new object[length];

            // Loop for every item.
            for (int i = 0; i < arrayElements.Length; i++)
            {
                // Decode our element at this index
                arrayElements[i] = ElementObject.ParseFromStorage(storageManager, elementLocation, rpcClient);

                // Determine how to iterate, dependent on if the array is dynamically sized or not, as described earlier,
                // (since it could compact multiple elements into a single storage slot).
                if (ElementObject.StorageEntryCount == 1 && storageLocation.DataOffset + ElementObject.SizeBytes <= UInt256.SIZE)
                {
                    // It was compacted with other data (or elligible to), so we advance data offset.
                    elementLocation.DataOffset += ElementObject.SizeBytes;

                    // If our data offset exceeded the size of a slot, we advance the slot.
                    if (elementLocation.DataOffset + ElementObject.SizeBytes > UInt256.SIZE)
                    {
                        elementLocation.SlotKeyInteger++;
                        elementLocation.DataOffset = 0;
                    }
                }
                else
                {
                    // We are not compacting our storage, so we advance for each element individually.
                    elementLocation.SlotKeyInteger += ElementObject.StorageEntryCount;
                    elementLocation.DataOffset = 0;
                }
            }

            // Return our obtained array elements
            return arrayElements;
        }
        #endregion
    }
}
