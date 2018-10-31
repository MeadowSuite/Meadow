using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json;

namespace Meadow.DebugAdapterServer
{
    class ProblemDebugEvent : DebugEvent
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("exception")]
        public string Exception { get; set; }

        public ProblemDebugEvent() : base("problemEvent")
        {

        }
    }
}