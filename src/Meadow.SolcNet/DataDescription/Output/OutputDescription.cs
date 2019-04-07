using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SolcNet.DataDescription.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SolcNet.DataDescription.Output
{
    public class OutputDescription
    {
        public static OutputDescription FromJsonString(string jsonStr)
        {
            var jObj = JObject.Parse(jsonStr);
            var output = jObj.ToObject<OutputDescription>(JsonSerializer.CreateDefault(new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error }));
            output.JObject = jObj;
            output.RawJsonOutput = jsonStr;
            return output;
        }

        /// <summary>
        /// Optional: not present if no errors/warnings were encountered
        /// </summary>
        [JsonProperty("errors")]
        public Error[] Errors { get; set; } = new Error[0];

        /// <summary>
        /// This contains the file-level outputs. In can be limited/filtered by the outputSelection settings.
        /// </summary>
        [JsonProperty("sources")]
        public Dictionary<string/*sol file name*/, SourceFileOutput> Sources { get; set; }

        /// <summary>
        /// This contains the contract-level outputs. It can be limited/filtered by the outputSelection settings.
        /// </summary>
        [JsonProperty("contracts")]
        public Dictionary<string/*sol file name*/, Dictionary<string/*contract name*/, Contract>> Contracts { get; set; }

        [JsonIgnore]
        (string, string, Contract)[] _contractsFlattened;

        [JsonIgnore]
        public (string SolFile, string ContractName, Contract Contract)[] ContractsFlattened => _contractsFlattened ?? (_contractsFlattened = Contracts.Flatten());

        [JsonIgnore]
        public string RawJsonOutput { get; set; }

        [JsonIgnore]
        public JObject JObject { get; set; }
    }

    public class SourceFileOutput
    {
        /// <summary>Identifier (used in source maps)</summary>
        [JsonProperty("id")]
        public uint? ID { get; set; }

        /// <summary>The AST object</summary>
        [JsonProperty("ast")]
        public Dictionary<string, object> Ast { get; set; }

        /// <summary>The legacy AST object</summary>
        [JsonProperty("legacyAST")]
        public Dictionary<string, object> LegacyAst { get; set; }
    }


    public class Contract
    {
        public static Contract FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Contract>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        }

        /// <summary>
        /// The Ethereum Contract ABI. If empty, it is represented as an empty array.
        /// See https://github.com/ethereum/wiki/wiki/Ethereum-Contract-ABI
        /// </summary>
        [JsonProperty("abi")]
        public IList<Abi> Abi { get; set; }

        [JsonIgnore]
        string _abiJsonString;

        [JsonIgnore]
        public string AbiJsonString => _abiJsonString ?? (_abiJsonString = JsonConvert.SerializeObject(Abi));

        [JsonProperty("metadata")]
        public string Metadata { get; set; }

        /// <summary>
        /// User documentation (natspec)
        /// </summary>
        [JsonProperty("userdoc")]
        public Doc Userdoc { get; set; }

        /// <summary>
        /// Developer documentation (natspec)
        /// </summary>
        [JsonProperty("devdoc")]
        public Doc Devdoc { get; set; }

        /// <summary>
        /// Intermediate representation (string)
        /// </summary>
        [JsonProperty("ir")]
        public object IR { get; set; }

        /// <summary>
        /// EVM-related outputs
        /// </summary>
        [JsonProperty("evm")]
        public Evm Evm { get; set; }
    }

}
