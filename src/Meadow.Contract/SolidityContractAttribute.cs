using System;

namespace Meadow.Contract
{
    public class SolidityContractAttribute : Attribute
    {
        public readonly Type ContractType;
        public readonly string FilePath;
        public readonly string ContractName;
        public readonly string BytecodeHash;
        public readonly string BytecodeDeployedHash;

        public SolidityContractAttribute(Type contractType, string filePath, string contractName, string bytecodeHash, string bytecodeDeployedHash)
        {
            ContractType = contractType;
            FilePath = filePath;
            ContractName = contractName;
            BytecodeHash = bytecodeHash;
            BytecodeDeployedHash = bytecodeDeployedHash;
        }
    }
}

