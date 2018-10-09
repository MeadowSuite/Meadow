using Meadow.Core.EthTypes;
using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes
{
    public class VarAddress : VarBase
    {
        #region Constructors
        public VarAddress(AstElementaryTypeName type) : base(type)
        {
            // Initialize our bounds
            InitializeBounds(1, Address.SIZE);
        }
        #endregion

        #region Functions
        public override object ParseData(Memory<byte> data)
        {
            // If there is insufficient data, return a zero address.
            if (data.Length < Address.SIZE)
            {
                return new Address(new byte[Address.SIZE]);
            }
            else
            {
                // Otherwise the boolean is parsed from the first byte.
                return new Address(data.Slice(data.Length - Address.SIZE, Address.SIZE).ToArray());
            }
        }
        #endregion
    }
}
