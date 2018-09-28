using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstStructDefinition : AstNode
    {
        #region Properties
        /// <summary>
        /// The name of our struct. 
        /// Ex: "struct Color" would refer to a struct with name "Color".
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The canonical name of our struct.
        /// Ex: "struct Color" in Contract "Test.sol" would refer to a struct with canonical name "Test.Color".
        /// </summary>
        public string CanonicalName { get; }
        public AstVariableDeclaration[] Members { get; }
        #endregion

        #region Constructor
        public AstStructDefinition(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            CanonicalName = node.SelectToken("canonicalName")?.Value<string>();
            Members = node.SelectTokens("members[*]").Select(x => Create<AstVariableDeclaration>((JObject)x)).ToArray();
        }

        public AstStructDefinition(AstNode node) : this(node.Node) { }
        #endregion
    }
}
