using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.JsonRpc.Client.TransportAdapter
{    
    // TODO: finish implementing and test

    public class WebSocketTransportAdapter : ITransportAdapter
    {
        public Uri EndPoint { get; protected set; }

        readonly TimeSpan _connectTimeout;

        ClientWebSocket _client;

        public WebSocketTransportAdapter(Uri endPoint, TimeSpan connectTimeout = default)
        {
            EndPoint = EndPoint;
            _connectTimeout = connectTimeout;

            switch (EndPoint.Scheme.ToLowerInvariant())
            {
                case "ws":
                case "wss":
                    break;
                default:
                    throw new ArgumentException("Websocket endpoint URI should start with ws:// or wss://");
            }
        }

        async Task EnsureConnected()
        {
            async Task<ClientWebSocket> Connect()
            {
                var client = new ClientWebSocket();
                CancellationToken cancelToken;
                if (_connectTimeout.Ticks == 0)
                {
                    cancelToken = CancellationToken.None;
                }
                else
                {
                    cancelToken = new CancellationTokenSource(_connectTimeout).Token;
                }

                try
                {
                    await client.ConnectAsync(EndPoint, cancelToken);
                    return client;
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }

            if (_client == null)
            {
                _client = await Connect();
            }
            else if (_client.State != WebSocketState.Open)
            {
                _client.Dispose();
                _client = null;
                _client = await Connect();
            }
        }

        public async Task<JObject> Request(JObject requestObject)
        {
            // TODO: optimize; use Span and/or array pools to reduce all the copies and heap allocations here

            await EnsureConnected();
            var msgJson = requestObject.ToString();
            var msgBytes = Encoding.UTF8.GetBytes(msgJson);
            var arrSegment = new ArraySegment<byte>(msgBytes);
            await _client.SendAsync(arrSegment, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

            var receiveBuffer = new ArraySegment<byte>(new byte[1024]);
            using (var memoryStream = new MemoryStream())
            {
                WebSocketReceiveResult response;
                do
                {
                    response = await _client.ReceiveAsync(receiveBuffer, CancellationToken.None);
                    memoryStream.Write(receiveBuffer.Array, 0, response.Count);
                }
                while (!response.EndOfMessage);

                memoryStream.Position = 0;
                using (var sr = new StreamReader(memoryStream, Encoding.UTF8))
                using (var reader = new JsonTextReader(sr))
                {
                    var readResult = reader.Read();
                    Debug.Assert(readResult);
                    Debug.Assert(reader.TokenType == JsonToken.StartObject);
                    var jObj = JObject.Load(reader);
                    return jObj;
                }
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }
}
