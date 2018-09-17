using Meadow.Core.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Meadow.SolCodeGen.CodeGenerators
{
    abstract class GeneratorBase
    {
        protected readonly string _namespace;

        public GeneratorBase(string @namespace)
        {
            _namespace = @namespace;
        }

        public (string SourceString, SyntaxTree SyntaxTree) GenerateSourceCode()
        {
            var code = GenerateNamespaceContainer();
            var formatted = FormatCode(code);
            return formatted;
        }

        protected virtual CSharpParseOptions GetCSharpParseOptions()
        {
            var opts = new CSharpParseOptions(LanguageVersion.Latest)
                .CommonWithKind(SourceCodeKind.Regular)
                .WithKind(SourceCodeKind.Regular)
                .WithDocumentationMode(DocumentationMode.Parse);
            return (CSharpParseOptions)opts;
        }

        protected (string SourceString, SyntaxTree SyntaxTree) FormatCode(string csCode)
        {
            var sourceText = SourceText.From(csCode, StringUtil.UTF8);
            var tree = CSharpSyntaxTree.ParseText(sourceText, GetCSharpParseOptions());
            var unitSyntax = tree.GetCompilationUnitRoot().NormalizeWhitespace(eol: "\r\n");
            var sourceString = unitSyntax.ToFullString();
            return (sourceString, tree);
        }


        protected string GenerateNamespaceContainer()
        {
            return $@"

// NOTICE: Do not change this file. This file is auto-generated and any changes will be reset.

// Generated date: {DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)} (UTC)

#pragma warning disable SA1000 // The keyword 'new' should be followed by a space
#pragma warning disable SA1003 // Symbols should be spaced correctly
#pragma warning disable SA1008 // Opening parenthesis should be preceded by a space
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1012 // Opening brace should be preceded by a space
#pragma warning disable SA1013 // Closing brace should be preceded by a space
#pragma warning disable SA1024 // Colons Should Be Spaced Correctly
#pragma warning disable SA1128 // Put constructor initializers on their own line
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable IDE1006 // Naming Styles

                {GenerateUsingDeclarations()}

                namespace {_namespace}
                {{
                    {GenerateClassDef()}
                }}
            ";
        }

        protected abstract string GenerateUsingDeclarations();

        protected abstract string GenerateClassDef();
    }
}
