using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstElementaryTypeName : AstNode
    {
        #region Properties
        /// <summary>
        /// The name of our type.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The descriptions for this type.
        /// </summary>
        public AstTypeDescriptions TypeDescriptions { get; }
        #endregion

        #region Constructor
        public AstElementaryTypeName(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            TypeDescriptions = new AstTypeDescriptions(node);
        }

        public AstElementaryTypeName(AstNode node) : this(node.Node) { }
        #endregion
    }
}
