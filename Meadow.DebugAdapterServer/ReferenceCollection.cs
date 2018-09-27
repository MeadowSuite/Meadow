using Meadow.CoverageReport.Debugging;
using Meadow.CoverageReport.Debugging.Variables;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.DebugAdapterServer
{
    public class ReferenceContainer
    {
        #region Fields
        private static int _nextId;

        // threadId -> stackFrame[] (callstack)
        private Dictionary<int, List<int>> threadIdToStackFrameIds;
        // reverse: stackFrameId -> threadId
        private Dictionary<int, int> stackFrameIdToThreadId;

        // stackFrameId -> stackFrame (actual)
        private Dictionary<int, (StackFrame stackFrame, int traceIndex)> stackFrames;


        // stackFrameId -> localScopeId
        private Dictionary<int, int> stackFrameIdToLocalScopeId;
        // reverse: localScopeId -> stackFrameId
        private Dictionary<int, int> localScopeIdToStackFrameId;
        // stackFrameId -> stateVariableScopeId
        private Dictionary<int, int> stackFrameIdToStateScopeId;
        // reverse: stackFrameId -> localScopeId
        private Dictionary<int, int> stateScopeIdToStackFrameId;


        // variableReferenceId -> sub-variableReferenceIds
        private Dictionary<int, List<int>> _variableReferenceIdToSubVariableReferenceIds;
        // reverse: sub-variableReferenceIds -> variableReferenceId
        private Dictionary<int, int> _subVariableReferenceIdToVariableReferenceId;
        // variableReferenceId -> (threadId, variableValuePair)
        private Dictionary<int, (int threadId, UnderlyingVariableValuePair underlyingVariableValuePair)> _variableReferenceIdToUnderlyingVariableValuePair;
        #endregion

        #region Constructor
        public ReferenceContainer()
        {
            // Initialize our lookups.
            threadIdToStackFrameIds = new Dictionary<int, List<int>>();
            stackFrameIdToThreadId = new Dictionary<int, int>();
            stackFrames = new Dictionary<int, (StackFrame stackFrame, int traceIndex)>();

            stackFrameIdToLocalScopeId = new Dictionary<int, int>();
            localScopeIdToStackFrameId = new Dictionary<int, int>();
            stackFrameIdToStateScopeId = new Dictionary<int, int>();
            stateScopeIdToStackFrameId = new Dictionary<int, int>();

            _variableReferenceIdToSubVariableReferenceIds = new Dictionary<int, List<int>>();
            _subVariableReferenceIdToVariableReferenceId = new Dictionary<int, int>();
            _variableReferenceIdToUnderlyingVariableValuePair = new Dictionary<int, (int threadId, UnderlyingVariableValuePair variableValuePair)>();
        }
        #endregion

        #region Functions
        public int GetUniqueId()
        {
            return _nextId++;
        }

        public bool TryGetStackFrames(int threadId, out List<StackFrame> result)
        {
            // If we have stack frame ids for this thread
            if (threadIdToStackFrameIds.TryGetValue(threadId, out var stackFrameIds))
            {
                // Obtain the stack frames from the ids.
                result = stackFrameIds.Select(x => stackFrames[x].stackFrame).ToList();
                return true;
            }

            result = null;
            return false;
        }

        public void LinkStackFrame(int threadId, StackFrame stackFrame, int traceIndex)
        {
            // Obtain our callstack for this thread or create a new one if one doesn't exist.
            if (!threadIdToStackFrameIds.TryGetValue(threadId, out var callstack))
            {
                callstack = new List<int>();
                threadIdToStackFrameIds[threadId] = callstack;
            }

            // Add our our stack frame list (id -> stack frame/trace scope)
            stackFrames[stackFrame.Id] = (stackFrame, traceIndex);

            // Add to our thread id -> stack frames lookup.
            callstack.Add(stackFrame.Id);

            // Add to our reverse stack frame id -> thread id lookup.
            stackFrameIdToThreadId[stackFrame.Id] = threadId;

            // Generate scope ids
            int localScopeId = GetUniqueId();
            int stateScopeId = GetUniqueId();
            LinkScopes(stackFrame.Id, stateScopeId, localScopeId);
        }

        private void LinkScopes(int stackFrameId, int stateScopeId, int localScopeId)
        {
            // Link our stack frame id -> scope ids.
            stackFrameIdToLocalScopeId[stackFrameId] = localScopeId;
            localScopeIdToStackFrameId[localScopeId] = stackFrameId;
            stackFrameIdToStateScopeId[stackFrameId] = stateScopeId;
            stateScopeIdToStackFrameId[stateScopeId] = stackFrameId;
        }

        public int? GetLocalScopeId(int stackFrameId)
        {
            // Try to obtain our scope id
            if (stackFrameIdToLocalScopeId.TryGetValue(stackFrameId, out int result))
            {
                return result;
            }

            return null;
        }

        public int? GetStateScopeId(int stackFrameId)
        {
            // Try to obtain our scope id
            if (stackFrameIdToStateScopeId.TryGetValue(stackFrameId, out int result))
            {
                return result;
            }

            return null;
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
            if (!_variableReferenceIdToUnderlyingVariableValuePair.TryGetValue(variableReference, out var result))
            {
                threadId = 0;
                variableValuePair = new UnderlyingVariableValuePair(null, null);
                return false;
            }

            // Obtain the thread id and variable value pair for this reference.
            threadId = result.threadId;
            variableValuePair = result.underlyingVariableValuePair;
            return true;
        }

        public bool ResolveLocalVariable(int variableReference, out int threadId, out int traceIndex)
        {
            // Try to obtain our stack frame id.
            if (!localScopeIdToStackFrameId.TryGetValue(variableReference, out int stackFrameId))
            {
                threadId = 0;
                traceIndex = 0;
                return false;
            }

            // Obtain the thread id and trace index for this stack frame.
            threadId = stackFrameIdToThreadId[stackFrameId];
            traceIndex = stackFrames[stackFrameId].traceIndex;
            return true;
        }

        public bool ResolveStateVariable(int variableReference, out int threadId, out int traceIndex)
        {
            // Try to obtain our stack frame id.
            if (!stateScopeIdToStackFrameId.TryGetValue(variableReference, out int stackFrameId))
            {
                threadId = 0;
                traceIndex = 0;
                return false;
            }

            // Obtain the thread id and trace index for this stack frame.
            threadId = stackFrameIdToThreadId[stackFrameId];
            traceIndex = stackFrames[stackFrameId].traceIndex;
            return true;
        }

        public void UnlinkThreadId(int threadId)
        {
            // Verify we have this thread id in our lookup.
            if (!threadIdToStackFrameIds.TryGetValue(threadId, out var stackFrameIds))
            {
                return;
            }

            // Remove this thread id from the lookup.
            threadIdToStackFrameIds.Remove(threadId);

            // Loop for all stack frame ids
            foreach (var stackFrameId in stackFrameIds)
            {
                // Unlink our reverse stack frame id -> thread id
                stackFrameIdToThreadId.Remove(stackFrameId);

                // Unlink our stack frame
                stackFrames.Remove(stackFrameId);

                // Unlink our state scope and sub variable references.
                if (stackFrameIdToStateScopeId.TryGetValue(stackFrameId, out int stateScopeId))
                {
                    stackFrameIdToStateScopeId.Remove(stackFrameId);
                    stateScopeIdToStackFrameId.Remove(stateScopeId);
                    UnlinkSubVariableReference(stateScopeId);
                }

                // Unlink our local scope and sub variable references.
                if (stackFrameIdToLocalScopeId.TryGetValue(stackFrameId, out int localScopeId))
                {
                    stackFrameIdToLocalScopeId.Remove(stackFrameId);
                    localScopeIdToStackFrameId.Remove(localScopeId);
                    UnlinkSubVariableReference(localScopeId);
                }
            }
        }
        #endregion
    }
}
