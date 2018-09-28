using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Pairing
{
    /// <summary>
    /// Represents a variable definition and its underlying value.
    /// </summary>
    public struct VariableValuePair
    {
        #region Properties
        public readonly BaseVariable Variable;
        public readonly object Value;
        #endregion

        #region Constructor
        public VariableValuePair(BaseVariable variable, object value)
        {
            // Set our properties
            Variable = variable;
            Value = value;
        }
        #endregion
    }
}
