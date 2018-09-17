using SolcNet.DataDescription.Output;

namespace Meadow.SolCodeGen
{
    class ContractInfo
    {
        public readonly string SolFile;
        public readonly string ContractName;
        public readonly SolcNet.DataDescription.Output.Contract ContractOutput;
        public readonly byte[] Hash;
        public readonly string Bytecode;

        public ContractInfo(string solFile, string contractName, SolcNet.DataDescription.Output.Contract contractOutput, byte[] hash, string bytecode)
        {
            SolFile = solFile;
            ContractName = contractName;
            ContractOutput = contractOutput;
            Hash = hash;
            Bytecode = bytecode;
        }

        public void Deconstruct(out string solFile, out string contractName, out SolcNet.DataDescription.Output.Contract contractOuput, out byte[] hash, out string bytecode)
        {
            solFile = SolFile;
            contractName = ContractName;
            contractOuput = ContractOutput;
            hash = Hash;
            bytecode = Bytecode;
        }
    }

}
