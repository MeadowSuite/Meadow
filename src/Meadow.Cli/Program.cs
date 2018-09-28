using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Meadow.Cli
{
    public static class Program
    {

        static void Main(string[] args)
        {
            var ver = RunPwshForOutput("-version");
            TestPwshVersion(ver);

            var startOptions = new List<string>();

            // Does not exit after running startup commands.
            startOptions.Add("-NoExit");

            // Does not load the PowerShell profiles.
            startOptions.Add("-NoProfile");

            // Hides the copyright banner at startup.
            startOptions.Add("-NoLogo");

            // Sets the working directory at the start of PowerShell given a valid PowerShell directory path.
            // startOptions.AddRange("-WorkingDirectory", EscapePath(@"C:\Users\matt\Projects\Meadow\pwsh_testing"));

            // Accepts a base64 encoded .NET Unicode encoded string version of a command.
            // var startCommands = string.Join(" ; ", GetPwshStartCommands());
            // var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(startCommands));
            // startOptions.AddRange("-EncodedCommand", encodedCommand);

            startOptions.AddRange("-File", GetInitScriptFilePath());

            var pwshExitCode = RunPwsh(startOptions.ToArray());

        }

        static void TestPwshVersion(string versionString)
        {
            var verPart = versionString.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
            verPart = verPart.Replace("powershell", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!Version.TryParse(verPart, out var version))
            {
                throw new Exception("Could not parse pwsh version: " + version);
            }

            var minVersion = new Version("6.1.0");
            if (version < minVersion)
            {
                throw new Exception($"The minimumn required pwsh version is {minVersion}. Installed version is: {versionString}");
            }
        }

        static string GetInitScriptFilePath()
        {
            var startCommands = string.Join(Environment.NewLine, GetPwshStartCommands());
            var tempScriptFile = Path.GetTempFileName();
            File.WriteAllText(tempScriptFile, startCommands);
            return tempScriptFile;
        }

        static Process CreatePwshProcess(params string[] args)
        {
            const string PWSH = "pwsh";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PWSH,
                    UseShellExecute = false
                }
            };

            foreach (var arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            return process;
        }

        static int RunPwsh(params string[] pwshArgs)
        {
            using (var process = CreatePwshProcess(pwshArgs))
            {
                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        static string RunPwshForOutput(params string[] pwshArgs)
        {
            using (var process = CreatePwshProcess(pwshArgs))
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                var result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return result;
            }
        }

        static IEnumerable<string> GetPwshStartCommands()
        {
            //var references = SolCodeGen.Compilation.GetReferencedAssembliesManual(typeof(Program).Assembly);
            //foreach (var asmRef in references.Values)
            //{
            //    yield return $"try {{ Add-Type -Path {EscapePath(asmRef.Location)} }} catch {{ }}";
            //}

            var moduleAssemblyPath = typeof(Program).Assembly.Location;
            yield return "Import-Module " + EscapePath(moduleAssemblyPath);

            yield return "Write-Host 'Meadow loaded'";

            string workspaceSetup = "manual";
            if (Environment.GetCommandLineArgs().Any(a => a.Equals("console", StringComparison.OrdinalIgnoreCase)))
            {
                workspaceSetup = "console";
            }
            else if (Environment.GetCommandLineArgs().Any(a => a.Equals("development", StringComparison.OrdinalIgnoreCase)))
            {
                workspaceSetup = "development";
            }

            yield return $"Initialize-Workspace -Setup {workspaceSetup}";

            yield return "$PSDefaultParameterValues['Out-Default:OutVariable'] = '__'";
        }

        static string EscapePath(string path)
        {
            return ArgumentEscaper.EscapeAndConcatenate(new[] { path });
        }

    }
}
