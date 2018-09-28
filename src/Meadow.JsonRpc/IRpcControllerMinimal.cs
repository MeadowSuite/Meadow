using Meadow.Core.EthTypes;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Types.Debugging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.JsonRpc
{
    /// <summary>
    /// An interface specifying a core subset of RPC methods that are typically used by most
    /// consumers.
    /// </summary>
    public interface IRpcControllerMinimal
    {
        /// <summary>
        /// eth_sendTransaction - Creates new message call transaction or a contract creation, if the data field contains code.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_sendtransaction"/>
        /// </summary>
        /// <param name="transactionParams">The transaction object.</param>
        /// <returns>The transaction hash.</returns>
        [RpcApiMethod(RpcApiMethod.eth_sendTransaction)]
        Task<Hash> SendTransaction(TransactionParams transactionParams);

        /// <summary>
        /// eth_call - Executes a new message call immediately without creating a transaction on the block chain.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_call"/>
        /// </summary>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending".</param>
        /// <returns>The return value of executed contract.</returns>
        [RpcApiMethod(RpcApiMethod.eth_call)]
        Task<byte[]> Call(CallParams callParams, DefaultBlockParameter blockParameter);

        /// <summary>
        /// eth_getTransactionReceipt - Returns the receipt of a transaction by transaction hash.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactionreceipt"/>
        /// </summary>
        /// <param name="transactionHash">32 Bytes - hash of a transaction.</param>
        /// <returns>
        /// Returns the receipt of a transaction by transaction hash. 
        /// Note that the receipt is not available for pending transactions.
        /// </returns>
        [RpcApiMethod(RpcApiMethod.eth_getTransactionReceipt)]
        Task<TransactionReceipt> GetTransactionReceipt(Hash transactionHash);

        /// <summary>
        /// eth_estimateGas - Generates and returns an estimate of how much gas is necessary to allow the 
        /// transaction to complete. The transaction will not be added to the blockchain. Note that the estimate may 
        /// be significantly more than the amount of gas actually used by the transaction, for a variety of reasons 
        /// including EVM mechanics and node performance.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_estimategas"/>
        /// </summary>
        /// <param name="callParams">
        /// See eth_call parameters, expect that all properties are optional. If no gas limit is specified geth uses the 
        /// block gas limit from the pending block as an upper bound. As a result the returned estimate might not be enough 
        /// to executed the call/transaction when the amount of gas is higher than the pending block gas limit.
        /// </param>
        /// <param name="blockParameter">Integer block number, or the string "latest", "earliest" or "pending".</param>
        /// <returns>The amount of gas used</returns>
        [RpcApiMethod(RpcApiMethod.eth_estimateGas)]
        Task<UInt256> EstimateGas(CallParams callParams, DefaultBlockParameter blockParameter);

        /// <summary>
        /// eth_gasPrice - Returns the current price per gas in wei.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gasprice"/>
        /// </summary>
        [RpcApiMethod(RpcApiMethod.eth_gasPrice)]
        Task<UInt256> GasPrice();

        /// <summary>
        /// eth_accounts - Returns a list of addresses owned by client.
        /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_accounts"/>
        /// </summary>
        /// <returns>20 Bytes - addresses owned by the client.</returns>
        [RpcApiMethod(RpcApiMethod.eth_accounts)]
        Task<Address[]> Accounts();

    }
}
