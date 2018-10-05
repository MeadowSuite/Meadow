using System;
using System.Numerics;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarRefBase : VarBase
    {
        #region Properties
        public VarLocation VariableLocation { get; private set; }
        #endregion

        #region Constructors
        public VarRefBase(AstElementaryTypeName type) : base(type)
        {

        }
        #endregion

        #region Functions
        public void InitializeBounds(int storageEntryCount, int storageSizeBytes, VarLocation variableLocation)
        {
            // Set our variables bounds.
            InitializeBounds(storageEntryCount, storageSizeBytes);

            // Set our extra properties
            VariableLocation = variableLocation;
        }

        public virtual object ParseDereferencedFromMemory(Memory<byte> memory, int offset)
        {
            // This is to be implemented by extending classes.
            throw new NotImplementedException();
        }

        public override object ParseFromMemory(Memory<byte> memory, int offset)
        {
            // Read a pointer bytes (size of word) to dereference the value.
            Memory<byte> pointerData = memory.Slice(offset, UInt256.SIZE);

            // Parse our pointer from the bytes.
            BigInteger pointer = BigIntegerConverter.GetBigInteger(pointerData.Span, false, UInt256.SIZE);

            // Parse our dereferenced value.
            return ParseDereferencedFromMemory(memory, (int)pointer);
        }

        public override object ParseFromStack(Data[] stack, int stackIndex, Memory<byte> memory, StorageManager storageManager, IJsonRpcClient rpcClient = null)
        {
            // If we exceeded our stack size
            if (stack.Length <= stackIndex)
            {
                throw new VarResolvingException("Could not parse variable from stack using reference type method because the stack is not populated up to the target index.");
            }

            // Obtain our pointer data from the stack.
            Memory<byte> stackEntryData = stack[stackIndex].GetBytes();

            // Switch on our location
            switch (VariableLocation)
            {
                case VarLocation.Memory:
                    // Parse our pointer from the bytes.
                    BigInteger pointer = BigIntegerConverter.GetBigInteger(stackEntryData.Span, false, UInt256.SIZE);

                    // Parse our dereferenced value from memory. (Using our stack data as a data offset)
                    return ParseDereferencedFromMemory(memory, (int)pointer);

                case VarLocation.Storage:
                    // Parse our stack data as a storage key
                    StorageLocation storageLocation = new StorageLocation(stackEntryData, 0);

                    // Parse our value from storage. (Using our stack data as a storage key)
                    return ParseFromStorage(storageManager, storageLocation, rpcClient);

                default:
                    throw new VarResolvingException("Could not parse variable from stack using reference type because the provided underlying location is invalid.");
            }
        }
        #endregion
    }
}
