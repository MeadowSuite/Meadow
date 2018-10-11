using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Meadow.CoverageReport.Debugging;
using Meadow.CoverageReport.Models;
using System.Collections.Concurrent;
using Meadow.CoverageReport.Debugging.Variables;
using Meadow.JsonRpc.Types.Debugging;
using Meadow.CoverageReport.Debugging.Variables.Enums;
using System.Globalization;
using Meadow.CoverageReport.Debugging.Variables.UnderlyingTypes;
using Meadow.CoverageReport.Debugging.Variables.Pairing;
using Meadow.JsonRpc.Client;
using System.Text;

namespace Meadow.DebugAdapterServer
{
    class ExampleCustomRequestWithResponse : DebugRequestWithResponse<ExampleRequestArgs, ExampleRequestResponse>
    {
        public ExampleCustomRequestWithResponse() : base("customRequestExample")
        {
        }
    }

    class ExampleRequestArgs
    {
        public string SessionID { get; set; }
    }

    class ExampleRequestResponse : ResponseBody
    {
        public string Response { get; set; }
    }

    public class MeadowSolidityDebugAdapter : DebugAdapterBase
    {
        #region Constants
        private string _contractsDirectory = "Contracts";
        private const int SIMULTANEOUS_TRACE_COUNT = 1;
        #endregion

        #region Fields
        readonly TaskCompletionSource<object> _terminatedTcs = new TaskCompletionSource<object>();

        public readonly TaskCompletionSource<object> CompletedInitializationRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedLaunchRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedConfigurationDoneRequest = new TaskCompletionSource<object>();
        private System.Threading.SemaphoreSlim _processTraceSemaphore = new System.Threading.SemaphoreSlim(SIMULTANEOUS_TRACE_COUNT, SIMULTANEOUS_TRACE_COUNT);

        readonly ConcurrentDictionary<string, int[]> _sourceBreakpoints = new ConcurrentDictionary<string, int[]>();
        #endregion

        #region Properties
        public Task Terminated => _terminatedTcs.Task;
        public SolidityMeadowConfigurationProperties ConfigurationProperties { get; private set; }
        public ConcurrentDictionary<int, MeadowDebugAdapterThreadState> ThreadStates { get; }
        public ReferenceContainer ReferenceContainer;
        public bool Exiting { get; private set; }
        #endregion

        #region Events
        public delegate void ExitingEventHandler(MeadowSolidityDebugAdapter sender);
        public event ExitingEventHandler OnDebuggerDisconnect;
        #endregion

        const string EXCEPTION_BREAKPOINT_FILTER_ALL = "All Exceptions";
        const string EXCEPTION_BREAKPOINT_FILTER_UNHANDLED = "Unhandled Exceptions";

        HashSet<string> _exceptionBreakpointFilters = new HashSet<string>();
        int _threadIDCounter = 0;

        #region Constructor
        public MeadowSolidityDebugAdapter()
        {
            // Initialize our thread state lookup.
            ThreadStates = new ConcurrentDictionary<int, MeadowDebugAdapterThreadState>();

            // Initialize our reference collection
            ReferenceContainer = new ReferenceContainer();
        }
        #endregion

        #region Functions
        public void InitializeStream(Stream input, Stream output)
        {
            InitializeProtocolClient(input, output);

            Protocol.RegisterRequestType<ExampleCustomRequestWithResponse, ExampleRequestArgs, ExampleRequestResponse>(r =>
            {
                r.SetResponse(new ExampleRequestResponse { Response = "good" });
            });
        }

        public async Task ProcessExecutionTraceAnalysis(IJsonRpcClient rpcClient, ExecutionTraceAnalysis traceAnalysis, bool expectingException)
        {
            // We don't have real threads here, only a unique execution context
            // per RPC callback (eth_call or eth_sendTransactions).
            // So just use a rolling ID for threads.
            int threadId = System.Threading.Interlocked.Increment(ref _threadIDCounter);

            // Create a thread state for this thread
            MeadowDebugAdapterThreadState threadState = new MeadowDebugAdapterThreadState(rpcClient, traceAnalysis, threadId, expectingException);

            // Acquire the semaphore for processing a trace.
            await _processTraceSemaphore.WaitAsync();

            // Set the thread state in our lookup
            ThreadStates[threadId] = threadState;

            // If we're not exiting, step through our 
            if (!Exiting)
            {
                // Send an event that our thread has exited.
                Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Started, threadState.ThreadId));

                // Continue our execution.
                ContinueExecution(threadState, DesiredControlFlow.Continue);

                // Lock execution is complete.
                await threadState.Semaphore.WaitAsync();

                // Send an event that our thread has exited.
                Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Exited, threadState.ThreadId));
            }

            // Remove the thread from our lookup.
            ThreadStates.Remove(threadId, out _);

            // Unlink our data for our thread id.
            ReferenceContainer.UnlinkThreadId(threadId);

            // Release the semaphore for processing a trace.
            _processTraceSemaphore.Release();
        }

        private void ContinueExecution(MeadowDebugAdapterThreadState threadState, DesiredControlFlow controlFlowAction = DesiredControlFlow.Continue, int stepsPriorToAction = 0)
        {
            // Unlink our data for our thread id.
            ReferenceContainer.UnlinkThreadId(threadState.ThreadId);

            // Create a variable to track if we have finished stepping through the execution.
            bool finishedExecution = false;

            // Determine the direction to take steps prior to any evaluation.
            if (stepsPriorToAction >= 0)
            {
                // Loop for each step to take forward.
                for (int i = 0; i < stepsPriorToAction; i++)
                {
                    // Take a step forward, if we could not, we finished execution, so we can stop looping.
                    if (!threadState.IncrementStep())
                    {
                        finishedExecution = true;
                        break;
                    }
                }
            }
            else
            {
                // Loop for each step to take backward
                for (int i = 0; i > stepsPriorToAction; i--)
                {
                    // Take a step backward, if we could not, we can stop early as we won't be able to step backwards anymore.
                    if (!threadState.DecrementStep())
                    {
                        break;
                    }
                }
            }

            // If we haven't finished execution, 
            if (!finishedExecution)
            {
                // Determine how to handle our desired control flow.
                switch (controlFlowAction)
                {
                    case DesiredControlFlow.Continue:
                        {
                            // Process the execution trace analysis
                            while (threadState.CurrentStepIndex.HasValue && !Exiting)
                            {
                                // If we encountered an event at this point, stop
                                if (EvaluateCurrentStep(threadState))
                                {
                                    return;
                                }

                                // If we couldn't step forward, this trace has been fully processed.
                                if (!threadState.IncrementStep())
                                {
                                    break;
                                }
                            }

                            // If we exited this way, our execution has concluded because we could not step any further (or there were never any steps).
                            finishedExecution = true;
                            break;
                        }

                    case DesiredControlFlow.StepBackwards:
                        {
                            // Decrement our step
                            bool decrementedStep = threadState.DecrementStep();

                            // If we stepped successfully, we evaluate, and if an event is encountered, we stop.
                            if (decrementedStep && EvaluateCurrentStep(threadState))
                            {
                                return;
                            }

                            // Send our continued event to force refresh of state.
                            Protocol.SendEvent(new ContinuedEvent(threadState.ThreadId));

                            // Signal our breakpoint event has occurred for this thread.
                            Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Step) { ThreadId = threadState.ThreadId });
                            break;
                        }

                    case DesiredControlFlow.StepInto:
                    case DesiredControlFlow.StepOver:
                    case DesiredControlFlow.StepOut:
                        {
                            // Here we handle Step Into + Step Over + Step Out
                            // The following conditions need to be checked:
                            //      (1) We step once unconditionally
                            //      (2) Each step should be evaluated for breakpoints/exceptions and checked to see if execution ended.
                            //      (3) If step into: break instantly.
                            //      (4) If step out: break if current scope is the parent of the initial scope.
                            //      (5) If step over: break if the current scope is the same OR the parent of the initial scope.

                            // Define our initial scope
                            ExecutionTraceScope initialTraceScope = null;
                            SourceFileLine[] initialSourceLines = Array.Empty<SourceFileLine>();
                            // Obtain the initial scope if we're stepping over or out (it will be needed for comparisons later).
                            if (threadState.CurrentStepIndex.HasValue && controlFlowAction != DesiredControlFlow.StepInto)
                            {
                                initialTraceScope = threadState.ExecutionTraceAnalysis.GetScope(threadState.CurrentStepIndex.Value);
                                initialSourceLines = threadState.ExecutionTraceAnalysis.GetSourceLines(threadState.CurrentStepIndex.Value);
                            }

                            // Loop for through all the steps.
                            do
                            {
                                // Increment our step
                                finishedExecution = !threadState.IncrementStep();

                                // If we stepped successfully, we evaluate, and if an event is encountered, we stop.
                                if (!finishedExecution && EvaluateCurrentStep(threadState))
                                {
                                    return;
                                }

                                // If this isn't a basic single step (step into), we'll want to determine if we want to step more, or halt.
                                if (controlFlowAction != DesiredControlFlow.StepInto)
                                {
                                    // Obtain our current execution trace point.
                                    ExecutionTraceScope currentTraceScope = threadState.ExecutionTraceAnalysis.GetScope(threadState.CurrentStepIndex.Value);

                                    // Check if we reached our target, if not, we iterate to step some more.
                                    bool currentScopeIsParent = currentTraceScope == initialTraceScope.Parent;
                                    bool currentScopeIsSame = currentTraceScope == initialTraceScope;
                                    if (controlFlowAction == DesiredControlFlow.StepOver)
                                    {
                                        // We're handling a step over.

                                        // If this isn't the same scope or parent, we keep step some more.
                                        if (!currentScopeIsParent && !currentScopeIsSame)
                                        {
                                            continue;
                                        }
                                        else if (currentScopeIsSame)
                                        {
                                            // If the scope is the same, we verify the line is different (since entering/exiting a function will create two steps at the same point, we skip the latter point).
                                            SourceFileLine[] currentSourceLines = threadState.ExecutionTraceAnalysis.GetSourceLines(threadState.CurrentStepIndex.Value);
                                            if (currentSourceLines?.Length == 0 ||
                                                (initialSourceLines.Length >= 1 &&
                                                currentSourceLines[0].LineNumber == initialSourceLines[0].LineNumber &&
                                                currentSourceLines[0].SourceFileMapParent?.SourceFileIndex == initialSourceLines[0].SourceFileMapParent?.SourceFileIndex))
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // We're handling a step out.

                                        // If this isn't the parent scope, we keep step some more.
                                        if (!currentScopeIsParent)
                                        {
                                            continue;
                                        }
                                    }
                                }

                                // Send our continued event to force refresh of state.
                                Protocol.SendEvent(new ContinuedEvent(threadState.ThreadId));

                                // Signal our breakpoint event has occurred for this thread.
                                Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Step) { ThreadId = threadState.ThreadId });
                                break;
                            }
                            // Loop while our execution hasn't finioshed.
                            while (!finishedExecution);

                            break;
                        }
            }
            }

            // If we finished execution, signal our thread
            if (finishedExecution)
            {
                threadState.Semaphore.Release();
            }
        }

        private bool EvaluateCurrentStep(MeadowDebugAdapterThreadState threadState, bool exceptions = true, bool breakpoints = true)
        {
            // Evaluate exceptions and breakpoints at this point in execution.
            return (exceptions && HandleExceptions(threadState)) || (breakpoints && HandleBreakpoint(threadState));
        }

        private bool HandleExceptions(MeadowDebugAdapterThreadState threadState)
        {
            bool ShouldProcessException()
            {
                lock (_exceptionBreakpointFilters)
                {
                    if (_exceptionBreakpointFilters.Contains(EXCEPTION_BREAKPOINT_FILTER_ALL))
                    {
                        return true;
                    }

                    if (!threadState.ExpectingException && _exceptionBreakpointFilters.Contains(EXCEPTION_BREAKPOINT_FILTER_UNHANDLED))
                    {
                        return true;
                    }

                    return false;
                }
            }

            // Try to obtain an exception at this point
            if (ShouldProcessException() && threadState.CurrentStepIndex.HasValue)
            {
                // Obtain an exception at the current point.
                ExecutionTraceException traceException = threadState.ExecutionTraceAnalysis.GetException(threadState.CurrentStepIndex.Value);

                // If we have an exception, throw it and return the appropriate status.
                if (traceException != null)
                {
                    // Send our continued event to force refresh of state.
                    Protocol.SendEvent(new ContinuedEvent(threadState.ThreadId));

                    // Send our exception event.
                    var stoppedEvent = new StoppedEvent(StoppedEvent.ReasonValue.Exception)
                    {
                        Text = traceException.Message,
                        ThreadId = threadState.ThreadId
                    };
                    Protocol.SendEvent(stoppedEvent);
                    return true;
                }
            }

            // We did not find an exception here, return false
            return false;
        }

        private bool HandleBreakpoint(MeadowDebugAdapterThreadState threadState)
        {
            // Verify we have a valid step at this point.
            if (!threadState.CurrentStepIndex.HasValue)
            {
                return false;
            }

            // Loop through all the source lines at this step.
            var sourceLines = threadState.ExecutionTraceAnalysis.GetSourceLines(threadState.CurrentStepIndex.Value);
            foreach (var sourceLine in sourceLines)
            {
                // Verify our source path.
                var sourceFilePath = sourceLine.SourceFileMapParent?.SourceFilePath;

                // Resolve relative path properly so it can simply be looked up.
                bool success = _sourceBreakpoints.TryGetValue(sourceFilePath, out var breakpointLines);

                // If we have a breakpoint at this line number..
                bool containsBreakpoint = success && breakpointLines.Any(x => x == sourceLine.LineNumber);
                if (containsBreakpoint)
                {
                    // Send our continued event to force refresh of state.
                    Protocol.SendEvent(new ContinuedEvent(threadState.ThreadId));

                    // Signal our breakpoint event has occurred for this thread.
                    Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint) { ThreadId = threadState.ThreadId });
                    return true;
                }
            }

            return false;
        }

        protected override void HandleInitializeRequestAsync(IRequestResponder<InitializeArguments, InitializeResponse> responder)
        {
            var response = new InitializeResponse
            {
                SupportsConfigurationDoneRequest = true,
                SupportsEvaluateForHovers = true,
                SupportsStepBack = true,
                SupportsStepInTargetsRequest = true,
                SupportTerminateDebuggee = false,
                SupportsRestartRequest = false,
                SupportsRestartFrame = false,
                SupportedChecksumAlgorithms = new List<ChecksumAlgorithm> { ChecksumAlgorithm.SHA256 },
                SupportsExceptionInfoRequest = true
            };

            response.ExceptionBreakpointFilters = new List<ExceptionBreakpointsFilter>
            {
                new ExceptionBreakpointsFilter
                {
                    Filter = EXCEPTION_BREAKPOINT_FILTER_ALL,
                    Label = EXCEPTION_BREAKPOINT_FILTER_ALL,
                    Default = false
                },
                new ExceptionBreakpointsFilter
                {
                    Filter = EXCEPTION_BREAKPOINT_FILTER_UNHANDLED,
                    Label = EXCEPTION_BREAKPOINT_FILTER_UNHANDLED,
                    Default = true
                }
            };

            responder.SetResponse(response);
            Protocol.SendEvent(new InitializedEvent());
            CompletedInitializationRequest.SetResult(null);
        }

        protected override void HandleSetExceptionBreakpointsRequestAsync(IRequestResponder<SetExceptionBreakpointsArguments> responder)
        {
            lock (_exceptionBreakpointFilters)
            {
                _exceptionBreakpointFilters = new HashSet<string>(responder.Arguments.Filters);
                var response = new SetExceptionBreakpointsResponse();
                responder.SetResponse(response);
            }
        }

        protected override void HandleAttachRequestAsync(IRequestResponder<AttachArguments> responder)
        {
            SetupConfigurationProperties(responder.Arguments.ConfigurationProperties);
            responder.SetResponse(new AttachResponse());
            CompletedLaunchRequest.SetResult(null);
        }

        protected override void HandleLaunchRequestAsync(IRequestResponder<LaunchArguments> responder)
        {
            SetupConfigurationProperties(responder.Arguments.ConfigurationProperties);
            responder.SetResponse(new LaunchResponse());
            CompletedLaunchRequest.SetResult(null);
        }

        void SetupConfigurationProperties(Dictionary<string, JToken> configProperties)
        {
            ConfigurationProperties = JObject.FromObject(configProperties).ToObject<SolidityMeadowConfigurationProperties>();
            if (Directory.Exists(Path.Combine(ConfigurationProperties.WorkspaceDirectory, "Contracts")))
            {
                _contractsDirectory = "Contracts";
            }
            else if (Directory.Exists(Path.Combine(ConfigurationProperties.WorkspaceDirectory, "contracts")))
            {
                _contractsDirectory = "contracts";
            }
            else
            {
                throw new Exception("No contracts directory found");
            }
        }

        protected override void HandleConfigurationDoneRequestAsync(IRequestResponder<ConfigurationDoneArguments> responder)
        {
            responder.SetResponse(new ConfigurationDoneResponse());
            CompletedConfigurationDoneRequest.SetResult(null);
        }

        protected override void HandleDisconnectRequestAsync(IRequestResponder<DisconnectArguments> responder)
        {
            // Set our exiting status
            Exiting = true;

            // Loop for each thread
            foreach (var threadStateKeyValuePair in ThreadStates)
            {
                // Release the execution lock on this thread state.
                // NOTE: This already happens when setting exiting status,
                // but only if the thread is currently stepping/continuing,
                // and not paused. This will allow it to continue if stopped.
                threadStateKeyValuePair.Value.Semaphore.Release();
            }

            // Set our response to the disconnect request.
            responder.SetResponse(new DisconnectResponse());
            Protocol.SendEvent(new TerminatedEvent());
            Protocol.SendEvent(new ExitedEvent(0));
            _terminatedTcs.TrySetResult(null);

            // If we have an event, invoke it
            OnDebuggerDisconnect?.Invoke(this);
        }

        public void SendTerminateAndExit()
        {
            Protocol.SendEvent(new TerminatedEvent());
            Protocol.SendEvent(new ExitedEvent(0));
        }

        #region Step, Continue, Pause

        protected override void HandleStepInRequestAsync(IRequestResponder<StepInArguments> responder)
        {
            // Set our response
            responder.SetResponse(new StepInResponse());

            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepInto);
            }
        }

        protected override void HandleNextRequestAsync(IRequestResponder<NextArguments> responder)
        {
            // Set our response
            responder.SetResponse(new NextResponse());

            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepOver);
            }
        }


        protected override void HandleStepInTargetsRequestAsync(IRequestResponder<StepInTargetsArguments, StepInTargetsResponse> responder)
        {
            responder.SetResponse(new StepInTargetsResponse());
        }

        protected override void HandleStepOutRequestAsync(IRequestResponder<StepOutArguments> responder)
        {
            // Set our response
            responder.SetResponse(new StepOutResponse());

            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepOut);
            }
        }

        protected override void HandleStepBackRequestAsync(IRequestResponder<StepBackArguments> responder)
        {
            // Set our response
            responder.SetResponse(new StepBackResponse());

            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepBackwards);
            }
        }

        protected override void HandleContinueRequestAsync(IRequestResponder<ContinueArguments, ContinueResponse> responder)
        {
            // Set our response
            responder.SetResponse(new ContinueResponse());

            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing, taking one step before continuing, as evaluation occurs before steps occur, and we want
                // to ensure we advanced position from our last and don't re-evaluate the same trace point. We only do this on
                // startup since we want the initial trace point to be evaluated. After that, we want to force advancement by
                // at least one step before continuation/re-evaluation.
                ContinueExecution(threadState, DesiredControlFlow.Continue, 1);
            }
        }

        protected override void HandleReverseContinueRequestAsync(IRequestResponder<ReverseContinueArguments> responder)
        {
            responder.SetResponse(new ReverseContinueResponse());
        }

        protected override void HandlePauseRequestAsync(IRequestResponder<PauseArguments> responder)
        {
            responder.SetResponse(new PauseResponse());
        }
        #endregion


        #region Threads, Scopes, Variables Requests

        protected override void HandleThreadsRequestAsync(IRequestResponder<ThreadsArguments, ThreadsResponse> responder)
        {
            // Create a list of threads from our thread states.
            List<Thread> threads = ThreadStates.Values.Select(x => new Thread(x.ThreadId, $"thread_{x.ThreadId}")).ToList();
            responder.SetResponse(new ThreadsResponse(threads));
        }

        protected override void HandleStackTraceRequestAsync(IRequestResponder<StackTraceArguments, StackTraceResponse> responder)
        {
            // Create a list of stack frames or try to get cached ones.
            List<StackFrame> stackFrames;
            bool cachedStackFrames = ReferenceContainer.TryGetStackFrames(responder.Arguments.ThreadId, out stackFrames);

            // Verify we have a thread state for this thread, and a valid step to represent in it.
            if (!cachedStackFrames && ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState) && threadState.CurrentStepIndex.HasValue)
            {
                // Initialize our stack frame list
                stackFrames = new List<StackFrame>();

                // Obtain the callstack
                var callstack = threadState.ExecutionTraceAnalysis.GetCallStack(threadState.CurrentStepIndex.Value);

                // Loop through our scopes.
                for (int i = 0; i < callstack.Length; i++)
                {
                    // Grab our current call frame
                    var currentStackFrame = callstack[i];

                    // If the scope could not be resolved within a function, and no lines could be resolved, skip to the next frame.
                    // as this is not a code section we can describe in any meaningful way.
                    if (!currentStackFrame.ResolvedFunction && currentStackFrame.CurrentPositionLines.Length == 0)
                    {
                         continue;
                    }

                    // If we couldn't resolve the current position or there were no lines representing it
                    if (currentStackFrame.Error || currentStackFrame.CurrentPositionLines.Length == 0)
                    {
                        continue;
                    }

                    // Obtain the method name we are executing in.
                    string frameName = currentStackFrame.FunctionName;
                    if (string.IsNullOrEmpty(frameName))
                    {
                        frameName = "<undefined>";
                    }

                    // Determine the bounds of our stack frame.
                    int startLine = 0;
                    int startColumn = 1;
                    int endLine = 0;
                    int endColumn = 0;

                    // Loop through all of our lines for this position.
                    for (int x = 0; x < currentStackFrame.CurrentPositionLines.Length; x++)
                    {
                        // Obtain our indexed line.
                        SourceFileLine line = currentStackFrame.CurrentPositionLines[x];

                        // Set our start position if relevant.
                        if (x == 0 || line.LineNumber <= startLine)
                        {
                            // Set our starting line number.
                            startLine = line.LineNumber;

                            // TODO: Determine our column start
                        }

                        // Set our end position if relevant.
                        if (x == 0 || line.LineNumber >= endLine)
                        {
                            // Set our ending line number.
                            endLine = line.LineNumber;

                            // TODO: Determine our column
                            endColumn = line.Length;
                        }
                    }

                    // Format agnostic path to platform specific path
                    var sourceFilePath = currentStackFrame.CurrentPositionLines[0].SourceFileMapParent.SourceFilePath;
                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        sourceFilePath = sourceFilePath.Replace('/', Path.DirectorySeparatorChar);
                    }
                
                    // Create our source object
                    Source stackFrameSource = new Source()
                    {
                        Name = currentStackFrame.CurrentPositionLines[0].SourceFileMapParent.SourceFileName,
                        Path = Path.Join(ConfigurationProperties.WorkspaceDirectory, _contractsDirectory, sourceFilePath)
                    };

                    // Create our stack frame
                    var stackFrame = new StackFrame()
                    {
                        Id = ReferenceContainer.GetStackFrameId(i),
                        Name = frameName,
                        Line = startLine,
                        Column = startColumn,
                        Source = stackFrameSource,
                        EndColumn = endColumn
                    };

                    if (endLine > 0)
                    {
                        stackFrame.EndLine = endLine;
                    }

                    // Add the stack frame to our reference list
                    ReferenceContainer.LinkStackFrame(threadState.ThreadId, stackFrame, currentStackFrame.CurrentPositionTraceIndex);
                    
                    // Add our stack frame to the result list
                    stackFrames.Add(stackFrame);
                }

            }

            // Return our stack frames in our response.
            responder.SetResponse(new StackTraceResponse(stackFrames));
        }

        protected override void HandleScopesRequestAsync(IRequestResponder<ScopesArguments, ScopesResponse> responder)
        {
            // Create our scope list
            List<Scope> scopeList = new List<Scope>();

            // If there is a thread linked
            if (ReferenceContainer.IsThreadLinked)
            {
                // We'll want to set the current stack frame
                ReferenceContainer.SetCurrentStackFrame(responder.Arguments.FrameId);

                // Add the relevant scopes for this stack frame.
                scopeList.Add(new Scope("State Variables", ReferenceContainer.StateScopeId, false));
                scopeList.Add(new Scope("Local Variables", ReferenceContainer.LocalScopeId, false));
            }

            // Set our response.
            responder.SetResponse(new ScopesResponse(scopeList));
        }

        private bool IsNestedVariableType(VarGenericType genericType)
        {
            // Check the type of variable this is
            return genericType == VarGenericType.Array ||
                genericType == VarGenericType.ByteArrayDynamic ||
                genericType == VarGenericType.ByteArrayFixed ||
                genericType == VarGenericType.Mapping ||
                genericType == VarGenericType.Struct;
        }

        private string GetVariableValueString(UnderlyingVariableValuePair variableValuePair)
        {
            // Determine how to format our value string.
            switch (variableValuePair.Variable.GenericType)
            {
                case VarGenericType.Array:
                    return $"{variableValuePair.Variable.BaseType} (size: {((object[])variableValuePair.Value).Length})";
                case VarGenericType.ByteArrayDynamic:
                    return $"{variableValuePair.Variable.BaseType} (size: {((Memory<byte>)variableValuePair.Value).Length})";
                case VarGenericType.ByteArrayFixed:
                    return variableValuePair.Variable.BaseType;
                case VarGenericType.Mapping:
                    return $"{variableValuePair.Variable.BaseType} (size: {((MappingKeyValuePair[])variableValuePair.Value).Length})";
                case VarGenericType.String:
                    return $"\"{variableValuePair.Value}\"";
                case VarGenericType.Struct:
                    return variableValuePair.Variable.BaseType;
                default:
                    // As a fallback, try to turn the object into a string.
                    return variableValuePair.Value?.ToString() ?? "null";
            }
        }

        protected override void HandleVariablesRequestAsync(IRequestResponder<VariablesArguments, VariablesResponse> responder)
        {
            // Obtain relevant variable resolving information.
            int threadId = 0;
            int traceIndex = 0;
            bool isLocalVariableScope = false;
            bool isStateVariableScope = false;
            bool isParentVariableScope = false;
            UnderlyingVariableValuePair parentVariableValuePair = new UnderlyingVariableValuePair(null, null);

            // Try to obtain the variable reference as a local variable scope.
            isLocalVariableScope = ReferenceContainer.ResolveLocalVariable(responder.Arguments.VariablesReference, out threadId, out traceIndex);
            if (!isLocalVariableScope)
            {
                // Try to obtain the variable reference as a state variable scope.
                isStateVariableScope = ReferenceContainer.ResolveStateVariable(responder.Arguments.VariablesReference, out threadId, out traceIndex);
                if (!isStateVariableScope)
                {
                    // Try to obtain the variable reference as a sub-variable scope.
                    isParentVariableScope = ReferenceContainer.ResolveParentVariable(responder.Arguments.VariablesReference, out threadId, out parentVariableValuePair);
                }
            }

            // Using our thread id, obtain our thread state
            ThreadStates.TryGetValue(threadId, out var threadState);

            // Verify the thread state is valid
            if (threadState == null)
            {
                responder.SetResponse(new VariablesResponse());
                return;
            }

            // Obtain the trace index for this scope.
            List<Variable> variableList = new List<Variable>();

            try
            {
                ResolveVariables(
                    variableList, 
                    responder.Arguments.VariablesReference, 
                    isLocalVariableScope, 
                    isStateVariableScope, 
                    isParentVariableScope, 
                    traceIndex,
                    parentVariableValuePair,
                    threadState);
            }
            catch (Exception ex)
            {
                LogException(ex, threadState);
            }

            // Respond with our variable list.
            responder.SetResponse(new VariablesResponse(variableList));
        }

        private void ResolveVariables(
            List<Variable> variableList,
            int variablesReference,
            bool isLocalVariableScope, 
            bool isStateVariableScope,
            bool isParentVariableScope,
            int traceIndex,
            UnderlyingVariableValuePair parentVariableValuePair,
            MeadowDebugAdapterThreadState threadState)
        { 
            // Obtain our local variables at this point in execution
            VariableValuePair[] variablePairs = Array.Empty<VariableValuePair>();
            if (isLocalVariableScope)
            {
                variablePairs = threadState.ExecutionTraceAnalysis.GetLocalVariables(traceIndex, threadState.RpcClient);
            }
            else if (isStateVariableScope)
            {
                variablePairs = threadState.ExecutionTraceAnalysis.GetStateVariables(traceIndex, threadState.RpcClient);
            }
            else if (isParentVariableScope)
            {
                // We're loading sub-variables for a variable.
                switch (parentVariableValuePair.Variable.GenericType)
                {
                    case VarGenericType.Struct:
                        {
                            // Cast our to an enumerable type.
                            variablePairs = ((IEnumerable<VariableValuePair>)parentVariableValuePair.Value).ToArray();
                            break;
                        }

                    case VarGenericType.Array:
                        {
                            // Cast our variable
                            var arrayVariable = ((VarArray)parentVariableValuePair.Variable);

                            // Cast to an object array.
                            var arrayValue = (object[])parentVariableValuePair.Value;

                            // Loop for each element
                            for (int i = 0; i < arrayValue.Length; i++)
                            {
                                // Create an underlying variable value pair for this element
                                var underlyingVariableValuePair = new UnderlyingVariableValuePair(arrayVariable.ElementObject, arrayValue[i]);

                                // Check if this is a nested variable type
                                bool nestedType = IsNestedVariableType(arrayVariable.ElementObject.GenericType);
                                int variablePairReferenceId = 0;
                                if (nestedType)
                                {
                                    // Create a new reference id for this variable if it's a nested type.
                                    variablePairReferenceId = ReferenceContainer.GetUniqueId();

                                    // Link our reference for any nested types.
                                    ReferenceContainer.LinkSubVariableReference(variablesReference, variablePairReferenceId, threadState.ThreadId, underlyingVariableValuePair);
                                }

                                // Obtain the value string for this variable and add it to our list.
                                string variableValueString = GetVariableValueString(underlyingVariableValuePair);
                                Variable variable = ReferenceContainer.CreateVariable($"[{i}]", variableValueString, variablePairReferenceId, underlyingVariableValuePair.Variable.BaseType);
                                variableList.Add(variable);
                            }


                            break;
                        }

                    case VarGenericType.ByteArrayDynamic:
                    case VarGenericType.ByteArrayFixed:
                        {
                            // Cast our to an enumerable type.
                            var bytes = (Memory<byte>)parentVariableValuePair.Value;
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                Variable variable = ReferenceContainer.CreateVariable($"[{i}]", bytes.Span[i].ToString(CultureInfo.InvariantCulture), 0, "byte");
                                variableList.Add(variable);
                            }

                            break;
                        }

                    case VarGenericType.Mapping:
                        {
                            // Obtain our mapping's key-value pairs.
                            var mappingKeyValuePairs = (MappingKeyValuePair[])parentVariableValuePair.Value;
                            variablePairs = new VariableValuePair[mappingKeyValuePairs.Length * 2];

                            // Loop for each key and value pair to add.
                            int variableIndex = 0;
                            for (int i = 0; i < mappingKeyValuePairs.Length; i++)
                            {
                                // Set our key and value in our variable value pair enumeration.
                                variablePairs[variableIndex++] = mappingKeyValuePairs[i].Key;
                                variablePairs[variableIndex++] = mappingKeyValuePairs[i].Value;
                            }

                            break;
                        }
                }
            }

            // Loop for each local variables
            foreach (VariableValuePair variablePair in variablePairs)
            {
                // Create an underlying variable value pair for this pair.
                var underlyingVariableValuePair = new UnderlyingVariableValuePair(variablePair);

                // Check if this is a nested variable type
                bool nestedType = IsNestedVariableType(variablePair.Variable.GenericType);
                int variablePairReferenceId = 0;
                if (nestedType)
                {
                    // Create a new reference id for this variable if it's a nested type.
                    variablePairReferenceId = ReferenceContainer.GetUniqueId();

                    // Link our reference for any nested types.
                    ReferenceContainer.LinkSubVariableReference(variablesReference, variablePairReferenceId, threadState.ThreadId, underlyingVariableValuePair);
                }

                // Obtain the value string for this variable and add it to our list.
                string variableValueString = GetVariableValueString(underlyingVariableValuePair);
                Variable variable = ReferenceContainer.CreateVariable(variablePair.Variable.Name, variableValueString, variablePairReferenceId, variablePair.Variable.BaseType);
                variableList.Add(variable);
            }
        }

        protected override void HandleEvaluateRequestAsync(IRequestResponder<EvaluateArguments, EvaluateResponse> responder)
        {
            // Obtain an evaluation for this variable expression.
            EvaluateResponse evalResponse = ReferenceContainer.GetVariableEvaluateResponse(responder.Arguments.Expression);

            // Set the response accordingly.
            responder.SetResponse(evalResponse ?? new EvaluateResponse());
        }

        protected override void HandleExceptionInfoRequestAsync(IRequestResponder<ExceptionInfoArguments, ExceptionInfoResponse> responder)
        {
            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState);
            if (success)
            {
                var ex = threadState.ExecutionTraceAnalysis.GetException(threadState.CurrentStepIndex.Value);

                // Get the exception call stack lines.
                var exStackTrace = threadState.ExecutionTraceAnalysis.GetCallStackString(ex.TraceIndex.Value);

                responder.SetResponse(new ExceptionInfoResponse("Error", ExceptionBreakMode.Always)
                {
                    Description = ex.Message,
                    Details = new ExceptionDetails
                    {
                        Message = ex.Message,
                        FormattedDescription = ex.Message,
                        StackTrace = exStackTrace
                    }
                });
            }
        }

        #endregion


        private string ConvertVSCodePathToInternalPath(string vsCodePath)
        {
            var relativeFilePath = vsCodePath.Substring(ConfigurationProperties.WorkspaceDirectory.Length);

            // Strip our contracts folder from our VS Code Path
            int index = relativeFilePath.IndexOf(_contractsDirectory, StringComparison.InvariantCultureIgnoreCase);
            string internalPath = relativeFilePath.Trim().Substring(index + _contractsDirectory.Length + 1);

            // Make path platform agnostic
            internalPath = internalPath.Replace('\\', '/');

            // Return our internal path.
            return internalPath;
        }

        protected override void HandleSetBreakpointsRequestAsync(IRequestResponder<SetBreakpointsArguments, SetBreakpointsResponse> responder)
        {
            // Ignore breakpoints for files that are not solidity sources
            if (!responder.Arguments.Source.Path.EndsWith(".sol", StringComparison.OrdinalIgnoreCase))
            {
                responder.SetResponse(new SetBreakpointsResponse());
                return;
            }

            if (!responder.Arguments.Source.Path.StartsWith(ConfigurationProperties.WorkspaceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Unexpected breakpoint source path: {responder.Arguments.Source.Path}, workspace: {ConfigurationProperties.WorkspaceDirectory}");
            }

            if (responder.Arguments.SourceModified.GetValueOrDefault())
            {
                throw new Exception("Debugging modified sources is not supported.");
            }


            // Obtain our internal path from a vs code path
            var relativeFilePath = ConvertVSCodePathToInternalPath(responder.Arguments.Source.Path);

            _sourceBreakpoints[relativeFilePath] = responder.Arguments.Breakpoints.Select(b => b.Line).ToArray();
            
            responder.SetResponse(new SetBreakpointsResponse());
        }

        protected override void HandleSetDebuggerPropertyRequestAsync(IRequestResponder<SetDebuggerPropertyArguments> responder)
        {
            base.HandleSetDebuggerPropertyRequestAsync(responder);
        }

        protected override void HandleLoadedSourcesRequestAsync(IRequestResponder<LoadedSourcesArguments, LoadedSourcesResponse> responder)
        {
            base.HandleLoadedSourcesRequestAsync(responder);
        }

        void LogException(Exception ex, MeadowDebugAdapterThreadState threadState)
        {
            // Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.ThreadEvent
            var msg = new StringBuilder();
            msg.AppendLine(ex.ToString());
            msg.AppendLine();
            msg.AppendLine("Solidity call stack: ");
            try
            {
                msg.AppendLine(threadState.ExecutionTraceAnalysis.GetCallStackString(threadState.CurrentStepIndex.Value));
            }
            catch (Exception callstackResolveEx)
            {
                msg.AppendLine("Exception resolving callstack: " + callstackResolveEx.ToString());
            }

            var exceptionEvent = new OutputEvent
            {
                Category = OutputEvent.CategoryValue.Stderr,
                Output = msg.ToString()
            };
            Protocol.SendEvent(exceptionEvent);
        }

        protected override void HandleProtocolError(Exception ex)
        {
            var exceptionEvent = new OutputEvent
            {
                Category = OutputEvent.CategoryValue.Stderr,
                Output = ex.ToString()
            };
            Protocol.SendEvent(exceptionEvent);
        }

        /*
        protected override ResponseBody HandleProtocolRequest(string requestType, object requestArgs)
        {
            return base.HandleProtocolRequest(requestType, requestArgs);
        }*/

        protected override void HandleRestartRequestAsync(IRequestResponder<RestartArguments> responder)
        {
            throw new NotImplementedException();
        }

        protected override void HandleTerminateThreadsRequestAsync(IRequestResponder<TerminateThreadsArguments> responder)
        {
            throw new NotImplementedException();
        }

        protected override void HandleSourceRequestAsync(IRequestResponder<SourceArguments, SourceResponse> responder)
        {
            throw new NotImplementedException();
        }

        protected override void HandleSetVariableRequestAsync(IRequestResponder<SetVariableArguments, SetVariableResponse> responder)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}