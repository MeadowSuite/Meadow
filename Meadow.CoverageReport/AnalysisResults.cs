using SolcNet.DataDescription.Output;
using System.Collections.Generic;

namespace Meadow.CoverageReport
{
    public class AnalysisResults
    {
        /// <summary>
        /// All nodes with no filtering.
        /// </summary>
        public AstNode[] FullNodeList { get; set; }

        /// <summary>
        /// All nodes that have a corresponding sourcemap entry.
        /// </summary>
        public AstNode[] ReachableNodes { get; set; }

        /// <summary>
        /// All nodes that represent executable source code
        /// </summary>
        public AstNode[] AllActiveNodes { get; set; }

        /// <summary>
        /// All nodes that have executable source code which are not compiled into the bytecode and thus are
        /// unreachable.
        /// </summary>
        public AstNode[] UnreachableNodes { get; set; }

        public AstNode[] FunctionNode { get; set; }

        public (AstNode Node, BranchType BranchType)[] BranchNodes { get; set; }

        public IReadOnlyDictionary<(string FileName, string ContractName, string BytecodeHash), (SourceMapEntry[] NonDeployed, SourceMapEntry[] Deployed)> SourceMaps;

    }
}
