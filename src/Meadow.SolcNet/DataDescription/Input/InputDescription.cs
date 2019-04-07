using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;
using SolcNet.DataDescription.Parsing;

namespace SolcNet.DataDescription.Input
{
    public class InputDescription
    {
        public static InputDescription FromJsonString(string jsonStr)
        {
            return JsonConvert.DeserializeObject<InputDescription>(jsonStr, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        }

        public override string ToString() => ToJsonString();

        /// <summary>
        /// Required: Source code language, such as "Solidity", "serpent", "lll", "assembly", etc.
        /// </summary>
        [JsonProperty("language", Required = Required.DisallowNull)]
        public Language Language { get; set; } = Language.Solidity;

        /// <summary>
        /// The keys here are the "global" names of the source files
        /// </summary>
        [JsonProperty("sources", Required = Required.DisallowNull)]
        public Dictionary<string, Source> Sources { get; set; } = new Dictionary<string, Source>();

        /// <summary>
        /// Optional
        /// </summary>
        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public Settings Settings { get; set; } = new Settings();
    }

    /// <summary>
    /// Source code language
    /// </summary>
    [JsonConverter(typeof(NamedStringTokenConverter<Language>))]
    public class Language : NamedStringToken
    {
        public static implicit operator Language(string value) => new Language { Value = value };
        public static implicit operator string(Language o) => o.Value;

        public static bool operator ==(Language a, Language b) => a?.Value == b?.Value;
        public static bool operator !=(Language a, Language b) => !(a == b);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is Language a ? a == this : false;

        public static readonly Language Solidity = "Solidity";
        public static readonly Language Serpent = "serpent";
        public static readonly Language Lll = "lll";
        public static readonly Language Assembly = "assembly";
    }

    public class Source
    {
        /// <summary>
        /// Optional: keccak256 hash of the source file. It is used to verify the source.
        /// </summary>
        [JsonProperty("keccak256", NullValueHandling = NullValueHandling.Ignore)]
        public string Keccak256 { get; set; }

        /// <summary>
        /// Required (unless "urls" is used): literal contents of the source file
        /// </summary>
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content
        {
            get => _content;
            set => _content = value;
        }
        string _content;

        /// <summary>
        /// Required (unless "content" is used, see below): URL(s) to the source file.
        /// URL(s) should be imported in this order and the result checked against the
        /// keccak256 hash (if available). If the hash doesn't match or none of the
        /// URL(s) result in success, an error should be raised.
        /// Examples:
        ///     "bzzr://56ab...",
        ///     "ipfs://Qma...",
        ///     "file:///tmp/path/to/file.sol"
        /// </summary>
        [JsonProperty("urls", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Urls { get; set; }
    }



}
