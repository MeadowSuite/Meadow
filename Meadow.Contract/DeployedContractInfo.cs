using Meadow.Core.EthTypes;
using System;

namespace Meadow.Contract
{
    public class DeployedContractInfo
    {
        public readonly string FilePath;
        public readonly string ContractName;
        public readonly string BytecodeHash;
        public readonly string BytecodeDeployedHash;

        /// <summary>
        /// The data from the contract deployment transaction (which is the undeployed bytecode with any constructor parameters appended).
        /// </summary>
        public readonly byte[] CodeBytes;

        public readonly Address ContractAddress;
        public readonly Type ContractType;

        public DeployedContractInfo(Type contractType, string fileName, string contractName, string bytecodeHash, string bytecodeDeployedHash, byte[] codeBytes, Address contractAddress)
        {
            ContractType = contractType;
            FilePath = fileName;
            ContractName = contractName;
            ContractAddress = contractAddress;
            BytecodeDeployedHash = bytecodeDeployedHash;
            BytecodeHash = bytecodeHash;
            CodeBytes = codeBytes;
        }
    }
}
