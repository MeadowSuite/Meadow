using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace Meadow.JsonRpc
{
    public class JsonRpcRequestObject
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc = "2.0";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonProperty("params")]
        public JArray Params { get; set; }

        public JsonRpcRequestObject()
        {

        }

        public JsonRpcRequestObject(long id, string method, JArray args)
        {
            ID = id;
            Method = method;
            Params = args;
        }
    }


}
