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

        public static int Main(params string[] args)
        {
            if (!CommandArgs.TryParse(args, out var appArgs, out var parseException))
            {
                Console.Error.WriteLine(parseException);
                return 1;
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

                IEnumerable<Exception> compilerExceptions = null;
                if (ex is AggregateException aggr && aggr.InnerException is CompilerException)
                {
                    compilerExceptions = aggr.InnerExceptions;
                }
                else if (ex is CompilerException solcEx)
                {
                    compilerExceptions = new[] { ex };
                }

                if (compilerExceptions != null)
                {
                    foreach (var solcEx in compilerExceptions)
                    {
                        var errMsg = string.Join(" ", solcEx.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !s.StartsWith("^--", StringComparison.Ordinal)));
                        Console.Error.WriteLine("Solidity compiler error: " + errMsg + " (see full build output for more details)");
                    }
                }
                else
                {
                    var exStackTrace = string.Join(" ", ex.StackTrace.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
                    Console.Error.WriteLine(ex.Message + " | StackTrace: " + exStackTrace);
                }

                return 1;
            }

            return 0;
        }

        public static SolCodeGenResults Run(string[] args)
        {
            if (!CommandArgs.TryParse(args, out var appArgs, out var parseException))
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
