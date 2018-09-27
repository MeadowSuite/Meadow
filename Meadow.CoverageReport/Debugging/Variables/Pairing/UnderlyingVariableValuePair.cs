using Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables.Pairing
{
    /// <summary>
    /// Similar to <see cref="VariableValuePair"/> but instead including the underlying variable object "<see cref="VarBase"/>" instead of a <see cref="BaseVariable"/> type.
    /// </summary>
    public struct UnderlyingVariableValuePair
    {
        #region Properties
        public readonly VarBase Variable;
        public readonly object Value;
        #endregion

        #region Constructor
        public UnderlyingVariableValuePair(VarBase variable, object value)
        {
            // Set our properties
            Variable = variable;
            Value = value;
        }
        public UnderlyingVariableValuePair(VariableValuePair variableValuePair)
        {
            // Set our properties
            Variable = variableValuePair.Variable.ValueParser;
            Value = variableValuePair.Value;
        }
        #endregion
    }
}
