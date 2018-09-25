using Newtonsoft.Json;

namespace Meadow.DebugAdapterServer
{
    public class SolidityMeadowConfigurationProperties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("request")]
        public string Request { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("program")]
        public string Program { get; set; }

        [JsonProperty("stopOnEntry")]
        public bool StopOnEntry { get; set; }

        [JsonProperty("__sessionId")]
        public string SessionID { get; set; }

        [JsonProperty("workspaceDirectory")]
        public string WorkspaceDirectory { get; set; }
    }
}
