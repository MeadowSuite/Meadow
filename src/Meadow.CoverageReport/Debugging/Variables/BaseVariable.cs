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
        /// Indicates the variable is strictly defined (has a type name), otherwise it is inferred, such as in the case with the "var" keyword.
        /// </summary>
        public bool IsStrictlyDefinedType => AstTypeName != null;
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
            Initialize(Declaration.Name, Declaration.TypeName, Declaration.TypeDescriptions);
        }

        protected void Initialize(string name, AstElementaryTypeName astTypeName, AstTypeDescriptions astTypeDescriptions = null)
        {
            // Set our name and type name.
            Name = name;
            AstTypeName = astTypeName;

            // Override our optionally provided type descriptions with one from the ast type name if applicable.
            astTypeDescriptions = (AstTypeName?.TypeDescriptions ?? astTypeDescriptions);

            // Parse the types from our type description.
            BaseType = VarParser.ParseTypeComponents(astTypeDescriptions.TypeString).baseType;
            GenericType = VarParser.GetGenericType(BaseType);

            // If we have a valid ast type name, obtain our value parser.
            if (IsStrictlyDefinedType)
            {
                ValueParser = VarParser.GetValueParser(AstTypeName, VariableLocation);
            }
            else
            {
                // TODO: Implement value parser for generic type.
            }
        }
        #endregion
    }
}
