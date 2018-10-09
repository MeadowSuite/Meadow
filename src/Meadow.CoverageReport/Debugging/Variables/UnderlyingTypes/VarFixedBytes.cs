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
            int sizeBytes = VarParser.GetFixedArraySizeInBytes(BaseType);

            // Initialize our bounds
            InitializeBounds(1, sizeBytes);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // If our data is not the correct size, return an empty array
            if (data.Length < SizeBytes)
            {
                return default(Memory<byte>);
            }

            // Slice our data off.
            return data.Slice(0, SizeBytes);
        }
        #endregion
    }
}
