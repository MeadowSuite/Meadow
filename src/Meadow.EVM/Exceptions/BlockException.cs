using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Exceptions
{
    /// <summary>
    /// An exception thrown to signal that our block encountered an error.
    /// </summary>
    public class BlockException : Exception
    {
        public BlockException() { }
        public BlockException(string message) : base(message) { }
        public BlockException(string message, Exception innerException) : base(message, innerException) { }
        public BlockException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
