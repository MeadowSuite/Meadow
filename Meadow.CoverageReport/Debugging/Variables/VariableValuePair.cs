using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables
{
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
