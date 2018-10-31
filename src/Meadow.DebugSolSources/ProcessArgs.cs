using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.DebugSolSources
{
    class ProcessArgs
    {
        [Option("-d|--directory", "Directory of the .sol source files.", CommandOptionType.SingleValue)]
        public string Directory { get; }

        [Option("-e|--entry", "The contract entry point in the form of 'ContractName.FunctionName'", CommandOptionType.SingleValue)]
        public string Entry { get; }

        [Option("-f|--singleFile", "A single solidity file to debug.", CommandOptionType.SingleValue)]
        public string SingleFile { get; }

        public static ProcessArgs Parse(string[] args)
        {
            var app = new CommandLineApplication<ProcessArgs>(throwOnUnexpectedArg: true);
            app.Conventions.UseDefaultConventions();
            app.Parse(args);
            return app.Model;
        }
    }
}
