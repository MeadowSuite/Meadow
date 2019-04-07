using SolcNet.DataDescription.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolcNet.CompileErrors
{
    public class CompilerException : Exception
    {
        public Error CompileError { get; }

        public Severity Severity => CompileError.Severity;

        public CompilerException(Error compileError) : base(compileError.FormattedMessage)
        {
            CompileError = compileError;
        }

        public static Exception GetCompilerExceptions(Error[] errors, CompileErrorHandling errorHandling)
        {
            if (errorHandling == CompileErrorHandling.ThrowOnError)
            {
                var exs = errors.Where(e => e.Severity == Severity.Error).Select(e => new CompilerException(e)).ToArray();
                if (exs.Length == 1)
                {
                    return exs[0];
                }
                else if (exs.Length > 1)
                {
                    return new AggregateException($"{exs.Length} compiler errors", exs);
                }
            }
            if (errorHandling == CompileErrorHandling.ThrowOnWarning)
            {
                var exs = errors.Where(e => e.Severity == Severity.Warning).Select(e => new CompilerException(e)).ToArray();
                if (exs.Length == 1)
                {
                    return exs[0];
                }
                else if (exs.Length > 1)
                {
                    return new AggregateException($"{exs.Length} compiler warnings", exs);
                }
            }

            return null;
        }
    }
}
