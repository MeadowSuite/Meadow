using Meadow.Contract;
using Meadow.CoverageReport.AstTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SolcNet.DataDescription.Output;

namespace Meadow.CoverageReport
{
    public abstract class AstParser
    {
        #region Fields
        private static object _parseLock = new object();
        private static ConcurrentDictionary<long, AstNode> _nodesById;
        private static ConcurrentDictionary<Type, List<AstNode>> _nodesByType;
        private static ConcurrentDictionary<(int index, long start, long length), List<AstNode>> _nodesBySourceMapEntryExact;
        private static ConcurrentDictionary<(int index, long start, long length), IEnumerable<AstNode>> _nodesBySourceMapEntryContained;
        #endregion

        #region Properties
        public static AstNode[] AllAstNodes { get; private set; }
        public static AstContractDefinition[] ContractNodes { get; private set; }
        public static bool IsParsed { get; private set; }
        #endregion

        #region Functions
        public static void Parse()
        {
            lock (_parseLock)
            {
                // If this is already parsed, stop
                if (IsParsed)
                {
                    return;
                }

                // Initialize our lookups
                _nodesById = new ConcurrentDictionary<long, AstNode>();
                _nodesByType = new ConcurrentDictionary<Type, List<AstNode>>();
                _nodesBySourceMapEntryExact = new ConcurrentDictionary<(int index, long start, long length), List<AstNode>>();
                _nodesBySourceMapEntryContained = new ConcurrentDictionary<(int index, long start, long length), IEnumerable<AstNode>>();

                // Get our generated solc data
                var solcData = GeneratedSolcData.Default.GetSolcData();

                // Obtain all ast nodes
                AllAstNodes = AstHelper.IndexAstNodes(solcData.SolcSourceInfo).FullNodeArray;

                // Populate our lookups for nodes.
                PopulateLookups();

                // And extract some node types
                ContractNodes = GetNodes<AstContractDefinition>().ToArray();

                // Set our parsed status
                IsParsed = true;
            }
        }

        private static void PopulateLookups()
        {
            // Loop through all nodes
            for (int i = 0; i < AllAstNodes.Length; i++)
            {
                // Obtain the current indexed node
                AstNode currentNode = AllAstNodes[i];

                // Update our lookup by ID
                _nodesById[currentNode.Id] = currentNode;

                // Initialize our nodes list for nodes of this type if we haven't yet.
                Type nodeType = currentNode.GetType();
                if (!_nodesByType.ContainsKey(nodeType))
                {
                    _nodesByType[nodeType] = new List<AstNode>();
                }

                // Add the node of this type to the lookup by type.
                _nodesByType[nodeType].Add(currentNode);

                // Populate the exact match lookup
                var exactMatchKey = (currentNode.SourceIndex, currentNode.SourceRange.Offset, currentNode.SourceRange.Length);

                // Obtain the list for this source map entry
                if (!_nodesBySourceMapEntryExact.TryGetValue(exactMatchKey, out var nodesForSourceRange))
                {
                    // Initialize a new list
                    nodesForSourceRange = new List<AstNode>();

                    // Set it in the lookup
                    _nodesBySourceMapEntryExact[exactMatchKey] = nodesForSourceRange;
                }

                // Add this node to the list
                nodesForSourceRange.Add(currentNode);
            }
        }

        public static IEnumerable<T> GetNodes<T>() where T : AstNode
        {
            // If we can obtain a list of nodes from the type, return it
            if (_nodesByType.TryGetValue(typeof(T), out var nodeListOfType))
            {
                return nodeListOfType.Cast<T>();
            }

            // Create a list of our resulting ast node type.
            List<T> resultList = new List<T>();

            // Loop for each ast node.
            foreach (AstNode node in AllAstNodes)
            {
                // If the type is our desired type, add it to our list.
                if (node is T)
                {
                    resultList.Add((T)node);
                }
            }

            // Return our resulting array
            return resultList;
        }

        public static IEnumerable<AstNode> GetNodes(SourceMapEntry sourceMapEntry, bool exactMatch = false)
        {
            // Create our lookup key
            var lookupKey = (sourceMapEntry.Index, sourceMapEntry.Offset, sourceMapEntry.Length);

            // Determine if we have a cached result, if so, return it.
            if (exactMatch)
            {
                // Try to obtain our list from the lookup
                if (_nodesBySourceMapEntryExact.TryGetValue(lookupKey, out var result))
                {
                    return result;
                }

                // If we already populated the exact match list, we'll we couldn't find our item then.
                return Array.Empty<AstNode>();
            }
            else
            {
                // Try to obtain our list from the lookup.
                if (_nodesBySourceMapEntryContained.TryGetValue(lookupKey, out var result))
                {
                    return result;
                }

                // Otherwise we populate for this position.
                var resultList = AllAstNodes.Where(a =>
                {
                    return a.SourceRange.SourceIndex == sourceMapEntry.Index &&
                           a.SourceRange.Offset >= sourceMapEntry.Offset &&
                           a.SourceRange.Offset + a.SourceRange.Length <=
                           sourceMapEntry.Offset + sourceMapEntry.Length;
                });
                _nodesBySourceMapEntryContained[lookupKey] = resultList;
                return resultList;
            }
        }

        public static IEnumerable<T> GetNodes<T>(SourceMapEntry sourceMapEntry, bool exactMatch = false) where T : AstNode
        {
            // Obtain our nodes for this source map
            var nodes = GetNodes(sourceMapEntry, exactMatch);

            // Obtain the nodes which are of the given type.
            return nodes.Where(a => a is T).Cast<T>();
        }

        public static IEnumerable<AstNode> GetNodes(SourceMapEntry sourceMapEntry, bool exactMatch, AstNodeType nodeType)
        {
            // Obtain our nodes for this source map
            var nodes = GetNodes(sourceMapEntry, exactMatch);

            // Obtain the nodes which are of the given type.
            return nodes.Where(a => a.NodeType == nodeType);
        }

        public static T GetNode<T>(long id) where T : AstNode
        {
            return (T)GetNode(id);
        }

        public static AstNode GetNode(long id)
        {
            // Try to obtain our ast node using this id.
            bool success = _nodesById.TryGetValue(id, out var node);

            // If we succeeded, return our node, otherwise null.
            if (success)
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        public static T GetNode<T>(AstNode node, string path) where T : AstNode
        {
            return GetNode<T>(node.Node, path);
        }

        public static T GetNode<T>(JObject node, string path) where T : AstNode
        {
            return (T)GetNode(node, path);
        }

        public static AstNode GetNode(AstNode node, string path)
        {
            return GetNode(node.Node, path);
        }

        public static AstNode GetNode(JObject node, string path)
        {
            // Obtain the id from the node.
            long? id = node.SelectToken($"{path}.id").Value<long>();

            // Verify we were able to obtain an ID.
            if (!id.HasValue)
            {
                throw new ArgumentException("Unable to resolve node ID when selecting node in AST parser.");
            }

            // Obtain our node by ID.
            return GetNode(id.Value);
        }

        public static AstContractDefinition[] GetLinearizedBaseContracts(AstContractDefinition contractNode)
        {
            // Obtain our linearized contracts
            AstContractDefinition[] linearizedContracts = contractNode.LinearizedBaseContracts.Select(x => GetNode<AstContractDefinition>(x)).ToArray();

            // Return the array
            return linearizedContracts;
        }

        public static AstVariableDeclaration[] GetStateVariableDeclarations(AstContractDefinition contractNode)
        {
            // Loop for each linearized base contract (includes the contract id itself).
            AstContractDefinition[] linearizedContracts = GetLinearizedBaseContracts(contractNode);

            // Create a list of our resulting state variables.
            List<AstVariableDeclaration> stateVariables = new List<AstVariableDeclaration>();

            // Loop for each contract our target contract is based off of, and add it's variable declarations.
            for (int i = linearizedContracts.Length - 1; i >= 0; i--)
            {
                stateVariables.AddRange(linearizedContracts[i].VariableDeclarations);
            }

            // Return our state variable array.
            return stateVariables.ToArray();
        }
        #endregion
    }
}
