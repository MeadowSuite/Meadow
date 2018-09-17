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
            // TODO: Implement
            return null;
        }
        #endregion
    }
}
