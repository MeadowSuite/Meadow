using Meadow.CoverageReport.AstTypes;
using Meadow.CoverageReport.AstTypes.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Meadow.CoverageReport
{
    public class AstNode : IEquatable<AstNode>
    {
        #region Fields
        private static Dictionary<string, AstNodeType> _nodeTypeLookup;
        #endregion

        #region Properties
        public SourceRange SourceRange { get; }
        public JObject Node { get; }
        public AstNodeType NodeType { get; }
        public long Id { get; }
        public int SourceIndex => SourceRange.SourceIndex;
        public string FileName { get; }
        public string FilePath { get; }
        #endregion

        string _nodeJsonString;
        public string NodeJsonString => _nodeJsonString ?? (_nodeJsonString = Node.ToString(Formatting.Indented));

        AstNode _parent;
        public AstNode Parent => _parent ?? (_parent = GetParent(this));

        #region Constructor
        public AstNode(JObject node)
        {
            SourceRange = new SourceRange(node.Value<string>("src"));
            Node = node;
            NodeType = GetTypeFromString(node.Value<string>("nodeType"));
            FileName = node.Value<string>("file");
            FilePath = node.Value<string>("absolutePath");
            Id = node.Value<long>("id");
        }
        #endregion

        #region Functions
        public static T Create<T>(JObject node) where T : AstNode
        {
            return Create(node) as T;
        }

        public static AstNode Create(JObject node)
        {
            // If the node is null, return null.
            if (node == null)
            {
                return null;
            }

            // Obtain the node type
            string nodeTypeString = node.SelectToken("nodeType")?.Value<string>();

            // Get our type from our string.
            AstNodeType nodeType = GetTypeFromString(nodeTypeString);

            // Determine what type of AST node this should be.
            switch (nodeType)
            {
                case AstNodeType.ContractDefinition:
                    return new AstContractDefinition(node);
                case AstNodeType.VariableDeclaration:
                    return new AstVariableDeclaration(node);
                case AstNodeType.FunctionDefinition:
                    return new AstFunctionDefinition(node);
                case AstNodeType.StructDefinition:
                    return new AstStructDefinition(node);
                case AstNodeType.EnumDefinition:
                    return new AstEnumDefinition(node);
                case AstNodeType.EnumValue:
                    return new AstEnumMember(node);
                case AstNodeType.ElementaryTypeName:
                    return new AstElementaryTypeName(node);
                case AstNodeType.UserDefinedTypeName:
                    return new AstUserDefinedTypeName(node);
                case AstNodeType.ArrayTypeName:
                    return new AstArrayTypeName(node);
                case AstNodeType.Mapping:
                    return new AstMappingTypeName(node);
            }

            // For any other type, we return a generic ast node.
            return new AstNode(node);
        }

        public static AstNodeType GetTypeFromString(string nodeTypeString)
        {
            // If the node type string is null, we return none
            if (string.IsNullOrEmpty(nodeTypeString))
            {   
                return AstNodeType.None;
            }   

            // If our type lookup isn't null
            if (_nodeTypeLookup == null)
            {
                // Create our node lookup.
                _nodeTypeLookup = new Dictionary<string, AstNodeType>(StringComparer.InvariantCultureIgnoreCase);

                // Obtain every enum option for this enum
                AstNodeType[] nodeTypes = (AstNodeType[])Enum.GetValues(typeof(AstNodeType));

                // Loop for each node type
                foreach (AstNodeType nodeType in nodeTypes)
                {
                    // Obtain the ast node type string attribute
                    FieldInfo fi = nodeType.GetType().GetField(nodeType.ToString());
                    if (fi == null)
                    {
                        continue;
                    }

                    // Obtain all attributes of type we are interested in.
                    var attributes = fi.GetCustomAttributes<AstNodeTypeStringAttribute>(false).ToArray();

                    // If one exists, cache it and return it.
                    if (attributes != null && attributes.Length > 0)
                    {
                        var attribute = attributes[0];
                        foreach (string typeString in attribute.NodeTypeStrings)
                        {
                            _nodeTypeLookup[typeString] = nodeType;
                        }
                    }
                }
            }

            // Try to obtain our type from lookup, if we fail return the 'other' type.
            bool success = _nodeTypeLookup.TryGetValue(nodeTypeString, out AstNodeType value);
            if (success)
            {
                return value;
            }   
            else
            {
                return AstNodeType.Other;
            }   
        }

        public static AstDeclarationVisibility GetVisibilityFromString(string visibilityTypeString)
        {
            switch (visibilityTypeString)
            {
                case "private":
                    return AstDeclarationVisibility.Private;
                case "public":
                    return AstDeclarationVisibility.Public;
                case "external":
                    return AstDeclarationVisibility.External;
                case "internal":
                    return AstDeclarationVisibility.Internal;
                default:
                    return AstDeclarationVisibility.Public;
            }
        }

        public static AstNode GetParent(AstNode node)
        {
            var parent = node.Node.Parent;
            while (parent != null)
            {
                if (parent is JObject jobj)
                {
                    if (jobj.ContainsKey("src"))
                    {
                        return Create(jobj);
                    }
                }

                parent = parent.Parent;
            }

            return null;
        }

        public bool ReferencesDeclaration(AstNode declarationNode, bool recursive = true)
        {
            // Grab the id for the declaration
            long? id = declarationNode.Node.SelectToken("id")?.Value<long>();

            // If our id does not exist, we could not have referenced it
            if (!id.HasValue)
            {
                return false;
            }

            // Check if this references our declaration
            return ReferencesDeclaration(this.Node, id.Value, recursive);
        }

        private static bool ReferencesDeclaration(JToken jToken, long declarationId, bool recursive = true)
        {
            // If this object directly references the declaration, we return true.
            long? referencesDeclaration = jToken.SelectToken("referencedDeclaration")?.Value<long?>();

            // If we have a value and it matches our ID, return true.
            if (referencesDeclaration.HasValue && referencesDeclaration.Value == declarationId)
            {
                return true;
            }

            // If we want to check recursively
            if (recursive)
            {
                // This object had no reference to the declaration, check our children.
                var children = jToken.Children();
                foreach (var child in children)
                {
                    // Check if our child references the declaration.
                    if (ReferencesDeclaration(child, declarationId, recursive))
                    {
                        return true;
                    }
                }
            }

            // Return false for we could not find a reference to this declaration identifier.
            return false;
        }

        public T GetImmediateOrAncestor<T>() where T : AstNode
        {
            // Loop upwards trying to find an ancestor of the specified type.
            AstNode currentNode = this;
            while (currentNode != null)
            {
                // If this node is the correct type.
                if (currentNode is T)
                {
                    return (T)currentNode;
                }

                // Set our current node as our parent.
                currentNode = currentNode.Parent;
            }

            // Return null if we couldn't find a node of our type
            return null;
        }

        public override bool Equals(object obj)
        {
            return obj is AstNode n && Equals(n);
        }

        public bool Equals(AstNode other)
        {
            return other.SourceRange.Equals(SourceRange);
        }

        public override int GetHashCode()
        {
            return SourceRange.GetHashCode();
        }

        public override string ToString()
        {
            return $"{NodeType}, {SourceRange}";
        }
        #endregion
    }


}

