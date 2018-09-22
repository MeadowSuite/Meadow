using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Meadow.CoverageReport.Debugging;

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
        #region Fields
        readonly TaskCompletionSource<object> _terminatedTcs = new TaskCompletionSource<object>();

        public readonly TaskCompletionSource<object> CompletedInitializationRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedLaunchRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedConfigurationDoneRequest = new TaskCompletionSource<object>();
        #endregion

        #region Properties
        public Task Terminated => _terminatedTcs.Task;
        public SolidityMeadowConfigurationProperties ConfigurationProperties { get; private set; }
        public Dictionary<int, MeadowDebugAdapterThreadState> ThreadStates { get; }
        #endregion

        #region Constructor
        public MeadowSolidityDebugAdapter()
        {
            // Initialize our thread state lookup.
            ThreadStates = new Dictionary<int, MeadowDebugAdapterThreadState>();
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

        public void ProcessExecutionTraceAnalysis(ExecutionTraceAnalysis traceAnalysis)
        {
            // Obtain our thread ID
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Create a thread state for this thread
            MeadowDebugAdapterThreadState threadState = new MeadowDebugAdapterThreadState(traceAnalysis, threadId);

            // Set the thread state in our lookup
            ThreadStates[threadId] = threadState;

            // Continue our execution.
            ContinueExecution(threadState, DesiredControlFlow.Continue);

            // Lock until we are allowed through.
            threadState.Semaphore.WaitOne();

            // Remove the thread from our lookup.
            ThreadStates.Remove(threadId);

            // TODO: Force thread list update here (if needed, depends on underlying architectural design of debug adapter).
        }

        private void ContinueExecution(MeadowDebugAdapterThreadState threadState, DesiredControlFlow controlFlow = DesiredControlFlow.Continue)
        {
            // Determine how to continue execution.
            bool finishedExecution = false;
            switch (controlFlow)
            {
                case DesiredControlFlow.StepInto:
                    {
                        // Increment our step
                        finishedExecution = !threadState.IncrementStep();

                        // Signal our breakpoint event has occurred for this thread.
                        Protocol.SendEvent(new StoppedEvent(StoppedEvent.ReasonValue.Step) { ThreadId = threadState.ThreadId });
                        break;
                    }

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
            threadState.Semaphore.Release();
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

                // TODO: Resolve relative path properly so it can simply be looked up.
                string relativePath = _sourceBreakpoints.Keys.First(x => x.Contains(sourceFilePath, StringComparison.InvariantCultureIgnoreCase));
                bool success = _sourceBreakpoints.TryGetValue(relativePath, out var breakpointLines);

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
                SupportsEvaluateForHovers = true,
                SupportsStepBack = true,
                SupportsStepInTargetsRequest = true,
                SupportTerminateDebuggee = false,
                SupportsRestartRequest = false,
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

        protected override StepInResponse HandleStepInRequest(StepInArguments arguments)
        {
            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepInto);
            }

            // Return our continue response
            return new StepInResponse();
        }

        protected override StepInTargetsResponse HandleStepInTargetsRequest(StepInTargetsArguments arguments)
        {
            return base.HandleStepInTargetsRequest(arguments);
        }

        protected override StepOutResponse HandleStepOutRequest(StepOutArguments arguments)
        {
            return base.HandleStepOutRequest(arguments);
        }

        protected override StepBackResponse HandleStepBackRequest(StepBackArguments arguments)
        {
            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(arguments.ThreadId, out var threadState);
            if (success)
            {
                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.StepBackwards);
            }

            // Return our continue response
            return new StepBackResponse();
        }

        protected override ContinueResponse HandleContinueRequest(ContinueArguments arguments)
        {
            // Obtain the current thread state
            bool success = ThreadStates.TryGetValue(arguments.ThreadId, out var threadState);
            if (success)
            {
                // Advance our step from our current position
                threadState.IncrementStep();

                // Continue executing
                ContinueExecution(threadState, DesiredControlFlow.Continue);
            }

            // Return our continue response
            return new ContinueResponse();
        }

        protected override ReverseContinueResponse HandleReverseContinueRequest(ReverseContinueArguments arguments)
        {
            return base.HandleReverseContinueRequest(arguments);
        }

        protected override PauseResponse HandlePauseRequest(PauseArguments arguments)
        {
            return base.HandlePauseRequest(arguments);
        }

        #endregion


        #region Threads, Scopes, Variables Requests

        protected override ThreadsResponse HandleThreadsRequest(ThreadsArguments arguments)
        {
            // Create a list of threads from our thread states.
            List<Thread> threads = ThreadStates.Values.Select(x => new Thread(x.ThreadId, $"thread_{x.ThreadId}")).ToList();
            return new ThreadsResponse(threads);
        }

        protected override StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
        {
            // Create our list of stack frames.
            List<StackFrame> stackFrames = new List<StackFrame>();

            // Verify we have a thread state for this thread, and a valid step to represent in it.
            if (ThreadStates.TryGetValue(arguments.ThreadId, out var threadState) && threadState.CurrentStepIndex.HasValue)
            {
                // Obtain the callstack
                var callstack = threadState.ExecutionTraceAnalysis.GetCallStack(threadState.CurrentStepIndex.Value);

                // Loop through our scopes.
                foreach (var scope in callstack)
                {
                    // If the scope is invalid, then we skip it.
                    if (scope.FunctionDefinition == null)
                    {
                        continue;
                    }

                    // Obtain the method name we are executing in.
                    string frameName = scope.FunctionDefinition.Name;
                    if (string.IsNullOrEmpty(frameName))
                    {
                        frameName = scope.FunctionDefinition.IsConstructor ? $".ctor ({scope.ContractDefinition.Name})" : "<N/A>";
                    }

                    var stackFrame = new StackFrame(scope.ScopeDepth, frameName, 10, 0);

                    //var fileName = _breakpointFiles[0].Name;
                    //var filePath = _breakpointFiles[0].Path;

                    //stackFrame.Source = new Source
                    //{
                    //    Name = fileName,
                    //    Path = filePath
                    //};

                    // Add our stack frame to the list
                    stackFrames.Add(stackFrame);
                }
            }

            // Return our stack frames in our response.
            return new StackTraceResponse(stackFrames);
        }

        protected override ScopesResponse HandleScopesRequest(ScopesArguments arguments)
        {
            // TODO
            var stackFrameID = arguments.FrameId;
            var scope = new Scope("Locals", 555, false);
            return new ScopesResponse(new List<Scope> { scope });
        }

        protected override VariablesResponse HandleVariablesRequest(VariablesArguments arguments)
        {
            // TODO: 
            var variablesScopeReference = arguments.VariablesReference;
            var response = new VariablesResponse(new List<Variable>
            {
                new Variable("exampleVar1", "var value 1", 5),
                new Variable("variable 2", "asdf", 6)
            });
            return response;
        }

        protected override ExceptionInfoResponse HandleExceptionInfoRequest(ExceptionInfoArguments arguments)
        {
            return base.HandleExceptionInfoRequest(arguments);
        }

        #endregion

        Dictionary<string, int[]> _sourceBreakpoints = new Dictionary<string, int[]>();

        List<(string Name, string Path)> _breakpointFiles = new List<(string Name, string Path)>();

        public bool TryGetSourceBreakpoints(string relativeFilePath, out int[] breakpointLines)
        {
            lock (_sourceBreakpoints)
            {
                return _sourceBreakpoints.TryGetValue(relativeFilePath, out breakpointLines);
            }
        }

        protected override SetBreakpointsResponse HandleSetBreakpointsRequest(SetBreakpointsArguments arguments)
        {
            _breakpointFiles.Add((arguments.Source.Name, arguments.Source.Path));

            if (!arguments.Source.Path.StartsWith(ConfigurationProperties.WorkspaceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Unexpected breakpoint source path: {arguments.Source.Path}, workspace: {ConfigurationProperties.WorkspaceDirectory}");
            }

            if (arguments.SourceModified.GetValueOrDefault())
            {
                throw new Exception("Debugging modified sources is not supported.");
            }

            var relativeFilePath = arguments.Source.Path
                .Substring(ConfigurationProperties.WorkspaceDirectory.Length)
                .TrimStart('/', '\\')
                .Replace('\\', '/');

            lock (_sourceBreakpoints)
            {
                _sourceBreakpoints[relativeFilePath] = arguments.Breakpoints.Select(b => b.Line).ToArray();
            }

            return new SetBreakpointsResponse();
        }

        protected override SetDebuggerPropertyResponse HandleSetDebuggerPropertyRequest(SetDebuggerPropertyArguments arguments)
        {
            return base.HandleSetDebuggerPropertyRequest(arguments);
        }

        protected override NextResponse HandleNextRequest(NextArguments arguments)
        {
            return base.HandleNextRequest(arguments);
        }

        protected override LoadedSourcesResponse HandleLoadedSourcesRequest(LoadedSourcesArguments arguments)
        {
            return base.HandleLoadedSourcesRequest(arguments);
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

        protected override RestartResponse HandleRestartRequest(RestartArguments arguments)
        {
            return base.HandleRestartRequest(arguments);
        }

        protected override TerminateThreadsResponse HandleTerminateThreadsRequest(TerminateThreadsArguments arguments)
        {
            return base.HandleTerminateThreadsRequest(arguments);
        }

        protected override SourceResponse HandleSourceRequest(SourceArguments arguments)
        {
            return base.HandleSourceRequest(arguments);
        }

        protected override SetVariableResponse HandleSetVariableRequest(SetVariableArguments arguments)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
