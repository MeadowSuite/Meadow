using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

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
        readonly TaskCompletionSource<object> _terminatedTcs = new TaskCompletionSource<object>();

        public readonly TaskCompletionSource<object> CompletedInitializationRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedLaunchRequest = new TaskCompletionSource<object>();
        public readonly TaskCompletionSource<object> CompletedConfigurationDoneRequest = new TaskCompletionSource<object>();

        public Task Terminated => _terminatedTcs.Task;

        public SolidityMeadowConfigurationProperties ConfigurationProperties { get; private set; }

        public MeadowSolidityDebugAdapter()
        {

        }

        public void InitializeStream(Stream input, Stream output)
        {
            InitializeProtocolClient(input, output);    

            Protocol.RegisterRequestType<ExampleCustomRequestWithResponse, ExampleRequestArgs, ExampleRequestResponse>(r =>
            {
                r.SetResponse(new ExampleRequestResponse { Response = "good" });
            });
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
            return base.HandleStepInRequest(arguments);
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
            return base.HandleStepBackRequest(arguments);
        }

        protected override ContinueResponse HandleContinueRequest(ContinueArguments arguments)
        {
            return base.HandleContinueRequest(arguments);
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
            var threadID = 1;
            var threadName = "some thread name";
            var thread = new Thread(threadID, threadName);
            return new ThreadsResponse(new List<Thread> { thread });
        }

        protected override StackTraceResponse HandleStackTraceRequest(StackTraceArguments arguments)
        {
            // TODO
            var stackFrameID = 0;
            var line = 10;
            var stackFrame = new StackFrame(stackFrameID, "example method name", line, 0);

            var fileName = _breakpointFiles[0].Name;
            var filePath = _breakpointFiles[0].Path;

            stackFrame.Source = new Source
            {
                Name = fileName,
                Path = filePath
            };
            return new StackTraceResponse(new List<StackFrame> { stackFrame });
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
            // TODO
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

    }
}
