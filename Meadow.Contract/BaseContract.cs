using SolcNet.DataDescription.Output;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meadow.Core.AbiEncoding;
using Meadow.Core.Utils;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Meadow.Contract
{

    public interface IContractInstanceSetup
    {
        void Setup(IJsonRpcClient rpcClient, Address contractAddress, Address defaultFromAccount);
        TransactionParams GetTransactionParams(TransactionParams optional);
        CallParams GetCallParams(CallParams optional);
        IJsonRpcClient JsonRpcClient { get; }

        string ContractSolFilePath { get; }
        string ContractName { get; }
        string ContractBytecodeHash { get; }
        string ContractBytecodeDeployedHash { get; }
    }

    public abstract class BaseContract : IContractInstanceSetup
    {
        public SolcNet.DataDescription.Output.Abi[] Abi => _abi.Value;
        readonly Lazy<SolcNet.DataDescription.Output.Abi[]> _abi;

        public Address ContractAddress { get; protected set; }
        public Address DefaultFromAccount { get; protected set; }

        public IJsonRpcClient JsonRpcClient { get; protected set; }

        protected abstract string ContractSolFilePath { get; }
        protected abstract string ContractName { get; }
        protected abstract string ContractBytecodeHash { get; }
        protected abstract string ContractBytecodeDeployedHash { get; }

        string IContractInstanceSetup.ContractSolFilePath => ContractSolFilePath;
        string IContractInstanceSetup.ContractName => ContractName;
        string IContractInstanceSetup.ContractBytecodeHash => ContractBytecodeHash;
        string IContractInstanceSetup.ContractBytecodeDeployedHash => ContractBytecodeDeployedHash;

        public BaseContract(IJsonRpcClient rpcClient, Address contractAddress, Address defaultFromAccount) : this()
        {
            ContractAddress = contractAddress;
            DefaultFromAccount = defaultFromAccount;
            JsonRpcClient = rpcClient;
        }

        public BaseContract()
        {
            _abi = new Lazy<Abi[]>(() => GeneratedSolcData.Default.GetContractJsonAbi(ContractSolFilePath, ContractName));
        }

        TransactionParams IContractInstanceSetup.GetTransactionParams(TransactionParams optional)
        {
            return new TransactionParams
            {
                From = optional?.From ?? DefaultFromAccount,
                To = optional?.To ?? ContractAddress,
                Value = optional?.Value,
                Gas = optional?.Gas,
                GasPrice = optional?.GasPrice,
                Nonce = optional?.Nonce,
                Data = optional?.Data
            };
        }

        CallParams IContractInstanceSetup.GetCallParams(CallParams optional)
        {
            return new CallParams
            {
                From = optional?.From ?? DefaultFromAccount,
                To = optional?.To ?? ContractAddress,
                Value = optional?.Value,
                Gas = optional?.Gas,
                GasPrice = optional?.GasPrice,
                Data = optional?.Data
            };
        }

        void IContractInstanceSetup.Setup(IJsonRpcClient rpcClient, Address contractAddress, Address defaultFromAccount)
        {
            ContractAddress = contractAddress;
            DefaultFromAccount = defaultFromAccount;
            JsonRpcClient = rpcClient;
        }
    }

    public class ContractConstructParams
    {
        public Uri Server { get; set; }

        public Address DefaultFromAccount { get; set; }
    }
}
