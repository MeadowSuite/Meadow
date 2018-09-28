using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types.Debugging
{
    public class ExecutionTrace
    {
        /// <summary>
        /// Represents the list of trace points in our execution trace.
        /// </summary>
        [JsonProperty("tracepoints")]
        public ExecutionTracePoint[] Tracepoints { get; set; }
        /// <summary>
        /// Represents the list of exceptions in our execution trace.
        /// </summary>
        [JsonProperty("exceptions")]
        public ExecutionTraceException[] Exceptions { get; set; }
    }
}
