using Newtonsoft.Json;

namespace Meadow.Contract
{
    public class SolcBytecodeInfo
    {
        /// <summary>
        /// The relative file path.
        /// For example if the absolute path is "C:\Projects\MyProject\Contracts\Zeppelin\StandardToken.sol 
        /// then this relatie file path would be "Zeppelin\StandardToken.sol".
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        /// <summary>
        /// Name of the contract as defined in the solidity source code.
        /// </summary>
        [JsonProperty("contractName")]
        public string ContractName { get; set; }

        /// <summary>
        /// Solc output for: evm.bytecode.sourceMap
        /// </summary>
        [JsonProperty("sourceMap")]
        public string SourceMap { get; set; }

        /// <summary>
        /// Solc output for: evm.bytecode.opcodes
        /// </summary>
        [JsonProperty("opcodes")]
        public string Opcodes { get; set; }

        /// <summary>
        /// Solc output for: evm.deployedBytecode.sourceMap
        /// </summary>
        [JsonProperty("sourceMapDeployed")]
        public string SourceMapDeployed { get; set; }

        /// <summary>
        /// Solc output for: evm.deployedBytecode.opcodes
        /// </summary>
        [JsonProperty("opcodesDeployed")]
        public string OpcodesDeployed { get; set; }

        [JsonProperty("bytecode")]
        public string Bytecode { get; set; }

        [JsonProperty("bytecodeDeployed")]
        public string BytecodeDeployed { get; set; }

        [JsonProperty("bytecodeHash")]
        public string BytecodeHash { get; set; }

        [JsonProperty("bytecodeDeployedHash")]
        public string BytecodeDeployedHash { get; set; }

        public SolcBytecodeInfo()
        {

        }

    }
}