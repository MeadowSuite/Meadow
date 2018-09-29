using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Security.Cryptography;
using McMaster.Extensions.CommandLineUtils;
using System.Diagnostics;
using Pipelines.Sockets.Unofficial;
using System.IO.Pipelines;
using System.Buffers.Text;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using System.Collections.Generic;

namespace Meadow.DebugAdapterProxy
{

    public class ProcessArgs
    {
        [Option("--log_file", "File to log message to.", CommandOptionType.SingleValue)]
        public string LogFile { get; }

        [Option("--trace", "Enable verbose logging to file", CommandOptionType.NoValue)]
        public bool Trace { get; }

        [Option("--attach_debugger", "Should launch/attach to debugger on start", CommandOptionType.NoValue)]
        public bool AttachDebugger { get; }

        [Option("--session", "The debug session ID to use for IPC", CommandOptionType.SingleValue)]
        public string Session { get; }

        [Option("--vscode_debug", "Is started from VSCode.", CommandOptionType.NoValue)]
        public bool VSCodeDebug { get; set; }

        public static ProcessArgs Parse(string[] args)
        {
            var app = new CommandLineApplication<ProcessArgs>(throwOnUnexpectedArg: true);
            app.Conventions.UseDefaultConventions();
            app.Parse(args);
            return app.Model;
        }
    }

    class Program
    {
        static ProcessArgs _args;

        static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static FileLogger _logger;

        static async Task Main(string[] args)
        {
            _args = ProcessArgs.Parse(args);

            if (_args.AttachDebugger)
            {
                if (!Debugger.IsAttached)
                {
                    Debugger.Launch();
                }
            }

            if (_args.Trace && !string.IsNullOrWhiteSpace(_args.LogFile))
            {
                _logger = new FileLogger(_args.LogFile);
            }

            _logger?.Log($"Started: " + string.Join("; ", args));

            // create a unique named pipe from the environment params provided by vscode for this debug session
            var pipeName = _args.Session;


            try
            {
                await StartIpcProxyClient(pipeName, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger?.Log("Exception while proxying between stdin/stdout and named pipes: " + ex);
            }

            _logger?.Log("Debug adpater exiting");
            _logger?.StopWait();
            _cancellationTokenSource.Cancel();
        }

        static async Task StartIpcProxyClient(string pipeName, CancellationToken cancellationToken)
        {
            // forward stdin/stdout from this process to a named pipe server
            using (var pipeClient = NamedPipes.CreatePipeClient(pipeName))
            {
                _logger?.Log("Connecting to debug server pipe...");
                await pipeClient.ConnectAsync(cancellationToken);
                _logger?.Log("Connected to debug server pipe");

                using (var stdIn = Console.OpenStandardInput())
                using (var stdOut = Console.OpenStandardOutput())
                {
                    // If verbose logging is enabled, setup interception streams for both the input & output of the debugger
                    // protocol stream. 
                    List<Task> streamTasks = new List<Task>();
                    Stream proxyStreamInput = null;
                    Stream proxyStreamOutput = null;
                    if (_args.Trace)
                    {
                        var proxyPipeInput = new Pipe();
                        proxyStreamInput = StreamConnection.GetDuplex(proxyPipeInput.Reader, proxyPipeInput.Writer);

                        var proxyPipeOutput = new Pipe();
                        proxyStreamOutput = StreamConnection.GetDuplex(proxyPipeOutput.Reader, proxyPipeOutput.Writer);

                        var verboseProxyLogTask = Task.WhenAll(
                            CreateVerboseProxyLogger("-> ", proxyPipeInput.Reader, cancellationToken),
                            CreateVerboseProxyLogger("<- ", proxyPipeOutput.Reader, cancellationToken));
                        streamTasks.Add(verboseProxyLogTask);
                    }

                    var pipeInputTask = NamedPipes.ConnectStream(stdIn, pipeClient, proxyStreamInput, cancellationToken);
                    var pipeOutputTask = NamedPipes.ConnectStream(pipeClient, stdOut, proxyStreamOutput, cancellationToken);

                    var checkClosedTask = Task.Run(() => 
                    {
                        while (pipeClient.IsConnected)
                        {
                            Thread.Sleep(100);
                        }
                    });

                    streamTasks.AddRange(new[] { checkClosedTask, pipeInputTask, pipeOutputTask });

                    var completedTask = await Task.WhenAny(streamTasks.ToArray());

                    if (completedTask.IsFaulted)
                    {
                        _logger?.Log("Exception while proxying between stdin/stdout and named pipes: " + completedTask.Exception.InnerException);
                    }

                    _logger?.Log("Debug server pipe connection ended");
                }
            }
        }
        

        static async Task CreateVerboseProxyLogger(string msgPrefix, PipeReader reader, CancellationToken cancellationToken)
        {
            while (true)
            {
                var msg = await GetNextMessage(reader, cancellationToken);
                _logger?.Log(msgPrefix + msg);
            }
        }

        static async ValueTask<string> GetNextMessage(PipeReader reader, CancellationToken cancellationToken)
        {
            while (true)
            {
                var read = await reader.ReadAsync(cancellationToken);
                if (read.IsCanceled)
                {
                    throw new OperationCanceledException();
                }

                // can we find a complete frame?
                var buffer = read.Buffer;
                if (TryParseFrame(
                    buffer,
                    out string nextMessage,
                    out SequencePosition consumedTo))
                {
                    reader.AdvanceTo(consumedTo);
                    return nextMessage;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (read.IsCompleted)
                {
                    throw new OperationCanceledException();
                }
            }
        }

        private static bool TryParseFrame(ReadOnlySequence<byte> buffer, out string nextMessage, out SequencePosition consumedTo)
        {
            // "Content-Length: \r\n\r\n".Length;
            const int PREFIX_LEN = 20;

            // find the end-of-line marker
            var eol = buffer.PositionOf((byte)'\r');
            if (eol == null)
            {
                nextMessage = default;
                consumedTo = default;
                return false;
            }

            // Parse the length string
            // "Content-Length: {len}\r\n\r\n"
            var lengthSequence = buffer.Slice(buffer.PositionOf((byte)' ').Value, eol.Value);
            Span<byte> lengthBytes = stackalloc byte[(int)lengthSequence.Length];
            lengthSequence.CopyTo(lengthBytes);

            var lengthInt = int.Parse(Encoding.UTF8.GetString(lengthBytes), CultureInfo.InvariantCulture);

            // TODO: fix fast utf8->uint parsing
            //if (!Utf8Parser.TryParse(lengthBytes, out uint xlengthInt, out var lengthParseBytesConsumed, standardFormat: 'D'))
            //{
            //    throw new Exception("failed");
            //}

            int fullPrefixLength = PREFIX_LEN + (int)lengthSequence.Length;
            int fullMessageLength = fullPrefixLength + lengthInt;

            if (buffer.Length < fullMessageLength)
            {
                nextMessage = default;
                consumedTo = default;
                return false;
            }

            // read past the message
            consumedTo = buffer.GetPosition(fullMessageLength - 1);

            // consume the data
            var payload = buffer.Slice(fullPrefixLength - 1, lengthInt);
            nextMessage = GetString(payload);
            return true;
        }

        static string GetString(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(buffer.First.Span);
            }

            return string.Create((int)buffer.Length, buffer, (span, sequence) =>
            {
                foreach (var segment in sequence)
                {
                    Encoding.UTF8.GetChars(segment.Span, span);
                    span = span.Slice(segment.Length);
                }
            });
        }



    }
}
