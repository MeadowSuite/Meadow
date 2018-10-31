using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolcNet;
using SolcNet.DataDescription.Input;
using SolcNet.DataDescription.Output;
using Meadow.SolCodeGen.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Meadow.Core.Utils;
using SolcNet.NativeLib;
using SolcNet.CompileErrors;

namespace Meadow.SolCodeGen
{
    public class Program
    {
        // MSBuild error/warning stderr formatting
        // https://blogs.msdn.microsoft.com/msbuild/2006/11/02/msbuild-visual-studio-aware-error-messages-and-message-formats/
        // https://stackoverflow.com/a/48117947/794962
        // Main.cs(17,20):Command line warning CS0168: The variable 'foo' is declared but never used
        // -------------- ------------ ------- ------  ----------------------------------------------
        // Origin         SubCategory  Cat.    Code    Text


        public static int Main(params string[] args)
        {
            CommandArgs appArgs = null;
            Exception parseException = null;

            if (!CommandArgs.TryParse(args, Console.WriteLine, out appArgs, out parseException))
            {
                if (parseException.Data.Contains(CommandArgs.MISSING_SOL_FILES))
                {
                    Console.Error.WriteLine("Solidity compiler warning MEADOW1008: No .sol files found in the 'contracts' solidity source directory.");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine($"SolCodeGen:Command arguments error MEADOW1009: {parseException.Message} (see full build output for more details)");
                    return 0;
                }
            }

            try
            {
                var sw = new Stopwatch();
                sw.Start();
                CodebaseGenerator.Generate(appArgs);
                sw.Stop();
                Console.WriteLine($"Solidity analysis and code generation process took: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                IEnumerable<CompilerException> compilerExceptions = null;
                if (ex is AggregateException aggr && aggr.InnerException is CompilerException)
                {
                    compilerExceptions = aggr.InnerExceptions.OfType<CompilerException>();
                }
                else if (ex is CompilerException solcEx)
                {
                    compilerExceptions = new[] { solcEx };
                }

                if (compilerExceptions != null && compilerExceptions.Count() > 0)
                {
                    foreach (var solcEx in compilerExceptions)
                    {
                        var err = solcEx.CompileError;

                        var singleLineMsg = string
                            .Join(" ", err.FormattedMessage.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !s.StartsWith("^--", StringComparison.Ordinal)))
                             + " (see full build output for more details)";

                        string origin;

                        if (!string.IsNullOrEmpty(err?.SourceLocation?.File))
                        {
                            origin = $"contracts/{err.SourceLocation.File}({err.SourceLocation.Start},{err.SourceLocation.End})";
                        }
                        else
                        {
                            origin = "Solc";
                        }

                        var msbuildError = $"{origin}:{err.Type} error SOLC1001: {singleLineMsg}";
                        Console.Error.WriteLine(msbuildError);
                    }
                }
                else
                {
                    var exStackTrace = string.Join(" ", ex.StackTrace.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
                    var msg = ex.Message + " | StackTrace: " + exStackTrace;
                    Console.Error.WriteLine($"SolCodeGen:Exception error MEADOW1010: {msg}");
                }
            }

            return 0;
        }

        public static SolCodeGenResults Run(string[] args)
        {
            if (!CommandArgs.TryParse(args, Console.WriteLine, out var appArgs, out var parseException))
            {
                throw parseException;
            }

            return CodebaseGenerator.Generate(appArgs);
        }

        public static SolCodeGenResults Run(CommandArgs args)
        {
            return CodebaseGenerator.Generate(args, returnFullSources: true);
        }



    }
}
