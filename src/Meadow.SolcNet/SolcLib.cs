using Newtonsoft.Json;
using SolcNet.CompileErrors;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SolcNet
{
    public class SolcLib
    {
        #region Constants
        private const string SOLC_PATH = "solc";
        #endregion

        #region Fields
        private static Regex _regex = new Regex(@"Version: (\S*)");
        private readonly string _solSourceRoot;
        #endregion

        #region Properties
        public Process Process { get; }

        public string VersionDescription
        {
            get
            {
                // Invoke the command and read the result.
                string output = RunCommand("--version").stdout;

                // Parse all lines for the version number.
                string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    // If we can match a version string from this line, return it.
                    Match match = _regex.Match(line);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                throw new ApplicationException($"Unable to resolve version string from output:\r\n{output}");
            }
        }

        public Version Version
        {
            get
            {
                return Version.Parse(VersionDescription.Split(new[] { '-', '+' }, 2, StringSplitOptions.RemoveEmptyEntries)[0]);
            }
        }
        #endregion

        #region Constructor
        public SolcLib(string solSourceRoot = null, string solcPath = SOLC_PATH)
        {
            _solSourceRoot = solSourceRoot;

            // Initialize a process provider.
            Process = new Process();
            Process.StartInfo.WorkingDirectory = _solSourceRoot;
            Process.StartInfo.FileName = solcPath;
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
        }
        #endregion

        #region Functions
        private OutputDescription CompileInputDescriptionJson(
            string jsonInput,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            // If we have a null source lookup, initialize a new one
            soliditySourceFileContent = soliditySourceFileContent ?? new Dictionary<string, string>();

            // Parse the input
            InputDescription inputDescription = InputDescription.FromJsonString(jsonInput);

            // Loop for each file to load
            string lastSourceDirectory = null;
            foreach (string sourcePath in inputDescription.Sources.Keys)
            {
                string sourceFilePath = sourcePath;
                // if given path is relative and a root is provided, combine them
                if (!Path.IsPathRooted(sourcePath) && _solSourceRoot != null)
                {
                    sourceFilePath = Path.Combine(_solSourceRoot, sourcePath);
                }

                if (!File.Exists(sourceFilePath) && lastSourceDirectory != null)
                {
                    sourceFilePath = Path.Combine(lastSourceDirectory, sourcePath);
                }

                sourceFilePath = sourceFilePath.Replace('\\', '/');
                if (!soliditySourceFileContent.TryGetValue(sourceFilePath, out _))
                {
                    if (File.Exists(sourceFilePath))
                    {
                        lastSourceDirectory = Path.GetDirectoryName(sourceFilePath);
                        string contents = File.ReadAllText(sourceFilePath, Encoding.UTF8);
                        contents = contents.Replace("\r\n", "\n");
                        soliditySourceFileContent.Add(sourceFilePath, contents);
                    }
                    else
                    {
                        throw new Exception("Source file not found: " + sourcePath);
                    }
                }
            }

            // Invoke the command and return the result.
            var compileOutput = RunCommand("--standard-json --allow-paths .", jsonInput).stdout;
            var result = OutputDescription.FromJsonString(compileOutput);

            var compilerException = CompilerException.GetCompilerExceptions(result.Errors, errorHandling);
            if (compilerException != null)
            {
                throw compilerException;
            }

            return result;
        }

        public OutputDescription Compile(
            InputDescription input,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            var jsonStr = input.ToJsonString();
            return CompileInputDescriptionJson(jsonStr, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(
            string contractFilePaths,
            OutputType[] outputSelection,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            return Compile(new[] { contractFilePaths }, outputSelection ?? OutputTypes.All, optimizer, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(
            string contractFilePaths,
            OutputType? outputSelection = null,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            return Compile(new[] { contractFilePaths }, outputSelection, optimizer, errorHandling, soliditySourceFileContent);
        }

        /// <param name="outputSelection">Defaults to all output types if not specified</param>
        public OutputDescription Compile(
            string[] contractFilePaths,
            OutputType? outputSelection = null,
            Optimizer optimizer = null,
            CompileErrorHandling errorHandling = CompileErrorHandling.ThrowOnError,
            Dictionary<string, string> soliditySourceFileContent = null)
        {
            var outputs = outputSelection == null ? OutputTypes.All : OutputTypes.GetItems(outputSelection.Value);
            return Compile(contractFilePaths, outputs, optimizer, errorHandling, soliditySourceFileContent);
        }

        public OutputDescription Compile(
            string[] contractFilePaths,
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

        private (string stdout, string stderr) RunCommand(string arguments, string input = null, int timeout = int.MaxValue)
        {
            // Set the process invocation arguments.
            Process.StartInfo.Arguments = arguments;

            // Create our output string builders for this invocation (waiting for process to end before reading stdout/stderr can cause it to hang).
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                // Create our asynchronous standard stream readers for stdout/stderr.
                Process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        if (!outputWaitHandle.SafeWaitHandle.IsClosed)
                        {
                            outputWaitHandle.Set();
                        }
                    }
                    else
                    {
                        output.Append(e.Data);
                    }
                };
                Process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        if (!errorWaitHandle.SafeWaitHandle.IsClosed)
                        {
                            errorWaitHandle.Set();
                        }
                    }
                    else
                    {
                        error.Append(e.Data);
                    }
                };

                try
                {
                    // Invoke the underlying process.
                    Process.Start();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Solc invocation error: {ex.Message}", ex);
                }

                // Start the asynchronous reading operations.
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                // Write any input to stdin as desired.
                if (input != null)
                {
                    Process.StandardInput.Write(input);
                    Process.StandardInput.Close();
                }

                // Wait for execution to finish, and for stdout and stderr to conclude.
                if (Process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    // Cancel the reading operations.
                    Process.CancelOutputRead();
                    Process.CancelErrorRead();

                    // Return the result.
                    return (output.ToString(), error.ToString());
                }
                else
                {
                    throw new TimeoutException($"Solc invocation timeout error: {Process.StartInfo.FileName} {Process.StartInfo.Arguments}");
                }
            }
        }
        #endregion
    }
}
