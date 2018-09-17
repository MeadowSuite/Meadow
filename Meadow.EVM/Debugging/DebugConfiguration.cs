using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Debugging.Tracing;
using Meadow.EVM.EVM.Execution;
using Meadow.EVM.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Debugging
{
    /// <summary>
    /// Provides debug execution state information and configuration.
    /// </summary>
    public class DebugConfiguration
    {
        #region Properties
        public Exception Error { get; private set; }
        public ExecutionTrace ExecutionTrace { get; private set; }
        public bool IsTracing { get; set; }
        public bool ThrowExceptionOnFailResult { get; set; }
        public bool IsContractSizeCheckDisabled { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Default contructor, initializes properties, etc. accordingly.
        /// </summary>
        public DebugConfiguration()
        {
            // Initialize our execution trace
            ExecutionTrace = new ExecutionTrace();

            // Set our default tracing
            IsTracing = false;

            // Set our default for exception throwing on failed results.
            ThrowExceptionOnFailResult = true;
        }
        #endregion

        #region Functions
        // Tracing
        public void RecordExecutionStart(MeadowEVM evm)
        {
            // If we're not tracing, clear our execution trace.
            if (!IsTracing)
            {
                ExecutionTrace = null;
            }

            // If our depth is 0, we should start a new trace map and clear errors.
            if (evm.Message.Depth == 0)
            {
                // Start a new trace map if we're tracing.
                if (IsTracing)
                {
                    ExecutionTrace = new ExecutionTrace();
                }

                // Clear our error.
                Error = null;
            }
        }

        public void RecordExecutionEnd(MeadowEVM evm)
        {
            // We only keep errors that occurred at the highest level in case an exception occurred in a call, and we accounted for this in our calling contract.
            if (evm.Message.Depth != 0)
            {
                Error = null;
            }
        }

        // Exceptions
        public void RecordException(Exception exception, bool isContractExecuting, bool onlyIfNoPreviousErrors = false)
        {
            // Use optional tracing information to return with the error for callstack/execution information.
            if (IsTracing && !onlyIfNoPreviousErrors)
            {
                ExecutionTrace?.RecordException(exception, isContractExecuting);
            }

            // If we already have an exception, stop
            if (Error != null)
            {
                return;
            }

            // Set our error.
            Error = exception;
        }
        #endregion
    }
}
