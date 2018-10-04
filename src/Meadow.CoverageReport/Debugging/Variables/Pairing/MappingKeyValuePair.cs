using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Pairing
{
    /// <summary>
    /// Represents a mapping key-value pair.
    /// </summary>
    public struct MappingKeyValuePair
    {
        #region Properties
        public readonly VariableValuePair Key;
        public readonly VariableValuePair Value;
        #endregion

        #region Constructor
        public MappingKeyValuePair(VariableValuePair key, VariableValuePair value)
        {
            // Set our properties
            Key = key;
            Value = value;
        }
        #endregion
    }
}
