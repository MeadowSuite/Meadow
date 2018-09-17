using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;

namespace Meadow.JsonRpc.Types
{
    /// <summary>
    /// <see href="https://github.com/ethereum/wiki/wiki/JSON-RPC#eth_gettransactionreceipt"/>
    /// </summary>
    public class TransactionReceipt
    {
        /// <summary>
        /// DATA, 32 Bytes - hash of the transaction.
        /// </summary>
        [JsonProperty("transactionHash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash TransactionHash { get; set; }

        /// <summary>
        /// QUANTITY - integer of the transactions index position in the block.
        /// </summary>
        [JsonProperty("transactionIndex"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong TransactionIndex { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - hash of the block where this transaction was in.
        /// </summary>
        [JsonProperty("blockHash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash BlockHash { get; set; }

        /// <summary>
        /// QUANTITY - block number where this transaction was in.
        /// </summary>
        [JsonProperty("blockNumber"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong BlockNumber { get; set; }

        /// <summary>
        /// QUANTITY - The total amount of gas used when this transaction was executed in the block.
        /// </summary>
        [JsonProperty("cumulativeGasUsed"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong CumulativeGasUsed { get; set; }

        /// <summary>
        /// QUANTITY - The amount of gas used by this specific transaction alone.
        /// </summary>
        [JsonProperty("gasUsed"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong GasUsed { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - The contract address created, if the transaction was a contract creation, otherwise null.
        /// </summary>
        [JsonProperty("contractAddress"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? ContractAddress { get; set; }

        /// <summary>
        /// Array - Array of log objects, which this transaction generated.
        /// </summary>
        [JsonProperty("logs")]
        public FilterLogObject[] Logs { get; set; }

        /// <summary>
        /// DATA, 256 Bytes - Bloom filter for light clients to quickly retrieve related logs.
        /// </summary>
        [JsonProperty("logsBloom"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] LogsBloom { get; set; }

        /// <summary>
        /// QUANTITY either 1 (success) or 0 (failure)
        /// </summary>
        [JsonProperty("status"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Status { get; set; }

    }
}
