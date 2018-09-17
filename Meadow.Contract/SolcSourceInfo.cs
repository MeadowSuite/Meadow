using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meadow.Contract
{

    public class SolcSourceInfo
    {
        /// <summary>
        /// This file's index in the solc sources list output.
        /// </summary>
        [JsonProperty("id")]
        public int ID { get; set; }

        /// <summary>
        /// The relative file path.
        /// For example if the absolute path is "C:\Projects\MyProject\Contracts\Zeppelin\StandardToken.sol 
        /// then this relatie file path would be "Zeppelin\StandardToken.sol".
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// AST data serialized as a json string.
        /// </summary>
        [JsonProperty("ast")]
        public JObject AstJson { get; set; }

        /// <summary>
        /// The full literal solidity file source code.
        /// </summary>
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        public SolcSourceInfo()
        {

        }

        public SolcSourceInfo(int id, string fileName, JObject astJson, string sourceCode)
        {
            ID = id;
            FileName = fileName;
            AstJson = astJson;
            SourceCode = sourceCode;
        }

        public static implicit operator SolcSourceInfo((int ID, string FileName, JObject AstJson, string SourceCode) val)
        {
            return new SolcSourceInfo(val.ID, val.FileName, val.AstJson, val.SourceCode);
        }

        public static implicit operator (int ID, string FileName, JObject AstJson, string SourceCode)(SolcSourceInfo val)
        {
            return (val.ID, val.FileName, val.AstJson, val.SourceCode);
        }

        public void Deconstruct(out int id, out string fileName, out JObject astJson, out string sourceCode)
        {
            id = ID;
            fileName = FileName;
            astJson = AstJson;
            sourceCode = SourceCode;
        }
    }
}