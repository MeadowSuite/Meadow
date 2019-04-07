using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SolcNet.DataDescription.Output
{
    public class Evm
    {
        /// <summary>
        /// Assembly (string)
        /// </summary>
        [JsonProperty("assembly")]
        public string Assembly { get; set; }

        /// <summary>
        /// Old-style assembly (object)
        /// </summary>
        [JsonProperty("legacyAssembly")]
        public LegacyAssembly LegacyAssembly { get; set; }

        /// <summary>
        /// Bytecode and related details.
        /// </summary>
        [JsonProperty("bytecode")]
        public Bytecode Bytecode { get; set; }

        [JsonProperty("deployedBytecode")]
        public Bytecode DeployedBytecode { get; set; }

        /// <summary>
        /// The list of function hashes
        /// </summary>
        [JsonProperty("methodIdentifiers")]
        public Dictionary<string/*function definition*/, string/*function hash*/> MethodIdentifiers { get; set; }

        /// <summary>
        /// Function gas estimates
        /// </summary>
        [JsonProperty("gasEstimates")]
        public GasEstimates GasEstimates { get; set; }
    }

    public class LegacyAssembly
    {
        [JsonProperty(".code")]
        public Code[] Code { get; set; }

        [JsonProperty(".data")]
        public Dictionary<string, Data> Data { get; set; }
    }

    public class Data
    {
        [JsonProperty(".auxdata")]
        public string Auxdata { get; set; }

        [JsonProperty(".code")]
        public Code[] Code { get; set; }

        [JsonProperty(".data")]
        public Dictionary<string, Data> DataChildren { get; set; }
    }

    public class Code
    {
        [JsonProperty("begin")]
        public int Begin { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class GasEstimates
    {
        [JsonProperty("creation")]
        public Creation Creation { get; set; }

        [JsonProperty("external")]
        public Dictionary<string/*function def*/, string> External { get; set; }

        [JsonProperty("internal")]
        public Dictionary<string/*function def*/, string> Internal { get; set; }
    }

    public class Creation
    {
        [JsonProperty("codeDepositCost")]
        public string CodeDepositCost { get; set; }

        [JsonProperty("executionCost")]
        public string ExecutionCost { get; set; }

        [JsonProperty("totalCost")]
        public string TotalCost { get; set; }
    }

}
