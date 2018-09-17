using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport
{
    public enum AstNodeType
    {
        [AstNodeTypeString("ContractDefinition")]
        ContractDefinition,
        [AstNodeTypeString("VariableDeclaration")]
        VariableDeclaration,
        [AstNodeTypeString("EventDefinition")]
        EventDefinition,
        [AstNodeTypeString("EnumDefinition")]
        EnumDefinition,
        [AstNodeTypeString("EnumValue")]
        EnumValue,
        [AstNodeTypeString("FunctionCall")]
        FunctionCall,
        [AstNodeTypeString("FunctionDefinition")]
        FunctionDefinition,
        [AstNodeTypeString("ModifierDefinition")]
        ModifierDefinition,
        [AstNodeTypeString("StructDefinition")]
        StructDefinition,

        [AstNodeTypeString("IfStatement")]
        IfStatement,
        [AstNodeTypeString("Conditional")]
        Conditional,

        [AstNodeTypeString("ElementaryTypeName")]
        ElementaryTypeName,

        [AstNodeTypeString("Literal")]
        Literal,
        [AstNodeTypeString("PlaceholderStatement")]
        PlaceholderStatement,

        [AstNodeTypeString("Assignment")]
        Assignment,
        [AstNodeTypeString("BinaryOperation")]
        BinaryOperation,
        [AstNodeTypeString("ExpressionStatement")]
        ExpressionStatement,
        [AstNodeTypeString("ForStatement")]
        ForStatement,
        [AstNodeTypeString("IndexAccess")]
        IndexAccess,
        [AstNodeTypeString("InlineAssembly")]
        InlineAssembly,
        [AstNodeTypeString("MemberAccess")]
        MemberAccess,
        [AstNodeTypeString("Return")]
        Return,
        [AstNodeTypeString("UnaryOperation")]
        UnaryOperation,
        [AstNodeTypeString("VariableDeclarationStatement")]
        VariableDeclarationStatement,

        [AstNodeTypeString("SourceUnit")]
        SourceUnit,
        [AstNodeTypeString("PragmaDirective")]
        PragmaDirective,
        [AstNodeTypeString("Block")]
        Block,
        [AstNodeTypeString("ParameterList")]
        ParameterList,
        [AstNodeTypeString("Identifier")]
        Identifier,

        [AstNodeTypeString("Throw")]
        Throw,
        [AstNodeTypeString("Mapping")]
        Mapping,
        [AstNodeTypeString("ArrayTypeName")]
        ArrayTypeName,
        [AstNodeTypeString("TupleExpression")]
        TupleExpression,

        [AstNodeTypeString("EmitStatement")]
        EmitStatement,
        [AstNodeTypeString("NewExpression")]
        NewExpression,
        [AstNodeTypeString("ElementaryTypeNameExpression")]
        ElementaryTypeNameExpression,

        [AstNodeTypeString("ModifierInvocation")]
        ModifierInvocation,
        [AstNodeTypeString("InheritanceSpecifier")]
        InheritanceSpecifier,
        [AstNodeTypeString("UserDefinedTypeName")]
        UserDefinedTypeName,

        [AstNodeTypeString("ImportDirective")]
        ImportDirective,

        None,
        Other
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class AstNodeTypeStringAttribute : System.Attribute
    {
        #region Properties
        /// <summary>
        /// Potential node type strings that match to the field we are attributing to.
        /// </summary>
        public string[] NodeTypeStrings { get; }
        #endregion

        #region Constructors
        public AstNodeTypeStringAttribute(params string[] nodeTypeStrings)
        {
            // Set our node types
            NodeTypeStrings = nodeTypeStrings;
        }
        #endregion
    }
}
