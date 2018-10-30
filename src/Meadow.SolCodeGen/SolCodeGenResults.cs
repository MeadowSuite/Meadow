using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Meadow.SolCodeGen
{
    public class SolCodeGenCSharpResult
    {
        public SyntaxTree SyntaxTree { get; set; }
        public string CSharpLiteralCode { get; set; }
        public string CSharpFilePath { get; set; }

        public SolCodeGenCSharpResult(string filePath, string literalCode, SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree.WithFilePath(filePath);
            CSharpLiteralCode = literalCode;
            CSharpFilePath = filePath;
        }
    }

    public class SolCodeGenCompilationResults
    {
        public string AssemblyFilePath { get; set; }
        public string PdbFilePath { get; set; }
        public string XmlDocFilePath { get; set; }
    }

    public class SolCodeGenResults
    {
        public List<SolCodeGenCSharpResult> GeneratedCSharpEntries { get; set; } = new List<SolCodeGenCSharpResult>();

        public string GeneratedResxFilePath { get; set; }
        public IReadOnlyDictionary<string, string> GeneratedResxResources { get; set; }

        public string SolcCodeBaseHash { get; set; }

        public SolCodeGenCompilationResults CompilationResults { get; set; }
    }
}
