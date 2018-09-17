using Meadow.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    static class AstHelper
    {

        /// <summary>
        /// Walk the entry AST node tree to create flattened list of all AstNodes
        /// </summary>
        public static IEnumerable<AstNode> Walk(JToken node)
        {
            if (node is JObject jObj)
            {
                if (jObj.ContainsKey("src"))
                {
                    yield return AstNode.Create(jObj);
                }
            }

            foreach (var child in node)
            {
                foreach (var subResult in Walk(child))
                {
                    yield return subResult;
                }
            }

        }


        /// <summary>
        /// Create a list without nodes that are a subset of another node
        /// </summary>
        public static AstNode[] RemoveSubsets(AstNode[] nodes)
        {
            var newList = nodes.ToList();
            for (var i = newList.Count - 1; i >= 0; i--)
            {
                var cur = newList[i];
                for (var j = 0; j < newList.Count; j++)
                {
                    var other = newList[j];
                    if (other != cur
                        && other.SourceIndex == cur.SourceIndex
                        && other.SourceRange.Contains(cur.SourceRange))
                    {
                        newList.RemoveAt(i);
                        break;
                    }
                }
            }

            return newList.ToArray();
        }

        /// <summary>
        /// Create a list without nodes that are a superset of another node
        /// </summary>
        public static AstNode[] RemoveSupersets(AstNode[] nodes)
        {
            var newList = nodes.ToList();
            for (var i = newList.Count - 1; i >= 0; i--)
            {
                var cur = newList[i];
                for (var j = 0; j < newList.Count; j++)
                {
                    var other = newList[j];
                    if (other != cur
                        && other.SourceIndex == cur.SourceIndex
                        && cur.SourceRange.Contains(other.SourceRange))
                    {
                        newList.RemoveAt(i);
                        break;
                    }
                }
            }

            return newList.ToArray();
        }




        /// <summary>
        /// Returns a list of nodes that are of a type in the given type list. 
        /// </summary>
        public static IEnumerable<AstNode> GetNodesOfTypes(
            SolcSourceInfo[] sourcesList,
            IEnumerable<AstNode> nodes,
            AstNodeType[] nodeTypes)
        {
            foreach (var node in nodes)
            {
                if (nodeTypes.Contains(node.NodeType))
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Returns a list of nodes that are not of a type in the given type list. 
        /// </summary>
        public static IEnumerable<AstNode> GetNodesNotOfTypes(
            SolcSourceInfo[] sourcesList,
            IEnumerable<AstNode> nodes,
            AstNodeType[] nodeTypes)
        {
            foreach (var node in nodes)
            {
                if (!nodeTypes.Contains(node.NodeType))
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Group AST nodes by their source file index as the dictionary key.
        /// </summary>
        public static (Dictionary<int, AstNode[]> Indexed, AstNode[] FullNodeArray) IndexAstNodes(
            SolcSourceInfo[] sourcesList)
        {
            var astDict = new Dictionary<int, List<AstNode>>();
            foreach (var sourceItem in sourcesList)
            {
                var astEntry = sourceItem.AstJson;
                var entryID = sourceItem.ID;
                foreach (var node in Walk(astEntry))
                {
                    if (!astDict.TryGetValue(node.SourceRange.SourceIndex, out var list))
                    {
                        list = new List<AstNode>();
                        astDict.Add(node.SourceRange.SourceIndex, list);
                    }

                    list.Add(node);
                }
            }

            var index = astDict.ToDictionary(e => e.Key, e => e.Value.ToArray());
            var entireArray = index.Values.SelectMany(e => e).ToArray();
            return (index, entireArray);
        }


    }
}
