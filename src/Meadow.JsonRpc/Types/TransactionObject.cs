using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    /// <summary>
    /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactionbyhash"/>
    /// </summary>
    public class TransactionObject
    {
        /// <summary>
        /// DATA, 32 Bytes - hash of the transaction.
        /// </summary>
        [JsonProperty("hash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash Hash { get; set; }

        /// <summary>
        /// QUANTITY - the number of transactions made by the sender prior to this one
        /// </summary>
        [JsonProperty("nonce"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Nonce { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - hash of the block where this transaction was in. null when its pending.
        /// </summary>
        [JsonProperty("blockHash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash? BlockHash { get; set; }

        /// <summary>
        /// QUANTITY - block number where this transaction was in. null when its pending.
        /// </summary>
        [JsonProperty("blockNumber"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong? BlockNumber { get; set; }

        /// <summary>
        /// QUANTITY - integer of the transactions index position in the block. null when its pending.
        /// </summary>
        [JsonProperty("transactionIndex"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong? TransactionIndex { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - address of the sender.
        /// </summary>
        [JsonProperty("from"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address From { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - address of the receiver. null when its a contract creation transaction.
        /// </summary>
        [JsonProperty("to"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? To { get; set; }

        /// <summary>
        /// QUANTITY - value transferred in Wei.
        /// </summary>
        [JsonProperty("value"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256 Value { get; set; }

        /// <summary>
        /// QUANTITY - gas price provided by the sender in Wei.
        /// </summary>
        [JsonProperty("gasPrice"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong GasPrice { get; set; }

        /// <summary>
        /// QUANTITY - gas provided by the sender.
        /// </summary>
        [JsonProperty("gas"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Gas { get; set; }

        /// <summary>
        /// DATA - the data send along with the transaction.
        /// </summary>
        [JsonProperty("input"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] Input { get; set; }
    }
}
