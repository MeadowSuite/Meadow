using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Debugging.Variables
{
    public class VarResolvingException : Exception
    {
        public VarResolvingException(string message, Exception inner) : base(message, inner)
        {

        }

        public VarResolvingException(string message) : base(message)
        {

        }
    }
}
