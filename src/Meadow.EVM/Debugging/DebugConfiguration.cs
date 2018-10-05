using Meadow.Core.Utils;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.Databases;
using Meadow.EVM.Data_Types.Trees;
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
        #region Constants
        private const string PREFIX_PREIMAGE = "pi";
        #endregion

        #region Properties
        public BaseDB Database { get; }
        public Exception Error { get; private set; }
        public ExecutionTrace ExecutionTrace { get; private set; }
        public bool IsTracing { get; set; }
        public bool IsTracingPreimages { get; set; }
        public bool ThrowExceptionOnFailResult { get; set; }
        public bool IsContractSizeCheckDisabled { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Default contructor, initializes properties, etc. accordingly.
        /// </summary>
        public DebugConfiguration(BaseDB database = null)
        {
            // Set/initialize our database.
            Database = database ?? new BaseDB();

            // Initialize our execution trace
            ExecutionTrace = new ExecutionTrace();

            // Set our default tracing
            IsTracing = false;

            // Set our default for preimage tracing
            IsTracingPreimages = true;

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

        // Pre-images
        public bool TryGetPreimage(byte[] hash, out byte[] preimage)
        {
            // Check our database for our preimage
            return Database.TryGet(PREFIX_PREIMAGE, hash, out preimage);
        }

        public void RecordPreimage(byte[] hash, byte[] preimage)
        {
            // If we're not tracing, do not record preimages
            if (!IsTracing || !IsTracingPreimages)
            {
                return;
            }

            // Set the preimage in our database.
            Database.Set(PREFIX_PREIMAGE, hash, preimage);
        }
        #endregion
    }
}
