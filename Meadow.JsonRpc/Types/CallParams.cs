using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;

namespace Meadow.JsonRpc.Types
{
    public class CallParams
    {
        /// <summary>
        /// DATA, 20 Bytes - The address the transaction is send from.
        /// </summary>
        [JsonProperty("from", Required = Required.Always), JsonConverter(typeof(JsonRpcHexConverter))]
        public Address? From { get; set; }

        /// <summary>
        /// DATA, 20 Bytes - (optional when creating new contract) The address the transaction is directed to.
        /// </summary>
        [JsonProperty("to", Required = Required.Always), JsonConverter(typeof(JsonRpcHexConverter))]
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
        [JsonProperty("data"), JsonConverter(typeof(JsonRpcHexConverter))]
        public byte[] Data { get; set; }


        public CallParams()
        {

        }

        public CallParams(Address? from = null, byte[] data = null, UInt256? value = null)
        {
            From = from;
            Data = data;
            Value = value;
        }
    }
}
