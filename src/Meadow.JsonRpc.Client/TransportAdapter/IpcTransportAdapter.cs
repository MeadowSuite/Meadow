using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.JsonRpc.Client.TransportAdapter
{
    // TODO: finish implementing and test on Windows and unix

    public class IpcTransportAdapter : ITransportAdapter
    {
        public Uri EndPoint { get; protected set; }

        NamedPipeClientStream _pipeClient;

        readonly string _pipeName;
        readonly TimeSpan _connectTimeout;

        public IpcTransportAdapter(Uri endPoint, TimeSpan connectTimeout)
        {
            _connectTimeout = connectTimeout;
            EndPoint = endPoint;

            var pipeName = endPoint.AbsoluteUri;
            switch (endPoint.Scheme.ToLowerInvariant())
            {
                case "file":
                    _pipeName = endPoint.LocalPath;
                    break;
                case "ipc":
                    _pipeName = endPoint.OriginalString.Substring("ipc://".Length);
                    break;
                default:
                    throw new ArgumentException("IPC uri should start with ipc:// or file://");
            }

            throw new NotImplementedException();
        }

        async Task EnsureConnected()
        {
            async Task<NamedPipeClientStream> ConnectPipe()
            {
                var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                try
                {
                    if (_connectTimeout.Ticks == 0)
                    {
                        await pipe.ConnectAsync();
                    }
                    else
                    {
                        await pipe.ConnectAsync((int)_connectTimeout.TotalMilliseconds);
                    }
            
                    return pipe;
                }
                catch
                {
                    pipe.Dispose();
                    throw;
                }
            }

            if (_pipeClient == null)
            {
                _pipeClient = await ConnectPipe();
            }
            else if (!_pipeClient.IsConnected)
            {
                _pipeClient.Dispose();
                _pipeClient = null;
                _pipeClient = await ConnectPipe();
            }
        }

        public async Task<JObject> Request(JObject requestObject)
        {
            await EnsureConnected();
            var msgJson = requestObject.ToString();
            var utf8Bytes = Encoding.UTF8.GetBytes(msgJson);
            await _pipeClient.WriteAsync(utf8Bytes, 0, utf8Bytes.Length);

            using (var streamReader = new StreamReader(_pipeClient, Encoding.UTF8, false, 1024, leaveOpen: true))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var readResult = await jsonReader.ReadAsync();
                Debug.Assert(readResult);
                Debug.Assert(jsonReader.TokenType == JsonToken.StartObject);
                var jObj = await JObject.LoadAsync(jsonReader);
                return jObj;
            }
        }

        public void Dispose()
        {
            _pipeClient?.Dispose();
            _pipeClient = null;
        }
    }
    
}
