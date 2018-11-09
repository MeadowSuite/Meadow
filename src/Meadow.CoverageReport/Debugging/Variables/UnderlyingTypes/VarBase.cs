using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;
using System;
using System.Numerics;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarBase
    {
        #region Properties
        public string TypeString { get; }
        public string BaseType { get;  }
        public VarGenericType GenericType { get; }
        public int StorageEntryCount { get; private set; }
        public int SizeBytes { get; private set; }
        #endregion

        #region Constructor
        public VarBase(string typeString)
        {
            // Set our type
            TypeString = typeString;

            // Obtain the components of our type and set them.
            BaseType = VarParser.ParseTypeComponents(TypeString).baseType;

            // Obtain our generic type.
            GenericType = VarParser.GetGenericType(BaseType);
        }
        #endregion

        #region Functions
        public void InitializeBounds(int storageEntryCount, int sizeBytes)
        {
            // Set our properties
            StorageEntryCount = storageEntryCount;
            SizeBytes = sizeBytes;
        }

        public virtual object ParseData(Memory<byte> data)
        {
            // This is to be implemented by extending classes.
            throw new NotImplementedException();
        }

        public virtual object ParseFromCallData(ref Memory<byte> callData)
        {
            // TODO: Implement
            return null;
        }

        public virtual object ParseFromStack(Data[] stack, int stackIndex, Memory<byte> memory, StorageManager storageManager, IJsonRpcClient rpcClient = null)
        {
            // If we exceeded our stack size
            if (stack.Length <= stackIndex)
            {
                throw new VarResolvingException("Could not parse variable from stack using base type method because the stack is not populated up to the target index.");
            }

            // Obtain our stack data and return our parsed object.
            Memory<byte> stackData = stack[stackIndex].GetBytes();
            return ParseData(stackData);
        }

        public virtual object ParseFromMemory(Memory<byte> memory, int offset)
        {
            // Obtain our data
            Memory<byte> data = memory.Slice(offset, UInt256.SIZE);

            // Parse the value
            return ParseData(data);
        }

        public virtual object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation, IJsonRpcClient rpcClient = null)
        {
            // Obtain our storage value for our given storage location.
            Memory<byte> storageValue = storageManager.ReadStorageSlot(storageLocation.SlotKey, storageLocation.DataOffset, SizeBytes);

            // Parse our object
            return ParseData(storageValue);
        }
        #endregion
    }
}
