using Meadow.CoverageReport.Debugging;
using Meadow.CoverageReport.Debugging.Variables;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Meadow.DebugAdapterServer
{
    public class ReferenceContainer
    {
        #region Constants
        private const int STACKFRAME_ID_RESERVED_COUNT = 0x10000;
        private const string VARIABLE_EVAL_TYPE_PREFIX = "vareval_";
        #endregion

        #region Fields
        private static int _nextId;

        // (callstack)
        private List<int> _currentStackFrameIds;

        // stackFrameId -> stackFrame (actual)
        private ConcurrentDictionary<int, (StackFrame stackFrame, int traceIndex)> _stackFrames;

        // variableReferenceId -> sub-variableReferenceIds
        private ConcurrentDictionary<int, List<int>> _variableReferenceIdToSubVariableReferenceIds;
        // reverse: sub-variableReferenceIds -> variableReferenceId
        private ConcurrentDictionary<int, int> _subVariableReferenceIdToVariableReferenceId;
        // variableReferenceId -> (threadId, variableValuePair)
        private ConcurrentDictionary<int, (int threadId, UnderlyingVariableValuePair underlyingVariableValuePair)> _variableReferenceIdToUnderlyingVariableValuePair;

        private ConcurrentDictionary<int, string> _variableEvaluationValues;

        private int _startingStackFrameId;
        private int _variableEvaluationId;
        #endregion

        #region Properties
        /// <summary>
        /// Indicates we are currently debugging/stepping through an execution trace, and there is a thread currently connected.
        /// </summary>
        public bool IsThreadLinked { get; private set; }
        /// <summary>
        /// Indicates the identifier of the current thread.
        /// </summary>
        public int CurrentThreadId { get; private set; }
        /// <summary>
        /// Indicates the identifier for the current stack frame being analyzed.
        /// </summary>
        public int CurrentStackFrameId { get; private set; }
        /// <summary>
        /// Indicates the identifier for the current stack frame's local scope.
        /// </summary>
        public int LocalScopeId { get; private set; }
        /// <summary>
        /// Indicates the identifier for the current stack frame's state scope.
        /// </summary>
        public int StateScopeId { get; private set; }
        #endregion

        #region Constructor
        public ReferenceContainer()
        {
            // Initialize our lookups.
            _currentStackFrameIds = new List<int>();
            _stackFrames = new ConcurrentDictionary<int, (StackFrame stackFrame, int traceIndex)>();
            _variableEvaluationValues = new ConcurrentDictionary<int, string>();

            LocalScopeId = GetUniqueId();
            StateScopeId = GetUniqueId();

            // Allocate our desired amount of callstack ids
            _startingStackFrameId = _nextId;
            Interlocked.Add(ref _nextId, STACKFRAME_ID_RESERVED_COUNT);

            _variableReferenceIdToSubVariableReferenceIds = new ConcurrentDictionary<int, List<int>>();
            _subVariableReferenceIdToVariableReferenceId = new ConcurrentDictionary<int, int>();
            _variableReferenceIdToUnderlyingVariableValuePair = new ConcurrentDictionary<int, (int threadId, UnderlyingVariableValuePair variableValuePair)>();
        }
        #endregion

        #region Functions
        /// <summary>
        /// Obtains a unique ID for a debug adapter component (stack frames, scopes, etc).
        /// Used to ensure there are no collisions in chosen IDs.
        /// </summary>
        /// <returns>Returns a unique ID to be used for a debug adapter component.</returns>
        public int GetUniqueId()
        {
            return Interlocked.Increment(ref _nextId);
        }

        /// <summary>
        /// Obtains the ID for a stack frame, given an index (where 0 indicates the latest stack frame).
        /// Used to ensure stack frames IDs can be recycled per index.
        /// </summary>
        /// <param name="index">The index of the stack frame to obtain the ID for.</param>
        /// <returns>Returns the ID for the given stack frame index.</returns>
        public int GetStackFrameId(int index = 0)
        {
            // Verify the index for our stack frame isn't outside of our allocated count
            if (index < 0 || index >= STACKFRAME_ID_RESERVED_COUNT)
            {
                throw new ArgumentException("Could not obtain stack frame ID because the provided index was out of the allocated bounds.");
            }

            // Return our ID
            return _startingStackFrameId + index;
        }

        /// <summary>
        /// Obtains all linked stack frames for a given thread ID.
        /// </summary>
        /// <param name="threadId">The thread ID to obtain any linked stack frames for.</param>
        /// <param name="result">Returns linked stack frames for the given thread ID.</param>
        /// <returns>Returns true if stack frames for the given thread ID existed.</returns>
        public bool TryGetStackFrames(int threadId, out List<StackFrame> result)
        {
            // If we have stack frame ids for this thread
            if (_currentStackFrameIds?.Count > 0)
            {
                // Obtain the stack frames from the ids.
                result = _currentStackFrameIds.Select(x => _stackFrames[x].stackFrame).ToList();
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Links a stack frame to a given thread ID, and sets that thread ID as the current/active thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to link the given stack frame to.</param>
        /// <param name="stackFrame">The stack frame to link to the given thread ID.</param>
        /// <param name="traceIndex">The last significant trace index in this frame before entering another.</param>
        public void LinkStackFrame(int threadId, StackFrame stackFrame, int traceIndex)
        {
            // Obtain our callstack for this thread or create a new one if one doesn't exist.
            if (_currentStackFrameIds == null)
            {
                _currentStackFrameIds = new List<int>();
            }

            // Add our our stack frame list (id -> stack frame/trace scope)
            _stackFrames[stackFrame.Id] = (stackFrame, traceIndex);

            // Add to our thread id -> stack frames lookup.
            _currentStackFrameIds.Add(stackFrame.Id);

            // Set our current thread.
            CurrentThreadId = threadId;

            // Set our thread linked status.
            IsThreadLinked = true;
        }

        /// <summary>
        /// Sets the current stack frame ID, used to resolve local/state variables in the correct context.
        /// </summary>
        /// <param name="stackFrameId">The ID of the stack frame which we want to set as the currently selected/active stack frame.</param>
        /// <returns>Returns true if the stack frame ID was known, and current stack frame was set.</returns>
        public bool SetCurrentStackFrame(int stackFrameId)
        {
            // If our stack frame exists in our lookup, set it as the current
            if (_stackFrames.ContainsKey(stackFrameId))
            {
                CurrentStackFrameId = stackFrameId;
                return true;
            }

            // We could not find this stack frame.
            return false;
        }

        /// <summary>
        /// Links a nested/sub-variable to a parent variable reference ID.
        /// </summary>
        /// <param name="parentVariableReference">The parent reference ID for this nested variable.</param>
        /// <param name="variableReference">The reference ID of nested/sub-variable that is being added.</param>
        /// <param name="threadId">The ID of the active thread we are linking variables for.</param>
        /// <param name="underlyingVariableValuePair">The underlying variable-value pair for this variable to link to the parent variable reference ID.</param>
        public void LinkSubVariableReference(int parentVariableReference, int variableReference, int threadId, UnderlyingVariableValuePair underlyingVariableValuePair)
        {
            _variableReferenceIdToUnderlyingVariableValuePair[variableReference] = (threadId, underlyingVariableValuePair);

            // Try to get our variable subreference id, or if a list doesn't exist, create one.
            if (!_variableReferenceIdToSubVariableReferenceIds.TryGetValue(parentVariableReference, out List<int> subVariableReferenceIds))
            {
                subVariableReferenceIds = new List<int>();
                _variableReferenceIdToSubVariableReferenceIds[parentVariableReference] = subVariableReferenceIds;
            }

            // Add our sub reference to the list
            subVariableReferenceIds.Add(variableReference);

            // Add to the reverse lookup.
            _subVariableReferenceIdToVariableReferenceId[variableReference] = parentVariableReference;
        }

        /// <summary>
        /// Unlinks the nested/sub-variable from a parent variable reference ID (recursively).
        /// </summary>
        /// <param name="variableReference">The variable reference ID to unlink all variables/sub-variables recursively for.</param>
        private void UnlinkSubVariableReference(int variableReference)
        {
            // Unlink our variable value pair
            _variableReferenceIdToUnderlyingVariableValuePair.TryRemove(variableReference, out _);

            // Try to get our list of sub variable references to unlink.
            if (_variableReferenceIdToSubVariableReferenceIds.TryGetValue(variableReference, out var subVariableReferenceIds))
            {
                // Remove our variable reference from our lookup since it existed
                _variableReferenceIdToSubVariableReferenceIds.TryRemove(variableReference, out _);
                foreach (var subVariableReferenceId in subVariableReferenceIds)
                {
                    // Remove the reverse lookup
                    _subVariableReferenceIdToVariableReferenceId.TryRemove(subVariableReferenceId, out _);

                    // Unlink recursively
                    UnlinkSubVariableReference(subVariableReferenceId);
                }
            }
        }

        /// <summary>
        /// Creates a variable object and links all relevant evaluation information for it.
        /// </summary>
        /// <param name="name">The name of the variable to display.</param>
        /// <param name="value">The value of the variable to display.</param>
        /// <param name="variablesReference">The variable reference ID for this variable.</param>
        /// <param name="type">The type of the variable to create.</param>
        /// <returns>Returns a variable object which represents all the provided variable information.</returns>
        public Variable CreateVariable(string name, string value, int variablesReference, string type)
        {
            // Obtain our next variable evaluation id and set it in our lookup.
            var evalID = System.Threading.Interlocked.Increment(ref _variableEvaluationId);
            _variableEvaluationValues[evalID] = value;

            // Create the variable accordingly.
            return new Variable(name, value, variablesReference)
            {
                Type = type,
                EvaluateName = VARIABLE_EVAL_TYPE_PREFIX + evalID.ToString(CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// Gets a variable evaluate response for a given expression.
        /// </summary>
        /// <param name="expression">The variable expression for which to obtain an evaluation response for.</param>
        /// <returns>Returns a variable evaluation for the given expression, or null if one could not be found.</returns>
        public EvaluateResponse GetVariableEvaluateResponse(string expression)
        {
            // Verify the expression starts with the variable eval type prefix.
            if (expression.StartsWith(VARIABLE_EVAL_TYPE_PREFIX, StringComparison.Ordinal))
            {
                // Obtain our id from our expression.
                var id = int.Parse(expression.Substring(VARIABLE_EVAL_TYPE_PREFIX.Length), CultureInfo.InvariantCulture);

                // Try to get our variable evaluation value using our id.
                if (_variableEvaluationValues.TryGetValue(id, out var evalResult))
                {
                    // If we were able to get a variable evaluation from the id, return it.
                   return new EvaluateResponse { Result = evalResult };
                }
            }

            // We could not evaluate the variable.
            return null;
        }

        /// <summary>
        /// Attempts to resolve a parent variable given the parent variable reference ID, and it's corresponding thread ID.
        /// Thus, this is used to resolve nested/child variables.
        /// </summary>
        /// <param name="variableReference">The parent variable reference ID for which we wish to obtain the variable for.</param>
        /// <param name="threadId">The thread ID for the parent variable reference ID.</param>
        /// <param name="variableValuePair">The variable value pair for the given variable reference ID.</param>
        /// <returns>Returns true if a variable-value pair could be resolved for the given variable reference ID.</returns>
        public bool ResolveParentVariable(int variableReference, out int threadId, out UnderlyingVariableValuePair variableValuePair)
        {
            // Try to obtain our thread id and variable value pair.
            if (_variableReferenceIdToUnderlyingVariableValuePair.TryGetValue(variableReference, out var result))
            {
                // Obtain the thread id and variable value pair for this reference.
                threadId = result.threadId;
                variableValuePair = result.underlyingVariableValuePair;
                return true;
            }

            // We could not resolve the variable.
            threadId = 0;
            variableValuePair = new UnderlyingVariableValuePair(null, null);
            return false;
        }

        /// <summary>
        /// Attemps to resolve a non-nested/top-level local variable given a variable reference ID. If the ID matches the local scope ID,
        /// then the thread ID and current trace index is obtained for the stack frame in which this local variable resides.
        /// </summary>
        /// <param name="variableReference">The parent variable reference ID of the variable, to verify this is the local variable scope.</param>
        /// <param name="threadId">The thread ID for the parent variable reference ID.</param>
        /// <param name="traceIndex">The last trace index in the current stack frame at which this variable should be resolved.</param>
        /// <returns>Returns true if the ID did reference the local variable scope, and we should resolve top level local variables at the given trace index.</returns>
        public bool ResolveLocalVariable(int variableReference, out int threadId, out int traceIndex)
        {
            // Check the variable reference references the target scope id, and we have sufficient information.
            if (IsThreadLinked && variableReference == LocalScopeId)
            {
                // Obtain the thread id and trace index for this stack frame.
                threadId = CurrentThreadId;
                traceIndex = _stackFrames[CurrentStackFrameId].traceIndex;
                return true;
            }

            // We could not resolve the variable.
            threadId = 0;
            traceIndex = 0;
            return false;
        }

        /// <summary>
        /// Attemps to resolve a non-nested/top-level state variable given a variable reference ID. If the ID matches the state scope ID,
        /// then the thread ID and last trace index is obtained for the stack frame in which the state variable is to be resolved.
        /// </summary>
        /// <param name="variableReference">The parent variable reference ID of the variable, to verify this is the state variable scope.</param>
        /// <param name="threadId">The thread ID for the parent variable reference ID.</param>
        /// <param name="traceIndex">The last trace index in the current stack frame at which this variable should be resolved.</param>
        /// <returns>Returns true if the ID did reference the state variable scope, and we should resolve top level state variables at the given trace index.</returns>
        public bool ResolveStateVariable(int variableReference, out int threadId, out int traceIndex)
        {
            // Check the variable reference references the target scope id, and we have sufficient information.
            if (IsThreadLinked && variableReference == StateScopeId)
            {
                // Obtain the thread id and trace index for this stack frame.
                threadId = CurrentThreadId;
                traceIndex = _stackFrames[CurrentStackFrameId].traceIndex;
                return true;
            }

            // We could not resolve the variable.
            threadId = 0;
            traceIndex = 0;
            return false;
        }

        /// <summary>
        /// Unlinks all references for the given thread ID, and it's underlying stack frame/scope/variable IDs (recusively).
        /// </summary>
        /// <param name="threadId">The thread ID to unlink all reference for (recursively).</param>
        public void UnlinkThreadId(int threadId)
        {
            // Remove this thread id from the lookup.
            _currentStackFrameIds.Clear();

            // Unlink our stack frame
            _stackFrames.Clear();

            // Unlink our local scope and sub variable references.
            UnlinkSubVariableReference(LocalScopeId);

            // Unlink our state scope and sub variable references.
            UnlinkSubVariableReference(StateScopeId);

            // Clear all of our variable evaluation and reset the id.
            _variableEvaluationValues.Clear();
            _variableEvaluationId = 0;

            // Set our thread as not linked
            IsThreadLinked = false;
        }
        #endregion
    }
}
