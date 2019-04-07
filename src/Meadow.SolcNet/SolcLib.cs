using Newtonsoft.Json;
using SolcNet.CompileErrors;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using SolcNet.NativeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolcNet
{
    public class SolcLib
    {
        private INativeSolcLib _native;
        public string NativeLibFilePath => _native.NativeLibFilePath;

        public string VersionDescription => _native.GetVersion();
        public Version Version => ParseVersionString(VersionDescription);

        public static Version ParseVersionString(string versionString)
        {
            return Version.Parse(versionString.Split(new[] { '-', '+' }, 2, StringSplitOptions.RemoveEmptyEntries)[0]);
        }

        public string License => _native.GetLicense();

        readonly string _solSourceRoot;

        public SolcLib(string solSourceRoot = null, string[] extraLibSearchDirs = null)
        {
            _native = new SolcLibSystemProvider(solSourceRoot);
            _solSourceRoot = solSourceRoot;
        }

        public SolcLib(INativeSolcLib nativeLib, string solSourceRoot = null)
        {
            _native = nativeLib;
            _solSourceRoot = solSourceRoot;
        }

        public static SolcLib Create(string solSourceRoot = null)
        {
            var callingAssembly = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            return new SolcLib(solSourceRoot, extraLibSearchDirs: new[] { callingAssembly });
        }

        public static SolcLib Create<TNativeLib>(string solSourceRoot = null) where TNativeLib : INativeSolcLib, new()
        {
            return new SolcLib(new TNativeLib(), solSourceRoot);
        }

        private OutputDescription CompileInputDescriptionJson(string jsonInput,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            // Wrap the resolver object in a using to avoid it from being garbage collected during the
            // execution of the native solc compile function.
            using (var sourceResolver = new SourceFileResolver(_solSourceRoot, soliditySourceFileContent))
            {
                var res = _native.Compile(jsonInput, sourceResolver.ReadFileDelegate);
                var output = OutputDescription.FromJsonString(res);

                var compilerException = CompilerException.GetCompilerExceptions(output.Errors, errorHandling);
                if (compilerException != null)
                {
                    throw compilerException;
                }
                return output;
            }
        }

        public OutputDescription Compile(InputDescription input,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            var jsonStr = input.ToJsonString();
            return CompileInputDescriptionJson(jsonStr, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string contractFilePaths,
            OutputType[] outputSelection,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            return Compile(new[] { contractFilePaths }, outputSelection ?? OutputTypes.All, optimizer, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string contractFilePaths,
            OutputType? outputSelection = null,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            return Compile(new[] { contractFilePaths }, outputSelection, optimizer, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(string[] contractFilePaths,
            OutputType? outputSelection = null,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            var outputs = outputSelection == null ? OutputTypes.All : OutputTypes.GetItems(outputSelection.Value);
            return Compile(contractFilePaths, outputs, optimizer, errorHandling, soliditySourceFileContent);
        }

        public OutputDescription Compile(string[] contractFilePaths,
            OutputType[] outputSelection,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            var inputDesc = new InputDescription();
            inputDesc.Settings.OutputSelection["*"] = new Dictionary<string, OutputType[]>
            {
                ["*"] = outputSelection,
                [""] = outputSelection
            };

            if (optimizer != null)
            {
                inputDesc.Settings.Optimizer = optimizer;
            }

            foreach (var filePath in contractFilePaths)
            {
                var normalizedPath = filePath.Replace('\\', '/');
                var source = new Source { Urls = new List<string> { normalizedPath } };
                inputDesc.Sources[normalizedPath] = source;
            }

            return Compile(inputDesc, errorHandling, soliditySourceFileContent);
        }

    }
}
