using Newtonsoft.Json;
using SolcNet.DataDescription.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolcNet.DataDescription.Output
{

    public class Abi
    {
        /// <summary>
        /// "function", "constructor", or "fallback" (the unnamed "default" function);
        /// </summary>
        [JsonProperty("type")]
        public AbiType Type { get; set; }

        /// <summary>
        /// The name of the function
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("inputs")]
        public Input[] Inputs { get; set; }

        [JsonProperty("outputs")]
        public Output[] Outputs { get; set; }

        /// <summary>
        /// true if function accepts ether, defaults to false
        /// </summary>
        [JsonProperty("payable")]
        public bool? Payable { get; set; }

        /// <summary>
        /// a string with one of the following values: pure (specified to not read blockchain state), view (specified to not modify the blockchain state), nonpayable and payable (same as payable above).
        /// </summary>
        [JsonProperty("stateMutability")]
        public StateMutability StateMutability { get; set; }

        /// <summary>
        /// true if function is either pure or view
        /// </summary>
        [JsonProperty("constant")]
        public bool? Constant { get; set; }

        /// <summary>
        /// true if the event was declared as anonymous
        /// </summary>
        [JsonProperty("anonymous")]
        public bool? Anonymous { get; set; }

        public static implicit operator Abi(string json) => JsonConvert.DeserializeObject<Abi>(json);
    }

    [JsonConverter(typeof(NamedStringTokenConverter<AbiType>))]
    public class AbiType : NamedStringToken
    {
        public static implicit operator AbiType(string value) => new AbiType { Value = value };
        public static implicit operator string(AbiType o) => o.Value;

        public static bool operator ==(AbiType a, AbiType b) => a?.Value == b?.Value;
        public static bool operator !=(AbiType a, AbiType b) => !(a == b);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is AbiType a ? a == this : false;

        public static readonly AbiType Function = "function";
        public static readonly AbiType Constructor = "constructor";
        public static readonly AbiType Fallback = "fallback";
        public static readonly AbiType Event = "event";
    }

    [JsonConverter(typeof(NamedStringTokenConverter<StateMutability>))]
    public class StateMutability : NamedStringToken
    {
        public static implicit operator StateMutability(string value) => new StateMutability { Value = value };
        public static implicit operator string(StateMutability o) => o.Value;

        public static bool operator ==(StateMutability a, StateMutability b) => a?.Value == b?.Value;
        public static bool operator !=(StateMutability a, StateMutability b) => !(a == b);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is StateMutability a ? a == this : false;

        public static readonly StateMutability Pure = "pure";
        public static readonly StateMutability View = "view";
        public static readonly StateMutability NonPayable = "nonpayable";
        public static readonly StateMutability Payable = "payable";
    }

    public class Input
    {
        /// <summary>
        /// the name of the parameter
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// the canonical type of the parameter (more below).
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// used for tuple types
        /// </summary>
        [JsonProperty("components")]
        public Component[] Components { get; set; }

        /// <summary>
        /// if the field is part of the log’s topics, false if it one of the log’s data segment.
        /// </summary>
        [JsonProperty("indexed")]
        public bool? Indexed { get; set; }
    }

    /// <summary>
    /// used for tuple types
    /// </summary>
    public class Component
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("components")]
        public Component[] Components { get; set; }
    }

    public class Output
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("components")]
        public Component[] Components { get; set; }
    }


}
