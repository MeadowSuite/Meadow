using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Meadow.MSTest.Runner
{
    public class Program
    {
        static void Main(string[] args)
        {

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                //System.Diagnostics.Debugger.Launch();
            }

#endif

            var cmdApp = new CommandLineApplication();
            var testAssemblyFileOptions = cmdApp.Option<string>("--test_assembly_file", "File to log message to.", CommandOptionType.MultipleValue);
            cmdApp.Execute(args);

            string[] assemblies;

            if (testAssemblyFileOptions.HasValue())
            {
                assemblies = testAssemblyFileOptions.Values.ToArray();
            }
            else
            {
                var dirs = new[]
                {
                     Directory.GetCurrentDirectory(),
                     Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]),
                };

                IEnumerable<string[]> GetFiles()
                {
                    foreach (var dir in dirs)
                    {
                        foreach (var ext in new[] { "*.dll", "*.exe" })
                        {
                            yield return Directory.GetFiles(dir, ext);
                        }
                    }
                }

                assemblies = GetFiles().SelectMany(p => p).Distinct().ToArray();
            }

            Console.WriteLine("Running tests on: " + string.Join(";", assemblies));
            var testRunner = ApplicationTestRunner.CreateFromAssemblies(assemblies);
            testRunner.RunTests();
        }
    }
}
