using Meadow.Core.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meadow.JsonRpc.Types;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Client;

namespace Meadow.Contract
{
    public class ContractDeployer<TContract> where TContract : IContractInstanceSetup, new()
    {
        readonly SolidityContractAttribute _contractAttribute;
        readonly IJsonRpcClient _rpcClient;
        readonly byte[] _bytecode;
        readonly TransactionParams _transactionParams;
        readonly Address? _defaultFromAccount;
        readonly ReadOnlyMemory<byte> _abiEncodedConstructorArgs;

        public ContractDeployer(
            IJsonRpcClient rpcClient,
            byte[] bytecode,
            TransactionParams transactionParams,
            Address? defaultFromAccount,
            ReadOnlyMemory<byte> abiEncodedConstructorArgs = default)
        {
            _contractAttribute = TypeAttributeCache<TContract, SolidityContractAttribute>.Attribute;
            _rpcClient = rpcClient;
            _bytecode = bytecode;
            _transactionParams = transactionParams;
            _defaultFromAccount = defaultFromAccount;
            _abiEncodedConstructorArgs = abiEncodedConstructorArgs;
        }

        public TaskAwaiter<TContract> GetAwaiter()
        {
            return Deploy().GetAwaiter();
        }

        async Task<TransactionParams> GetTransactionParams()
        {
            var txParams = _transactionParams ?? new TransactionParams();
            var fromAccount = _defaultFromAccount ?? txParams.From ?? (await _rpcClient.Accounts())[0];
            txParams.From = txParams.From ?? fromAccount;
            return txParams;
        }

        public async Task<TContract> Deploy()
        {
            var txParams = await GetTransactionParams();
            var contractAddr = await ContractFactory.Deploy(_contractAttribute, _rpcClient, _bytecode, txParams, _abiEncodedConstructorArgs);
            var contractInstance = new TContract();
            contractInstance.Setup(_rpcClient, contractAddr, _defaultFromAccount ?? txParams.From.Value);
            return contractInstance;
        }   
        
        /// <summary>
        /// Creates new contract deployment transaction and expects a revert.
        /// Throws an exception if transaction does not revert.
        /// </summary>
        public async Task ExpectRevert()
        {
            var txParams = await GetTransactionParams();
            (JsonRpcError error, Hash transactionHash) = await ContractFactory.TryDeploy(expectException: true, _contractAttribute, _rpcClient, _bytecode, txParams, _abiEncodedConstructorArgs);

            if (error == null)
            {
                var receipt = await _rpcClient.GetTransactionReceipt(transactionHash);
                if (receipt == null)
                {
                    throw new Exception($"Expected contract deployment transaction to revert, but server node did not return a transaction receipt for transaction hash: {transactionHash}");
                }

                if (receipt.Status != 0)
                {
                    throw new Exception($"Expected contract deployment transaction to revert");
                }
            }
        }
    }

}
