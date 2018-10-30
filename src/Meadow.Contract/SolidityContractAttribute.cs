using System;

namespace Meadow.Contract
{
    public class SolidityContractAttribute : Attribute
    {
        public readonly Type ContractType;
        public readonly string FilePath;
        public readonly string ContractName;

        public SolidityContractAttribute(Type contractType, string filePath, string contractName)
        {
            ContractType = contractType;
            FilePath = filePath;
            ContractName = contractName;
        }
    }
}

