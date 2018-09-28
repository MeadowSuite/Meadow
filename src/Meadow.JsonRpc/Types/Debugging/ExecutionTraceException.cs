using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types.Debugging
{
    public class ExecutionTraceException
    {
        #region Properties
        /// <summary>
        /// DATA, variable length, a string representing the exception message.
        /// </summary>
        [JsonProperty("exception")]
        public string Message { get; set; }
        /// <summary>
        /// INTEGER, 32-bit, nullable, represents the trace index (if any) at which this occurred. Is null if the exception occurred outside of VM execution.
        /// </summary>
        [JsonProperty("traceIndex"), JsonConverter(typeof(JsonRpcHexConverter))]
        public int? TraceIndex { get; set; }
        #endregion
    }
}
