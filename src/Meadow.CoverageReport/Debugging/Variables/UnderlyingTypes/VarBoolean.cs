using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarBoolean : VarBase
    {
        #region Constructors
        public VarBoolean(string typeString) : base(typeString)
        {
            // Initialize our bounds
            InitializeBounds(1, 1);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // If there is no data, return false
            if (data.Length == 0)
            {
                return false;
            }
            else
            {
                // Otherwise the boolean is parsed from the first byte.
                return data.Span[data.Length - 1] != 0;
            }
        }
        #endregion
    }
}
