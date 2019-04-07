using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace SolcNet.DataDescription.Output
{
    public class Error
    {
        /// <summary>
        /// Optional: Location within the source file.
        /// </summary>
        [JsonProperty("sourceLocation")]
        public SourceLocation SourceLocation { get; set; }

        /// <summary>
        /// Mandatory: Error type, such as "TypeError", "InternalCompilerError", "Exception", etc.
        /// See below for complete list of types.
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }

        /// <summary>
        /// Mandatory: Component where the error originated, such as "general", "ewasm", etc.
        /// </summary>
        [JsonProperty("component", Required = Required.Always)]
        public string Component { get; set; }

        /// <summary>
        /// Mandatory ("error" or "warning")
        /// </summary>
        [JsonProperty("severity", Required = Required.Always)]
        public Severity Severity { get; set; }

        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; }

        /// <summary>
        /// Optional: the message formatted with source location
        /// Ex: "sourceFile.sol:100: Invalid keyword"
        /// </summary>
        [JsonProperty("formattedMessage")]
        public string FormattedMessage { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        [EnumMember(Value = "error")]
        Error,
        [EnumMember(Value = "warning")]
        Warning
    }

    public class SourceLocation
    {
        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("start")]
        public uint Start { get; set; }

        [JsonProperty("end")]
        public uint End { get; set; }
    }

}
