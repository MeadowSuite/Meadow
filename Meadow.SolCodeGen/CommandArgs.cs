using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;

namespace Meadow.SolCodeGen
{
    [Flags]
    public enum GenerateOutputType
    {
        Source = 1 << 0,
        Assembly = 1 << 1
    }

    public class CommandArgs
    {

        public string SolSourceDirectory { get; set; }

        public string Namespace { get; set; }

        public GenerateOutputType Generate { get; set; }

        public string LegacySolcPath { get; set; }

        public Version SolcVersion { get; set; }

        public int SolcOptimizer { get; set; }

        public string OutputDirectory { get; set; }

        public string AssemblyOutputDirectory { get; set; }


        public static bool TryParse(string[] args, out CommandArgs argResult, out Exception parseException)
        {
            var result = new CommandArgs();

            var app = new CommandLineApplication();
            app.HelpOption();
            app.ThrowOnUnexpectedArgument = true;

            var sourceOption = app
                .Option("-s|--source <SOURCE>", "Path to the directory containing solidity source files", CommandOptionType.SingleValue)
                .IsRequired()
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    opt = Path.GetFullPath(opt);
                    if (!Directory.Exists(opt))
                    {
                        return new ValidationResult("Solidity source file directory does not exist: " + opt);
                    }

                    var containsSolFiles = Directory.EnumerateFiles(opt, "*.sol", SearchOption.AllDirectories).Any();
                    if (!containsSolFiles)
                    {
                        return new ValidationResult("Solidity source file directory does not contained any .sol files: " + opt);
                    }

                    result.SolSourceDirectory = opt;
                    Console.WriteLine("Solidity source directory specified: " + opt);
                    return ValidationResult.Success;
                });

            var namespaceOption = app
                .Option("-n|--namespace", "The namespace for the generated code", CommandOptionType.SingleValue)
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    if (!SyntaxFacts.IsValidIdentifier(opt.Replace('.', '_')))
                    {
                        return new ValidationResult($"The specified namespace '{opt}' is not valid intenfier syntax");
                    }

                    result.Namespace = opt;
                    return ValidationResult.Success;
                });


            var generateOption = app
                .Option<GenerateOutputType>("-g|--generate", "Generation output type, either 'source' or 'assembly'", CommandOptionType.MultipleValue)
                .IsRequired()
                .OnValidate(ctc =>
                {
                    var opts = (ctc.ObjectInstance as CommandOption<GenerateOutputType>).ParsedValues;
                    result.Generate = opts.Aggregate(default(GenerateOutputType), (a, b) => a | b);
                    return ValidationResult.Success;
                })
                .Accepts().Values("source", "assembly");

            var legacySolcOption = app
                .Option("--legacysolc", "Path to .NET legacy solc native resolver lib, required if solcversion options is set", CommandOptionType.SingleValue)
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    if (!string.IsNullOrWhiteSpace(opt))
                    {
                        opt = Path.GetFullPath(opt);
                        if (!File.Exists(opt))
                        {
                            return new ValidationResult($"Legacy solc lib path is set but file does not exist: {opt}");
                        }

                        result.LegacySolcPath = opt;
                        Console.WriteLine($"Legacy solc lib set: {opt}");
                    }

                    return ValidationResult.Success;
                });

            var solcVersionOption = app
                .Option("--solcversion", "Version of the libsolc compiler to use, requires the legacysolc option to be set", CommandOptionType.SingleValue)
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    if (!string.IsNullOrWhiteSpace(opt))
                    {
                        if (!Version.TryParse(opt, out var solcVersionParsed))
                        {
                            return new ValidationResult($"Could not parse specified solc version: {opt}");
                        }

                        result.SolcVersion = solcVersionParsed;
                        Console.WriteLine($"Solc version specified: {solcVersionParsed}");
                    }

                    return ValidationResult.Success;
                });

            var solcOptimizerOption = app
                .Option("--solcoptimizer", "Enables the solc optimizer with the given run number", CommandOptionType.SingleValue)
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    if (!string.IsNullOrWhiteSpace(opt))
                    {
                        if (!int.TryParse(opt, out var solcOptimizerRuns))
                        {
                            return new ValidationResult($"Could not parse specified solc optimizer run setting: '{opt}'. Should be an integer of 1 or high, or set to 0 or empty string for disabling optimizer.");
                        }

                        if (solcOptimizerRuns > 0)
                        {
                            result.SolcOptimizer = solcOptimizerRuns;
                            Console.WriteLine($"Solc optimizer enabled with run count: {solcOptimizerRuns}");
                        }
                    }

                    return ValidationResult.Success;
                });

            var outputDirectoryOption = app
                .Option("-o|--output", "Generated output directory", CommandOptionType.SingleValue)
                .OnValidate(ctc =>
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    result.OutputDirectory = opt;
                    return ValidationResult.Success;
                });

            var assemblyOutputDirectoryOption = app
                .Option("--assembly-output", "Compilation output directory", CommandOptionType.SingleValue)
                .OnValidate(ctc => 
                {
                    var opt = (ctc.ObjectInstance as CommandOption).Value();
                    result.AssemblyOutputDirectory = opt;
                    return ValidationResult.Success;
                });
                

            ValidationResult validationError = null;
            app.OnValidationError(v =>
            {
                validationError = v;
            });

            bool successfulExecute = false;
            app.OnExecute(() =>
            {
                successfulExecute = true;
            });

            try
            {
                app.Execute(args);
                if (!successfulExecute || validationError != null)
                {
                    throw new Exception(validationError.ErrorMessage);
                }

                parseException = null;
                argResult = result;
                return true;
            }
            catch (Exception ex)
            {
                parseException = ex;
                argResult = null;
                return false;
            }

        }
    }
}
