using McMaster.Extensions.CommandLineUtils;
using Meadow.Contract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Meadow.SolCodeGen.Test
{
    public class CommandArgParsing : IDisposable
    {
        readonly string _guid;
        readonly string _tempDir;
        readonly string _outputDir;
        readonly string _assemblyOutputDir;
        readonly string _sourceDir;
        readonly string _sourceEmptyDir;
        readonly string _namespace;

        public CommandArgParsing()
        {
            _guid = Guid.NewGuid().ToString().Replace("-", "", StringComparison.Ordinal);
            _tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), _guid)).FullName;

            _outputDir = Path.Combine(_tempDir, "GeneratedOutput");
            _assemblyOutputDir = Path.Combine(_tempDir, "GeneratedAssembly");
            _sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "Contracts");
            _sourceEmptyDir = Path.Combine(Directory.GetCurrentDirectory(), "ContractsEmpty");

            _namespace = "Gen" + Guid.NewGuid().ToString().Replace("-", "", StringComparison.Ordinal) + ".Contracts";
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void MissingNamespace()
        {
            var processArgs = new string[]
            {
                "--namespace", "*badname space^ str",
                "--source", _sourceDir,
                "--generate", "source",
                "--output", _outputDir,
                "--solcoptimizer", "0"
            };

            try
            {
                Meadow.SolCodeGen.Program.Run(processArgs);
            }
            catch (Exception ex) when (ex.Message.Contains("not valid intenfier syntax", StringComparison.Ordinal))
            {
                return;
            }

            throw new Exception("Should have failed test");
        }

        [Fact]
        public void InvalidSourceDirectory()
        {
            var processArgs = new string[]
            {
                "--namespace", _namespace,
                "--source", "/bad source diretory/not exists/1235",
                "--generate", "source",
                "--output", _outputDir,
                "--solcoptimizer", "0"
            };

            try
            {
                Meadow.SolCodeGen.Program.Run(processArgs);
            }
            catch (Exception ex) when (ex.Message.Contains("source file directory does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new Exception("Should have failed test");
        }

        [Fact]
        public void InvalidGenerationType()
        {
            var processArgs = new string[]
            {
                "--namespace", _namespace,
                "--source", _sourceDir,
                "--generate", "invalid generation type asdf1234",
                "--output", _outputDir,
                "--solcoptimizer", "0"
            };

            try
            {
                Meadow.SolCodeGen.Program.Run(processArgs);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid value specified for generate", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new Exception("Should have failed test");
        }

        [Fact]
        public void InvalidSolcOptimizer()
        {
            var processArgs = new string[]
            {
                "--namespace", _namespace,
                "--source", _sourceDir,
                "--generate", "source",
                "--output", _outputDir,
                "--solcoptimizer", "unparsable int"
            };

            try
            {
                Meadow.SolCodeGen.Program.Run(processArgs);
            }
            catch (Exception ex) when (ex.Message.Contains("Could not parse specified solc optimizer", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new Exception("Should have failed test");
        }

        [Fact]
        public void MissingContractSources()
        {
            var processArgs = new string[]
            {
                "--namespace", _namespace,
                "--source", _sourceEmptyDir,
                "--generate", "source",
                "--output", _outputDir,
                "--solcoptimizer", "0"
            };

            try
            {
                Meadow.SolCodeGen.Program.Run(processArgs);
            }
            catch (Exception ex) when (ex.Message.Contains("directory does not contain any .sol files", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new Exception("Should have failed test");
        }

        [Fact]
        public void AllValidOptions()
        {
            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }

            var testNamespace = _namespace;
            var processArgs = new string[]
            {
                "--namespace", testNamespace,
                "--source", _sourceDir,
                "--generate", "source",
                "--generate", "assembly",
                "--output", _outputDir,
                "--assembly-output", _assemblyOutputDir,
                "--solcoptimizer", "0"
            };

            var exitCode = Meadow.SolCodeGen.Program.Main(processArgs);
            if (exitCode != 0)
            {
                throw new Exception("Failed with exit code: " + exitCode);
            }

            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }
        }

        [Fact]
        public void MinimalOptions()
        {
            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }

            var testNamespace = _namespace;
            var processArgs = new string[]
            {
                "--namespace", testNamespace,
                "--source", _sourceDir,
                "--generate", "source"
            };

            var exitCode = Meadow.SolCodeGen.Program.Main(processArgs);
            if (exitCode != 0)
            {
                throw new Exception("Failed with exit code: " + exitCode);
            }

            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }
        }


        [Fact]
        public void TestCompiledAssembly()
        {
            var solCodeGenArgs = new CommandArgs
            {
                OutputDirectory = _outputDir,
                AssemblyOutputDirectory = _assemblyOutputDir,
                SolSourceDirectory = _sourceDir,
                Generate = GenerateOutputType.Source | GenerateOutputType.Assembly,
                Namespace = _namespace
            };

            var solCodeGenResults = CodebaseGenerator.Generate(solCodeGenArgs);

            Assembly loadedAssembly;
            var assemblyBytes = File.ReadAllBytes(solCodeGenResults.CompilationResults.AssemblyFilePath);
            var pdbBytes = File.ReadAllBytes(solCodeGenResults.CompilationResults.PdbFilePath);

            loadedAssembly = Assembly.Load(assemblyBytes, pdbBytes);

            var solcDataParser = GeneratedSolcData.Create(loadedAssembly);
            var data = solcDataParser.GetSolcData();
        }

    }
}
