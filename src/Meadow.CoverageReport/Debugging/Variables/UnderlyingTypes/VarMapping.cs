using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarMapping : VarRefBase
    {
        #region Properties
        public AstMappingTypeName MappingTypeName { get; }
        #endregion

        #region Constructors
        public VarMapping(AstMappingTypeName type) : base(type)
        {
            // Set our type name
            MappingTypeName = type;

            // Initialize our bounds. (Mappings are Storage-only objects).
            InitializeBounds(1, UInt256.SIZE, VarLocation.Storage);
        }
        #endregion

        #region Functions
        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation, IJsonRpcClient rpcClient = null)
        {
            // Create our result array
            var results = new List<MappingKeyValuePair>();

            // We'll want to loop for every key in storage at this point
            var storage = storageManager.ExecutionTrace.Tracepoints[storageManager.TraceIndex].Storage;
            foreach (Memory<byte> storageKey in storage.Keys)
            {
                // Define our current key (in case we must iterate upward through children to reach this mapping).
                Memory<byte> currentStorageKey = storageKey;

                // The point from which we will iterate/recursive upwards on.
                IterateNested:

                // Try to obtain a preimage from this key
                var storageKeyPreimage = rpcClient?.GetHashPreimage(currentStorageKey.ToArray())?.Result;
                if (storageKeyPreimage != null)
                {
                    // Verify our pre-image is 2 WORDs in length.
                    if (storageKeyPreimage.Length == UInt256.SIZE * 2)
                    {
                        // If so, slice the latter WORD (parent location) and compare it to our current storage location
                        var derivedBaseLocation = storageKeyPreimage.Slice(UInt256.SIZE);
                        if (storageLocation.SlotKey.Span.SequenceEqual(derivedBaseLocation))
                        {
                            // Obtain our value hashed with our parent location (original key to our mapping).
                            byte[] originalStorageKeyData = storageKeyPreimage.Slice(0, UInt256.SIZE);

                            // Obtain our key and value's variable-value-pair.
                            StateVariable storageKeyVariable = new StateVariable($"K[{results.Count}]", MappingTypeName.KeyType);
                            StateVariable storageValueVariable = new StateVariable($"V[{results.Count}]", MappingTypeName.ValueType);

                            // Obtain our resulting key-value pair.
                            var keyValuePair = new MappingKeyValuePair(
                                new VariableValuePair(storageKeyVariable, storageKeyVariable.ValueParser.ParseData(originalStorageKeyData)), 
                                new VariableValuePair(storageValueVariable, storageValueVariable.ValueParser.ParseFromStorage(storageManager, new StorageLocation(currentStorageKey, 0), rpcClient)));

                            // Add our key-value pair to the results.
                            results.Add(keyValuePair);
                        }
                        else
                        {
                            // This derived location is not referencing this location. Iterate upward on it to determine if it's a child.
                            currentStorageKey = derivedBaseLocation;
                            goto IterateNested;
                        }
                    }
                }

            }

            // Return our results array.
            return results.ToArray();
        }
        #endregion
    }
}
