using Meadow.Contract;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using Meadow.CoverageReport.Models;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using Newtonsoft.Json.Linq;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Meadow.CoverageReport
{
    public static class ReportGenerator
    {
        public const string REPORT_INDEX_FILE = "index.html";
        public const string TEST_RESULTS_TXT_FILE = "test_results.txt";
        public const string COVERAGE_JSON_FILE = "coverage.json";

        static readonly UTF8Encoding UTF8 = new UTF8Encoding(false);

        public static void CreateReport(
            string solidityCompilerVersion,
            (CompoundCoverageMap Coverage, SolcBytecodeInfo Contract)[] coverageMaps,
            SolcSourceInfo[] sourcesList,
            SolcBytecodeInfo[] byteCodeData,
            IGrouping<string, UnitTestResult>[] unitTestOutcome,
            string[] ignoredSourceFiles,
            string reportOutputDirectory,
            List<Exception> catchExceptions = null)
        {

            string reportIndexFilePath = Path.Combine(reportOutputDirectory, REPORT_INDEX_FILE);

            var analysis = SourceAnalysis.Run(sourcesList, byteCodeData);

            var sourceFileMaps = CreateSourceFileMaps(sourcesList, analysis);

            foreach (var node in analysis.UnreachableNodes)
            {
                var sourceFileLine = SourceLineMatching.GetSourceFileLineFromAstNode(node, sourceFileMaps);
                sourceFileLine.IsActive = true;
                sourceFileLine.IsUnreachable = true;
            }

            foreach (var node in analysis.ReachableNodes)
            {
                var sourceFileLine = SourceLineMatching.GetSourceFileLineFromAstNode(node, sourceFileMaps);
                sourceFileLine.IsActive = true;
                sourceFileLine.IsUnreachable = false;
            }

            foreach (var branch in analysis.BranchNodes)
            {
                var sourceFileLine = SourceLineMatching.GetSourceFileLineFromAstNode(branch.Node, sourceFileMaps);
                if (!sourceFileLine.IsActive)
                {
                    throw new Exception("Found a branch node that is not correlated with an active source line");
                }

                sourceFileLine.IsBranch = true;
                sourceFileLine.BranchType = branch.BranchType;
            }

            foreach (var (coverageMap, contractInst) in coverageMaps)
            {
                // Find the sourcemap matching this contract file name and contract name.
                var sourceMapNonDeployed = analysis.SourceMaps[(contractInst.FilePath, contractInst.ContractName, contractInst.Bytecode)].NonDeployed;

                // Find the sourcemap matching this contract file name and contract name.
                var sourceMapDeployed = analysis.SourceMaps[(contractInst.FilePath, contractInst.ContractName, contractInst.Bytecode)].Deployed;

                // Find the SourceFileMap (viewmodel used by the html templating) matching this contract file name.
                var (sourceIndex, sourceFileMap) = sourceFileMaps.FirstOrDefault(s => s.Value.SourceFilePath == contractInst.FilePath);
                if (sourceFileMap == null)
                {
                    // If this source file was not explicitly ignored then throw exception
                    if (!ignoredSourceFiles.Contains(contractInst.FilePath))
                    {
                        throw new Exception($"Could not find source file in coverage map parsing {contractInst.FilePath}");
                    }
                }

                // Find the solc evm.bytecode data matching this contract file name.
                var matchedByteCodeData = byteCodeData.Single(s => s.FilePath == contractInst.FilePath && s.ContractName == contractInst.ContractName);

                // The evm.bytecode.opcodes string corresponds to code executed during the deployment/construction of a contract.
                IdentifyExecutedSourceLines(coverageMap.UndeployedMap, matchedByteCodeData.Opcodes, sourceMapNonDeployed, analysis, sourceFileMaps, catchExceptions);

                // The evm.bytecodeDeployed.opcodes string corresponds to code that can be excuted on a deployed contract via transactions or calls.
                IdentifyExecutedSourceLines(coverageMap.DeployedMap, matchedByteCodeData.OpcodesDeployed, sourceMapDeployed, analysis, sourceFileMaps, catchExceptions);

                // Identify branch coverage using instruction indexes from branch nodes corresponding to code executed during the deployment/construction of a contract.
                IdentifySourceLineBranchCoverage(coverageMap.UndeployedMap, matchedByteCodeData.Opcodes, sourceMapNonDeployed, analysis, sourceFileMaps);

                // Identify branch coverage using instruction indexes from branch nodes corresponding to code that can be excuted on a deployed contract via transactions or calls.
                IdentifySourceLineBranchCoverage(coverageMap.DeployedMap, matchedByteCodeData.OpcodesDeployed, sourceMapDeployed, analysis, sourceFileMaps);
            }

            foreach (var node in analysis.FunctionNode)
            {
                var sourceFileLines = SourceLineMatching.GetSourceFileLinesContainedWithinAstNode(node, sourceFileMaps);
                var sourceFileLine = sourceFileLines.FirstOrDefault(s => s.IsActive);
                if (sourceFileLine != null)
                {
                    sourceFileLine.SourceFileMapParent.FunctionCount++;
                    if (sourceFileLine.IsCovered)
                    {
                        sourceFileLine.SourceFileMapParent.FunctionCoveredCount++;
                    }
                }
                else
                {
                    // throw new Exception("Could not find active source line for function node");
                }
            }


            foreach (var sourceFileMap in sourceFileMaps.Values)
            {
                int pathDepth = sourceFileMap.SourceFilePath.Split('/', StringSplitOptions.None).Length;
                var indexHtmlPath = string.Join(string.Empty, Enumerable.Repeat("../", pathDepth)) + REPORT_INDEX_FILE;
                sourceFileMap.IndexHtmlFilePath = indexHtmlPath;
                sourceFileMap.SolidityCompilerVersion = solidityCompilerVersion;

                foreach (var sourceFileLine in sourceFileMap.SourceFileLines)
                {
                    if (sourceFileLine.IsActive)
                    {
                        sourceFileMap.LineCount++;
                        if (sourceFileLine.IsCovered)
                        {
                            sourceFileMap.LineCoveredCount++;
                        }
                    }

                    // If this is a branch
                    if (sourceFileLine.IsBranch)
                    {
                        // Add to the branch count
                        sourceFileMap.BranchCount += 2;

                        // If we covered both, add to our covered count.
                        if (sourceFileLine.BranchState == BranchCoverageState.CoveredBoth)
                        {
                            sourceFileMap.BranchCoveredCount += 2;
                        }

                        // If we covered one but not both, add to our covered count.
                        else if (sourceFileLine.BranchState != BranchCoverageState.CoveredNone)
                        {
                            // one of the branches were executed
                            sourceFileMap.BranchCoveredCount++;
                        }
                    }
                }
            }

            // render all SourceFileMap objects into html report pages
            var pages = new List<(SourceFileMap SourceFileMap, string HtmlReport)>();
            foreach (var sourceFileMap in sourceFileMaps.Values)
            {
                pages.Add((sourceFileMap, CoveragePageRenderer.Instance.RenderCoverageReport(sourceFileMap)));
            }

            var indexView = new IndexViewModel
            {
                SolidityCompilerVersion = solidityCompilerVersion,
                SourceFileMaps = new SourceFileMap[pages.Count],
                UnitTestOutcome = unitTestOutcome
            };

            // write html page results to disk
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var sourceFileMap = page.SourceFileMap;
                indexView.SourceFileMaps[i] = sourceFileMap;

                var outputFileName = sourceFileMap.SourceFileName + ".html";
                var filePath = Path.Combine(reportOutputDirectory, "pages", sourceFileMap.SourceFileDirectory);
                Directory.CreateDirectory(filePath);
                var outputFilePath = Path.Combine(filePath, outputFileName);
                File.WriteAllText(outputFilePath, page.HtmlReport, UTF8);

                indexView.LineCount += sourceFileMap.LineCount;
                indexView.LineCoveredCount += sourceFileMap.LineCoveredCount;

                indexView.BranchCount += sourceFileMap.BranchCount;
                indexView.BranchCoveredCount += sourceFileMap.BranchCoveredCount;

                indexView.FunctionCount += sourceFileMap.FunctionCount;
                indexView.FunctionCoveredCount += sourceFileMap.FunctionCoveredCount;
            }

            Array.Sort(indexView.SourceFileMaps, (a, b) => string.CompareOrdinal(a.SourceFilePath, b.SourceFilePath));

            var reportTxtFileWriter = new ReportTxtFileWriter();
            reportTxtFileWriter.WriteTestResults(unitTestOutcome, indexView, reportOutputDirectory, TEST_RESULTS_TXT_FILE);

            using (var jsonFileStream = new FileStream(Path.Combine(reportOutputDirectory, COVERAGE_JSON_FILE), FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var coverageJsonWriter = new CoverageJsonWriter(indexView);
                coverageJsonWriter.WriteJson(jsonFileStream);
            }

            var indexHtmlString = CoveragePageRenderer.Instance.RenderIndexPage(indexView);
            var indexFilePath = reportIndexFilePath;
            File.WriteAllText(indexFilePath, indexHtmlString, UTF8);

            //OpenBrowser(outputDir);
        }

        static string HashSha256(string content)
        {
            var sha256 = System.Security.Cryptography.SHA256.Create();
            Span<byte> utf8Bytes = new byte[UTF8.GetByteCount(content)];
            Span<byte> hashOutput = new byte[sha256.HashSize / 8];
            UTF8.GetBytes(content, utf8Bytes);
            if (!sha256.TryComputeHash(utf8Bytes, hashOutput, out int bytesWritten) || bytesWritten != hashOutput.Length)
            {
                throw new Exception("Error computing SHA256 hash");
            }

            return HexUtil.GetHexFromBytes(hashOutput).ToUpperInvariant();
        }

        /// <summary>
        /// Takes all the solidity source code lists, splits the code by new line characters, and creates the 
        /// coverage report ViewModel objects which the html template code uses to generate the web page report.
        /// </summary>
        public static Dictionary<int, SourceFileMap> CreateSourceFileMaps(
            SolcSourceInfo[] sourcesList,
            AnalysisResults analysis)
        {
            var sourceIndexes = new Dictionary<int, SourceFileMap>();

            foreach (var solSource in sourcesList)
            {
                var index = solSource.ID;
                var sourceFileMap = sourceIndexes[index] = new SourceFileMap();
                sourceFileMap.SourceFileIndex = index;
                sourceFileMap.SourceFileName = Path.GetFileName(solSource.FileName);
                sourceFileMap.SourceFilePath = solSource.FileName;
                sourceFileMap.SourceFileDirectory = Path.GetDirectoryName(solSource.FileName);
                sourceFileMap.SourceHashSha256 = HashSha256(solSource.SourceCode);

                // We must handle CR LF, CR, and LF line endings. So we split on CRLF, then split on CR/LF.
                List<SourceFileLine> lines = new List<SourceFileLine>();
                int currentOffset = 0;
                int lineNumber = 0;
                var crlfLines = solSource.SourceCode.Split("\r\n");
                for (int i = 0; i < crlfLines.Length; i++)
                {
                    string[] crOrLfLines = crlfLines[i].Split(new[] { '\r', '\n' });
                    for (int x = 0; x < crOrLfLines.Length; x++)
                    {
                        // Create a representation for this line.
                        var sourceFileLine = new SourceFileLine();
                        sourceFileLine.SourceFileMapParent = sourceFileMap;
                        sourceFileLine.LiteralSourceCodeLine = crOrLfLines[x];
                        sourceFileLine.LineNumber = ++lineNumber;
                        sourceFileLine.Offset = currentOffset;
                        sourceFileLine.Length = UTF8.GetByteCount(sourceFileLine.LiteralSourceCodeLine);
                        currentOffset += sourceFileLine.Length;

                        sourceFileLine.CorrelatedAstNodes = SourceLineMatching.MatchAstNodesToSourceFileLine(analysis, sourceFileLine).ToArray();

                        // Add the line to the list.
                        lines.Add(sourceFileLine);

                        // Add CR or LF line ending length.
                        if (x < crOrLfLines.Length - 1)
                        {
                            currentOffset += 1;
                        }
                    }

                    // Add CR LF line ending length.
                    if (i < crlfLines.Length - 1)
                    {
                        currentOffset += 2;
                    }
                }

                // Set our resulting lines.
                sourceFileMap.SourceFileLines = lines.ToArray();
            }

            return sourceIndexes;
        }

        /// <summary>
        /// For each passed ast node, determine all bytecode-offset and instruction-indexes contained within the node source code range.
        /// </summary>
        static void IdentifySourceLineBranchCoverage(CoverageMap coverageMap, string opcodesString, SourceMapEntry[] sourceMap, AnalysisResults analysis, Dictionary<int, SourceFileMap> sourceFileMaps)
        {
            // If the coverage map is null, exit
            if (coverageMap == null)
            {
                return;
            }

            // Create a mapping of instruction-indexs to bytecode-offsets
            var instructionOffsetToNumber = CoverageOpcodeMapping.GetInstructionOffsetToNumberLookup(opcodesString);
            var instructionNumberToOffset = CoverageOpcodeMapping.GetInstructionNumberToOffsetLookup(opcodesString);
            var translatedOpcodes = CoverageOpcodeMapping.ConvertToSolidityCoverageMap(coverageMap, opcodesString);

            // Obtain a set of our instruction indexes we jumped from.
            HashSet<int> jumpIndexes = new HashSet<int>(translatedOpcodes.JumpIndexes);
            HashSet<int> nonJumpIndexes = new HashSet<int>(translatedOpcodes.NonJumpIndexes);
            foreach (var branch in analysis.BranchNodes)
            {
                // Obtain all source map entries which refer to source which is a subset or equal to this branch's source range.
                List<(int instructionIndex, SourceMapEntry sourceMapEntry)> branchInstructionMaps = new List<(int instructionIndex, SourceMapEntry sourceMapEntry)>();
                for (var i = 0; i < sourceMap.Length; i++)
                {
                    // Obtain the source map for this instruction index.
                    var instructionSourceMap = sourceMap[i];

                    // Check if this source map is a subset of our branch, if so, add it.
                    if (instructionSourceMap.Index == branch.Node.SourceIndex && instructionSourceMap.Offset >= branch.Node.SourceRange.Offset && instructionSourceMap.Offset + instructionSourceMap.Length <= branch.Node.SourceRange.OffsetEnd)
                    {
                        branchInstructionMaps.Add((i, instructionSourceMap));
                    }
                }

                // For branches, there may be embedded conditions/jumps/etc. So we want to find source
                // ranges to remove embedded condition instruction's source maps from the above branch 
                // instruction source maps.
                List<string> sourceMapEntriesToRemove = new List<string>();
                if (branch.BranchType == BranchType.IfStatement)
                {
                    // If statements have 2-3 parts to remove
                    // 1) Condition (Any code related to evaluating the condition)
                    // 2) True Body (Any code we run if condition was met)
                    // 3) Else Body (Any code we run if condition was not met) (OPTIONAL)
                    string conditionSrc = branch.Node.Node.SelectToken("condition.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(conditionSrc))
                    {
                        sourceMapEntriesToRemove.Add(conditionSrc);
                    }

                    string trueBodySrc = branch.Node.Node.SelectToken("trueBody.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(trueBodySrc))
                    {
                        sourceMapEntriesToRemove.Add(trueBodySrc);
                    }

                    string falseBodySrc = branch.Node.Node.SelectToken("falseBody.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(falseBodySrc))
                    {
                        sourceMapEntriesToRemove.Add(falseBodySrc);
                    }
                }
                else if (branch.BranchType == BranchType.Assert || branch.BranchType == BranchType.Require)
                {
                    // Assert and Require Statements have to remove any code executed when evaluating arguments.
                    string[] argumentsSrc = branch.Node.Node.SelectTokens("arguments[*].src")?.Values<string>().ToArray();
                    sourceMapEntriesToRemove.AddRange(argumentsSrc);
                }
                else if (branch.BranchType == BranchType.Ternary)
                {
                    // Ternary statements have 3 parts to remove.
                    // 1) Condition (Any code related to the evaluating the condition)
                    // 2) True Expression (Any code we run if condition was met)
                    // 3) Else Expression (Any code we run if condition was not met)
                    string conditionSrc = branch.Node.Node.SelectToken("condition.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(conditionSrc))
                    {
                        sourceMapEntriesToRemove.Add(conditionSrc);
                    }

                    string trueExpressionSrc = branch.Node.Node.SelectToken("trueExpression.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(trueExpressionSrc))
                    {
                        sourceMapEntriesToRemove.Add(trueExpressionSrc);
                    }

                    string falseExpressionSrc = branch.Node.Node.SelectToken("falseExpression.src")?.Value<string>();
                    if (!string.IsNullOrEmpty(falseExpressionSrc))
                    {
                        sourceMapEntriesToRemove.Add(falseExpressionSrc);
                    }
                }

                // Subtract all instructions which fall in our ranges marked to remove.
                for (int i = 0; i < sourceMapEntriesToRemove.Count; i++)
                {
                    // If this source map entry to remove is null, skip to the next item.
                    if (string.IsNullOrEmpty(sourceMapEntriesToRemove[i]))
                    {
                        continue;
                    }

                    // Parse the source range into parts, split by a comma.
                    int[] sourceRangeParts = sourceMapEntriesToRemove[i].Split(':', StringSplitOptions.RemoveEmptyEntries).Select(k => int.Parse(k, CultureInfo.InvariantCulture)).ToArray();

                    // Obtain our range to remove.
                    (int offset, int length, int index) removeRange = (sourceRangeParts[0], sourceRangeParts[1], sourceRangeParts[2]);

                    // Loop for every branch instruction map (to see if we any instructions fall into this range).
                    int x = 0;
                    while (x < branchInstructionMaps.Count)
                    {
                        // Obtain our branch instruction map item
                        var mapItem = branchInstructionMaps[x];

                        // Verify the source map range is contained in the one we wish to remove.
                        bool contained = mapItem.sourceMapEntry.Index >= removeRange.index &&
                            mapItem.sourceMapEntry.Offset >= removeRange.offset &&
                              mapItem.sourceMapEntry.Offset + mapItem.sourceMapEntry.Length <= removeRange.offset + removeRange.length;

                        // If this map item is inside of the range we're removing from, we remove the map item.
                        if (contained)
                        {
                            branchInstructionMaps.RemoveAt(x);
                        }
                        else
                        {
                            // Otherwise we increment our iterator.
                            x++;
                        }
                    }
                }

                // A list of instruction-indexes that are contained within this ast node source code range.
                var instructionIndexes = branchInstructionMaps.Select(k => k.instructionIndex).Distinct().ToArray();
                var sourceMapEntries = branchInstructionMaps.Select(k => k.sourceMapEntry).Distinct().ToArray();

                SourceFileLine sourceFileLine = null;
                foreach (var sourceMapEntry in sourceMapEntries)
                {
                    if (sourceMapEntry.Index == -1)
                    {
                        // "In the case of instructions that are not associated with any particular source 
                        //  file, the source mapping assigns an integer identifier of -1. This may happen 
                        //  for bytecode sections stemming from compiler-generated inline assembly statements."
                        // http://solidity.readthedocs.io/en/v0.4.24/miscellaneous.html#source-mappings
                        continue;
                    }

                    // Try to obtain a source file map for this index.
                    if (!sourceFileMaps.TryGetValue(sourceMapEntry.Index, out var sourceFileMap))
                    {
                        continue;
                    }

                    // Obtain all possible candidate source file lines.
                    var candidateSourceFileLines = sourceFileMap.SourceFileLines
                        .Where(s => sourceMapEntry.Offset >= s.Offset && sourceMapEntry.Offset <= s.OffsetEnd);

                    // Try every candidate source file line to see if it satisfies our conditions.
                    foreach (var candidateSourceFileLine in candidateSourceFileLines)
                    {
                        if (candidateSourceFileLine.SourceFileMapParent.SourceFileIndex != sourceMapEntry.Index)
                        {
                            throw new Exception($"Source file index mismatch between {candidateSourceFileLine.SourceFileMapParent.SourceFileIndex} and {sourceMapEntry.Index}");
                        }

                        // Check if null (if source file ignored)
                        if (candidateSourceFileLine == null)
                        {
                            continue;
                        }

                        // If the SourceFileLine is not marked active then the line of code does not contain
                        // any AST nodes that we recognize as executable code to do coverage tracking on.
                        if (!candidateSourceFileLine.IsActive)
                        {
                            continue;
                        }

                        if (candidateSourceFileLine.IsUnreachable)
                        {
                            throw new Exception("Branch coverage on source line that is supposed to be unreachable");
                        }

                        sourceFileLine = candidateSourceFileLine;
                        break;
                    }

                    // If our source file line was found, break
                    if (sourceFileLine == null)
                    {
                        break;
                    }
                }

                // If we couldn't find a source line, skip to the next source map entry.
                if (sourceFileLine == null)
                {
                    continue;
                }

                // Determine if which branch we took.
                bool tookJumpBranch = false;
                bool tookDefaultBranch = false;

                // Check if we jumped. Loop for each instruction index
                foreach (int instructionIndex in instructionIndexes)
                {
                    // If the instruction index is in the jump indexes set, we covered a branch.
                    if (jumpIndexes.Contains(instructionIndex))
                    {
                        tookDefaultBranch = true;
                    }

                    // If the instruction index is in the non-jump indexes set, we covered a branch.
                    if (nonJumpIndexes.Contains(instructionIndex))
                    {
                        tookJumpBranch = true;
                    }

                }

                // If it's a ternary/assert/require, the jump/not jump have inverse meanings.
                if (branch.BranchType == BranchType.Ternary || branch.BranchType == BranchType.Assert || branch.BranchType == BranchType.Require)
                {
                    // Swap our items.
                    bool temp = tookJumpBranch;
                    tookJumpBranch = tookDefaultBranch;
                    tookDefaultBranch = temp;
                }

                // Determine branch status based off of determine status from another coverage map.
                if (tookJumpBranch)
                {
                    sourceFileLine.BranchState |= BranchCoverageState.CoveredIf;
                }

                if (tookDefaultBranch)
                {
                    sourceFileLine.BranchState |= BranchCoverageState.CoveredElse;
                }
            }

        }

        /// <summary>
        /// Uses the opcode string (from solc output) to translate coverage byte-offsets into instruction offsets,
        /// then matches the instruction offset a sourcemap entry (from solc output), then matches the sourcemap
        /// entry to a SourceFileLine and increments the execution count.
        /// </summary>
        static void IdentifyExecutedSourceLines(CoverageMap coverageMap, string opcodesString, SourceMapEntry[] sourceMap, AnalysisResults analysis, Dictionary<int, SourceFileMap> sourceFileMaps, List<Exception> catchExceptions = null)
        {
            // If the coverage map is null, exit
            if (coverageMap == null)
            {
                return;
            }

            // The EVM returns coverage data by byte offsets, convert them to instruction offsets.
            var translatedOpcodes = CoverageOpcodeMapping.ConvertToSolidityCoverageMap(coverageMap, opcodesString);

            var processedSourceLines = new HashSet<(string, int)>();
            for (var instructionIndex = 0; instructionIndex < translatedOpcodes.InstructionIndexCoverage.Length; instructionIndex++)
            {
                var execCount = (int)translatedOpcodes.InstructionIndexCoverage[instructionIndex];
                if (execCount == 0)
                {
                    // Opcode index was never executed.
                    continue;
                }

                var sourceMapEntry = sourceMap[instructionIndex];
                if (sourceMapEntry.Index == -1)
                {
                    // "In the case of instructions that are not associated with any particular source 
                    //  file, the source mapping assigns an integer identifier of -1. This may happen 
                    //  for bytecode sections stemming from compiler-generated inline assembly statements."
                    // http://solidity.readthedocs.io/en/v0.4.24/miscellaneous.html#source-mappings
                    continue;
                }

                // Find the SourceFileLine containing the source code range in this source map entry.
                var sourceFileLine = SourceLineMatching.GetSourceFileLineFromSourceMapEntry(sourceMapEntry, sourceFileMaps);
                if (sourceFileLine == null)
                {
                    // Null if the source file was ignored
                    continue;
                }

                // If the SourceFileLine is not marked active then the line of code does not contain
                // any AST nodes that we recognize as executable code to do coverage tracking on.
                if (!sourceFileLine.IsActive)
                {
                    continue;
                }

                // Try to find an ast node that exactly matches the sourcemap range
                var exactAstNodeMatch = sourceFileLine.CorrelatedAstNodes.FirstOrDefault(a =>
                {
                    return a.SourceRange.SourceIndex == sourceMapEntry.Index &&
                        a.SourceRange.Offset == sourceMapEntry.Offset &&
                        a.SourceRange.Length == sourceMapEntry.Length;
                });

                // Ignore variable declaration node types
                if (exactAstNodeMatch?.NodeType == AstNodeType.VariableDeclaration)
                {
                    continue;
                }

                // Verify we haven't already processed this line number's execution count
                if (!processedSourceLines.Contains((sourceFileLine.SourceFileMapParent.SourceFilePath, sourceFileLine.LineNumber)))
                {
                    // Add to the execution count, and add it to our hash map
                    sourceFileLine.ExecutionCount += execCount;
                    processedSourceLines.Add((sourceFileLine.SourceFileMapParent.SourceFilePath, sourceFileLine.LineNumber));
                }

                if (sourceFileLine.IsUnreachable)
                {
                    var ex = new Exception("Coverage on source line that is supposed to be unreachable");
                    if (catchExceptions != null)
                    {
                        catchExceptions.Add(ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }


    }
}
