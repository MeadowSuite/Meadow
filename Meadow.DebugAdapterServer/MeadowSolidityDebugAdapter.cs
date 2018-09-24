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
        private const string CONTRACTS_PREFIX = "Contracts\\";
        private const int SIMULTANEOUS_TRACE_COUNT = 1;
        #endregion

        #region Fields
        readonly TaskCompletionSource<object> _terminatedTcs = new TaskCompletionSource<object>();

        public readonly TaskCompletionSource<object> CompletedInitializationRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedLaunchRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedConfigurationDoneRequest = new TaskCompletionSource<object>();
        private System.Threading.SemaphoreSlim _processTraceSemaphore = new System.Threading.SemaphoreSlim(SIMULTANEOUS_TRACE_COUNT, SIMULTANEOUS_TRACE_COUNT);
        #endregion

        #region Properties
        public Task Terminated => _terminatedTcs.Task;
        public SolidityMeadowConfigurationProperties ConfigurationProperties { get; private set; }
        public ConcurrentDictionary<int, MeadowDebugAdapterThreadState> ThreadStates { get; }
        #endregion

        #region Constructor
        public MeadowSolidityDebugAdapter()
        {
            // Initialize our thread state lookup.
            ThreadStates = new ConcurrentDictionary<int, MeadowDebugAdapterThreadState>();
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

        public async Task ProcessExecutionTraceAnalysis(ExecutionTraceAnalysis traceAnalysis)
        {
            // Obtain our thread ID
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Create a thread state for this thread
            MeadowDebugAdapterThreadState threadState = new MeadowDebugAdapterThreadState(traceAnalysis, threadId);

            // Acquire the semaphore for processing a trace.
            await _processTraceSemaphore.WaitAsync();

            // Set the thread state in our lookup
            ThreadStates[threadId] = threadState;

            // Send an event that our thread has exited.
            Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Started, threadState.ThreadId));

            // Continue our execution.
            ContinueExecution(threadState, DesiredControlFlow.Continue);

            // Lock execution is complete.
            await threadState.Semaphore.WaitAsync();

            // Remove the thread from our lookup.
            ThreadStates.Remove(threadId, out _);

            // Send an event that our thread has exited.
            Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Exited, threadState.ThreadId));

            // Release the semaphore for processing a trace.
            _processTraceSemaphore.Release();
        }

        private void ContinueExecution(MeadowDebugAdapterThreadState threadState, DesiredControlFlow controlFlow = DesiredControlFlow.Continue)
        {
            // Determine how to continue execution.
            bool finishedExecution = false;
            switch (controlFlow)
            {
                case DesiredControlFlow.StepOver:
                // TODO: Implement

                case DesiredControlFlow.StepInto:
                    {
                        // Increment our step
                        finishedExecution = !threadState.IncrementStep();

                        // Signal our breakpoint event has occurred for this thread.
                        Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Step) { ThreadId = threadState.ThreadId });
                        break;
                    }

                case DesiredControlFlow.StepOut:
                // TODO: Implement

                case DesiredControlFlow.StepBackwards:
                    {
                        // Decrement our step
                        threadState.DecrementStep();

                        // Signal our breakpoint event has occurred for this thread.
                        Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Step) { ThreadId = threadState.ThreadId });

                        // TODO: Check if we couldn't decrement step. Disable step backward if we can.
                        break;
                    }

                case DesiredControlFlow.Continue:
                    {
                        // Process the execution trace analysis
                        while (threadState.CurrentStepIndex.HasValue)
                        {
                            // Obtain our breakpoints.
                            bool hitBreakpoint = CheckBreakpointExists(threadState);

                            // If we hit a breakpoint, we can signal our breakpoint and exit this execution method.
                            if (hitBreakpoint)
                            {
                                // Signal our breakpoint event has occurred for this thread.
                                Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Breakpoint) { ThreadId = threadState.ThreadId });
                                return;
                            }

                            // Increment our position
                            bool successfulStep = threadState.IncrementStep();

                            // If we couldn't step, break out of our loop
                            if (!successfulStep)
                            {
                                break;
                            }
                        }

                        // If we exited this way, our execution has concluded because we could not step any further (or there were never any steps).
                        finishedExecution = true;
                        break;
                    }
            }

            // If we finished execution, signal our thread
            if (finishedExecution)
            {
                threadState.Semaphore.Release();
            }
        }

        private bool CheckBreakpointExists(MeadowDebugAdapterThreadState threadState)
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
                var sourceFilePath = sourceLine.SourceFileMapParent?.SourceFileName;

                // Resolve relative path properly so it can simply be looked up.
                bool success = _sourceBreakpoints.TryGetValue(sourceFilePath, out var breakpointLines);

                // If we have a breakpoint at this line number..
                bool containsBreakpoint = success && breakpointLines.Any(x => x == sourceLine.LineNumber);
                if (containsBreakpoint)
                {
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
                SupportsEvaluateForHovers = false,
                SupportsStepBack = true,
                SupportsStepInTargetsRequest = true,
                SupportTerminateDebuggee = false,
                SupportsRestartRequest = false,
                SupportsRestartFrame = false,
                SupportedChecksumAlgorithms = new List<ChecksumAlgorithm> { ChecksumAlgorithm.SHA256 }
            };

            responder.SetResponse(response);
            Protocol.SendEvent(new InitializedEvent());
            CompletedInitializationRequest.SetResult(null);
        }

        protected override void HandleAttachRequestAsync(IRequestResponder<AttachArguments> responder)
        {
            ConfigurationProperties = JObject.FromObject(responder.Arguments.ConfigurationProperties).ToObject<SolidityMeadowConfigurationProperties>();
            responder.SetResponse(new AttachResponse());
            CompletedLaunchRequest.SetResult(null);
        }

        protected override void HandleLaunchRequestAsync(IRequestResponder<LaunchArguments> responder)
        {
            ConfigurationProperties = JObject.FromObject(responder.Arguments.ConfigurationProperties).ToObject<SolidityMeadowConfigurationProperties>();
            responder.SetResponse(new LaunchResponse());
            CompletedLaunchRequest.SetResult(null);
        }

        protected override void HandleConfigurationDoneRequestAsync(IRequestResponder<ConfigurationDoneArguments> responder)
        {
            responder.SetResponse(new ConfigurationDoneResponse());
            CompletedConfigurationDoneRequest.SetResult(null);
        }

        protected override void HandleDisconnectRequestAsync(IRequestResponder<DisconnectArguments> responder)
        {
            responder.SetResponse(new DisconnectResponse());
            Protocol.SendEvent(new TerminatedEvent());
            Protocol.SendEvent(new ExitedEvent(0));
            _terminatedTcs.TrySetResult(null);
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
            responder.SetResponse(new StepOutResponse());
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
                // Advance our step from our current position
                threadState.IncrementStep();

                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.Continue);
            }
        }

        protected override void HandleReverseContinueRequestAsync(IRequestResponder<ReverseContinueArguments> responder)
        {
            base.HandleReverseContinueRequestAsync(responder);
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
            // Create our list of stack frames.
            List<StackFrame> stackFrames = new List<StackFrame>();

            // Verify we have a thread state for this thread, and a valid step to represent in it.
            if (ThreadStates.TryGetValue(responder.Arguments.ThreadId, out var threadState) && threadState.CurrentStepIndex.HasValue)
            {
                // Obtain the callstack
                var callstack = threadState.ExecutionTraceAnalysis.GetCallStack(threadState.CurrentStepIndex.Value);

                // Loop through our scopes.
                for (int i = 0; i < callstack.Length; i++)
                {
                    // Grab our current call frame
                    var currentCallFrame = callstack[i];

                    // If the scope is invalid, then we skip it.
                    if (currentCallFrame.FunctionDefinition == null)
                    {
                        continue;
                    }

                    // We obtain our relevant source lines for this call stack frame.
                    SourceFileLine[] lines = null;

                    // If it's the most recent call, we obtain the line for the current trace.
                    if (i == 0)
                    {
                        lines = threadState.ExecutionTraceAnalysis.GetSourceLines(threadState.CurrentStepIndex.Value);
                    }
                    else
                    {
                        // If it's not the most recent call, we obtain the position at our 
                        var previousCallFrame = callstack[i - 1];
                        if (previousCallFrame.ParentFunctionCall != null)
                        {
                            lines = threadState.ExecutionTraceAnalysis.GetSourceLines(previousCallFrame.ParentFunctionCall);
                        }
                        else
                        {
                            throw new Exception("TODO: Stack Trace could not be generated because previous call frame's function call could not be resolved. Update behavior in this case.");
                        }
                    }

                    // Obtain the method name we are executing in.
                    string frameName = currentCallFrame.FunctionDefinition.Name;
                    if (string.IsNullOrEmpty(frameName))
                    {
                        frameName = currentCallFrame.FunctionDefinition.IsConstructor ? $".ctor ({currentCallFrame.ContractDefinition.Name})" : "<unresolved>";
                    }

                    // Determine the bounds of our stack frame.
                    int startLine = 0;
                    int startColumn = 0;
                    int endLine = 0;
                    int endColumn = 0;

                    // Loop through all of our lines for this position.
                    for (int x = 0; x < lines.Length; x++)
                    {
                        // Obtain our indexed line.
                        SourceFileLine line = lines[x];

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

                    // Create our source object
                    Source stackFrameSource = new Source()
                    {
                        Name = lines[0].SourceFileMapParent.SourceFileName,
                        Path = Path.Join(ConfigurationProperties.WorkspaceDirectory, CONTRACTS_PREFIX + lines[0].SourceFileMapParent.SourceFilePath)
                    };

                    var stackFrame = new StackFrame()
                    {
                        Id = currentCallFrame.ScopeDepth,
                        Name = frameName,
                        Line = startLine,
                        Column = startColumn,
                        Source = stackFrameSource,
                        EndLine = endLine,
                        EndColumn = endColumn
                    };
                    
                    // Add our stack frame to the list
                    stackFrames.Add(stackFrame);
                }

            }

            // Return our stack frames in our response.
            responder.SetResponse(new StackTraceResponse(stackFrames));
        }

        protected override void HandleScopesRequestAsync(IRequestResponder<ScopesArguments, ScopesResponse> responder)
        {
            // TODO
            List<Scope> scopeList = new List<Scope>();
            var stackFrameID = responder.Arguments.FrameId;
            scopeList.Add(new Scope("State Variables", 444, false));
            scopeList.Add(new Scope("Local Variables", 555, false));
            
            responder.SetResponse(new ScopesResponse(scopeList));
        }

        protected override void HandleVariablesRequestAsync(IRequestResponder<VariablesArguments, VariablesResponse> responder)
        {
            // TODO: Obtain the thread id for this scope.
            int threadId = -1;

            // Using our trace index, obtain our thread state
            ThreadStates.TryGetValue(threadId, out var threadState);

            // Verify the thread state is valid
            if (threadState == null)
            {
                responder.SetResponse(new VariablesResponse());
                return;
            }

            // Obtain the trace index for this scope.
            int traceIndex = -1;
            List<Variable> variableList = new List<Variable>();
            
            bool isLocalVariable = responder.Arguments.VariablesReference == 555;
            if (isLocalVariable)
            {
                // Obtain our local variables at this point in execution
                var localVariables = threadState.ExecutionTraceAnalysis.GetLocalVariables(traceIndex);

                // Loop for each local variables
                foreach (var localVariable in localVariables)
                {
                    // Temporary: Some values will need to be structured. For now, they're not.
                    variableList.Add(new Variable(localVariable.variable.Name, localVariable.value?.ToString() ?? "<unresolved>", 0));
                }
            }
            else
            {
                // Obtain our state variables
                var stateVariables = threadState.ExecutionTraceAnalysis.GetStateVariables(traceIndex);

                // Loop for each state variables
                foreach (var stateVariable in stateVariables)
                {
                    // Temporary: Some values will need to be structured. For now, they're not.
                    variableList.Add(new Variable(stateVariable.variable.Name, stateVariable.value?.ToString() ?? "<unresolved>", 0));
                }
            }

            // Respond with our variable list.
            responder.SetResponse(new VariablesResponse(variableList));
        }

        protected override void HandleEvaluateRequestAsync(IRequestResponder<EvaluateArguments, EvaluateResponse> responder)
        {
            responder.SetResponse(new EvaluateResponse());
        }

        protected override void HandleExceptionInfoRequestAsync(IRequestResponder<ExceptionInfoArguments, ExceptionInfoResponse> responder)
        {
            base.HandleExceptionInfoRequestAsync(responder);
        }

        #endregion

        Dictionary<string, int[]> _sourceBreakpoints = new Dictionary<string, int[]>();

        List<(string Name, string Path)> _breakpointFiles = new List<(string Name, string Path)>();

        public bool TryGetSourceBreakpoints(string relativeFilePath, out int[] breakpointLines)
        {
            // Obtain our internal path from a vs code path
            relativeFilePath = ConvertVSCodePathToInternalPath(relativeFilePath);

            lock (_sourceBreakpoints)
            {
                return _sourceBreakpoints.TryGetValue(relativeFilePath, out breakpointLines);
            }
        }

        private string ConvertVSCodePathToInternalPath(string vsCodePath)
        {
            // Strip our contracts folder from our VS Code Path
            int index = vsCodePath.IndexOf(CONTRACTS_PREFIX, StringComparison.InvariantCultureIgnoreCase);
            string internalPath = vsCodePath.Trim().Substring(index + CONTRACTS_PREFIX.Length + 1);


            // Return our internal path.
            return internalPath;
        }

        protected override void HandleSetBreakpointsRequestAsync(IRequestResponder<SetBreakpointsArguments, SetBreakpointsResponse> responder)
        {
            _breakpointFiles.Add((responder.Arguments.Source.Name, responder.Arguments.Source.Path));

            if (!responder.Arguments.Source.Path.StartsWith(ConfigurationProperties.WorkspaceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Unexpected breakpoint source path: {responder.Arguments.Source.Path}, workspace: {ConfigurationProperties.WorkspaceDirectory}");
            }

            if (responder.Arguments.SourceModified.GetValueOrDefault())
            {
                throw new Exception("Debugging modified sources is not supported.");
            }

            var relativeFilePath = responder.Arguments.Source.Path
                .Substring(ConfigurationProperties.WorkspaceDirectory.Length)
                .TrimStart('/', '\\')
                .Replace('\\', '/');

            // Obtain our internal path from a vs code path
            relativeFilePath = ConvertVSCodePathToInternalPath(relativeFilePath);

            lock (_sourceBreakpoints)
            {
                _sourceBreakpoints[relativeFilePath] = responder.Arguments.Breakpoints.Select(b => b.Line).ToArray();
            }

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

        protected override void HandleProtocolError(Exception ex)
        {
            base.HandleProtocolError(ex);
        }

        /*
        protected override ResponseBody HandleProtocolRequest(string requestType, object requestArgs)
        {
            return base.HandleProtocolRequest(requestType, requestArgs);
        }*/

        protected override void HandleRestartRequestAsync(IRequestResponder<RestartArguments> responder)
        {
            base.HandleRestartRequestAsync(responder);
        }

        protected override void HandleTerminateThreadsRequestAsync(IRequestResponder<TerminateThreadsArguments> responder)
        {
            base.HandleTerminateThreadsRequestAsync(responder);
        }

        protected override void HandleSourceRequestAsync(IRequestResponder<SourceArguments, SourceResponse> responder)
        {
            base.HandleSourceRequestAsync(responder);
        }

        protected override void HandleSetVariableRequestAsync(IRequestResponder<SetVariableArguments, SetVariableResponse> responder)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}