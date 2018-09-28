using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstContractDefinition : AstNode
    {
        #region Properties
        public string Name { get; set; }
        public long[] LinearizedBaseContracts { get; }
        public AstNode[] Nodes { get; }
        public AstVariableDeclaration[] VariableDeclarations { get; }
        public AstEnumDefinition[] EnumDefinitions { get; }
        public AstNode[] EventDefinitions { get; }
        public AstFunctionDefinition[] FunctionDefinitions { get; }
        public AstNode[] ModifierDefinitions { get; }
        #endregion

        #region Constructor
        public AstContractDefinition(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            FunctionDefinitions = Array.Empty<AstFunctionDefinition>();

            // Obtain our children and base contracts
            Nodes = node.SelectTokens("nodes[*]").Select(x => Create((JObject)x)).ToArray();
            LinearizedBaseContracts = node.SelectTokens("linearizedBaseContracts[*]")?.Values<long>()?.ToArray();

            // Obtain our filtered children types.
            VariableDeclarations = Nodes.Where(x => x is AstVariableDeclaration).Select(x => (AstVariableDeclaration)x).ToArray();
            EnumDefinitions = Nodes.Where(x => x is AstEnumDefinition).Select(x => new AstEnumDefinition(x)).ToArray();
            FunctionDefinitions = Nodes.Where(x => x is AstFunctionDefinition).Select(x => new AstFunctionDefinition(x)).ToArray();

            // Obtain our filtered node types (TODO: type classes to be implemented).
            EventDefinitions = Nodes.Where(x => x.NodeType == AstNodeType.EventDefinition).ToArray();
            ModifierDefinitions = Nodes.Where(x => x.NodeType == AstNodeType.ModifierDefinition).ToArray();
        }

        public AstContractDefinition(AstNode node) : this(node.Node) { }
        #endregion
    }
}
