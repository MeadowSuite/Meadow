using Meadow.CoverageReport.Debugging;
using Meadow.CoverageReport.Debugging.Variables;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Meadow.DebugAdapterServer
{
    public class ReferenceContainer
    {
        #region Constants
        private const int STACKFRAME_ID_RESERVED_COUNT = 0x10000;
        #endregion

        #region Fields
        private static int _nextId;

        // (callstack)
        private List<int> _currentStackFrameIds;

        // stackFrameId -> stackFrame (actual)
        private Dictionary<int, (StackFrame stackFrame, int traceIndex)> _stackFrames;

        // variableReferenceId -> sub-variableReferenceIds
        private Dictionary<int, List<int>> _variableReferenceIdToSubVariableReferenceIds;
        // reverse: sub-variableReferenceIds -> variableReferenceId
        private Dictionary<int, int> _subVariableReferenceIdToVariableReferenceId;
        // variableReferenceId -> (threadId, variableValuePair)
        private Dictionary<int, (int threadId, UnderlyingVariableValuePair underlyingVariableValuePair)> _variableReferenceIdToUnderlyingVariableValuePair;

        private int _startingStackFrameId;
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
            _stackFrames = new Dictionary<int, (StackFrame stackFrame, int traceIndex)>();

            LocalScopeId = GetUniqueId();
            StateScopeId = GetUniqueId();

            // Allocate our desired amount of callstack ids
            _startingStackFrameId = _nextId;
            Interlocked.Add(ref _nextId, STACKFRAME_ID_RESERVED_COUNT);

            _variableReferenceIdToSubVariableReferenceIds = new Dictionary<int, List<int>>();
            _subVariableReferenceIdToVariableReferenceId = new Dictionary<int, int>();
            _variableReferenceIdToUnderlyingVariableValuePair = new Dictionary<int, (int threadId, UnderlyingVariableValuePair variableValuePair)>();
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
            _variableReferenceIdToUnderlyingVariableValuePair.Remove(variableReference);

            // Try to get our list of sub variable references to unlink.
            if (_variableReferenceIdToSubVariableReferenceIds.TryGetValue(variableReference, out var subVariableReferenceIds))
            {
                // Remove our variable reference from our lookup since it existed
                _variableReferenceIdToSubVariableReferenceIds.Remove(variableReference);
                foreach (var subVariableReferenceId in subVariableReferenceIds)
                {
                    // Remove the reverse lookup
                    _subVariableReferenceIdToVariableReferenceId.Remove(subVariableReferenceId);

                    // Unlink recursively
                    UnlinkSubVariableReference(subVariableReferenceId);
                }
            }
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

            // Set our thread as not linked
            IsThreadLinked = false;
        }
        #endregion
    }
}
