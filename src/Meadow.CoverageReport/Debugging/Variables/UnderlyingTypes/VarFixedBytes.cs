using Meadow.CoverageReport.AstTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarFixedBytes : VarBase
    {
        #region Constructors
        public VarFixedBytes(AstElementaryTypeName type) : base(type)
        {
            // Determine the size of our fixed array.
            int sizeBytes = VarTypes.GetFixedArraySizeInBytes(BaseType);

            // Initialize our bounds
            InitializeBounds(1, sizeBytes);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // Slice our data off.
            return data.Slice(0, SizeBytes);
        }
        #endregion
    }
}
