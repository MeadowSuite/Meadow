using Meadow.Core.Utils;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.DebugSolSources
{
    class AppOptions
    {
        const string GENERATED_DATA_DIR = ".meadow-generated";

        public string SourceOutputDir { get; private set; }
        public string BuildOutputDir { get; private set; }
        public string SolCompilationSourcePath { get; private set; }

        public string SingleFile { get; private set; }

        public string EntryContractName { get; private set; }
        public string EntryContractFunctionName { get; private set; }


        public static AppOptions ParseProcessArgs(string[] args)
        {

            var opts = new AppOptions();

            // Parse process arguments.
            var processArgs = ProcessArgs.Parse(args);
            opts.EntryContractName = null;
            opts.EntryContractFunctionName = null;
            if (!string.IsNullOrWhiteSpace(processArgs.Entry))
            {
                var entryParts = processArgs.Entry.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
                opts.EntryContractName = entryParts[0];
                if (entryParts.Length > 1)
                {
                    opts.EntryContractFunctionName = entryParts[1];
                }
            }

            string workspaceDir;
            if (!string.IsNullOrEmpty(processArgs.Directory))
            {
                workspaceDir = processArgs.Directory.Replace('\\', '/');
            }
            else if (!string.IsNullOrEmpty(processArgs.SingleFile))
            {
                // If workspace is not provided, derive a determistic temp directory for the single file.
                workspaceDir = Path.Combine(Path.GetTempPath(), HexUtil.GetHexFromBytes(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(processArgs.SingleFile))));
                Directory.CreateDirectory(workspaceDir);
                workspaceDir = workspaceDir.Replace('\\', '/');
            }
            else
            {
                throw new Exception("A directory or single file for debugging must be specified.");
            }

            string outputDir = workspaceDir + "/" + GENERATED_DATA_DIR;
            opts.SourceOutputDir = outputDir + "/src";
            opts.BuildOutputDir = outputDir + "/build";

            opts.SolCompilationSourcePath = workspaceDir;

            if (!string.IsNullOrEmpty(processArgs.SingleFile))
            {
                // Normalize file path.
                opts.SingleFile = processArgs.SingleFile.Replace('\\', '/');

                // Check if provided file is inside the workspace directory.
                if (opts.SingleFile.StartsWith(workspaceDir, StringComparison.OrdinalIgnoreCase))
                {
                    opts.SingleFile = opts.SingleFile.Substring(workspaceDir.Length).Trim('/');
                }
                else
                {
                    // File is outside of workspace so setup special pathing.
                    opts.SolCompilationSourcePath = opts.SingleFile;
                }
            }

            return opts;
        }

    }
}