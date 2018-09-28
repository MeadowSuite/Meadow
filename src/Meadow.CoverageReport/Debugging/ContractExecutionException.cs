using System;

namespace Meadow.CoverageReport.Debugging
{
    public class ContractExecutionException : Exception
    {
        public ContractExecutionException(string message, Exception inner) : base(message, inner)
        {

        }

        public ContractExecutionException(string message) : base(message)
        {

        }
    }
}

