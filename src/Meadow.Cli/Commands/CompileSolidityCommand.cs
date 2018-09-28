using Meadow.Contract;
using Meadow.SolCodeGen;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace Meadow.Cli.Commands
{
    [Cmdlet(ApprovedVerbs.Build, "Solidity")]
    [Alias("compileSol", "Compile-Solidity")]
    public class CompileSolidityCommand : PSCmdlet
    {        
        
        // TODO: This requires the ability to unload/reload an assembly which is not yet avaiable in dotnetcore.
        //       Investigate compiling Contract classes with GUID, then use powershell alias for easy access,
        //       and re-defining aliases during new contract class assembly loading.

        public static void Run(SessionState sessionState)
        {
            var guid = Util.GetUniqueID();
            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), guid)).FullName;
            var sourceOutputDir = Path.Combine(tempDir, "GeneratedOutput");
            var assemblyOutputDir = Path.Combine(tempDir, "GeneratedAssembly");
            var @namespace = "Gen" + guid + ".Contracts";

            var config = Config.Read(sessionState.Path.CurrentLocation.Path);
            string solSourceDir = Util.GetSolSourcePath(config, sessionState);

            Version solcVersion = null;

            if (!string.IsNullOrWhiteSpace(config.SolcVersion) && !config.SolcVersion.Trim().Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    solcVersion = Version.Parse(config.SolcVersion);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Could not parse solc version value '{config.SolcVersion}'");
                    Console.Error.WriteLine(ex);
                    return;
                }
            }

            var solCodeGenResults = CodebaseGenerator.Generate(new CommandArgs
            {
                Generate = GenerateOutputType.Source | GenerateOutputType.Assembly,
                Namespace = @namespace,
                OutputDirectory = sourceOutputDir,
                AssemblyOutputDirectory = assemblyOutputDir,
                SolSourceDirectory = solSourceDir,
                SolcOptimizer = (int)config.SolcOptimizer,
                SolcVersion = solcVersion
            });

            var assemblyBytes = File.ReadAllBytes(solCodeGenResults.CompilationResults.AssemblyFilePath);
            var pdbBytes = File.ReadAllBytes(solCodeGenResults.CompilationResults.PdbFilePath);
            var loadedAssembly = Assembly.Load(assemblyBytes, pdbBytes);
            var referencedAssemblies = Compilation.GetReferencedAssemblies(loadedAssembly)
                .Select(a => a.Value.Location)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

#if CAN_RUN_CMD
            var result = PowerShell.Create(RunspaceMode.CurrentRunspace)
                .AddCommand("Add-Type")
                .AddParameter("-Path", solCodeGenResults.CompilationResults.AssemblyFilePath)
                .AddParameter("-ReferencedAssemblies", referencedAssemblies.ToArray())
                .Invoke();
#endif

            var contractTypes = loadedAssembly.ExportedTypes
                .Where(t => t.BaseType == typeof(BaseContract))
                .ToArray();

            GlobalVariables.SetContractTypes(sessionState, contractTypes);

            // TODO: programmatic generation of format files for Contract and Event types, then load with Update-FormatData

            dynamic contractHolder = new ContractTypeHolder(contractTypes);
            sessionState.PSVariable.Set(new PSVariable(GlobalVariables.VAR_NAMES.CONTRACTS, contractHolder, ScopedItemOptions.AllScope));
        }

        protected override void EndProcessing()
        {
            Run(SessionState);
        }
    }
}
