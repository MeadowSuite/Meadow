using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using System.Linq;
using Meadow.Core.Utils;
using Meadow.Core.EthTypes;
using System.Reflection;
using Meadow.JsonRpc.JsonConverters;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Globalization;
using System.Net.WebSockets;
using System.Buffers;
using System.Threading;
using System.Diagnostics;

namespace Meadow.JsonRpc.Server
{
    /// <summary>
    /// Supports http and websockets
    /// </summary>
    public class JsonRpcHttpServer : IDisposable
    {
        public IWebHost WebHost { get; }

        public string[] ServerAddresses => _serverAddressFeature().Addresses.ToArray();

        public int ServerPort => GetServerPort();
        public Uri ServerAddress => GetServerHostAddress();

        Func<IServerAddressesFeature> _serverAddressFeature;
        IRpcController _serverHandler;
        ActionBlock<RpcRequestItem> _messageQueue;
        readonly Lazy<(MethodInfo MethodInfo, RpcApiMethod RpcApiMethod)?[]> _methodInfos;

        class RpcRequestItem
        {
            public readonly JObject RequestMessage;
            public readonly TaskCompletionSource<JToken> ResponseTask;
            public RpcRequestItem(JObject requestMessage)
            {
                RequestMessage = requestMessage;
                ResponseTask = new TaskCompletionSource<JToken>();
            }
        }


        /// <param name="port">If null or unspecified the http server binds to a random port.</param>
        public JsonRpcHttpServer(IRpcController serverHandler, Func<IWebHostBuilder, IWebHostBuilder> configure = null, int? port = null, IPAddress address = null)
        {
            _serverHandler = serverHandler;
            var webHostBuilder = new WebHostBuilder()
               .Configure(app =>
               {
                   app.UseWebSockets();
                   app.Use(Handle);
                   _serverAddressFeature = () => app.ServerFeatures.Get<IServerAddressesFeature>();
               })
               .UseSockets()
               .UseKestrel(options => 
               {
                   options.Listen(address ?? IPAddress.Loopback, port ?? 0);
                   //options.Listen(port ?? 0);
               });

            if (configure != null)
            {
                webHostBuilder = configure(webHostBuilder);
            }

            WebHost = webHostBuilder.Build();

            // Setup a serialized message queue handler.
            _messageQueue = new ActionBlock<RpcRequestItem>(
                RpcRequestItemHandler,
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true,
                    MaxDegreeOfParallelism = 1
                });

            _methodInfos = new Lazy<(MethodInfo MethodInfo, RpcApiMethod RpcApiMethod)?[]>(GetRpcMethodInfos);
        }

        public void Start() => WebHost.Start();

        public Task StartAsync() => WebHost.StartAsync();

        public Task StopAsync() => WebHost.StopAsync();

        public void Stop() => WebHost.StopAsync().GetAwaiter().GetResult();
        
        Uri GetServerHostAddress()
        {
            var addrs = ServerAddresses;
            if (addrs.Length == 0)
            {
                throw new Exception("Web server has no addresses or binded ports. Has it been started?");
            }

            var httpAddr = addrs.FirstOrDefault(addr => addr.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
            return new Uri(httpAddr);
        }

        int GetServerPort()
        {
            return GetServerHostAddress().Port;
        }

        async Task Handle(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await HandleWebSocketRequest(context, next);
            }
            else if (context.Request.Method == "POST")
            {
                await HandlePostRequest(context, next);
            }
            else
            {
                await next();
                return;
            }

        }

        async Task HandleWebSocketRequest(HttpContext context, Func<Task> next)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var arrayPool = ArrayPool<byte>.Shared;
            var receiveBuffer = arrayPool.Rent(30000);
            var memoryStream = new MemoryStream();

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    memoryStream.Position = 0;
                    using (var sr = new StreamReader(memoryStream, Encoding.UTF8, false, 1024, leaveOpen: true))
                    using (var reader = new JsonTextReader(sr))
                    {
                        WebSocketReceiveResult receiveResult;
                        do
                        {
                            receiveResult = await webSocket.ReceiveAsync(
                                new ArraySegment<byte>(receiveBuffer),
                                CancellationToken.None);

                            memoryStream.Write(receiveBuffer, 0, receiveResult.Count);
                        }
                        while (!receiveResult.EndOfMessage);

                        memoryStream.Position = 0;
                        var readResult = reader.Read();
                        Debug.Assert(readResult);
                        Debug.Assert(reader.TokenType == JsonToken.StartObject);

                        var data = JObject.Load(reader);
                        var requestItem = new RpcRequestItem(data);

                        // We want RPC method handling to be serialized (synchronous)
                        // but we don't want to block the http server so post
                        // the request to an ordered async message queue.
                        _messageQueue.Post(requestItem);

                        // Wait for the queue handler to mark the response as ready.
                        var response = await requestItem.ResponseTask.Task;

                        // Format response as json data and reply to websocket request.
                        var responseJson = response.ToString();
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                        await webSocket.SendAsync(
                            new ArraySegment<byte>(responseBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                memoryStream.Dispose();
                arrayPool.Return(receiveBuffer);
            }
        }

        async Task HandlePostRequest(HttpContext context, Func<Task> next)
        {
            JObject data;

            // Read http request post data as string then parse as json
            using (var streamReader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                var postData = await streamReader.ReadToEndAsync();
                data = JObject.Parse(postData);
            }

            var requestItem = new RpcRequestItem(data);

            // We want RPC method handling to be serialized (synchronous)
            // but we don't want to block the http server so post
            // the request to an ordered async message queue.
            _messageQueue.Post(requestItem);

            // Wait for the queue handler to mark the response as ready.
            var response = await requestItem.ResponseTask.Task;

            // Format response as json data and reply to http request.
            var responseJson = response.ToString();
            await context.Response.WriteAsync(responseJson);
        }

        async Task RpcRequestItemHandler(RpcRequestItem item)
        {
            try
            {
                await ProcessServerHandler(item);
            }
            catch (Exception ex)
            {
                item.ResponseTask.SetException(ex);
                // TODO: some error logging and reporting
            }
        }

        class ReflectedRpcMethodInfo
        {
            public MethodInfo MethodInfo;
            public ParameterInfo[] ParamTypes;
            public Func<object[], Task> Invoke;
        }

        ConcurrentDictionary<RpcApiMethod, ReflectedRpcMethodInfo> _reflectedRpcMethodCache = new ConcurrentDictionary<RpcApiMethod, ReflectedRpcMethodInfo>();

        (MethodInfo MethodInfo, RpcApiMethod RpcApiMethod)?[] GetRpcMethodInfos()
        {
            var methodAttrs = _serverHandler
                .GetType()
                .GetInterfaces()
                .Concat(new[] { _serverHandler.GetType() })
                .Distinct()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Select<MethodInfo, (MethodInfo m, RpcApiMethodAttribute attr)>(m => (m, m.GetCustomAttribute<RpcApiMethodAttribute>(inherit: true)))
                .Where(a => a.attr != null)
                .Select(a => new ValueTuple<MethodInfo, RpcApiMethod>?((a.m, a.attr.Method)))
                .ToArray();

            return methodAttrs;
        }

        ReflectedRpcMethodInfo GetReflectedRpcMethod(RpcApiMethod rpcApiMethod)
        {
            MethodInfo serverMethod = _methodInfos.Value
                .SingleOrDefault(m => m.Value.RpcApiMethod == rpcApiMethod)
                ?.MethodInfo;

            if (serverMethod == null)
            {
                return null;
            }

            ParameterInfo[] paramTypes = serverMethod.GetParameters();

            var funcGenericTypes = paramTypes.Select(p => p.ParameterType).Concat(new[] { serverMethod.ReturnType }).ToArray();
            var methodDelegateType = Expression.GetDelegateType(funcGenericTypes);
            var methodDelegate = Delegate.CreateDelegate(methodDelegateType, _serverHandler, serverMethod, throwOnBindFailure: true);

            var fastInvoke = GetFastInvokeDelegate(methodDelegate, paramTypes.Length);

            return new ReflectedRpcMethodInfo { MethodInfo = serverMethod, ParamTypes = paramTypes, Invoke = fastInvoke };
        }

        static Func<object[], Task> GetFastInvokeDelegate(Delegate del, int paramCount)
        {
            dynamic dynDel = del;
            switch (paramCount)
            {
                case 0:
                    return args => dynDel();
                case 1:
                    return args => dynDel((dynamic)args[0]);
                case 2:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1]);
                case 3:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2]);
                case 4:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3]);
                case 5:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4]);
                case 6:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5]);
                case 7:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6]);
                case 8:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7]);
                case 9:
                    return args => dynDel((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7], (dynamic)args[8]);
                default:
                    throw new NotImplementedException("Does not support " + paramCount + " arguments");
            }
        }

        // Validate json rpc request, parse rpc method params,
        // match method string to the IRpcServerHandler method,
        // invoke the server handler, json serialize response.
        async Task ProcessServerHandler(RpcRequestItem item)
        {
            if (!item.RequestMessage.TryGetValue("jsonrpc", out var rpcVer))
            {
                throw new Exception("Invalid jsonrpc message");
            }

            if (rpcVer.Value<string>() != "2.0")
            {
                throw new Exception("Invalid jsonrpc version: " + rpcVer);
            }

            long id = item.RequestMessage.Value<long>("id");
            var response = new JObject();
            response["id"] = id;
            response["jsonrpc"] = "2.0";

            try
            {
                string rpcMethodString = item.RequestMessage.Value<string>("method");
                JToken[] methodJsonParams = item.RequestMessage["params"].ToArray();
                JToken result = null;

                RpcApiMethod rpcMethod = RpcApiMethods.Create(rpcMethodString);

                var reflectedInfo = _reflectedRpcMethodCache.GetOrAdd(rpcMethod, GetReflectedRpcMethod);

                if (reflectedInfo == null)
                {
                    throw new Exception("Unsupported rpc method: " + rpcMethodString);
                }

                object[] paramObjects = new object[reflectedInfo.ParamTypes.Length];

                if (paramObjects.Length != methodJsonParams.Length)
                {
                    throw new Exception($"Parameter count mismatch. Server expected {paramObjects.Length} params but RPC request contained {methodJsonParams.Length}");
                }

                // Convert json-rpc encoded params into C# types.
                // Usually primitive eth types like int, address, hash, etc.. are hex encoded.
                // Json object types are deserialized to a corresponding C# class.
                // Some types are regular json bools and ints. 
                for (var i = 0; i < paramObjects.Length; i++)
                {
                    paramObjects[i] = methodJsonParams[i].ToObject(reflectedInfo.ParamTypes[i].ParameterType, JsonRpcSerializer.Serializer);
                }

                // Invoke the method.
                Task methodTask = reflectedInfo.Invoke(paramObjects);

                // Check if task has exception
                if (methodTask.IsFaulted)
                {
                    response["error"] = GetJsonRpcErrorFromException(methodTask.Exception);
                }
                else
                {
                    // Check if Task is successfully completed then skip the await.
                    // If the task has faulted then await to trigger the exception.
                    if (methodTask.Status != TaskStatus.RanToCompletion)
                    {
                        await methodTask;
                    }

                    // if type is Task<T> then extract the result
                    if (reflectedInfo.MethodInfo.ReturnType.IsGenericType)
                    {
                        // Use dynamic keyword which is the highest performance method
                        // to access a property on a generic type that is only known
                        // during runtime (compared to reflection or messy IL gen).
                        dynamic taskDynamic = methodTask;
                        object taskValue = taskDynamic.Result;
                        if (taskValue == null)
                        {
                            result = null;
                        }
                        else
                        {
                            result = JToken.FromObject(taskValue, JsonRpcSerializer.Serializer);
                        }
                    }

                    response["result"] = result;
                }
            }
            catch (JsonRpcErrorException rpcEx)
            {
                response["error"] = JObject.FromObject(rpcEx.Error);
            }
            catch (Exception ex)
            {
                response["error"] = GetJsonRpcErrorFromException(ex);
            }

            item.ResponseTask.SetResult(response);
        }

        JObject GetJsonRpcErrorFromException(Exception ex)
        {
            // If exception is an AggregateException with a single entry then unwrap it
            if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
            {
                ex = aggEx.InnerException;
            }

            var jsonRpcException = new JsonRpcErrorException(JsonRpcErrorCode.ServerErrorStart, ex.Message, ex).Error;
            return JObject.FromObject(jsonRpcException);
        }

        public void Dispose()
        {
            WebHost.Dispose();
        }
    }
}
