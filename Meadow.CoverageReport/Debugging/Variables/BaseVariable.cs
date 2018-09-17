using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.AstTypes.Enums;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables
{
    /// <summary>
    /// Represents the base for a local/state variable derived from certain execution state components from execution traces/runtime.
    /// </summary>
    public abstract class BaseVariable
    {
        #region Properties
        /// <summary>
        /// The variable declaration ast node which defines this variable.
        /// </summary>
        public AstVariableDeclaration Declaration { get; set; }
        /// <summary>
        /// The name of this variable.
        /// </summary>
        public string Name => Declaration.Name;
        /// <summary>
        /// The base type string of this variable, which is likened to the full <see cref="Type"/> but with location information stripped.
        /// </summary>
        public string BaseType { get; private set; }
        /// <summary>
        /// The generic type enum derived from other type information, which can be most easily used to categorize this variable's type.
        /// </summary>
        public VarGenericType GenericType { get; private set; }
        /// <summary>
        /// Represents the variable's underlying data location in less trivial cases.
        /// </summary>
        public abstract VarLocation VariableLocation { get; }
        /// <summary>
        /// Represents the underlying data type value parser, which parses values for the variable given components of execution states.
        /// </summary>
        public VarBase ValueParser { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes the variable with a given declaration, parsing all relevant type information and associating the appropriate value parser. 
        /// </summary>
        /// <param name="declaration">The ast variable declaration which constitutes the solidity declaration of the variable we wish to interface with.</param>
        public BaseVariable(AstVariableDeclaration declaration)
        {
            // Set our properties
            Declaration = declaration;
            BaseType = VarTypes.ParseTypeComponents(Declaration.TypeName.TypeDescriptions.TypeString).baseType;
            GenericType = VarTypes.GetGenericType(BaseType);
            ValueParser = VarTypes.GetVariableObject(Declaration.TypeName, VariableLocation);
        }
        #endregion

        #region Functions
        #endregion
    }
}
