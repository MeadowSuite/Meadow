using McMaster.Extensions.CommandLineUtils;
using Meadow.Contract;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Meadow.SolCodeGen.Test
{

    public class Integration
    {
        /// <summary>
        /// 1) Programmatically build the Meadow.SolCodeGen nupkg.
        /// 2) Install the package into a test project containing solidity contract source files.
        /// 3) Build the test project (which triggers the contract class code generation).
        /// 4) Load the compile test project assembly and verifies the generated classes.
        /// </summary>
        [Fact]
        public void FullEndToEnd()
        {
            // Find the solution directory (start at this assembly directory and move up).
            string solutionDir = Util.FindSolutionDirectory();

            string testAppDir = Path.Combine(solutionDir, "Meadow.SolCodeGen.TestApp");
            string testProjPath = Path.Combine(testAppDir, "Meadow.SolCodeGen.TestApp.csproj");
            string solCodeGenDir = Path.Combine(solutionDir, "Meadow.SolCodeGen");

            string outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) + Path.DirectorySeparatorChar;

            // Build the Meadow.SolCodeGen project get its generated nupkg file for install.
            string packageDir;
            string packageVer = DateTime.UtcNow.ToString("yyMM.ddHH.mmss", CultureInfo.InvariantCulture);
            void BuildPackage()
            {
                var output = RunDotnet("build", solCodeGenDir, "-c", "Release", "--no-incremental", "/p:PackageVersion=" + packageVer, "-o", outputDir);
                // Successfully created package 'C:\Users\matt\Projects\Meadow.Core\Meadow.SolCodeGen\bin\Debug\Meadow.SolCodeGen.0.2.1.nupkg'.
                var pubLine = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Last(line => line.Contains(".nupkg", StringComparison.OrdinalIgnoreCase) && line.Contains("Meadow.SolCodeGen", StringComparison.OrdinalIgnoreCase) && line.Contains("created package", StringComparison.OrdinalIgnoreCase));
                packageDir = pubLine.Split("created package '")[1].Split("Meadow.SolCodeGen.")[0];
            }

            BuildPackage();

            // Delete any existing generated contracts.
            string generatedContractsDir = Path.Combine(testAppDir, "GeneratedContracts");
            if (Directory.Exists(generatedContractsDir))
            {
                Directory.Delete(generatedContractsDir, recursive: true);
            }

            // Install the Meadow.SolCodeGen nupkg into the test project.
            void InstallPackage()
            {
                RunDotnet("add", testProjPath, "package", "-s", outputDir, /*packageDir,*/ "-v", packageVer, "Meadow.SolCodeGen");
            }

            InstallPackage();

            // Build the test project (which triggers code generation).
            string compiledAssemblyPath;
            void CompileGeneratedCode()
            {
                // First build command trigger the code generation, but the generated source files do not
                // get added the assembly. 
                var output = RunDotnet("build", testProjPath, "-c", "Release", "--no-incremental");

                var lines = output.Split(new char[] { '\n', '\r' });
                var assemblyLine = lines.Last(p => p.Contains("Meadow.SolCodeGen.TestApp -> ", StringComparison.OrdinalIgnoreCase));
                compiledAssemblyPath = assemblyLine.Split("Meadow.SolCodeGen.TestApp -> ")[1];
            }

            CompileGeneratedCode();

            // Load the compiled assembly at runtime and use reflection to test the generated types. 
            void LoadCompiledAssembly()
            {
                var compiledAsm = Assembly.LoadFrom(compiledAssemblyPath);

                var solcData = GeneratedSolcData.Create(compiledAsm).GetSolcData();
                Assert.True(solcData.SolcBytecodeInfo.Length > 0);
                Assert.True(solcData.SolcSourceInfo.Length > 0);

                // var compiledAsm = AssemblyLoadContext.Default.LoadFromAssemblyPath(compiledAssemblyPath);

                var exampleContractType = compiledAsm.GetType("Meadow.SolCodeGen.TestApp.ExampleContract", throwOnError: true);

                Type addressType = GetTypeByName("Meadow.Core", "Meadow.Core.EthTypes.Address");
                Type jsonRpcClientType = GetTypeByName("Meadow.JsonRpc.Client", "Meadow.JsonRpc.Client.JsonRpcClient");
                Type baseContractType = GetTypeByName("Meadow.Contract", "Meadow.Contract.BaseContract");

                object contractAddr = Activator.CreateInstance(addressType, "0x0");
                object fromAccount = Activator.CreateInstance(addressType, "0x0");
                var rpcClient = JsonRpcClient.Create(new Uri("http://localhost"), ArbitraryDefaults.DEFAULT_GAS_LIMIT, ArbitraryDefaults.DEFAULT_GAS_PRICE);

                var atMethod = exampleContractType.GetMethod("At", BindingFlags.Static | BindingFlags.Public);
                dynamic contractAtTask = atMethod.Invoke(null, new object[] { rpcClient, contractAddr, fromAccount });

                object instance = contractAtTask.Result;

                Assert.True(baseContractType.IsAssignableFrom(instance.GetType()));
            }

            LoadCompiledAssembly();

            // Cleanup - remove nuget package
            void UninstallNugetPackage()
            {
                RunDotnet("remove", testProjPath, "package", "Meadow.SolCodeGen");
            }

            UninstallNugetPackage();

            // Cleanup - delete generated contact source files
            if (Directory.Exists(generatedContractsDir))
            {
                Directory.Delete(generatedContractsDir, recursive: true);
            }
        }

        static Type GetTypeByName(string assembly, string fullTypeName)
        {
            return Type.GetType(Assembly.CreateQualifiedName(assembly, fullTypeName), throwOnError: true);
        }

        static string RunDotnet(params string[] args)
        {
            var processArgs = ArgumentEscaper.EscapeAndConcatenate(args);

            var processStartInfo = new ProcessStartInfo(DotNetExe.FullPathOrDefault(), processArgs);
            var runCommand = $"{processStartInfo.FileName} {processStartInfo.Arguments}";
            Console.WriteLine($"Running: {runCommand}");
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            using (var process = Process.Start(processStartInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new Exception($"Error running: {runCommand}{Environment.NewLine}{error}");
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Bad exit code '{process.ExitCode}' when running: {runCommand}{Environment.NewLine}{output}");
                }

                return output;
            }
        }
    }
}
