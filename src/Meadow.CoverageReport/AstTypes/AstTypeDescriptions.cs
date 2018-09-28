using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstTypeDescriptions
    {
        #region Properties
        public string TypeIdentifier { get; }
        public string TypeString { get; }
        #endregion

        #region Constructor
        public AstTypeDescriptions(JObject node)
        {
            // Set our properties
            TypeIdentifier = node.SelectToken("typeDescriptions.typeIdentifier")?.Value<string>();
            TypeString = node.SelectToken("typeDescriptions.typeString")?.Value<string>();
        }

        public AstTypeDescriptions(AstNode node) : this(node.Node) { }
        #endregion
    }
}
