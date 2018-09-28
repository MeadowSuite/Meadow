using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstArrayTypeName : AstElementaryTypeName
    {
        #region Properties
        public AstElementaryTypeName BaseType { get; }
        #endregion

        #region Constructor
        public AstArrayTypeName(JObject node) : base(node)
        {
            // Set our properties
            JToken keyType = node.SelectToken("baseType");
            if (keyType != null)
            {
                BaseType = Create<AstElementaryTypeName>((JObject)keyType);
            }
        }

        public AstArrayTypeName(AstNode node) : this(node.Node) { }
        #endregion
    }
}
