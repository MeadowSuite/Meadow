using Meadow.Contract;
using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.SolCodeGen.CodeGenerators;
using Newtonsoft.Json.Linq;
using SolcNet;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using SolcNet.NativeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.SolCodeGen
{
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
        readonly string _generatedContractsDirectory;
        readonly string _generatedAssemblyDirectory;
        readonly string _namespace;
        readonly string _legacySolcPath;
        readonly Version _solcVersion;
        readonly string _solidityCompilerVersion;
        readonly SolcLib _solcLib;
        readonly int _solcOptimzer;

        public static SolCodeGenResults Generate(CommandArgs appArgs, bool returnFullSources = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var isGenerateAssemblyEnabled = appArgs.Generate.HasFlag(GenerateOutputType.Assembly);

            var generator = new CodebaseGenerator(appArgs, returnFullSources | isGenerateAssemblyEnabled);
            generator.GenerateSources();
            if (isGenerateAssemblyEnabled)
            {
                generator.GenerateCompilationOutput();
            }

            sw.Stop();
            Console.WriteLine($"SolCodeGen completed in: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            return generator._genResults;
        }

        private CodebaseGenerator(CommandArgs appArgs, bool returnFullSources)
        {
            _assemblyVersion = typeof(Program).Assembly.GetName().Version.ToString();
            _returnFullSources = returnFullSources;
            _genResults = new SolCodeGenResults();

            // Normalize source directory
            _solSourceDirectory = Path.GetFullPath(appArgs.SolSourceDirectory);

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

            if (!string.IsNullOrEmpty(appArgs.LegacySolcPath))
            {
                _legacySolcPath = Path.GetFullPath(appArgs.LegacySolcPath);
            }

            _namespace = appArgs.Namespace;
            _solcVersion = appArgs.SolcVersion;
            _solcOptimzer = appArgs.SolcOptimizer;
            _solcLib = SetupSolcLib();
            _solidityCompilerVersion = _solcLib.Version.ToString(3);
        }

        SolcLib SetupSolcLib()
        {
            SolcLibDefaultProvider solcNativeLibProvider = null;

            if (_solcVersion != null)
            {
                if (!string.IsNullOrEmpty(_legacySolcPath))
                {
                    try
                    {
                        var legacyNativeLibPath = LegacySolcNet.ResolveNativeLibPath(_legacySolcPath, _solcVersion);
                        solcNativeLibProvider = new SolcLibDefaultProvider(legacyNativeLibPath);
                        var resultSolcVersion = SolcLib.ParseVersionString(solcNativeLibProvider.GetVersion());
                        if (_solcVersion != resultSolcVersion)
                        {
                            throw new Exception($"A legacy solc version ({_solcVersion}) is specified but resolver returned a different version ({resultSolcVersion})");
                        }
                    }
                    catch
                    {
                        // There was an error trying to use the specific solcversion with the legacy package,
                        // for giving up lets check if the specified solcversion is valid in the latest solcnet
                        // lib.
                        solcNativeLibProvider = new SolcLibDefaultProvider();
                        var defaultSolcVersion = SolcLib.ParseVersionString(solcNativeLibProvider.GetVersion());
                        if (_solcVersion != defaultSolcVersion)
                        {
                            // Latest version is not valid, so throw the original exception.
                            throw;
                        }
                    }
                }
                else
                {
                    solcNativeLibProvider = new SolcLibDefaultProvider();
                    var defaultSolcVersion = SolcLib.ParseVersionString(solcNativeLibProvider.GetVersion());
                    if (_solcVersion != defaultSolcVersion)
                    {
                        throw new Exception("A legacy solc version is specified but resolver lib path is not specified. Is the SolcNet.Legacy package not installed?");
                    }
                }
            }

            SolcLib solcLib;
            if (solcNativeLibProvider != null)
            {
                solcLib = new SolcLib(solcNativeLibProvider, _solSourceDirectory);
            }
            else
            {
                solcLib = SolcLib.Create(_solSourceDirectory);
            }

            Console.WriteLine($"Using native libsolc version {solcLib.VersionDescription} at {solcLib.NativeLibFilePath}");
            return solcLib;
        }

        void GenerateCompilationOutput()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var compilation = new Compilation(_genResults, _namespace, _generatedAssemblyDirectory);
            compilation.Compile();

            sw.Stop();
            Console.WriteLine($"Compilation of generated C# code and resx completed in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        void GenerateSources()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var solFiles = GetSolContractFiles(_solSourceDirectory);

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

            Console.WriteLine("Compiling solidity files in " + _solSourceDirectory);
            var soliditySourceContent = new Dictionary<string, string>();
            var solcOutput = _solcLib.Compile(solFiles, outputFlags, solcOptimizerSettings, soliditySourceFileContent: soliditySourceContent);

            sw.Stop();
            Console.WriteLine($"Compiling solidity completed in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");


            Console.WriteLine("Writing generated output to directory: " + _generatedContractsDirectory);


            // Initial pass through contracts to get the generated class and file names.
            (string GeneratedContractName, string SolFile, SolcNet.DataDescription.Output.Contract Contract)[] contracts = new (string, string, SolcNet.DataDescription.Output.Contract)[solcOutput.ContractsFlattened.Length];
            var flattenedContracts = solcOutput.ContractsFlattened.OrderBy(c => c.SolFile).ToArray();

            for (var i = 0; i < contracts.Length; i++)
            {
                // Check if any previous contracts have the same name as this one.
                var c = flattenedContracts[i];
                int dupNames = 0;
                for (var f = 0; f < i; f++)
                {
                    if (flattenedContracts[f].ContractName == c.ContractName)
                    {
                        dupNames++;
                    }
                }

                // If there are duplicate contract names, prepend a unique amount of underscore suffixes.
                string dupeNameSuffix;
                if (dupNames > 0)
                {
                    dupeNameSuffix = new string(Enumerable.Repeat('_', dupNames).ToArray());
                }
                else
                {
                    dupeNameSuffix = string.Empty;
                }

                contracts[i] = (c.ContractName + dupeNameSuffix, c.SolFile, c.Contract);

            }

            #region Output directory cleanup
            if (!Directory.Exists(_generatedContractsDirectory))
            {
                Console.WriteLine("Creating directory: " + _generatedContractsDirectory);
                Directory.CreateDirectory(_generatedContractsDirectory);
            }
            else
            {
                var expectedFiles = contracts
                    .Select(c => c.GeneratedContractName)
                    .Concat(new[] { EventHelperFile, SolcOutputDataHelperFile })
                    .Select(c => NormalizePath($"{_generatedContractsDirectory}/{c}{G_CS_FILE_EXT}"))
                    .ToArray();

                var existingFiles = Directory
                    .GetFiles(_generatedContractsDirectory, $"*{G_CS_FILE_EXT}", SearchOption.TopDirectoryOnly)
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
                        Console.WriteLine("Deleting outdated file: " + existingFile);
                        File.Delete(existingFile);
                    }
                }
            }
            #endregion


            #region Generated hashes for solidity sources
            sw.Restart();

            ContractInfo[] contractInfos = new ContractInfo[solcOutput.ContractsFlattened.Length];

            for (var i = 0; i < contracts.Length; i++)
            {
                var c = contracts[i];
                contractInfos[i] = new ContractInfo(
                    Util.GetRelativeFilePath(_solSourceDirectory, c.SolFile),
                    c.GeneratedContractName,
                    c.Contract,
                    GetSourceHashesXor(c.Contract),
                    c.Contract.Evm.Bytecode.Object);
            }


            var contractPathsHash = KeccakHashString(string.Join("\n", contractInfos.SelectMany(c => new[] { c.SolFile, c.ContractName })));
            var codeBaseHash = XorAllHashes(contractInfos.Select(c => c.Hash).Concat(new[] { contractPathsHash }).ToArray());
            Console.WriteLine($"Generated sol source file hashes in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            sw.Stop();
            #endregion


            #region AST output generation
            sw.Restart();
            GenerateSolcOutputDataFiles(solcOutput, soliditySourceContent, codeBaseHash);
            sw.Stop();
            Console.WriteLine($"Resx file for solc output generation took: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            #endregion


            #region
            sw.Restart();
            var generatedEvents = new List<GeneratedEventMetadata>();
            GeneratedContractSourceFiles(contractInfos, generatedEvents);
            GenerateEventHelper(generatedEvents);
            sw.Stop();
            Console.WriteLine($"Contract and event source code generation took: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
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
                Console.WriteLine("Skipping writing already up-to-date source file: " + SolcOutputDataHelperFile);
                Console.WriteLine("Skipping writing already up-to-date source file: " + SolcOutputDataResxFile);
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
            var (generatedContractCode, syntaxTree) = generator.GenerateSourceCode();
            using (var fs = new StreamWriter(outputHelperFilePath, append: false, encoding: StringUtil.UTF8))
            {
                Console.WriteLine("Writing source file: " + outputHelperFilePath);
                var hashHex = HexUtil.GetHexFromBytes(codeBaseHash);
                fs.WriteLine("//" + hashHex);
                fs.WriteLine(generatedContractCode);
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
                    Console.WriteLine("Writing source file: " + outputFilePath);
                    fs.WriteLine("//" + hashHex);
                    fs.WriteLine(generatedContractCode);

                    var generatedCSharpEntry = new SolCodeGenCSharpResult(outputFilePath, generatedContractCode, syntaxTree);
                    _genResults.GeneratedCSharpEntries.Add(generatedCSharpEntry);
                }


            }

            if (skippedAlreadyUpdated > 0)
            {
                Console.WriteLine($"Detected already up-to-date generated files: {skippedAlreadyUpdated} contracts");
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
                Console.WriteLine("Skipping writing already up-to-date source file: " + EventHelperFile);
                return;
            }

            var generator = new EventHelperGenerator(generatedEvents, _namespace);
            var (generatedCode, syntaxTree) = generator.GenerateSourceCode();
            using (var fs = new StreamWriter(outputFilePath, append: false, encoding: StringUtil.UTF8))
            {
                Console.WriteLine("Writing source file: " + outputFilePath);
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

        static byte[] XorAllHashes(byte[][] hashes)
        {
            byte[] codeBaseHash = new byte[32];
            Span<ulong> int64Span = MemoryMarshal.Cast<byte, ulong>(codeBaseHash);

            for (var i = 0; i < hashes.Length; i++)
            {
                if (i == 0)
                {
                    new Span<byte>(hashes[i]).CopyTo(codeBaseHash);
                }
                else
                {
                    Span<ulong> inputSpan = MemoryMarshal.Cast<byte, ulong>(hashes[i]);
                    for (var j = 0; j < int64Span.Length; j++)
                    {
                        int64Span[j] ^= inputSpan[j];
                    }
                }
            }

            return codeBaseHash;
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

        static string GetContractsDir(string projDir)
        {
            var contractsDir = $"{projDir}{Path.DirectorySeparatorChar}Contracts";
            if (!Directory.Exists(contractsDir))
            {
                contractsDir = $"{projDir}{Path.DirectorySeparatorChar}contracts";
            }

            if (!Directory.Exists(contractsDir))
            {
                throw new Exception("Project must contain a Contracts directory: " + contractsDir);
            }

            return NormalizePath(contractsDir);
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

    }
}
