using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;
using Meadow.JsonRpc.Types;
using Meadow.JsonRpc.Client;
using System.Diagnostics;

namespace Meadow.Contract
{
    public static class ContractFactory
    {
        /// <summary>
        /// Deploys a contract with constructor arguments
        /// </summary>
        /// <param name="abiEncodedConstructorArgs">ABI encoded function selector and constructor parameters</param>
        public static async Task<Address> Deploy(
            SolidityContractAttribute contractAttribute,
            IJsonRpcClient rpcClient,
            byte[] bytecode,
            TransactionParams sendParams,
            ReadOnlyMemory<byte> abiEncodedConstructorArgs = default)
        {
            (JsonRpcError error, Hash transactionHash) = await TryDeploy(contractAttribute, rpcClient, bytecode, sendParams, abiEncodedConstructorArgs);
            if (error != null)
            {
                if (rpcClient.ErrorFormatter != null)
                {
                    var formattedException = await rpcClient.ErrorFormatter(rpcClient, error);
                    throw formattedException;
                }
                else
                {
                    throw error.ToException();
                }
            }

            var receipt = await rpcClient.GetTransactionReceipt(transactionHash);

            if (receipt == null)
            {
                throw new Exception("Contract deployment transaction failed: no transaction receipt returned from server node.");
            }

            if (receipt.Status == 0)
            {
                // TODO: the server should have returned a json rpc error for this transaction rather than ending up here.
                if (rpcClient.ErrorFormatter != null)
                {
                    var formattedException = await rpcClient.ErrorFormatter(rpcClient, null);
                    throw formattedException;
                }

                throw new Exception("Transaction failed: bad status code on transaction receipt.");
            }

            var contractAddress = receipt.ContractAddress.Value;
            return contractAddress;
        }

        public static async Task<(JsonRpcError Error, Hash TransactionHash)> TryDeploy(
            SolidityContractAttribute contractAttribute,
            IJsonRpcClient rpcClient,
            byte[] bytecode,
            TransactionParams sendParams,
            ReadOnlyMemory<byte> abiEncodedConstructorArgs = default)
        {

            // If we have no code, we shouldn't append our constructor arguments, so we blank ours out.
            if (bytecode == null || bytecode.Length == 0)
            {
                abiEncodedConstructorArgs = default;
            }

            // If constructor args are provided, append to contract bytecode.
            if (!abiEncodedConstructorArgs.IsEmpty)
            {
                sendParams.Data = new byte[bytecode.Length + abiEncodedConstructorArgs.Length];
                Memory<byte> deploymentMem = new Memory<byte>(sendParams.Data);
                bytecode.CopyTo(deploymentMem);
                abiEncodedConstructorArgs.CopyTo(deploymentMem.Slice(bytecode.Length));
            }
            else
            {
                sendParams.Data = bytecode;
            }
            
            return await rpcClient.TrySendTransaction(sendParams);
        }


    }
}
