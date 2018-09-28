using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types.Debugging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Storage
{
    /// <summary>
    /// Tracks storage across execution traces and helps with various storage calculation/functions.
    /// </summary>
    public class StorageManager
    {
        #region Properties
        /// <summary>
        /// The execution trace to initialize and track storage across.
        /// </summary>
        public ExecutionTrace ExecutionTrace { get; private set; }
        /// <summary>
        /// The primary trace index to use, if none is provided.
        /// </summary>
        public int TraceIndex { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the storage manager with the provided execution trace.
        /// </summary>
        /// <param name="executionTrace">The execution trace to initialize and track storage across.</param>
        public StorageManager(ExecutionTrace executionTrace)
        {
            // Set our properties
            ExecutionTrace = executionTrace;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Resolves storage locations for a collection of state variables.
        /// </summary>
        /// <param name="stateVariables">State variables for which we wish to resolve storage locations.</param>
        /// <param name="nextStorageLocation">An optional storage location object which indicates the next storage location from which we should start. If null, we start from the beginning.</param>
        /// <returns>Returns the next potential storage slot after enumerating the last variable.</returns>
        public static StorageLocation ResolveStorageSlots(IEnumerable<StateVariable> stateVariables, StorageLocation nextStorageLocation = null)
        {
            // Track our storage slot index and data offset over each variable
            BigInteger storageSlotIndex = 0;
            int dataOffset = 0;

            // If we had a next storage location, set the variables from it
            if (nextStorageLocation != null)
            {
                storageSlotIndex = nextStorageLocation.SlotKeyInteger;
                dataOffset = nextStorageLocation.DataOffset;
            }

            // Loop for every state variable.
            foreach (StateVariable stateVariable in stateVariables)
            {
                // If this is a constant, it comes from offset and slot 0.
                if (stateVariable.Declaration.Constant)
                {
                    // Set our storage location
                    stateVariable.StorageLocation = new StorageLocation(0, 0);
                }
                else
                {
                    // Ensure if our state variable doesn't fit in the current slot, we advance to the next one.
                    if (dataOffset + stateVariable.ValueParser.SizeBytes > UInt256.SIZE)
                    {
                        storageSlotIndex++;
                        dataOffset = 0;
                    }

                    // Set our storage location
                    stateVariable.StorageLocation = new StorageLocation(storageSlotIndex, dataOffset);

                    // If our variable can allow for other variables to share this storage slot,
                    // we simply advance offset. Otherwise we skip to the next storage slot that should be open.
                    bool canCompactVariable = stateVariable.ValueParser.StorageEntryCount == 1 && stateVariable.ValueParser.SizeBytes + stateVariable.StorageLocation.DataOffset <= UInt256.SIZE;
                    if (canCompactVariable)
                    {
                        // Other variable can share this storage slot, we advance offset.
                        dataOffset += stateVariable.ValueParser.SizeBytes;
                    }
                    else
                    {
                        // We skip to the next available slot index and reset offset.
                        storageSlotIndex += stateVariable.ValueParser.StorageEntryCount;
                        dataOffset = 0;
                    }
                }
            }

            // Return our next storage location to use.
            return new StorageLocation(storageSlotIndex, dataOffset);
        }

        /// <summary>
        /// Obtains the storage lookup for the current set <see cref="TraceIndex"/>.
        /// </summary>
        /// <returns>Returns the storage lookup for the set <see cref="TraceIndex"/>.</returns>
        private Dictionary<Memory<byte>, byte[]> GetStorage()
        {
            // Return the most recent tracepoint's storage.
            return GetStorage(TraceIndex);
        }

        /// <summary>
        /// Obtains the storage lookup for the given <paramref name="traceIndex"/>.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point in the current execution trace for which we wish to obtain storage.</param>
        /// <returns>Returns the storage lookup for the given trace index.</returns>
        private Dictionary<Memory<byte>, byte[]> GetStorage(int traceIndex)
        {
            // If we have an invalid storage index
            if (traceIndex < 0 || traceIndex >= ExecutionTrace.Tracepoints.Length)
            {
                return null;
            }

            // Return the indexed tracepoint's storage.
            return ExecutionTrace.Tracepoints[traceIndex].Storage;
        }

        /// <summary>
        /// Reads the value from the storage slot accessed by the given key at the set <see cref="TraceIndex"/>.
        /// </summary>
        /// <param name="key">The key used to obtain the storage value.</param>
        /// <returns>Returns the storage value for the provided storage key at the set <see cref="TraceIndex"/>.</returns>
        public Memory<byte> ReadStorageSlot(Memory<byte> key)
        {
            return ReadStorageSlot(TraceIndex, key);
        }

        /// <summary>
        /// Reads a range of the storage value for the given storage key at the set <see cref="TraceIndex"/>.
        /// NOTE: Offset and size are indexed from the left hand side, but actually operated on from the right hand side.
        /// </summary>
        /// <param name="key">The key used to obtain the storage value.</param>
        /// <param name="offset">The offset to begin reading from in the obtained storage value.</param>
        /// <param name="size">The size of the data to read from the obtained storage value.</param>
        /// <returns>Returns the desired range of the value for the provided storage key at the set <see cref="TraceIndex"/>.</returns>
        public Memory<byte> ReadStorageSlot(Memory<byte> key, int offset, int size)
        {
            return ReadStorageSlot(TraceIndex, key, offset, size);
        }

        /// <summary>
        /// Reads the value from the storage slot accessed by the given key at <paramref name="traceIndex"/>.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point in the current execution trace which we wish to read storage at.</param>
        /// <param name="key">The key used to obtain the storage value.</param>
        /// <returns>Returns the storage value for the provided storage key at the set <paramref name="traceIndex"/>.</returns>
        public Memory<byte> ReadStorageSlot(int traceIndex, Memory<byte> key)
        {
            return ReadStorageSlot(traceIndex, key, 0, UInt256.SIZE);
        }

        /// <summary>
        /// Reads a range of the storage value for the given storage key at the provided <paramref name="traceIndex"/>.
        /// NOTE: Offset and size are indexed from the left hand side, but actually operated on from the right hand side.
        /// </summary>
        /// <param name="traceIndex">The index of the trace point in the current execution trace which we wish to read storage at.</param>
        /// <param name="key">The key used to obtain the storage value.</param>
        /// <param name="offset">The offset to begin reading from in the obtained storage value.</param>
        /// <param name="size">The size of the data to read from the obtained storage value.</param>
        /// <returns>Returns the desired range of the value for the provided storage key at the set <paramref name="traceIndex"/>.</returns>
        public Memory<byte> ReadStorageSlot(int traceIndex, Memory<byte> key, int offset, int size)
        {
            // Obtain our trace point
            ExecutionTracePoint tracePoint = ExecutionTrace.Tracepoints[traceIndex];

            // If storage doesn't contain our key, we return zero.
            bool succeeded = tracePoint.Storage.TryGetValue(key, out var storageValue);
            if (!succeeded)
            {
                return new byte[UInt256.SIZE];
            }

            // Otherwise we slice our data
            Memory<byte> valueMemory = storageValue;

            // Adjust our offset to account from the right hand side.
            offset = valueMemory.Length - size - offset;

            // Return our slice of storage.
            return valueMemory.Slice(offset, size);
        }
        #endregion
    }
}
