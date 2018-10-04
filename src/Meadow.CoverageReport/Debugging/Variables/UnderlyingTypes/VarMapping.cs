using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Meadow.CoverageReport.Debugging.Variables.Storage;
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
        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation)
        {
            // Create our result array
            var results = new List<(VariableValuePair key, VariableValuePair value)>();

            // We'll want to loop for every key in storage at this point
            var storage = storageManager.ExecutionTrace.Tracepoints[storageManager.TraceIndex].Storage;
            foreach (var storageKey in storage.Keys)
            {
                // Try to obtain a preimage from this key
                if (storageManager.ExecutionTrace.StoragePreimages.TryGetValue(storageKey, out var storageKeyPreimage))
                {
                    // Verify our pre-image is 2 WORDs in length.
                    if (storageKeyPreimage.Length == UInt256.SIZE * 2)
                    {
                        // If so, slice the latter WORD (parent location) and compare it to our current storage location
                        var derivedBaseLocation = storageKeyPreimage.Slice(UInt256.SIZE);
                        if (storageLocation.SlotKey.Span.SequenceEqual(derivedBaseLocation))
                        {
                            // Obtain our value hashed with our parent location (original key to our mapping).
                            var storageKeyOriginal = storageKeyPreimage.Slice(0, UInt256.SIZE);

                            // Obtain our value for this key in our mapping
                            var storageValueOriginal = storage[storageKey];

                            // Obtain our key and value's variable-value-pair.
                            var storageKeyVariable = VarParser.GetVariableObject(MappingTypeName.KeyType, VarLocation.Storage);
                            var storageValueVariable = VarParser.GetVariableObject(MappingTypeName.ValueType, VarLocation.Storage);

                            // TODO: Add our key-value pair to our results
                            results.Add((new VariableValuePair(), new VariableValuePair()));
                        }
                    }
                }
            }

            return results;
        }
        #endregion
    }
}
