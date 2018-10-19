using Meadow.CoverageReport.AstTypes.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.AstTypes
{
    public class AstVariableDeclaration : AstNode
    {
        #region Properties
        public string Name { get; }
        /// <summary>
        /// The visibility of this variable.
        /// </summary>
        public AstDeclarationVisibility Visibility { get; }
        public AstElementaryTypeName TypeName { get; }
        public AstVariableStorageLocation StorageLocation { get; }
        public bool Constant { get; }
        public bool StateVariable { get; }
        /// <summary>
        /// The descriptions for this type.
        /// </summary>
        public AstTypeDescriptions TypeDescriptions { get; }
        #endregion

        #region Constructor
        public AstVariableDeclaration(JObject node) : base(node)
        {
            // Set our properties
            Name = node.SelectToken("name")?.Value<string>();
            Visibility = GetVisibilityFromString(node.SelectToken("visibility")?.Value<string>());
            TypeName = Create<AstElementaryTypeName>(node.SelectToken("typeName") as JObject);
            StateVariable = node.SelectToken("stateVariable")?.Value<bool>() == true;
            Constant = node.SelectToken("constant")?.Value<bool>() == true;

            // Determine our storage location
            string storageLoc = node.SelectToken("storageLocation")?.Value<string>()?.ToLower(CultureInfo.InvariantCulture);

            switch (storageLoc)
            {
                case "default":
                    StorageLocation = AstVariableStorageLocation.Default;
                    break;
                case "memory":
                    StorageLocation = AstVariableStorageLocation.Memory;
                    break;
                case "storage":
                    StorageLocation = AstVariableStorageLocation.Storage;
                    break;

                default:
                    throw new ArgumentException($"Invalid {nameof(AstVariableStorageLocation)} values when parsing {nameof(AstVariableDeclaration)}");
            }

            // Parse our type descriptions
            TypeDescriptions = new AstTypeDescriptions(node);
        }

        public AstVariableDeclaration(AstNode node) : this(node.Node) { }
        #endregion
    }
}
