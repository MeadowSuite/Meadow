using Meadow.Core.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Meadow.JsonRpc
{
#pragma warning disable SA1300 // Element should begin with upper-case letter
    public enum RpcApiMethod
    {
        [EnumMember(Value = "web3_clientVersion")]
        web3_clientVersion,

        [EnumMember(Value = "web3_sha3")]
        web3_sha3,

        [EnumMember(Value = "net_version")]
        net_version,

        [EnumMember(Value = "net_listening")]
        net_listening,

        [EnumMember(Value = "net_peerCount")]
        net_peerCount,

        [EnumMember(Value = "eth_protocolVersion")]
        eth_protocolVersion,

        [EnumMember(Value = "eth_syncing")]
        eth_syncing,

        [EnumMember(Value = "eth_coinbase")]
        eth_coinbase,

        [EnumMember(Value = "eth_mining")]
        eth_mining,

        [EnumMember(Value = "eth_hashrate")]
        eth_hashrate,

        [EnumMember(Value = "eth_gasPrice")]
        eth_gasPrice,

        [EnumMember(Value = "eth_accounts")]
        eth_accounts,

        [EnumMember(Value = "eth_blockNumber")]
        eth_blockNumber,

        [EnumMember(Value = "eth_getBalance")]
        eth_getBalance,

        [EnumMember(Value = "eth_getStorageAt")]
        eth_getStorageAt,

        [EnumMember(Value = "eth_getTransactionCount")]
        eth_getTransactionCount,

        [EnumMember(Value = "eth_getBlockTransactionCountByHash")]
        eth_getBlockTransactionCountByHash,

        [EnumMember(Value = "eth_getBlockTransactionCountByNumber")]
        eth_getBlockTransactionCountByNumber,

        [EnumMember(Value = "eth_getUncleCountByBlockHash")]
        eth_getUncleCountByBlockHash,

        [EnumMember(Value = "eth_getUncleCountByBlockNumber")]
        eth_getUncleCountByBlockNumber,

        [EnumMember(Value = "eth_getCode")]
        eth_getCode,

        [EnumMember(Value = "eth_sign")]
        eth_sign,

        [EnumMember(Value = "eth_sendTransaction")]
        eth_sendTransaction,

        [EnumMember(Value = "eth_sendRawTransaction")]
        eth_sendRawTransaction,

        [EnumMember(Value = "eth_call")]
        eth_call,

        [EnumMember(Value = "eth_estimateGas")]
        eth_estimateGas,

        [EnumMember(Value = "eth_getBlockByHash")]
        eth_getBlockByHash,

        [EnumMember(Value = "eth_getBlockByNumber")]
        eth_getBlockByNumber,

        [EnumMember(Value = "eth_getTransactionByHash")]
        eth_getTransactionByHash,

        [EnumMember(Value = "eth_getTransactionByBlockHashAndIndex")]
        eth_getTransactionByBlockHashAndIndex,

        [EnumMember(Value = "eth_getTransactionByBlockNumberAndIndex")]
        eth_getTransactionByBlockNumberAndIndex,

        [EnumMember(Value = "eth_getTransactionReceipt")]
        eth_getTransactionReceipt,

        [EnumMember(Value = "eth_getUncleByBlockHashAndIndex")]
        eth_getUncleByBlockHashAndIndex,

        [EnumMember(Value = "eth_getUncleByBlockNumberAndIndex")]
        eth_getUncleByBlockNumberAndIndex,

        [EnumMember(Value = "eth_getCompilers")]
        eth_getCompilers,

        [EnumMember(Value = "eth_compileSolidity")]
        eth_compileSolidity,

        [EnumMember(Value = "eth_compileLLL")]
        eth_compileLLL,

        [EnumMember(Value = "eth_compileSerpent")]
        eth_compileSerpent,

        [EnumMember(Value = "eth_newFilter")]
        eth_newFilter,

        [EnumMember(Value = "eth_newBlockFilter")]
        eth_newBlockFilter,

        [EnumMember(Value = "eth_newPendingTransactionFilter")]
        eth_newPendingTransactionFilter,

        [EnumMember(Value = "eth_uninstallFilter")]
        eth_uninstallFilter,

        [EnumMember(Value = "eth_getFilterChanges")]
        eth_getFilterChanges,

        [EnumMember(Value = "eth_getFilterLogs")]
        eth_getFilterLogs,

        [EnumMember(Value = "eth_getLogs")]
        eth_getLogs,

        [EnumMember(Value = "eth_getWork")]
        eth_getWork,

        [EnumMember(Value = "eth_submitWork")]
        eth_submitWork,

        [EnumMember(Value = "eth_submitHashrate")]
        eth_submitHashrate,

        [EnumMember(Value = "eth_chainId")]
        eth_chainId,

        [EnumMember(Value = "db_putString")]
        db_putString,

        [EnumMember(Value = "db_getString")]
        db_getString,

        [EnumMember(Value = "db_putHex")]
        db_putHex,

        [EnumMember(Value = "db_getHex")]
        db_getHex,

        [EnumMember(Value = "shh_version")]
        shh_version,

        [EnumMember(Value = "shh_post")]
        shh_post,

        [EnumMember(Value = "shh_newIdentity")]
        shh_newIdentity,

        [EnumMember(Value = "shh_hasIdentity")]
        shh_hasIdentity,

        [EnumMember(Value = "shh_newGroup")]
        shh_newGroup,

        [EnumMember(Value = "shh_addToGroup")]
        shh_addToGroup,

        [EnumMember(Value = "shh_newFilter")]
        shh_newFilter,

        [EnumMember(Value = "shh_uninstallFilter")]
        shh_uninstallFilter,

        [EnumMember(Value = "shh_getFilterChanges")]
        shh_getFilterChanges,

        [EnumMember(Value = "shh_getMessages")]
        shh_getMessages,

        [EnumMember(Value = "evm_snapshot")]
        evm_snapshot,

        [EnumMember(Value = "evm_revert")]
        evm_revert,

        [EnumMember(Value = "evm_increaseTime")]
        evm_increaseTime,

        [EnumMember(Value = "evm_mine")]
        evm_mine,

        #region Custom RPC
        [EnumMember(Value = "testing_setTracingEnabled")]
        testing_setTracingEnabled,
        [EnumMember(Value = "testing_getExecutionTrace")]
        testing_getExecutionTrace,
        [EnumMember(Value = "testing_getHashPreimage")]
        testing_getHashPreimage,

        [EnumMember(Value = "testing_setCoverageEnabled")]
        testing_setCoverageEnabled,
        [EnumMember(Value = "testing_getAllCoverageMaps")]
        testing_getAllCoverageMaps,
        [EnumMember(Value = "testing_getSingleCoverageMap")]
        testing_getSingleCoverageMap,
        [EnumMember(Value = "testing_clearSingleCoverage")]
        testing_clearSingleCoverage,
        [EnumMember(Value = "testing_clearAllCoverage")]
        testing_clearAllCoverage,

        [EnumMember(Value = "testing_setContractSizeCheckEnabled")]
        testing_setContractSizeCheckDisabled
        #endregion
    }
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public static class RpcApiMethods
    {
        static ConcurrentDictionary<RpcApiMethod, string> _enumStringMap = new ConcurrentDictionary<RpcApiMethod, string>();
        static ConcurrentDictionary<string, RpcApiMethod> _stringEnumMap = new ConcurrentDictionary<string, RpcApiMethod>();

        static int _maxEnumInt;

        static RpcApiMethods()
        {
            var enumType = typeof(RpcApiMethod);
            var methodEnums = Enum.GetValues(enumType).Cast<RpcApiMethod>();
            _maxEnumInt = methodEnums.Cast<int>().Max();
            foreach (var method in methodEnums)
            {
                var memberValue = method.GetMemberValue();
                _enumStringMap[method] = memberValue;
                _stringEnumMap[memberValue] = method;
            }
        }

        public static string Value(this RpcApiMethod method)
        {
            return _enumStringMap[method];
        }

        public static RpcApiMethod Create(string value)
        {
            if (_stringEnumMap.TryGetValue(value, out var methodEnum))
            {
                return methodEnum;
            }

            var newEnum = (RpcApiMethod)Interlocked.Increment(ref _maxEnumInt);
            _stringEnumMap[value] = newEnum;
            _enumStringMap[newEnum] = value;
            return newEnum;
        }
    }
}
