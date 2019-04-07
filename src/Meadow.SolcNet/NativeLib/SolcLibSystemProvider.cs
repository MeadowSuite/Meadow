using SolcNet.DataDescription.Input;
using SolcNet.NativeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SolcNet.NativeLib
{
    public class SolcLibSystemProvider : INativeSolcLib
    {
        #region Constants
        public const string SYSTEM_LOCATION = "solc";
        #endregion

        #region Properties
        public Process Process { get; }
        public string NativeLibFilePath => "";
        private static Regex _regex = new Regex(@"Version: (\S*)");
        #endregion

        #region Constructors
        public SolcLibSystemProvider(string workingDirectory)
        {
            // Initialize a process provider.
            Process = new Process();
            Process.StartInfo.WorkingDirectory = workingDirectory;
            Process.StartInfo.FileName = SYSTEM_LOCATION;
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
        }
        #endregion

        #region Functions
        public string GetLicense()
        {
            return null;
        }

        public string GetVersion()
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

        public string Compile(string input, ReadFileCallback readCallback)
        {
            // Parse the input
            InputDescription inputDescription = InputDescription.FromJsonString(input);
            foreach (string sourcePath in inputDescription.Sources.Keys)
            {
                string contents = "";
                string error = "";
                readCallback(sourcePath, ref contents, ref error);
            }

            // Invoke the command and return the result.
            var result = RunCommand("--standard-json --allow-paths .", input);
            return result.stdout;
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

                // Invoke the underlying process.
                Process.Start();

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
                    throw new TimeoutException($"Command invocation failed for command: {Process.StartInfo.FileName} {Process.StartInfo.Arguments}");
                }
            }
        }

        public void Dispose()
        {
            Process.Dispose();
        }
        #endregion
    }
}
