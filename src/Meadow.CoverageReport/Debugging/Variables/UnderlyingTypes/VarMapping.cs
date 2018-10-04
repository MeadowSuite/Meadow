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
            foreach (var storageKey in storage.Keys)
            {
                // Try to obtain a preimage from this key
                var storageKeyPreimage = rpcClient?.GetHashPreimage(storageKey.ToArray())?.Result;
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

                            // Obtain our value for this key in our mapping
                            byte[] originalStorageValueData = storage[storageKey];

                            // Obtain our key and value's variable-value-pair.
                            StateVariable storageKeyVariable = new StateVariable($"K[{results.Count}]", MappingTypeName.KeyType);
                            StateVariable storageValueVariable = new StateVariable($"V[{results.Count}]", MappingTypeName.ValueType);

                            // Obtain our resulting key-value pair.
                            var keyValuePair = new MappingKeyValuePair(
                                new VariableValuePair(storageKeyVariable, storageKeyVariable.ValueParser.ParseData(originalStorageKeyData)), 
                                new VariableValuePair(storageValueVariable, storageValueVariable.ValueParser.ParseFromStorage(storageManager, new StorageLocation(storageKey, 0), rpcClient)));

                            // Add our key-value pair to the results.
                            results.Add(keyValuePair);
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
