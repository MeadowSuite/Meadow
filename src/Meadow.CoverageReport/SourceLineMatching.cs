using Meadow.CoverageReport.Models;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    static class SourceLineMatching
    {
        public static SourceFileLine GetSourceFileLineFromAstNode(AstNode node, IReadOnlyDictionary<int, SourceFileMap> sourceIndexes)
        {
            var sourceFileMap = sourceIndexes[node.SourceIndex];
            var sourceFileLine = sourceFileMap.SourceFileLines
                .First(s => node.SourceRange.Offset >= s.Offset && node.SourceRange.Offset < s.OffsetEnd);
            return sourceFileLine;
        }

        public static IEnumerable<SourceFileLine> GetSourceFileLinesContainingAstNode(AstNode node, IReadOnlyDictionary<int, SourceFileMap> sourceIndexes)
        {
            var sourceFileMap = sourceIndexes[node.SourceIndex];
            var sourceFileLines = sourceFileMap.SourceFileLines
                .Where(sourceLine => node.SourceRange.Offset >= sourceLine.Offset && node.SourceRange.Offset < sourceLine.OffsetEnd);
            foreach (var l in sourceFileLines)
            {
                if (l.SourceFileMapParent.SourceFileIndex != node.SourceIndex)
                {
                    throw new Exception($"Source file index mismatch between {l.SourceFileMapParent.SourceFileIndex} and {node.SourceIndex}");
                }

                yield return l;
            }
        }

        public static IEnumerable<SourceFileLine> GetSourceFileLinesContainedWithinAstNode(AstNode node, IReadOnlyDictionary<int, SourceFileMap> sourceIndexes)
        {
            var sourceFileMap = sourceIndexes[node.SourceIndex];
            var sourceFileLines = sourceFileMap.SourceFileLines
                .Where(sourceLine => sourceLine.Offset >= node.SourceRange.Offset && sourceLine.Offset <= node.SourceRange.OffsetEnd);
            foreach (var l in sourceFileLines)
            {
                if (l.SourceFileMapParent.SourceFileIndex != node.SourceIndex)
                {
                    throw new Exception($"Source file index mismatch between {l.SourceFileMapParent.SourceFileIndex} and {node.SourceIndex}");
                }

                yield return l;
            }
        }

        public static SourceFileLine GetSourceFileLineFromSourceMapEntry(SourceMapEntry entry, IReadOnlyDictionary<int, SourceFileMap> sourceIndexes)
        {
            // This should only ever be missing when the source file is explicitly ignored
            if (!sourceIndexes.TryGetValue(entry.Index, out var sourceFileMap))
            {
                return null;
            }

            var sourceFileLine = sourceFileMap.SourceFileLines
                .First(s => entry.Offset >= s.Offset && entry.Offset < s.OffsetEnd);
            if (sourceFileLine.SourceFileMapParent.SourceFileIndex != entry.Index)
            {
                throw new Exception($"Source file index mismatch between {sourceFileLine.SourceFileMapParent.SourceFileIndex} and {entry.Index}");
            }

            return sourceFileLine;
        }

        public static IEnumerable<SourceFileLine> GetSourceFileLinesFromSourceMapEntry(SourceMapEntry entry, IReadOnlyDictionary<int, SourceFileMap> sourceIndexes)
        {
            // This should only ever be missing when the source file is explicitly ignored
            if (!sourceIndexes.TryGetValue(entry.Index, out var sourceFileMap))
            {
                return null;
            }

            var sourceFileLines = sourceFileMap.SourceFileLines
                .Where(s => entry.Offset >= s.Offset && entry.Offset < s.OffsetEnd);
            foreach (var l in sourceFileLines)
            {
                if (l.SourceFileMapParent.SourceFileIndex != entry.Index)
                {
                    throw new Exception($"Source file index mismatch between {l.SourceFileMapParent.SourceFileIndex} and {entry.Index}");
                }
            }

            return sourceFileLines;
        }

        public static IEnumerable<AstNode> MatchAstNodesToSourceFileLine(AnalysisResults analysis, SourceFileLine sourceFileLine)
        {
            foreach (var node in analysis.FullNodeList)
            {
                if (node.SourceIndex != sourceFileLine.SourceFileMapParent.SourceFileIndex)
                {
                    continue;
                }

                if (node.SourceRange.Offset >= sourceFileLine.Offset && node.SourceRange.Offset < sourceFileLine.OffsetEnd)
                {
                    yield return node;
                }
            }
        }

    }
}
