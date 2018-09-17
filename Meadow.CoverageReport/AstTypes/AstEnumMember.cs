using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstEnumMember : AstNode
    {
        #region Properties
        public string Name { get; }
        #endregion

        #region Constructor
        public AstEnumMember(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
        }

        public AstEnumMember(AstNode node) : this(node.Node) { }
        #endregion
    }
}
