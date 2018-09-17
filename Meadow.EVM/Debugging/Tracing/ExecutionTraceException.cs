using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Debugging.Tracing
{
    public class ExecutionTraceException
    {
        #region Properties
        /// <summary>
        /// The <see cref="ExecutionTracePoint"/> index in our <see cref="ExecutionTrace"/> which indicates the point in execution where the <see cref="Exception"/> occurred.
        /// </summary>
        public int? TraceIndex { get; }
        /// <summary>
        /// The exception that occurred during execution.
        /// </summary>
        public Exception Exception { get; }
        #endregion

        #region Constructors
        public ExecutionTraceException(int? traceIndex, Exception exception)
        {
            // Set our properties
            TraceIndex = traceIndex;
            Exception = exception;
        }
        #endregion
    }
}
