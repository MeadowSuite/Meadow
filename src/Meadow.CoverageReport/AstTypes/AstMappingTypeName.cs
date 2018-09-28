using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstMappingTypeName : AstElementaryTypeName
    {
        #region Properties
        public AstElementaryTypeName KeyType { get; }
        public AstElementaryTypeName ValueType { get; }
        #endregion

        #region Constructor
        public AstMappingTypeName(JObject node) : base(node)
        {
            // Set our properties
            JToken keyType = node.SelectToken("keyType");
            if (keyType != null)
            {
                KeyType = Create<AstElementaryTypeName>((JObject)keyType);
            }

            JToken valueType = node.SelectToken("valueType");
            if (valueType != null)
            {
                ValueType = Create<AstElementaryTypeName>((JObject)valueType);
            }
        }

        public AstMappingTypeName(AstNode node) : this(node.Node) { }
        #endregion
    }
}
