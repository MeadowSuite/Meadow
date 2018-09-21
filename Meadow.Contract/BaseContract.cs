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

namespace Meadow.Contract
{

    public interface IContractInstanceSetup
    {
        void Setup(IJsonRpcClient rpcClient, Address contractAddress, Address defaultFromAccount);
        TransactionParams GetTransactionParams(TransactionParams optional);
        CallParams GetCallParams(CallParams optional);
        IJsonRpcClient JsonRpcClient { get; }
    }

    public abstract class BaseContract : IContractInstanceSetup
    {
        public abstract string ContractSolFilePath { get; }
        public abstract string ContractName { get; }
        public abstract string ContractBytecodeHash { get; }
        public abstract string ContractBytecodeDeployedHash { get; }

        public Address ContractAddress { get; protected set; }
        public Address DefaultFromAccount { get; protected set; }

        public IJsonRpcClient JsonRpcClient { get; protected set; }

        public BaseContract(IJsonRpcClient rpcClient, Address contractAddress, Address defaultFromAccount)
        {
            ContractAddress = contractAddress;
            DefaultFromAccount = defaultFromAccount;
            JsonRpcClient = rpcClient;
        }

        public BaseContract()
        {

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
}
