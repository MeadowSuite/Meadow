using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Newtonsoft.Json.Linq;
using SolcNet.DataDescription.Output;
using SolcNet.DataDescription.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    static class SourceAnalysis
    {
        // Broad goals:
        // * Create a list of ranges from the AST nodes
        // * Identify ranges with that contain an entry in the sourcemap as active code
        // * Figure out all the ast node types that can have active code


        /*
         * The ast tree from solc output contains a source range for every entry, with the range 
         * length getting shorter (more precise) the deeper into the tree you traverse.
         * 
         * We walking the tree and create a dictionary with the source code file ID as key, and 
         * a list of nodes as the value.
         * 
         * Then we sort the AST node lists by the length of the source code range, from most precise
         * to least precise.
         * 
         * We then get the opcode sourcemap from solc, where each entry specifies: an opcode index, 
         * a source code file ID, and a source code range.
         * 
         * We iterate the sourcemap entries and match each entry to the first AST node that contains
         * the source code range.
         * 
         * The result is filtered list of AST nodes that represent active source code ranges.
         * 
         * We then do covered/uncovered highlighting of those ranges. Some AST node source code spans
         * may be too precise. We manually identify AST node types that are too precise and create
         * list of AST node types where we use the source code range from its parent or grandparent 
         * AST node.
         * 
         */

        /// <summary>
        /// Ast node types not allowed when mapping sourcemap entries
        /// </summary>
        static readonly AstNodeType[] FilterNodeTypes =
        {
            AstNodeType.ContractDefinition,
            AstNodeType.FunctionDefinition,
            AstNodeType.ElementaryTypeName,
            AstNodeType.VariableDeclaration,
            AstNodeType.Literal,
            AstNodeType.PlaceholderStatement,
            AstNodeType.UserDefinedTypeName
        };

        /// <summary>
        /// Ast node types to use when walking the entire ast node tree
        /// </summary>
        static readonly AstNodeType[] InterestNodeTypes =
        {
            AstNodeType.Assignment,
            AstNodeType.BinaryOperation,
            AstNodeType.ExpressionStatement,
            AstNodeType.ForStatement,
            AstNodeType.FunctionCall,
            AstNodeType.IndexAccess,
            AstNodeType.InlineAssembly,
            AstNodeType.MemberAccess,
            AstNodeType.Return,
            AstNodeType.UnaryOperation,
            // AstNodeType.VariableDeclarationStatement
        };

        public static AnalysisResults Run(
            SolcSourceInfo[] sourcesList,
            SolcBytecodeInfo[] byteCodeData)
        {

            // Parses the ast tree json for all the sources, and creates a dictionary with the source file index/ID as the key.
            var (astDict, fullNodeArray) = AstHelper.IndexAstNodes(sourcesList);

            // source node lists by their source code range length (shortest / most precise lengths first)
            foreach (var arr in astDict.Values)
            {
                Array.Sort(arr, (a, b) => a.SourceRange.Length - b.SourceRange.Length);
            }

            // AST nodes matching entries in the evm.bytecode.sourcemap output from solc.
            // These are source code ranges that are executing during deployment of a contract - such as the constructor function.
            var sourceMapNodes = new List<AstNode>();

            // AST nodes matching entries in the evm.bytecodeDeployed.sourcemap output from solc.
            // These are source code ranges that can be executed by transactions and calls to a deployed contract.
            var sourceMapDeployedNodes = new List<AstNode>();

            // Dictionary of all parsed sourcemap entries.
            var sourceMaps = new Dictionary<(string FileName, string ContractName, string BytecodeHash), (SourceMapEntry[] nonDeployed, SourceMapEntry[] deployed)>();

            foreach (var entry in byteCodeData)
            {

                var sourceMapEntriesNonDeployed = new List<SourceMapEntry>();
                var sourceMapEntriesDeployed = new List<SourceMapEntry>();

                // Match evm.bytecode.sourcemap entries to ast nodes.
                if (!string.IsNullOrEmpty(entry.SourceMap))
                {
                    var (sourceMapItems, matchedNodes) = ParseSourceMap(entry.SourceMap, astDict);
                    sourceMapEntriesNonDeployed.AddRange(sourceMapItems);
                    sourceMapNodes.AddRange(matchedNodes);
                }

                // Match evm.bytecodeDeployed.sourcemap entries to ast nodes.
                if (!string.IsNullOrEmpty(entry.SourceMapDeployed))
                {
                    var (sourceMapItems, matchedNodes) = ParseSourceMap(entry.SourceMapDeployed, astDict);
                    sourceMapEntriesDeployed.AddRange(sourceMapItems);
                    sourceMapDeployedNodes.AddRange(matchedNodes);
                }

                if (sourceMapEntriesNonDeployed.Count == 0 && sourceMapEntriesDeployed.Count == 0)
                {
                    continue;
                }

                sourceMaps.Add((entry.FilePath, entry.ContractName, entry.BytecodeHash), (sourceMapEntriesNonDeployed.ToArray(), sourceMapEntriesDeployed.ToArray()));
            }

            // Get nodes with duplicates filtered out, and the useless/harmful node types fitlered out
            var nodeLists = new
            {
                SourceMapNodes = AstHelper.GetNodesNotOfTypes(sourcesList, sourceMapNodes, FilterNodeTypes).Distinct().ToArray(),
                SourceMapDeployedNodes = AstHelper.GetNodesNotOfTypes(sourcesList, sourceMapDeployedNodes, FilterNodeTypes).Distinct().ToArray(),
                AllActiveNodes = AstHelper.GetNodesOfTypes(sourcesList, fullNodeArray, InterestNodeTypes).Distinct().ToArray(),
                FunctionNodes = GetFunctionNodesWithActiveCode(sourcesList, fullNodeArray).ToArray()
            };

#if DEBUG_AST
            // Get node lists with the subsets filtered out
            var uniqueSubsetFiltered = new {
                SourceMapNodes = AstHelper.RemoveSubsets(nodeLists.SourceMapNodes),
                SourceMapDeployedNodes = AstHelper.RemoveSubsets(nodeLists.SourceMapDeployedNodes),
                AllActiveNodes = AstHelper.RemoveSubsets(nodeLists.AllActiveNodes),
            };

            // Get node lists with the superset nodes filtered out
            var uniqueSupersetFiltered = new {
                SourceMapNodes = AstHelper.RemoveSupersets(nodeLists.SourceMapNodes),
                SourceMapDeployedNodes = AstHelper.RemoveSupersets(nodeLists.SourceMapDeployedNodes),
                AllActiveNodes = AstHelper.RemoveSupersets(nodeLists.AllActiveNodes)
            };


            // Get the node types for the various node lists
            var nodeTypes = new {
                SourceMapNodes = nodeLists.SourceMapNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                SourceMapNodesSubsetFiltered = uniqueSubsetFiltered.SourceMapNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                SourceMapNodesSupersetFiltered = uniqueSupersetFiltered.SourceMapNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),

                SourceMapDeployedNodes = nodeLists.SourceMapDeployedNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                SourceMapDeployedNodesSubsetFiltered = uniqueSubsetFiltered.SourceMapDeployedNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                SourceMapDeployedNodesSupersetFiltered = uniqueSupersetFiltered.SourceMapDeployedNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),

                AllActiveNodes = nodeLists.AllActiveNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                AllActiveNodesSubsetFiltered = uniqueSubsetFiltered.AllActiveNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
                AllActiveNodesSupersetFiltered = uniqueSupersetFiltered.AllActiveNodes.Select(s => s.NodeType).Distinct().OrderBy(s => s).ToArray(),
            };
#endif

            // Create our combined list.
            var combinedSourceMapNodes = nodeLists.SourceMapDeployedNodes.Concat(nodeLists.SourceMapNodes).Distinct().ToArray();

            // Find any nodes that represent active code but are not found in nodes that correspond to source map entries.
            var unreachableNodes = nodeLists.AllActiveNodes.Where(a => !combinedSourceMapNodes.Any(b => b.SourceRange.Contains(a.SourceRange))).ToArray();


            // Check if each node is a branch to add to our branch nodes list.
            List<(AstNode, BranchType)> branchNodes = new List<(AstNode, BranchType)>();

            foreach (var node in fullNodeArray)
            {
                // Obtain our branch type
                BranchType branchType = GetNodeBranchType(node);

                // If it's not a branch, we skip it
                if (branchType == BranchType.None)
                {
                    continue;
                }

                // Add it to our list
                branchNodes.Add((node, branchType));
            }

            // Create our initial analysis results.
            var analysisResults = new AnalysisResults
            {
                FullNodeList = fullNodeArray,
                ReachableNodes = combinedSourceMapNodes,
                AllActiveNodes = nodeLists.AllActiveNodes,
                UnreachableNodes = unreachableNodes,
                BranchNodes = branchNodes.ToArray(),
                FunctionNode = nodeLists.FunctionNodes,
                SourceMaps = sourceMaps
            };

            // Return our analysis results
            return analysisResults;
        }

        static IEnumerable<AstNode> GetFunctionNodesWithActiveCode(SolcSourceInfo[] sourcesList, IEnumerable<AstNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType == AstNodeType.FunctionDefinition)
                {
                    /*if (node.Node.Value<bool>("implemented"))
                    {
                        yield return node;
                    }*/
                    
                    if (node.Node.TryGetValue("body", out var nodeBody)
                        && nodeBody is JObject bodyObj
                        && bodyObj.TryGetValue("statements", out var statements)
                        && statements.HasValues)
                    {
                        yield return node;
                    }
                }
            }
        }

        private static BranchType GetNodeBranchType(AstNode node)
        {
            // If it's an If statement, we found a branch.
            if (node.NodeType == AstNodeType.IfStatement)
            {
                return BranchType.IfStatement;
            }
            else if (node.NodeType == AstNodeType.Conditional)
            {
                return BranchType.Ternary;
            }

            // Check if it's an assert or require.
            else if (node.NodeType == AstNodeType.FunctionCall)
            {
                // If we found a function call, find an expression name and node type, and verify them.
                string expressionNodeType = node.Node.SelectToken("expression.nodeType").Value<string>();
                if (expressionNodeType == "Identifier")
                {
                    // Obtain our expression name.
                    string expressionName = node.Node.SelectToken("expression.name").Value<string>();

                    // Check the name.
                    if (expressionName == "assert")
                    {
                        return BranchType.Assert;
                    }
                    else if (expressionName == "require")
                    {
                        return BranchType.Require;
                    }
                }
            }


            // We couldn't find anything to detect this token as a branch.
            return BranchType.None;
        }


        /// <summary>
        /// Parses an encoded sourcemap string and matches each entry to an AST Node
        /// </summary>
        static (SourceMapEntry[] SourceMapEntries, AstNode[] AstNodes) ParseSourceMap(string encodedSourceMap, Dictionary<int, AstNode[]> astDict)
        {
            var sourceMapItems = SourceMapParser.Parse(encodedSourceMap);
            var astNodes = new List<AstNode>();
            for (var i = 0; i < sourceMapItems.Length; i++)
            {
                var sourceMap = sourceMapItems[i];
                var node = MatchSourceMapEntry(sourceMap, astDict);
                if (node != null)
                {
                    astNodes.Add(node);
                }
            }

            return (sourceMapItems, astNodes.ToArray());
        }

        /// <summary>
        /// Finds an ast node that matches the source range from a given SourceMapEntry
        /// </summary>
        static AstNode MatchSourceMapEntry(SourceMapEntry sourceMap, Dictionary<int, AstNode[]> astDict)
        {
            if (sourceMap.Index == -1)
            {
                return null;
            }

            if (!astDict.TryGetValue(sourceMap.Index, out var astNodes))
            {
                // This should only ever happen if a source file was explicitly ignored/removed from the list
                return null;
            }

            var astNode = astNodes.FirstOrDefault(a => a.SourceRange.Offset == sourceMap.Offset && a.SourceRange.Length == sourceMap.Length);

            if (astNode != null)
            {
                return astNode;
            }

            astNode = astNodes.FirstOrDefault(a => a.SourceRange.Contains(sourceMap));
            if (astNode != null)
            {
                return astNode;
            }

            throw new Exception($"Could not find matching AST node for source map entry {{index: {sourceMap.Index}, offset: {sourceMap.Offset}, length: {sourceMap.Length}}}");
        }


    }

}
