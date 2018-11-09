using Meadow.Core.AbiEncoding;
using Meadow.Core.Utils;
using Meadow.CoverageReport.AstTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarInt : VarBase
    {
        #region Constructors
        public VarInt(string typeString) : base(typeString)
        {
            // Obtain our size in bytes
            int sizeBytes = VarParser.GetIntegerSizeInBytes(BaseType, GenericType);

            // Initialize our bounds
            InitializeBounds(1, sizeBytes);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // Read an signed integer of the specified size
            return BigIntegerConverter.GetBigInteger(data.Span, true, SizeBytes);
        }
        #endregion
    }
}
