using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
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
            EndPoint = endPoint;
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

            var arrayPool = ArrayPool<byte>.Shared;
            var receiveBuffer = arrayPool.Rent(30000);
            try
            {
                using (var memoryStream = new MemoryStream())
                using (var sr = new StreamReader(memoryStream, Encoding.UTF8))
                using (var reader = new JsonTextReader(sr))
                {
                    WebSocketReceiveResult response;
                    do
                    {
                        response = await _client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        memoryStream.Write(receiveBuffer, 0, response.Count);
                    }
                    while (!response.EndOfMessage);

                    memoryStream.Position = 0;
                    var readResult = reader.Read();
                    Debug.Assert(readResult);
                    Debug.Assert(reader.TokenType == JsonToken.StartObject);
                    var jObj = JObject.Load(reader);
                    return jObj;
                }
            }
            finally
            {
                arrayPool.Return(receiveBuffer);
            }
        }

        public void Dispose()
        {
            if (_client != null)
            {
                var clientCopy = _client;
                _client = null;

                var closeTask = clientCopy.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "close",
                    new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

                closeTask.ContinueWith(t => clientCopy.Dispose());
            }

        }
    }
}
