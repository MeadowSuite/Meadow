using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    public class Block
    {
        /// <summary>
        /// QUANTITY - the block number. null when its pending block.
        /// </summary>
        [JsonProperty("number"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong? Number { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - hash of the block. null when its pending block.
        /// </summary>
        [JsonProperty("hash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash? Hash { get; set; }

        /// <summary>
        /// Hash which proves combined with the nonce that a sufficient amount of computation has been carried out on this block
        /// </summary>
        [JsonProperty("mixHash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash? MixHash { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - hash of the parent block.
        /// </summary>
        [JsonProperty("parentHash"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash ParentHash { get; set; }

        /// <summary>
        /// DATA, 8 Bytes - hash of the generated proof-of-work. null when its pending block.
        /// </summary>
        [JsonProperty("nonce"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Nonce { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - SHA3 of the uncles data in the block.
        /// </summary>
        [JsonProperty("sha3Uncles"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Hash Sha3Uncles { get; set; }

        /// <summary>
        /// DATA, 256 Bytes - the bloom filter for the logs of the block. null when its pending block
        /// </summary>
        [JsonProperty("logsBloom"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] LogsBloom { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - the root of the transaction trie of the block.
        /// </summary>
        [JsonProperty("transactionsRoot"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Data TransactionsRoot { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - the root of the final state trie of the block.
        /// </summary>
        [JsonProperty("stateRoot")]
        public Data StateRoot { get; set; }

        /// <summary>
        /// DATA, 32 Bytes - the root of the receipts trie of the block.
        /// </summary>
        [JsonProperty("receiptsRoot"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Data ReceiptsRoot { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - the address of the beneficiary to whom the mining rewards were given.
        /// </summary>
        [JsonProperty("miner"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address Miner { get; set; }

        /// <summary>
        /// QUANTITY - integer of the difficulty for this block.
        /// </summary>
        [JsonProperty("difficulty"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Difficulty { get; set; }

        /// <summary>
        /// QUANTITY - integer of the total difficulty of the chain until this block.
        /// </summary>
        [JsonProperty("totalDifficulty"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong TotalDifficulty { get; set; }

        /// <summary>
        /// DATA - the "extra data" field of this block.
        /// </summary>
        [JsonProperty("extraData"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] ExtraData { get; set; }

        /// <summary>
        /// QUANTITY - integer the size of this block in bytes.
        /// </summary>
        [JsonProperty("size"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Size { get; set; }

        /// <summary>
        /// QUANTITY - the maximum gas allowed in this block.
        /// </summary>
        [JsonProperty("gasLimit"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong GasLimit { get; set; }

        /// <summary>
        /// QUANTITY - the total used gas by all transactions in this block.
        /// </summary>
        [JsonProperty("gasUsed"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong GasUsed { get; set; }

        /// <summary>
        /// QUANTITY - the unix timestamp for when the block was collated.
        /// </summary>
        [JsonProperty("timestamp"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong Timestamp { get; set; }

        /// <summary>
        /// Array - Array of transaction objects, or 32 Bytes transaction hashes depending on the last given parameter.
        /// </summary>
        [JsonProperty("transactions")]
        public object Transactions { get; set; }

        /// <summary>
        /// Array - Array of uncle hashes.
        /// </summary>
        [JsonProperty("uncles", ItemConverterType = typeof(JsonRpcHexConverter))]
        public Hash?[] Uncles { get; set; }

        /// <summary>
        /// Properties not deserialized any members
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; }

    }
}
