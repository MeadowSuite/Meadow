using Meadow.CoverageReport.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport
{
    class ReportTxtFileWriter
    {
        public ReportTxtFileWriter()
        {

        }

        public void WriteTestResults(IGrouping<string, UnitTestResult>[] unitTestOutcome, IndexViewModel indexView, string outputDirectory, string filename)
        {
            var sb = new StringBuilder();
            if (unitTestOutcome != null)
            {
                WriteTestOutcomes(sb, unitTestOutcome);
            }

            WriteCoverageTable(sb, indexView);

            WriteFileHashes(sb, indexView);

            var rawUnitTestOutcomeReport = sb.ToString().Trim();
            string outputFile = Path.Combine(outputDirectory, filename);
            File.WriteAllText(outputFile, rawUnitTestOutcomeReport, new UTF8Encoding(false, false));
        }

        static void WriteTestOutcomes(StringBuilder sb, IGrouping<string, UnitTestResult>[] unitTestOutcome)
        {
            TimeSpan durationPassed = TimeSpan.Zero;
            TimeSpan durationFailed = TimeSpan.Zero;

            int countPassed = 0;
            int countFailed = 0;

            foreach (var testGroup in unitTestOutcome)
            {
                sb.AppendLine();
                sb.AppendLine($"{testGroup.Key}");
                foreach (var testResult in testGroup.OrderBy(t => t.TestName))
                {
                    if (testResult.Passed)
                    {
                        durationPassed += testResult.Duration;
                        countPassed++;
                    }
                    else
                    {
                        durationFailed += testResult.Duration;
                        countFailed++;
                    }

                    var icon = testResult.Passed ? "√" : "✗";
                    sb.AppendLine($"  {icon} {testResult.TestName} ({Math.Round(testResult.Duration.TotalMilliseconds)}ms)");
                }
            }

            sb.AppendLine();
            if (countPassed > 0)
            {
                sb.AppendLine($"{countPassed} passed ({Math.Round(durationPassed.TotalSeconds)}s)");
            }

            if (countFailed > 0)
            {
                sb.AppendLine($"{countFailed} failed ({Math.Round(durationFailed.TotalSeconds)}s)");
            }
        }

        static void WriteCoverageTable(StringBuilder sb, IndexViewModel indexView)
        {

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("--------");
            sb.AppendLine("Coverage");

            var rows = new List<string[]>();

            string[] CreateRow(string rowName, CoverageStats entry)
            {
                return new string[]
                {
                    rowName,
                    $"{entry.LineCoveredCount}/{entry.LineCount}",
                    $"{entry.LineCoveragePercent}%",
                    $"{entry.BranchCoveredCount}/{entry.BranchCount}",
                    $"{entry.BranchCoveragePercent}%",
                    $"{entry.FunctionCoveredCount}/{entry.FunctionCount}",
                    $"{entry.FunctionCoveragePercent}%"
                };
            }

            foreach (var entry in indexView.SourceFileMaps)
            {
                rows.Add(CreateRow(entry.SourceFilePath, entry));
            }

            var coverageTable = new AsciiTable
            {
                Columns = new[] { "File", "Lines", string.Empty, "Branches", string.Empty, "Functions", string.Empty },
                Rows = rows.ToArray(),
                Footer = CreateRow("All files", indexView)
            };

            coverageTable.WriteToString(sb);
        }

        static void WriteFileHashes(StringBuilder sb, IndexViewModel indexView)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("--------");
            sb.AppendLine("Source File Hashes");

            var rows = new List<string[]>();

            foreach (var entry in indexView.SourceFileMaps)
            {
                rows.Add(new string[] { entry.SourceFilePath, entry.SourceHashSha256 });
            }

            var hashTable = new AsciiTable
            {
                Columns = new[] { "File", "Fingerprint (SHA256)" },
                Rows = rows.ToArray(),
                SplitterChar = ' '
            };

            hashTable.WriteToString(sb);
        }
    }
}
