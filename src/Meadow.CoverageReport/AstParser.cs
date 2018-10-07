using Meadow.Contract;
using Meadow.CoverageReport.AstTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.CoverageReport
{
    public abstract class AstParser
    {
        #region Fields
        private static object _parseLock = new object();
        private static Dictionary<long, AstNode> _nodesById;
        private static Dictionary<Type, List<AstNode>> _nodesByType;
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
                _nodesById = new Dictionary<long, AstNode>();
                _nodesByType = new Dictionary<Type, List<AstNode>>();

                // Get our generated solc data
                var solcData = GeneratedSolcData.Default.GetSolcData();

                // Obtain all ast nodes
                AllAstNodes = AstHelper.IndexAstNodes(solcData.SolcSourceInfo).FullNodeArray;

                // Populate our lookups for nodes.
                PopulateLookups();

                // And extract some node types
                ContractNodes = GetNodes<AstContractDefinition>();

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
            }
        }

        public static T[] GetNodes<T>() where T : AstNode
        {
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
            return resultList.ToArray();
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
