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
        public bool IsThreadLinked { get; private set; }
        public int CurrentThreadId { get; private set; }
        public int CurrentStackFrameId { get; private set; }
        public int LocalScopeId { get; private set; }
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
        public int GetUniqueId()
        {
            return Interlocked.Increment(ref _nextId);
        }

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
