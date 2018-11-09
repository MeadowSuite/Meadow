using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    public class FilterOptions
    {
        /// <summary>
        /// (optional, default: "latest") Integer block number, or "latest" for 
        /// the last mined block or "pending", "earliest" for not yet mined transactions.
        /// </summary>
        [JsonProperty("fromBlock"), JsonConverter(typeof(DefaultBlockParameterConverter))]
        public DefaultBlockParameter FromBlock { get; set; }

        /// <summary>
        /// (optional, default: "latest") Integer block number, or "latest" for the 
        /// last mined block or "pending", "earliest" for not yet mined transactions.
        /// </summary>
        [JsonProperty("toBlock"), JsonConverter(typeof(DefaultBlockParameterConverter))]
        public DefaultBlockParameter ToBlock { get; set; }

        /// <summary>
        /// (optional) Contract address or a list of addresses from which logs should originate.
        /// </summary>
        [JsonProperty("address"), JsonConverter(typeof(AddressHexArrayJsonConverter))]
        public Address[] Address { get; set; }

        /// <summary>
        /// (optional) Array of 32 Bytes DATA topics. Topics are order-dependent. 
        /// Each topic can also be an array of DATA with "or" options.
        /// A note on specifying topic filters: Topics are order-dependent. A transaction with a
        /// log with topics [A, B] will be matched by the following topic filters:
        ///     [] "anything"
        ///     [A] "A in first position (and anything after)"
        ///     [null, B] "anything in first position AND B in second position (and anything after)"
        ///     [A, B] "A in first position AND B in second position (and anything after)"
        ///     [[A, B], [A, B]] "(A OR B) in first position AND (A OR B) in second position (and anything after)"
        /// </summary>
        [JsonProperty("topics", ItemConverterType = typeof(DataHexJsonConverter))]
        public Data?[] Topics { get; set; }
    }
}
