using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Exceptions
{
    /// <summary>
    /// A basic exception type used by our EVM to signal a failure in the virtual execution.
    /// This is caught and converted to a failed transaction/call result.
    /// </summary>
    public class EVMException : Exception
    {
        public EVMException() { }
        public EVMException(string message) : base(message) { }
        public EVMException(string message, Exception innerException) : base(message, innerException) { }
        public EVMException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
