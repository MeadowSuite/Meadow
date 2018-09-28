using Meadow.Core.EthTypes;
using Meadow.JsonRpc.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    [JsonConverter(typeof(SyncStatusConverter))]
    public class SyncStatus
    {
        /// <summary>
        /// If false then the other properties in this object will be null.
        /// </summary>
        [JsonIgnore]
        public bool IsSyncing { get; set; }

        /// <summary>
        /// (Only set if syncing is true).
        /// The block at which the import started (will only be reset, after the sync reached his head).
        /// </summary>
        [JsonProperty("startingBlock"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? StartingBlock { get; set; }

        /// <summary>
        /// (Only set if syncing is true).
        /// The current block, same as eth_blockNumber.
        /// </summary>
        [JsonProperty("currentBlock"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? CurrentBlock { get; set; }

        /// <summary>
        /// (Only set if syncing is true).
        /// The estimated highest block
        /// </summary>
        [JsonProperty("highestBlock"), JsonConverter(typeof(JsonRpcHexConverter))]
        public UInt256? HighestBlock { get; set; }

    }

    class SyncStatusConverter : JsonConverter<SyncStatus>
    {
        public override SyncStatus ReadJson(JsonReader reader, Type objectType, SyncStatus existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Boolean)
                {
                    return new SyncStatus { IsSyncing = JToken.Load(reader).Value<bool>() };
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    var jObj = JObject.Load(reader);
                    return new SyncStatus
                    {
                        IsSyncing = true,
                        StartingBlock = jObj["startingBlock"].ToObject<UInt256?>(JsonRpcSerializer.Serializer),
                        CurrentBlock = jObj["currentBlock"].ToObject<UInt256?>(JsonRpcSerializer.Serializer),
                        HighestBlock = jObj["highestBlock"].ToObject<UInt256?>(JsonRpcSerializer.Serializer)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception deserializing json value for {nameof(SyncStatus)}: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Unexpected json value for {nameof(SyncStatus)}: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, SyncStatus value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteToken(JsonToken.Null);
            }

            try
            {
                if (value.IsSyncing)
                {
                    var jObj = new JObject();
                    jObj["startingBlock"] = JToken.FromObject(value.StartingBlock, JsonRpcSerializer.Serializer);
                    jObj["currentBlock"] = JToken.FromObject(value.CurrentBlock, JsonRpcSerializer.Serializer);
                    jObj["highestBlock"] = JToken.FromObject(value.HighestBlock, JsonRpcSerializer.Serializer);
                    jObj.WriteTo(writer);
                }
                else
                {
                    writer.WriteToken(JsonToken.Boolean, false);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value for {nameof(SyncStatus)}: '{value}'", ex);
            }
        }
    }
}
