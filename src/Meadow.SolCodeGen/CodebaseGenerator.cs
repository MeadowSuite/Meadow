using Meadow.Contract;
using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.SolCodeGen.CodeGenerators;
using Newtonsoft.Json.Linq;
using SolcNet;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.SolCodeGen
{

    public delegate void LoggerDelegate(string message);

    public class CodebaseGenerator
    {        
        
        // File extension for generated C# files
        public const string G_CS_FILE_EXT = ".sol.cs";
        public const string G_RESX_FILE_EXT = ".sol.resx";

        public const string GeneratedContractsDefaultDirectory = "GeneratedContracts";
        public const string GeneratedAssemblyDefaultDirectory = "GeneratedAssembly";
        public const string EventHelperFile = "ContractEventLogHelper";
        public const string SolcOutputDataHelperFile = "SolcOutputDataHelper";
        public const string SolcOutputDataResxFile = GeneratedSolcData.SOLC_OUTPUT_DATA_FILE;

        readonly SolCodeGenResults _genResults;

        readonly string _assemblyVersion;
        readonly bool _returnFullSources;

        readonly string _solSourceDirectory;
        readonly string _solSourceSingleFile;
        readonly string _generatedContractsDirectory;
        readonly string _generatedAssemblyDirectory;
        readonly string _namespace;
        readonly string _solidityCompilerVersion;
        readonly SolcLib _solcLib;
        readonly int _solcOptimzer;
        readonly LoggerDelegate _logger;

        public static SolCodeGenResults Generate(CommandArgs appArgs, bool returnFullSources = false, LoggerDelegate logger = null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            logger = (logger ?? Console.WriteLine);

            var isGenerateAssemblyEnabled = appArgs.Generate.HasFlag(GenerateOutputType.Assembly);

            var generator = new CodebaseGenerator(appArgs, returnFullSources | isGenerateAssemblyEnabled, logger);
            generator.GenerateSources();
            if (isGenerateAssemblyEnabled)
            {
                generator.GenerateCompilationOutput();
            }

            sw.Stop();
            logger($"SolCodeGen completed in: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            return generator._genResults;
        }

        private CodebaseGenerator(CommandArgs appArgs, bool returnFullSources, LoggerDelegate logger)
        {
            _logger = logger;
            _assemblyVersion = typeof(Program).Assembly.GetName().Version.ToString();
            _returnFullSources = returnFullSources;
            _genResults = new SolCodeGenResults();

            // Normalize source directory
            _solSourceDirectory = Path.GetFullPath(appArgs.SolSourceDirectory);

            // If we were passed a single sol file rather than a source directory
            if (_solSourceDirectory.EndsWith(".sol", StringComparison.OrdinalIgnoreCase) && File.Exists(_solSourceDirectory))
            {
                _solSourceSingleFile = _solSourceDirectory;
                _solSourceDirectory = Path.GetDirectoryName(_solSourceDirectory);
            }

            // Match the solc file path output format which replaces Windows path separator characters with unix style.
            if (Path.DirectorySeparatorChar == '\\')
            {
                _solSourceDirectory = _solSourceDirectory.Replace('\\', '/');
            }

            if (string.IsNullOrWhiteSpace(appArgs.OutputDirectory))
            {
                string sourceFilesParentDir = Directory.GetParent(_solSourceDirectory).FullName;
                _generatedContractsDirectory = Path.Combine(sourceFilesParentDir, GeneratedContractsDefaultDirectory);
            }
            else
            {
                _generatedContractsDirectory = Path.GetFullPath(appArgs.OutputDirectory);
            }

            if (string.IsNullOrWhiteSpace(appArgs.AssemblyOutputDirectory))
            {
                string sourceFilesParentDir = Directory.GetParent(_solSourceDirectory).FullName;
                _generatedAssemblyDirectory = Path.Combine(sourceFilesParentDir, GeneratedAssemblyDefaultDirectory);
            }
            else
            {
                _generatedAssemblyDirectory = Path.GetFullPath(appArgs.AssemblyOutputDirectory);
            }

            if (_solSourceSingleFile != null)
            {
                _solSourceDirectory = string.Empty;
            }

            _namespace = appArgs.Namespace;
            _solcOptimzer = appArgs.SolcOptimizer;
            _solcLib = SetupSolcLib();
            _solidityCompilerVersion = _solcLib.Version.ToString(3);
        }

        SolcLib SetupSolcLib()
        { 
            
            string sourceDir = string.IsNullOrEmpty(_solSourceSingleFile) ? _solSourceDirectory : null;
            SolcLib solcLib = new SolcLib(sourceDir);
            _logger($"Using solc version {solcLib.VersionDescription}");
            return solcLib;
        }

        void GenerateCompilationOutput()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            bool assemblyAlreadyUpToDate = false;
            var solCodeHashFile = Path.Combine(_generatedAssemblyDirectory, _namespace + ".solcodehash");

            if (File.Exists(solCodeHashFile))
            {
                try
                {
                    var solCodeHashFileContents = File.ReadAllText(solCodeHashFile, new UTF8Encoding(false, false));
                    if (solCodeHashFileContents.Trim().Equals(_genResults.SolcCodeBaseHash.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        assemblyAlreadyUpToDate = true;
                        _genResults.CompilationResults = new SolCodeGenCompilationResults
                        {
                            AssemblyFilePath = Path.Combine(_generatedAssemblyDirectory, _namespace + ".dll"),
                            PdbFilePath = Path.Combine(_generatedAssemblyDirectory, _namespace + ".pdb"),
                            XmlDocFilePath = Path.Combine(_generatedAssemblyDirectory, _namespace + ".xml")
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger("Exception loading existing assembly for cache checking:");
                    _logger(ex.ToString());
                }
            }

            if (!assemblyAlreadyUpToDate)
            {
                var compilation = new Compilation(_genResults, _namespace, _generatedAssemblyDirectory);
                compilation.Compile();
                File.WriteAllText(solCodeHashFile, _genResults.SolcCodeBaseHash, new UTF8Encoding(false, false));
            }

            sw.Stop();
            _logger($"Compilation of generated C# code and resx completed in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        void GenerateSources()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            string[] solFiles;
            if (!string.IsNullOrEmpty(_solSourceSingleFile))
            {
                solFiles = new[] { _solSourceSingleFile };
            }
            else
            {
                solFiles = GetSolContractFiles(_solSourceDirectory);
            }

            var outputFlags = new[]
            {
                OutputType.Abi,
                OutputType.EvmBytecodeObject,
                OutputType.EvmBytecodeOpcodes,
                OutputType.EvmBytecodeSourceMap,
                OutputType.EvmDeployedBytecodeObject,
                OutputType.EvmDeployedBytecodeOpcodes,
                OutputType.EvmDeployedBytecodeSourceMap,
                OutputType.DevDoc,
                OutputType.UserDoc,
                OutputType.Metadata,
                OutputType.Ast
            };

            var solcOptimizerSettings = new Optimizer();
            if (_solcOptimzer > 0)
            {
                solcOptimizerSettings.Enabled = true;
                solcOptimizerSettings.Runs = _solcOptimzer;
            }

            _logger("Compiling solidity files in " + _solSourceDirectory);
            var soliditySourceContent = new Dictionary<string, string>();
            var solcOutput = _solcLib.Compile(solFiles, outputFlags, solcOptimizerSettings, soliditySourceFileContent: soliditySourceContent);

            sw.Stop();
            _logger($"Compiling solidity completed in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");

            #region Generated hashes for solidity sources
            sw.Restart();

            // Calculate a deterministic hash of the solidity source code base, including file paths and the Meadow assembly version.
            var codeBaseHash = KeccakHash.FromString(string.Join('|', soliditySourceContent
                .OrderBy(k => k.Key)
                .SelectMany(k => new[] { k.Key, k.Value })
                .Concat(new[] { _assemblyVersion })));

            _genResults.SolcCodeBaseHash = HexUtil.GetHexFromBytes(codeBaseHash);

            var flattenedContracts = solcOutput.ContractsFlattened.OrderBy(c => c.SolFile).ToArray();
            ContractInfo[] contractInfos = new ContractInfo[solcOutput.ContractsFlattened.Length];

            for (var i = 0; i < contractInfos.Length; i++)
            {
                var c = flattenedContracts[i];

                // Check if any previous contracts have the same name as this one.
                int dupNames = 0;
                for (var f = 0; f < i; f++)
                {
                    if (flattenedContracts[f].ContractName == c.ContractName)
                    {
                        dupNames++;
                    }
                }

                string generatedContractName = c.ContractName;

                // If there are duplicate contract names, prepend a unique amount of underscore suffixes.
                if (dupNames > 0)
                {
                    generatedContractName += new string(Enumerable.Repeat('_', dupNames).ToArray());
                }

                contractInfos[i] = new ContractInfo(
                    Util.GetRelativeFilePath(_solSourceDirectory, c.SolFile),
                    generatedContractName,
                    c.Contract,
                    GetSourceHashesXor(c.Contract),
                    c.Contract.Evm.Bytecode.Object);
            }



            _logger($"Generated sol source file hashes in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            sw.Stop();
            #endregion


            _logger("Writing generated output to directory: " + _generatedContractsDirectory);

            #region Output directory cleanup
            if (!Directory.Exists(_generatedContractsDirectory))
            {
                _logger("Creating directory: " + _generatedContractsDirectory);
                Directory.CreateDirectory(_generatedContractsDirectory);
            }
            else
            {
                var expectedFiles = contractInfos
                    .Select(c => c.ContractName)
                    .Concat(new[] { EventHelperFile, SolcOutputDataHelperFile })
                    .Select(c => NormalizePath($"{_generatedContractsDirectory}/{c}{G_CS_FILE_EXT}"))
                    .ToArray();

                var existingFiles = Directory
                    .GetFiles(_generatedContractsDirectory, $"*{G_CS_FILE_EXT}", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".sol.cs", StringComparison.Ordinal) || f.EndsWith(".sol.resx", StringComparison.Ordinal))
                    .Select(f => NormalizePath(f))
                    .ToArray();

                // Delete existing files with no corresponding file that can be generated
                foreach (var existingFile in existingFiles)
                {
                    bool found = false;
                    foreach (var expected in expectedFiles)
                    {
                        if (expected.Equals(existingFile, StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _logger("Deleting outdated file: " + existingFile);
                        File.Delete(existingFile);
                    }
                }
            }
            #endregion

            #region AST output generation
            sw.Restart();
            GenerateSolcOutputDataFiles(solcOutput, soliditySourceContent, codeBaseHash);
            sw.Stop();
            _logger($"Resx file for solc output generation took: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            #endregion


            #region
            sw.Restart();
            var generatedEvents = new List<GeneratedEventMetadata>();
            GeneratedContractSourceFiles(contractInfos, generatedEvents);
            GenerateEventHelper(generatedEvents);
            sw.Stop();
            _logger($"Contract and event source code generation took: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            #endregion

        }


        void GenerateSolcOutputDataFiles(
            OutputDescription solcOutput,
            Dictionary<string, string> soliditySourceContent,
            byte[] codeBaseHash)
        {
            var codeBaseHashBytes = HexUtil.GetHexFromBytes(codeBaseHash);
            var codeBaseHashHexBytes = Encoding.ASCII.GetBytes(codeBaseHashBytes);

            var outputHelperFilePath = Path.Combine(_generatedContractsDirectory, SolcOutputDataHelperFile + G_CS_FILE_EXT);
            var outputResxFilePath = Path.Combine(_generatedContractsDirectory, SolcOutputDataResxFile + G_RESX_FILE_EXT);

            if (!_returnFullSources && FileStartsWithHash(outputHelperFilePath, codeBaseHashHexBytes) && File.Exists(outputResxFilePath))
            {
                _logger("Skipping writing already up-to-date source file: " + SolcOutputDataHelperFile);
                _logger("Skipping writing already up-to-date source file: " + SolcOutputDataResxFile);
                return;
            }

            var solcOutputDataResxGenerator = new SolcOutputDataResxGenerator(solcOutput, soliditySourceContent, _solSourceDirectory, _solidityCompilerVersion);
            var solcOutputDataResxWriter = solcOutputDataResxGenerator.GenerateResx();
            using (var fs = new StreamWriter(outputResxFilePath, append: false, encoding: new UTF8Encoding(false)))
            {
                solcOutputDataResxWriter.Save(fs);
                _genResults.GeneratedResxFilePath = outputResxFilePath;
            }

            var generator = new SolcOutputHelperGenerator(codeBaseHash, _namespace);
            var (generatedCode, syntaxTree) = generator.GenerateSourceCode();
            using (var fs = new StreamWriter(outputHelperFilePath, append: false, encoding: StringUtil.UTF8))
            {
                _logger("Writing source file: " + outputHelperFilePath);
                var hashHex = HexUtil.GetHexFromBytes(codeBaseHash);
                fs.WriteLine("//" + hashHex);
                fs.WriteLine(generatedCode);

                var generatedCSharpEntry = new SolCodeGenCSharpResult(outputHelperFilePath, generatedCode, syntaxTree);
                _genResults.GeneratedCSharpEntries.Add(generatedCSharpEntry);

            }

            if (_returnFullSources)
            {
                _genResults.GeneratedResxResources = solcOutputDataResxWriter.Resources;
            }
        }

        void GeneratedContractSourceFiles(
            ContractInfo[] contractInfos,
            List<GeneratedEventMetadata> generatedEvents)
        {
            
            int skippedAlreadyUpdated = 0;
            foreach (var contractInfo in contractInfos)
            {
                var (solFile, contractName, contract, hash, bytecode) = contractInfo;
                var hashHex = HexUtil.GetHexFromBytes(hash);
                var hashHexBytes = Encoding.ASCII.GetBytes(hashHex);
                var contactEventInfo = GeneratedEventMetadata.Parse(contractName, _namespace, contract).ToList();
                generatedEvents.AddRange(contactEventInfo);

                var outputFilePath = Path.Combine(_generatedContractsDirectory, contractName + G_CS_FILE_EXT);
                if (!_returnFullSources && FileStartsWithHash(outputFilePath, hashHexBytes))
                {
                    skippedAlreadyUpdated++;
                    continue;
                }

                var generator = new ContractGenerator(contractInfo, _solSourceDirectory, _namespace, contactEventInfo);
                var (generatedContractCode, syntaxTree) = generator.GenerateSourceCode();
                using (var fs = new StreamWriter(outputFilePath, append: false, encoding: StringUtil.UTF8))
                {
                    _logger("Writing source file: " + outputFilePath);
                    fs.WriteLine("//" + hashHex);
                    fs.WriteLine(generatedContractCode);

                    var generatedCSharpEntry = new SolCodeGenCSharpResult(outputFilePath, generatedContractCode, syntaxTree);
                    _genResults.GeneratedCSharpEntries.Add(generatedCSharpEntry);
                }


            }

            if (skippedAlreadyUpdated > 0)
            {
                _logger($"Detected already up-to-date generated files: {skippedAlreadyUpdated} contracts");
            }

        }


        void GenerateEventHelper(List<GeneratedEventMetadata> generatedEvents)
        {
            var eventMetadataHash = GeneratedEventMetadata.GetHash(generatedEvents);
            var eventMetadataHashHex = HexUtil.GetHexFromBytes(eventMetadataHash);
            var eventMetadataHashHexBytes = Encoding.ASCII.GetBytes(eventMetadataHashHex);

            var outputFilePath = Path.Combine(_generatedContractsDirectory, EventHelperFile + G_CS_FILE_EXT);
            if (!_returnFullSources && FileStartsWithHash(outputFilePath, eventMetadataHashHexBytes))
            {
                _logger("Skipping writing already up-to-date source file: " + EventHelperFile);
                return;
            }

            var generator = new EventHelperGenerator(generatedEvents, _namespace);
            var (generatedCode, syntaxTree) = generator.GenerateSourceCode();
            using (var fs = new StreamWriter(outputFilePath, append: false, encoding: StringUtil.UTF8))
            {
                _logger("Writing source file: " + outputFilePath);
                fs.WriteLine("//" + eventMetadataHashHex);
                fs.WriteLine(generatedCode);

                var generatedCSharpEntry = new SolCodeGenCSharpResult(outputFilePath, generatedCode, syntaxTree);
                _genResults.GeneratedCSharpEntries.Add(generatedCSharpEntry);
            }

        }


        static string[] GetSolContractFiles(string contractsDir)
        {
            if (!Directory.Exists(contractsDir))
            {
                throw new Exception("Solidity source directory does not exist: " + contractsDir);
            }

            var solFiles = Directory
                .GetFiles(contractsDir, "*.sol", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(contractsDir, f))
                .ToArray();

            if (solFiles.Length == 0)
            {
                throw new Exception("No solidity source files found in: " + contractsDir);
            }

            return solFiles;
        }


        static bool FileStartsWithHash(string filePath, Span<byte> hash)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var fileBuffer = new byte[2 + hash.Length];
            using (var fileStream = File.OpenRead(filePath))
            {
                var bytesRead = fileStream.Read(fileBuffer, 0, fileBuffer.Length);
                if (bytesRead != fileBuffer.Length)
                {
                    return false;
                }

                if (fileBuffer[0] != (byte)'/' || fileBuffer[1] != (byte)'/')
                {
                    return false;
                }

                if (new Span<byte>(fileBuffer).Slice(2).SequenceEqual(hash))
                {
                    return true;
                }
            }

            return false;
        }



        /// <summary>
        /// Returns xor of metadata source hashes. If there are no source hashes, returns the 
        /// hash of the json abi string.
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        byte[] GetSourceHashesXor(SolcNet.DataDescription.Output.Contract contract)
        {
            if (string.IsNullOrEmpty(contract.Metadata))
            {
                return KeccakHashString(_assemblyVersion + "\n" + contract.AbiJsonString);
            }

            var hashes = JObject.Parse(contract.Metadata)
                .SelectTokens("sources.*.keccak256")
                .Values<string>()
                .ToArray();

            Span<byte> hashBuff = new byte[32];
            Span<ulong> hashBuffLong = MemoryMarshal.Cast<byte, ulong>(hashBuff);

            byte[] resultBuffer = new byte[32];
            KeccakHashString(_assemblyVersion, resultBuffer);

            Span<ulong> resultBufferLong = MemoryMarshal.Cast<byte, ulong>(resultBuffer);

            for (var i = 0; i < hashes.Length; i++)
            {
                var hash = hashes[i].AsSpan(2, 64);

                HexUtil.HexToSpan(hash, hashBuff);
                for (var j = 0; j < resultBufferLong.Length; j++)
                {
                    resultBufferLong[j] ^= hashBuffLong[j];
                }
            }

            return resultBuffer;
        }

        static byte[] KeccakHashString(string str)
        {
            var hash = new byte[KeccakHash.HASH_SIZE];
            KeccakHashString(str, hash);
            return hash;
        }

        static void KeccakHashString(string str, Span<byte> output)
        {
            var strBytes = StringUtil.UTF8.GetBytes(str);
            KeccakHash.ComputeHash(strBytes, output);
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

    }
}
