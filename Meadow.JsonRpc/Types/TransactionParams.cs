using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;

namespace Meadow.JsonRpc.Types
{
    public class TransactionParams
    {
        /// <summary>
        /// DATA, 20 Bytes - The address the transaction is send from.
        /// </summary>
        [JsonProperty("from", Required = Required.Always), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? From { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - (optional when creating new contract) The address the transaction is directed to.
        /// </summary>
        [JsonProperty("to"), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? To { get; set; }

        /// <summary>
        /// QUANTITY - (optional, default: 90000) Integer of the gas provided for the transaction execution. It will return unused gas.
        /// </summary>
        [JsonProperty("gas"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? Gas { get; set; }

        /// <summary>
        /// QUANTITY - (optional, default: To-Be-Determined) Integer of the gasPrice used for each paid gas
        /// </summary>
        [JsonProperty("gasPrice"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? GasPrice { get; set; }

        /// <summary>
        /// QUANTITY - (optional) Integer of the value sent with this transaction
        /// </summary>
        [JsonProperty("value"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? Value { get; set; }

        /// <summary>
        /// Hex string of the compiled code of a contract OR hash of the invoked method signature and encoded parameters
        /// </summary>
        [JsonProperty("data", Required = Required.Always), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] Data { get; set; }

        /// <summary>
        /// QUANTITY - (optional) Integer of a nonce. This allows to overwrite your own pending transactions that use the same nonce.
        /// </summary>
        [JsonProperty("nonce"), JsonConverter(typeof(JsonRpcHexConverter))]
        public ulong? Nonce { get; set; }

        public TransactionParams()
        {

        }

        public TransactionParams(Address? from = null, byte[] data = null, UInt256? value = null)
        {
            From = from;
            Data = data;
            Value = value;
        }
    }
}
