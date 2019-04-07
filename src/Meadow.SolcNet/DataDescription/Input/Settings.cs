using System.Collections.Generic;
using Newtonsoft.Json;
using SolcNet.DataDescription.Parsing;

namespace SolcNet.DataDescription.Input
{
    public class Settings
    {
        /// <summary>
        /// Optional: Sorted list of remappings
        /// </summary>
        [JsonProperty("remappings", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Remappings { get; set; } = new List<string>();

        /// <summary>
        /// Optional: Optimizer settings (enabled defaults to false)
        /// </summary>
        [JsonProperty("optimizer", NullValueHandling = NullValueHandling.Ignore)]
        public Optimizer Optimizer { get; set; } = new Optimizer();

        /// <summary>
        /// Version of the EVM to compile for. Affects type checking and code generation.
        /// </summary>
        [JsonProperty("evmVersion", Required = Required.DisallowNull)]
        public EvmVersion EvmVersion { get; set; } = EvmVersion.Byzantium;

        /// <summary>
        /// Metadata settings (optional)
        /// </summary>
        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Metadata Metadata { get; set; } = new Metadata();

        /// <summary>
        /// Addresses of the libraries. If not all libraries are given here, it can result in unlinked objects whose output data is different.
        /// The top level key is the the name of the source file where the library is used.
        /// If remappings are used, this source file should match the global path after remappings were applied.
        /// If this key is an empty string, that refers to a global level.
        /// </summary>
        [JsonProperty("libraries", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, string>> Libraries { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// The following can be used to select desired outputs.
        /// If this field is omitted, then the compiler loads and does type checking, but will not generate any outputs apart from errors.
        /// The first level key is the file name and the second is the contract name, where empty contract name refers to the file itself,
        /// while the star refers to all of the contracts.
        /// Note that using a using `evm`, `evm.bytecode`, `ewasm`, etc. will select every
        /// target part of that output. Additionally, `*` can be used as a wildcard to request everything.
        /// </summary>
        [JsonProperty("outputSelection", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, OutputType[]>> OutputSelection { get; set; } = new Dictionary<string, Dictionary<string, OutputType[]>>();
    }

    /// <summary>
    /// Version of the EVM to compile for. Affects type checking and code generation.
    /// </summary>
    [JsonConverter(typeof(NamedStringTokenConverter<EvmVersion>))]
    public class EvmVersion : NamedStringToken
    {
        public static implicit operator EvmVersion(string value) => new EvmVersion { Value = value };
        public static implicit operator string(EvmVersion o) => o.Value;

        public static bool operator ==(EvmVersion a, EvmVersion b) => a?.Value == b?.Value;
        public static bool operator !=(EvmVersion a, EvmVersion b) => !(a == b);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is EvmVersion a ? a == this : false;

        public static readonly EvmVersion Homestead = "homestead";
        public static readonly EvmVersion TangerineWhistle = "tangerineWhistle";
        public static readonly EvmVersion SpuriousDragon = "spuriousDragon";
        public static readonly EvmVersion Byzantium = "byzantium";
        public static readonly EvmVersion Constantinople = "constantinople";
    }

    /// <summary>
    /// Optional: Optimizer settings (enabled defaults to false)
    /// </summary>
    public class Optimizer
    {
        /// <summary>
        /// Disabled by default
        /// </summary>
        [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Optimize for how many times you intend to run the code.
        /// Lower values will optimize more for initial deployment cost, higher values will optimize more for high-frequency usage.
        /// </summary>
        [JsonProperty("runs", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public long? Runs { get; set; }
    }

    /// <summary>
    /// Metadata settings (optional)
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Use only literal content and not URLs (false by default)
        /// </summary>
        [JsonProperty("useLiteralContent", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseLiteralContent { get; set; }
    }


}
