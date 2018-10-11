using Meadow.CoverageReport.Debugging;
using Meadow.JsonRpc.Client;
using Meadow.JsonRpc.Types.Debugging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Meadow.DebugAdapterServer
{
    /// <summary>
    /// Represents the state/properties of a thread which is debugging solidity code.
    /// </summary>
    public class MeadowDebugAdapterThreadState
    {
        #region Fields
        /// <summary>
        /// Indiciates the index into the significant step list <see cref="ExecutionTraceAnalysis.SignificantStepIndices"/>.
        /// This is used to walk through all significant steps.
        /// </summary>
        private int _significantStepIndexIndex;
        #endregion

        #region Properties
        /// <summary>
        /// The thread id of this thread state.
        /// </summary>
        public int ThreadId { get; }
        /// <summary>
        /// Indicates the current tracepoint index into the execution trace <see cref="ExecutionTrace"/>.
        /// This is used to
        /// </summary>
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

        /// <summary>
        /// The current execution trace which is being analyzed on this thread.
        /// </summary>
        public ExecutionTrace ExecutionTrace
        {
            get
            {
                return ExecutionTraceAnalysis.ExecutionTrace;
            }
        }

        /// <summary>
        /// The execution trace analysis currently being analyzed/walked on this thread.
        /// </summary>
        public ExecutionTraceAnalysis ExecutionTraceAnalysis { get; }

        /// <summary>
        /// The RPC client which entered the debugging session on this thread.
        /// </summary>
        public IJsonRpcClient RpcClient { get; }

        /// <summary>
        /// Indicates whether we are expecting exceptions during this execution trace's analysis.
        /// Used to determine if we have unhandled exceptions we should pause for.
        /// </summary>
        public bool ExpectingException { get; }

        /// <summary>
        /// The thread locking object used to pause the test executing thread while debugging is occuring on it's execution trace.
        /// </summary>
        public SemaphoreSlim Semaphore { get; }
        #endregion

        #region Constructors
        public MeadowDebugAdapterThreadState(IJsonRpcClient rpcClient, ExecutionTraceAnalysis traceAnalysis, int threadId, bool expectingException)
        {
            // Initialize our thread locking
            Semaphore = new SemaphoreSlim(0, int.MaxValue);

            // Set our rpc client
            RpcClient = rpcClient;

            // Set our execution trace analysis
            ExecutionTraceAnalysis = traceAnalysis;

            // Set our initial step index index
            _significantStepIndexIndex = 0;

            // Set our thread id
            ThreadId = threadId;

            ExpectingException = expectingException;
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
