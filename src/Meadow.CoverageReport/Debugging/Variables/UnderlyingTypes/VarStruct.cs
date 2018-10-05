using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Meadow.CoverageReport.Debugging.Variables.Storage;
using Meadow.JsonRpc.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarStruct : VarRefBase
    {
        #region Properties
        public AstStructDefinition StructDefinition { get; }
        public StateVariable[] Members { get; }
        #endregion

        #region Constructors
        public VarStruct(AstUserDefinedTypeName type, VarLocation location) : base(type)
        {
            // Set our struct definition
            StructDefinition = AstParser.GetNode<AstStructDefinition>(type.ReferencedDeclaration);

            // Obtain our struct members.
            Members = StructDefinition.Members.Select(x => new StateVariable(x)).ToArray();

            // Resolve all of the storage locations for these state variables.
            StorageLocation endLocation = StorageManager.ResolveStorageSlots(Members);

            // Obtain our next free slot based off of our end location.
            int nextFreeSlot = (int)endLocation.SlotKeyInteger;
            if (endLocation.DataOffset > 0)
            {
                nextFreeSlot++;
            }

            // Our next free slot signifies our used storage entry count to that point.

            // Initialize our bounds
            InitializeBounds(nextFreeSlot, UInt256.SIZE, location);
        }
        #endregion

        #region Functions
        public override object ParseDereferencedFromMemory(Memory<byte> memory, int offset)
        {
            // Create our result array
            var results = new VariableValuePair[Members.Length];

            // Loop for each result we need to evaluate
            for (int i = 0; i < Members.Length; i++)
            {
                // Set our indexed result
                results[i] = new VariableValuePair(Members[i], Members[i].ValueParser.ParseFromMemory(memory, offset));

                // Advance our offset
                offset += UInt256.SIZE;
            }

            return results;
        }

        public override object ParseFromStorage(StorageManager storageManager, StorageLocation storageLocation, IJsonRpcClient rpcClient = null)
        {
            // Create our result array
            var results = new VariableValuePair[Members.Length];

            // Loop for each result we need to evaluate
            for (int i = 0; i < Members.Length; i++)
            {
                // Define our member's definite storage location (this struct's location + the member's static location).
                StorageLocation memberLocation = new StorageLocation(storageLocation.SlotKeyInteger + Members[i].StorageLocation.SlotKeyInteger, storageLocation.DataOffset + Members[i].StorageLocation.DataOffset);

                // Parse our result for our indexed variable.
                results[i] = new VariableValuePair(Members[i], Members[i].ValueParser.ParseFromStorage(storageManager, memberLocation, rpcClient));
            }

            return results;
        }
        #endregion
    }
}
