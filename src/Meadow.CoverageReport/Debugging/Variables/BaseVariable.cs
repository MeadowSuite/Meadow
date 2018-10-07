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
        public string Name { get; private set; }
        /// <summary>
        /// The AST node which describes the type for this variable.
        /// </summary>
        public AstElementaryTypeName AstTypeName { get; private set; }
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

        #region Functions
        protected void Initialize(AstVariableDeclaration declaration)
        {
            // Set our properties
            Declaration = declaration;

            // Initialize by name and type.
            Initialize(Declaration.Name, Declaration.TypeName);
        }

        protected void Initialize(string name, AstElementaryTypeName astTypeName)
        {
            // Set our properties
            Name = name;
            AstTypeName = astTypeName;
            BaseType = VarParser.ParseTypeComponents(AstTypeName.TypeDescriptions.TypeString).baseType;
            GenericType = VarParser.GetGenericType(BaseType);
            ValueParser = VarParser.GetVariableObject(AstTypeName, VariableLocation);
        }
        #endregion
    }
}
