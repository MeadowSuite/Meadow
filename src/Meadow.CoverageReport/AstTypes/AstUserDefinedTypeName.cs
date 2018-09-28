using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstUserDefinedTypeName : AstElementaryTypeName
    {
        #region Properties
        /// <summary>
        /// The ID of the node which declares this type definition.
        /// </summary>
        public long ReferencedDeclaration { get; }
        #endregion

        #region Constructor
        public AstUserDefinedTypeName(JObject node) : base(node)
        {
            // Set our properties.
            ReferencedDeclaration = node.SelectToken("referencedDeclaration").Value<long>();
        }

        public AstUserDefinedTypeName(AstNode node) : this(node.Node) { }
        #endregion
    }
}
