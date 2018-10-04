using Meadow.JsonRpc.JsonConverters;
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

        /// <summary>
        /// Represents the lookup of sha3 hash -> pre-images (original data) so storage slots original locations can be
        /// computed. This is useful as mapping keys can be obtained from the hashes used to calculate the storage entry
        /// for the key's corresponding value.
        /// </summary>
        [JsonProperty("storage_preimages"), JsonConverter(typeof(ByteArrayDictionaryConverter))]
        public Dictionary<Memory<byte>, byte[]> StoragePreimages { get; set; }
    }
}
