using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Exceptions
{
    /// <summary>
    /// An exception thrown to signal that our transaction was invalid.
    /// </summary>
    public class TransactionException : Exception
    {
        public TransactionException() { }
        public TransactionException(string message) : base(message) { }
        public TransactionException(string message, Exception innerException) : base(message, innerException) { }
        public TransactionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
