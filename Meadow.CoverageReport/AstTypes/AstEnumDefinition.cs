using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstEnumDefinition : AstNode
    {
        #region Properties
        /// <summary>
        /// The name of our enum. 
        /// Ex: "enum Color" would refer to an enum with name "Color".
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The canonical name of our enum.
        /// Ex: "enum Color" in Contract "Test.sol" would refer to an enum with canonical name "Test.Color".
        /// </summary>
        public string CanonicalName { get; }
        public AstEnumMember[] Members { get; }
        #endregion

        #region Constructor
        public AstEnumDefinition(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            CanonicalName = node.SelectToken("canonicalName")?.Value<string>();
            Members = node.SelectTokens("members[*]").Select(x => Create<AstEnumMember>((JObject)x)).ToArray();
        }

        public AstEnumDefinition(AstNode node) : this(node.Node) { }
        #endregion
    }
}
