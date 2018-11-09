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
        public string Name { get; set; }
        /// <summary>
        /// The type string derived from AST nodes.
        /// </summary>
        public string TypeString { get; private set; }
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
            Initialize(Declaration.Name, Declaration.TypeName, Declaration.TypeDescriptions);
        }

        protected void Initialize(string name, AstElementaryTypeName astTypeName, AstTypeDescriptions astTypeDescriptions = null)
        {
            // Override our optionally provided type descriptions with one from the ast type name, if available.
            astTypeDescriptions = (astTypeName?.TypeDescriptions ?? astTypeDescriptions);

            // Initialize using whatever type provider is available
            Initialize(name, astTypeDescriptions.TypeString);
        }

        protected void Initialize(string name, string typeString)
        {
            // Set our name and type string.
            Name = name;
            TypeString = typeString;

            // Parse the types from our type description.
            BaseType = VarParser.ParseTypeComponents(typeString).baseType;
            GenericType = VarParser.GetGenericType(BaseType);

            // Obtain our value parser
            ValueParser = VarParser.GetValueParser(typeString, VariableLocation);
        }
        #endregion
    }
}
