using Meadow.CoverageReport.Debugging;
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

        public (int threadId, int traceIndex, bool isLocalVariable) GetVariableResolvingInformation(int variableReference)
        {
            // Check what type the variable reference is.
            bool isLocalVariable = true;
            if (!localScopeIdToStackFrameId.TryGetValue(variableReference, out int stackFrameId))
            {
                isLocalVariable = false;
                if (!stateScopeIdToStackFrameId.TryGetValue(variableReference, out stackFrameId))
                {
                    // TODO: Check if this is a struct or nested variable
                    throw new NotImplementedException("Nested variable/variable references are not yet supported.");
                }
            }

            // Obtain the thread id and trace index for this stack frame.
            return (stackFrameIdToThreadId[stackFrameId], stackFrames[stackFrameId].traceIndex, isLocalVariable);
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

                // Unlink our state scope
                if (stackFrameIdToStateScopeId.TryGetValue(stackFrameId, out int stateScopeId))
                {
                    stackFrameIdToStateScopeId.Remove(stackFrameId);
                    stateScopeIdToStackFrameId.Remove(stateScopeId);
                }

                // Unlink our local scope
                if (stackFrameIdToLocalScopeId.TryGetValue(stackFrameId, out int localScopeId))
                {
                    stackFrameIdToLocalScopeId.Remove(stackFrameId);
                    localScopeIdToStackFrameId.Remove(localScopeId);
                }
            }
        }
        #endregion
    }
}
