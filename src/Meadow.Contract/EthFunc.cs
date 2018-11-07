using Meadow.Core.AbiEncoding;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Meadow.JsonRpc.Types;
using Meadow.Core.EthTypes;
using Meadow.JsonRpc;

namespace Meadow.Contract
{

    public class EthFuncBase
    {
        protected readonly IContractInstanceSetup _contract;
        protected readonly byte[] _callData;

        /// <summary>
        /// 
        /// </summary>
        public byte[] RawCallData => _callData;

        public EthFuncBase(IContractInstanceSetup contract, byte[] callData)
        {
            _contract = contract;
            _callData = callData;
        }

        public TaskAwaiter<TransactionReceipt> GetAwaiter()
        {
            return TransactionReceipt(null).GetAwaiter();
        }


        /// <summary>
        /// Creates new message call transaction on the block chain.
        /// </summary>
        /// <returns>The transaction hash</returns>
        public async virtual Task<Hash> SendTransaction(TransactionParams transactionParams = null)
        {
            transactionParams = _contract.GetTransactionParams(transactionParams);
            transactionParams.Data = _callData;
            Hash transactionHash;
            try
            {
                transactionHash = await _contract.JsonRpcClient.SendTransaction(transactionParams);
            }
            catch (JsonRpcErrorException rpcEx) when (_contract.JsonRpcClient.ErrorFormatter != null)
            {
                var formattedException = await _contract.JsonRpcClient.ErrorFormatter(_contract.JsonRpcClient, rpcEx.Error);
                throw formattedException;
            }
            
            
            if (_contract.JsonRpcClient.CheckBadTransactionStatus)
            {
                var transactionReceipt = await _contract.JsonRpcClient.GetTransactionReceipt(transactionHash);
                if (transactionReceipt == null)
                {
                    throw new Exception("Transaction failed: no transaction receipt returned from server node.");
                }

                if (transactionReceipt.Status == 0)
                {
                    // TODO: the server should have returned a json rpc error for this transaction rather than ending up here.
                    if (_contract.JsonRpcClient.ErrorFormatter != null)
                    {
                        var formattedException = await _contract.JsonRpcClient.ErrorFormatter(_contract.JsonRpcClient, null);
                        throw formattedException;
                    }

                    throw new Exception("Transaction failed: bad status code on transaction receipt.");
                }
            }

            return transactionHash;
        }

        /// <summary>
        /// Creates new transaction and expects a revert.
        /// Throws an exception if transaction does not revert.
        /// </summary>
        public async Task ExpectRevertTransaction(TransactionParams transactionParams = null)
        {
            transactionParams = _contract.GetTransactionParams(transactionParams);
            transactionParams.Data = _callData;
            var (error, transactionHash) = await _contract.JsonRpcClient.TrySendTransaction(transactionParams, expectingException: true);
            if (error == null)
            {
                var receipt = await _contract.JsonRpcClient.GetTransactionReceipt(transactionHash);
                if (receipt == null)
                {
                    throw new Exception($"Expected transaction to revert, but server node did not return a transaction receipt for transaction hash: {transactionHash}");
                }

                if (receipt.Status != 0)
                {
                    throw new Exception($"Expected transaction to revert");
                }
            }
        }

        /// <summary>
        /// Creates new message call transaction on the block chain and gets the transaction receipt.
        /// </summary>
        /// <returns>The transaction receipt</returns>
        public async virtual Task<TransactionReceipt> TransactionReceipt(TransactionParams transactionParams = null)
        {
            var hash = await SendTransaction(transactionParams);
            var receipt = await _contract.JsonRpcClient.GetTransactionReceipt(hash);
            if (receipt == null)
            {
                throw new Exception($"Transaction failed: no transaction receipt returned from server node for transaction hash {hash}");
            }

            return receipt;
        }

        public async Task<UInt256> EstimateGas(CallParams callParams = null, DefaultBlockParameter blockParameter = null)
        {
            callParams = _contract.GetCallParams(callParams);
            callParams.Data = _callData;
            blockParameter = blockParameter ?? BlockParameterType.Latest;
            var result = await _contract.JsonRpcClient.EstimateGas(callParams, blockParameter);
            return result;
        }


        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and returns the first (oldest) event log of the given type.
        /// </summary>
        /// <returns>Throws exception if not found.</returns>
        public async Task<TEventLog> FirstEventLog<TEventLog>(TransactionParams transactionParams = null) where TEventLog : EventLog
        {
            var transaction = await TransactionReceipt(transactionParams);
            return transaction.FirstEventLog<TEventLog>();
        }

        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and returns the first (oldest) event log of the given type.
        /// </summary>
        /// <returns>Return null if no matching event is found.</returns>
        public async Task<TEventLog> FirstOrDefaultEventLog<TEventLog>(TransactionParams transactionParams = null) where TEventLog : EventLog
        {
            var transaction = await TransactionReceipt(transactionParams);
            return transaction.FirstOrDefaultEventLog<TEventLog>();
        }

        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and returns the last (newest) event log of the given type.
        /// </summary>
        /// <returns>Throws exception if not found.</returns>
        public async Task<TEventLog> LastEventLog<TEventLog>(TransactionParams transactionParams = null) where TEventLog : EventLog
        {
            var transaction = await TransactionReceipt(transactionParams);
            return transaction.LastEventLog<TEventLog>();
        }

        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and returns the last (newest) event log of the given type.
        /// </summary>
        /// <returns>Return null if no matching event is found.</returns>
        public async Task<TEventLog> LastOrDefaultEventLog<TEventLog>(TransactionParams transactionParams = null) where TEventLog : EventLog
        {
            var transaction = await TransactionReceipt(transactionParams);
            return transaction.LastOrDefaultEventLog<TEventLog>();
        }

        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and returns all event logs that can be matched to an event type. 
        /// Unmatched logs are left out of the result.
        /// </summary>
        public async Task<EventLog[]> EventLogs(TransactionParams transactionParams = null)
        {
            var transaction = await TransactionReceipt(transactionParams);
            return EventLogUtil.EventLogs(transaction);
        }

        /// <summary>
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt, and searches the event logs for any that match the given event type.
        /// </summary>
        /// <returns>Returns an empty array if no matching events are found.</returns>
        public async Task<TEventLog[]> EventLogs<TEventLog>(TransactionParams transactionParams = null) where TEventLog : EventLog
        {
            var transaction = await TransactionReceipt(transactionParams);
            return transaction.EventLogs<TEventLog>();
        }

        /// <summary>
        /// Eventlog helper for get a tuple eventlogs,which could contains 2-8 different type of evens, in one transaction in one time
        /// Creates new message call transaction on the block chain, gets the transaction 
        /// receipt.
        /// </summary>
        /// <returns>Returns tuple of event log </returns>
        public async Task<(T1 Event1, T2 Event2)> EventLogs<T1, T2>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1));
        }

        public async Task<(T1 Event1, T2 Event2, T3 Event3)> EventLogs<T1, T2, T3>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2));
        }

        public async Task<(T1 Event1, T2 Event2, T3 Event3, T4 Event4)> EventLogs<T1, T2, T3, T4>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog where T4 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2),
                ParseEvent<T4>(receipt, 3));
        }


        public async Task<(T1 Event1, T2 Event2, T3 Event3, T4 Event4, T5 Event5)> EventLogs<T1, T2, T3, T4, T5>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog where T4 : EventLog where T5 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2),
                ParseEvent<T4>(receipt, 3),
                ParseEvent<T5>(receipt, 4));
        }


        public async Task<(T1 Event1, T2 Event2, T3 Event3, T4 Event4, T5 Event5, T6 Event6)> EventLogs<T1, T2, T3, T4, T5, T6>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog where T4 : EventLog where T5 : EventLog where T6 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2),
                ParseEvent<T4>(receipt, 3),
                ParseEvent<T5>(receipt, 4),
                ParseEvent<T6>(receipt, 5));
        }

        public async Task<(T1 Event1, T2 Event2, T3 Event3, T4 Event4, T5 Event5, T6 Event6, T7 Event7)> EventLogs<T1, T2, T3, T4, T5, T6, T7>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog where T4 : EventLog where T5 : EventLog where T6 : EventLog where T7 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2),
                ParseEvent<T4>(receipt, 3),
                ParseEvent<T5>(receipt, 4),
                ParseEvent<T6>(receipt, 5),
                ParseEvent<T7>(receipt, 6));
        }

        public async Task<(T1 Event1, T2 Event2, T3 Event3, T4 Event4, T5 Event5, T6 Event6, T7 Event7, T8 Event8)> EventLogs<T1, T2, T3, T4, T5, T6, T7, T8>(TransactionParams transactionParams = null) where T1 : EventLog where T2 : EventLog where T3 : EventLog where T4 : EventLog where T5 : EventLog where T6 : EventLog where T7 : EventLog where T8 : EventLog
        {
            var receipt = await TransactionReceipt(transactionParams);
            return (
                ParseEvent<T1>(receipt, 0),
                ParseEvent<T2>(receipt, 1),
                ParseEvent<T3>(receipt, 2),
                ParseEvent<T4>(receipt, 3),
                ParseEvent<T5>(receipt, 4),
                ParseEvent<T6>(receipt, 5),
                ParseEvent<T7>(receipt, 6),
                ParseEvent<T8>(receipt, 7));
        }

        static TEventLog ParseEvent<TEventLog>(TransactionReceipt receipt, int index) where TEventLog : EventLog
        {
            if (!receipt.Logs[index].TryParse<TEventLog>(out var result))
            {
                throw new Exception($"Expected event type {typeof(TEventLog).Name} at log position {index}");
            }

            return result;
        }

    }

    public class EthFunc<TReturn> : EthFuncBase
    {
        public delegate TReturn ParseResponseDelegate(ReadOnlyMemory<byte> data);

        protected ParseResponseDelegate _parseResponse;

        public EthFunc(BaseContract contract, byte[] callData, ParseResponseDelegate parseResponse) : base(contract, callData)
        {
            _parseResponse = parseResponse;
        }

        /// <summary>
        /// Performs first an RPC call to get the function return value, then an RPC sendTransaction and getTransactionReceipt.
        /// </summary>
        public async Task<(TReturn Result, TransactionReceipt Receipt)> CallAndTransact(TransactionParams transactionParams = null)
        {
            transactionParams = _contract.GetTransactionParams(transactionParams);
            transactionParams.Data = _callData;

            var callParams = _contract.GetCallParams(new CallParams
            {
                Data = transactionParams.Data,
                From = transactionParams.From,
                Gas = transactionParams.Gas,
                GasPrice = transactionParams.GasPrice,
                To = transactionParams.To,
                Value = transactionParams.Value
            });
            callParams.Data = _callData;

            var result = await Call(callParams);
            var receipt = await TransactionReceipt(transactionParams);
            return (result, receipt);
        }

        /// <summary>
        /// Executes a new message call immediately without creating a transaction on the block chain. 
        /// Returns the raw ABI encoded response. 
        /// </summary>
        public async Task<byte[]> CallRaw(CallParams callParams = null, DefaultBlockParameter blockParameter = null)
        {
            callParams = _contract.GetCallParams(callParams);
            callParams.Data = _callData;
            blockParameter = blockParameter ?? BlockParameterType.Latest;

            byte[] callResult;

            try
            {
                callResult = await _contract.JsonRpcClient.Call(callParams, blockParameter);
            }
            catch (JsonRpcErrorException rpcEx) when (_contract.JsonRpcClient.ErrorFormatter != null)
            {
                var formattedException = await _contract.JsonRpcClient.ErrorFormatter(_contract.JsonRpcClient, rpcEx.Error);
                throw formattedException;
            }

            return callResult;
        }

        /// <summary>
        /// Executes a new message call immediately without creating a transaction on the block chain.
        /// </summary>
        public async Task<TReturn> Call(CallParams callParams = null, DefaultBlockParameter blockParameter = null)
        {
            var callResult = await CallRaw(callParams, blockParameter);
            var result = ParseReturnData(callResult);
            return result;
        }

        /// <summary>
        /// Executes a new message call immediately without creating a transaction on the block chain and expects a revert.
        /// Throws an exception if call does not revert.
        /// </summary>
        public async Task ExpectRevertCall(CallParams callParams = null, DefaultBlockParameter blockParameter = null)
        {
            callParams = _contract.GetCallParams(callParams);
            callParams.Data = _callData;
            blockParameter = blockParameter ?? BlockParameterType.Latest;
            var (error, callResult) = await _contract.JsonRpcClient.TryCall(callParams, blockParameter, expectingException: true);
            if (error == null)
            {
                // Check if call is void (no return value)
                if (this is EthFunc)
                {
                    throw new Exception("Expected call to revert");
                }
                else
                {
                    var result = ParseReturnData(callResult);
                    throw new Exception($"Expected call to revert but got back result: {result}");
                }
            }
        }

        public TReturn ParseReturnData(byte[] bytes)
        {
            var result = _parseResponse(bytes);
            return result;
        }

    }

    /* 
     * Preface: Lots of code duplication follows..
     * This is the idiotmatic method for pseudo-variadic generics in C#.
     * The C# language designers wanted to avoid the C# generic syntax from
     * becoming a Turing complete nightmare like the C++ template syntax.
     * 
     * Simplicity at the expense of minor code duplication.
     * 
     * An example of Microsoft doing the same thing for Tuple<T1... Tn>:
     *  https://referencesource.microsoft.com/#mscorlib/system/tuple.cs,83
     * And Action<T1... Tn>:
     *  https://referencesource.microsoft.com/#mscorlib/system/action.cs,29
     */

    public class EthFunc : EthFunc<object>
    {
        public EthFunc(BaseContract contract, byte[] callData, ParseResponseDelegate parseResponse) 
            : base(contract, callData, parseResponse)
        {
        }

        public new Task Call(CallParams sendParams = null, DefaultBlockParameter blockParameter = null)
        {
            return base.Call(sendParams, blockParameter);
        }

        public static EthFunc Create(
            BaseContract contract, byte[] callData)
        {
            return new EthFunc(contract, callData, _ => null);
        }

        public static EthFunc<T1> Create<T1>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1)
        {
            T1 Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1);
                d1(s1, ref buff, out var i1);
                return i1;
            }

            return new EthFunc<T1>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2)> Create<T1, T2>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2)
        {
            (T1, T2) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                return (i1, i2);
            }

            return new EthFunc<(T1, T2)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3)> Create<T1, T2, T3>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3)
        {
            (T1, T2, T3) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                return (i1, i2, i3);
            }

            return new EthFunc<(T1, T2, T3)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4)> Create<T1, T2, T3, T4>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4)
        {
            (T1, T2, T3, T4) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                return (i1, i2, i3, i4);
            }

            return new EthFunc<(T1, T2, T3, T4)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5)> Create<T1, T2, T3, T4, T5>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5)
        {
            (T1, T2, T3, T4, T5) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                return (i1, i2, i3, i4, i5);
            }

            return new EthFunc<(T1, T2, T3, T4, T5)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6)> Create<T1, T2, T3, T4, T5, T6>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6)
        {
            (T1, T2, T3, T4, T5, T6) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                return (i1, i2, i3, i4, i5, i6);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7)> Create<T1, T2, T3, T4, T5, T6, T7>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7)
        {
            (T1, T2, T3, T4, T5, T6, T7) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                return (i1, i2, i3, i4, i5, i6, i7);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8)> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7,
            AbiTypeInfo s8, DecodeDelegate<T8> d8)
        {
            (T1, T2, T3, T4, T5, T6, T7, T8) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7, s8);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                d8(s8, ref buff, out var i8);
                return (i1, i2, i3, i4, i5, i6, i7, i8);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7,
            AbiTypeInfo s8, DecodeDelegate<T8> d8,
            AbiTypeInfo s9, DecodeDelegate<T9> d9)
        {
            (T1, T2, T3, T4, T5, T6, T7, T8, T9) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7, s8, s9);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                d8(s8, ref buff, out var i8);
                d9(s9, ref buff, out var i9);
                return (i1, i2, i3, i4, i5, i6, i7, i8, i9);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7,
            AbiTypeInfo s8, DecodeDelegate<T8> d8,
            AbiTypeInfo s9, DecodeDelegate<T9> d9,
            AbiTypeInfo s10, DecodeDelegate<T10> d10)
        {
            (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                d8(s8, ref buff, out var i8);
                d9(s9, ref buff, out var i9);
                d10(s10, ref buff, out var i10);
                return (i1, i2, i3, i4, i5, i6, i7, i8, i9, i10);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7,
            AbiTypeInfo s8, DecodeDelegate<T8> d8,
            AbiTypeInfo s9, DecodeDelegate<T9> d9,
            AbiTypeInfo s10, DecodeDelegate<T10> d10,
            AbiTypeInfo s11, DecodeDelegate<T11> d11)
        {
            (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                d8(s8, ref buff, out var i8);
                d9(s9, ref buff, out var i9);
                d10(s10, ref buff, out var i10);
                d11(s11, ref buff, out var i11);
                return (i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>(contract, callData, Parse);
        }

        public static EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            BaseContract contract, byte[] callData,
            AbiTypeInfo s1, DecodeDelegate<T1> d1,
            AbiTypeInfo s2, DecodeDelegate<T2> d2,
            AbiTypeInfo s3, DecodeDelegate<T3> d3,
            AbiTypeInfo s4, DecodeDelegate<T4> d4,
            AbiTypeInfo s5, DecodeDelegate<T5> d5,
            AbiTypeInfo s6, DecodeDelegate<T6> d6,
            AbiTypeInfo s7, DecodeDelegate<T7> d7,
            AbiTypeInfo s8, DecodeDelegate<T8> d8,
            AbiTypeInfo s9, DecodeDelegate<T9> d9,
            AbiTypeInfo s10, DecodeDelegate<T10> d10,
            AbiTypeInfo s11, DecodeDelegate<T11> d11,
            AbiTypeInfo s12, DecodeDelegate<T12> d12)
        {
            (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12) Parse(ReadOnlyMemory<byte> mem)
            {
                var buff = new AbiDecodeBuffer(mem, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12);
                d1(s1, ref buff, out var i1);
                d2(s2, ref buff, out var i2);
                d3(s3, ref buff, out var i3);
                d4(s4, ref buff, out var i4);
                d5(s5, ref buff, out var i5);
                d6(s6, ref buff, out var i6);
                d7(s7, ref buff, out var i7);
                d8(s8, ref buff, out var i8);
                d9(s9, ref buff, out var i9);
                d10(s10, ref buff, out var i10);
                d11(s11, ref buff, out var i11);
                d12(s12, ref buff, out var i12);
                return (i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12);
            }

            return new EthFunc<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>(contract, callData, Parse);
        }
    }

}
