using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Meadow.JsonRpc
{
    public class JsonRpcError
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// Properties not deserialized any members
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; }

        public JsonRpcErrorException ToException() => new JsonRpcErrorException(this);
    }

    public class JsonRpcErrorException : Exception
    {
        public long Code => Error.Code;
        public readonly JsonRpcError Error;

        public readonly string ServerData;

        public JsonRpcErrorException(JsonRpcError err) : base(err.Message)
        {
            Error = err;
            if (err.Data is JObject dataDict)
            {
                ServerData = dataDict.ToString(Formatting.Indented);
                // Populate this exception Data dictionary with entries from the RPC data object.
                foreach (var entry in dataDict)
                {
                    var entryVal = entry.Value.ToString();
                    Data[entry.Key] = entryVal;
                }
            }
            else
            {
                ServerData = err.Data?.ToString() ?? string.Empty;
            }
        }

        public JsonRpcErrorException(JsonRpcErrorCode errorCode, string message, Exception ex) : base(message, ex)
        {
            Error = new JsonRpcError
            {
                Code = (long)errorCode,
                Message = message
            };
            var dataDict = new Dictionary<string, object>();
            dataDict["exception"] = ex.ToString();
            // Populate the RPC data object with entries from the exception's Data dictionary.
            foreach (var item in ex.Data.Keys)
            {
                var dataVal = ex.Data[item];
                if (dataVal != null)
                {
                    dataDict[item.ToString()] = dataVal.ToString();
                }
            }

            Error.Data = dataDict;
        }

        public JsonRpcErrorException(JsonRpcErrorCode errorCode, string message) : base(message)
        {
            Error = new JsonRpcError
            {
                Code = (long)errorCode,
                Message = message
            };
        }

        public override string ToString()
        {
            return $"{base.ToString()}{Environment.NewLine}ServerData:{Environment.NewLine}{ServerData}";
        }
    }


    public enum JsonRpcErrorCode
    {
        /// <summary>
        /// Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text.
        /// </summary>
        ParseError = -32700,

        /// <summary>
        /// The JSON sent is not a valid Request object.
        /// </summary>
        InvalidRequest = -32600,

        /// <summary>
        /// The method does not exist / is not available.
        /// </summary>
        MethodNotFound = -32601,

        /// <summary>
        /// Invalid method parameter(s).
        /// </summary>
        InvalidParams = -32602,

        /// <summary>
        /// Internal JSON-RPC error.
        /// </summary>
        InternalError = -32603,


        ServerErrorStart = -32000,
        ServerErrorEnd = -32099
    }
}
