using Meadow.Contract;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Meadow.SolCodeGen
{
    public class Compilation
    {
        //public const string GENERATED_ASM_NAME = "GeneratedSol";

        readonly SolCodeGenResults _codeGenResults;
        readonly string _outputDirectory;

        readonly string _namespace;
        readonly string _generatedAssemblyFile;
        readonly string _generatedPdbFile;
        readonly string _generatedXmlDocFile;

        public Compilation(SolCodeGenResults codeGenResults, string @namespace, string outputDirectory)
        {
            _codeGenResults = codeGenResults;
            _outputDirectory = outputDirectory;
            _namespace = @namespace;

            _generatedAssemblyFile = Path.Combine(_outputDirectory, _namespace + ".dll");
            _generatedPdbFile = Path.Combine(_outputDirectory, _namespace + ".pdb");
            _generatedXmlDocFile = Path.Combine(_outputDirectory, _namespace + ".xml");
        }

        public void Compile()
        {
            CleanOutputDirectory();

            // Convert the solc data into compiled resx form.
            var generatedResxResourceDescription = CreateSolcResxResource(_namespace);
            var manifestResources = new[] { generatedResxResourceDescription };

            // Create compilation options (output library, debug mode, any CPU).
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Warn)
                .WithReportSuppressedDiagnostics(true)
                .WithPlatform(Platform.AnyCpu)
                .WithOptimizationLevel(OptimizationLevel.Debug);

            // Add runtime, Meadow, and other dependency assembly references.
            var metadataReferences = GetReferencedAssemblies(typeof(BaseContract).Assembly)
                .Select(a => MetadataReference.CreateFromFile(a.Value.Location)).ToArray();

            // Create compilation context with the code syntax trees and resx resources.
            var compileContext = CSharpCompilation.Create(
                assemblyName: _namespace,
                syntaxTrees: _codeGenResults.GeneratedCSharpEntries.Select(g => g.SyntaxTree),
                references: metadataReferences,
                options: compilationOptions);

            EmitResult emitResult;

            using (var asmFileStream = new FileStream(_generatedAssemblyFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var pdbFileStream = new FileStream(_generatedPdbFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var xmlDocFileStream = new FileStream(_generatedXmlDocFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);

                // Compile sources into assembly, pdb and xmldoc.
                emitResult = compileContext.Emit(
                    options: emitOptions,
                    peStream: asmFileStream,
                    pdbStream: pdbFileStream,
                    xmlDocumentationStream: xmlDocFileStream,
                    manifestResources: manifestResources);
            }

            CheckEmitResult(emitResult);

            _codeGenResults.CompilationResults = new SolCodeGenCompilationResults
            {
                AssemblyFilePath = _generatedAssemblyFile,
                PdbFilePath = _generatedPdbFile,
                XmlDocFilePath = _generatedXmlDocFile
            };

        }

        // Deletes all the files (recursive) in the provided directory. Creates the directory if does not exist.
        void CleanOutputDirectory()
        {
            if (Directory.Exists(_outputDirectory))
            {
                var existingFiles = Directory.GetFiles(_outputDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var file in existingFiles)
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        // Convert the solc data into compiled resx form.
        ResourceDescription CreateSolcResxResource(string generatedAsmName)
        {
            Stream CreateResxDataStream()
            {
                var backingStream = new MemoryStream();
                using (var resourceWriter = new ResourceWriter(backingStream))
                {
                    foreach (var resxEntry in _codeGenResults.GeneratedResxResources)
                    {
                        resourceWriter.AddResource(resxEntry.Key, resxEntry.Value);
                    }

                    resourceWriter.Generate();
                    return new MemoryStream(backingStream.GetBuffer(), 0, (int)backingStream.Length);
                }
            }

            var resxName = $"{generatedAsmName}.{CodebaseGenerator.SolcOutputDataResxFile}.sol.resources";

            var generatedResxResourceDescription = new ResourceDescription(
                resourceName: resxName,
                dataProvider: CreateResxDataStream,
                isPublic: true);

            return generatedResxResourceDescription;
        }

        // Throws exception if emit failed.
        void CheckEmitResult(EmitResult emitResult)
        {
            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                var exceptions = errors.Select(s => new Exception(s.GetMessage(CultureInfo.InvariantCulture))).ToArray();
                if (exceptions.Length > 1)
                {
                    throw new AggregateException("Errors compiling generated code", exceptions);
                }
                else
                {
                    throw exceptions[0];
                }
            }
        }

        public static Dictionary<string, Assembly> GetReferencedAssemblies(Assembly assembly)
        {
            var context = DependencyContext.Load(assembly);
            if (context == null)
            {
                return GetReferencedAssembliesManual(assembly);
            }

            var items = context.GetDefaultAssemblyNames();
            return items.ToDictionary(t => t.FullName, t => Assembly.Load(t));
        }

        // Recursively gets the assembly dependencies of a given assembly.
        public static Dictionary<string, Assembly> GetReferencedAssembliesManual(Assembly assembly)
        {
            void Iterate(Assembly asm, Dictionary<string, Assembly> assemblies)
            {
                if (!assemblies.ContainsKey(asm.FullName))
                {
                    assemblies.Add(asm.FullName, asm);
                    var referenced = asm.GetReferencedAssemblies();
                    foreach (var child in referenced)
                    {
                        Assembly childAsm;
                        try
                        {
                            childAsm = Assembly.Load(child);
                        }
                        catch
                        {
                            var simpleAssemblyName = new AssemblyName { Name = child.Name };
                            childAsm = Assembly.Load(simpleAssemblyName);
                        }

                        Iterate(childAsm, assemblies);
                    }
                }
            }

            var dict = new Dictionary<string, Assembly>();
            Iterate(assembly, dict);
            return dict;
        }
    }
}
