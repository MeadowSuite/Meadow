using Meadow.CoverageReport.Debugging;
using Meadow.JsonRpc.Types.Debugging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Meadow.DebugAdapterServer
{
    public class MeadowDebugAdapterThreadState
    {
        #region Fields
        private int _significantStepIndexIndex;
        #endregion

        #region Properties
        public int ThreadId { get; }
        public int? CurrentStepIndex
        {
            get
            {
                // Verify we have a index into our significant step indices.
                if (_significantStepIndexIndex < 0 || _significantStepIndexIndex >= ExecutionTraceAnalysis.SignificantStepIndices.Count)
                {
                    return null;
                }

                // Return the significant step index at this position.
                return ExecutionTraceAnalysis.SignificantStepIndices[_significantStepIndexIndex];
            }
        }

        public ExecutionTrace ExecutionTrace
        {
            get
            {
                return ExecutionTraceAnalysis.ExecutionTrace;
            }
        }

        public ExecutionTraceAnalysis ExecutionTraceAnalysis { get; }

        public Semaphore Semaphore { get; }
        #endregion

        #region Constructors
        public MeadowDebugAdapterThreadState(ExecutionTraceAnalysis traceAnalysis, int threadId)
        {
            // Initialize our thread locking
            Semaphore = new Semaphore(0, 1);

            // Set our execution trace analysis
            ExecutionTraceAnalysis = traceAnalysis;

            // Set our initial step index index
            _significantStepIndexIndex = 0;

            // Set our thread id
            ThreadId = threadId;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Decrements the current step to the previous significant step.
        /// </summary>
        /// <returns>Returns true if the step was decremented. False if we have reached the start of the trace and could not decrement step.</returns>
        public bool DecrementStep()
        {
            // If we can decrement further, do so.
            if (_significantStepIndexIndex > 0)
            {
                _significantStepIndexIndex--;
                return true;
            }

            // We could not decrement
            return false;
        }

        /// <summary>
        /// Increments the current step to the next significant step.
        /// </summary>
        /// <returns>Returns true if the step was incremented. False if we have reached the end of the trace and could not increment step.</returns>
        public bool IncrementStep()
        {
            // If we can increment further, do so.
            if (_significantStepIndexIndex + 1 < ExecutionTraceAnalysis.SignificantStepIndices.Count)
            {
                _significantStepIndexIndex++;
                return true;
            }

            // We could not increment further.
            return false;
        }
        #endregion
    }
}
