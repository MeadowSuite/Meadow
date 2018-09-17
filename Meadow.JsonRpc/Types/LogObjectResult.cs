using Meadow.Core.EthTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    public enum LogObjectResultType
    {
        /// <summary>
        /// For filters created with eth_newBlockFilter or eth_newPendingTransactionFilter
        /// </summary>
        Hashes,

        /// <summary>
        /// For filters created with eth_newFilter
        /// </summary>
        LogObjects
    }

    [JsonConverter(typeof(LogObjectResultConverter))]
    public class LogObjectResult
    {
        public LogObjectResultType ResultType { get; set; }

        /// <summary>
        /// Only set when <see cref="ResultType"/> is <see cref="LogObjectResultType.Hashes"/>
        /// (filters created with eth_newBlockFilter or eth_newPendingTransactionFilter), otherwise null.
        /// </summary>
        public Hash[] Hashes { get; set; }

        /// <summary>
        /// Only set when <see cref="ResultType"/> is <see cref="LogObjectResultType.LogObjects"/> (filters created with eth_newFilter), otherwise null.
        /// </summary>
        public FilterLogObject[] LogObjects { get; set; }

    }


    class LogObjectResultConverter : JsonConverter<LogObjectResult>
    {
        public override LogObjectResult ReadJson(JsonReader reader, Type objectType, LogObjectResult existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    var arr = JArray.Load(reader);
                    var tokens = arr.Children().ToArray();
                    if (tokens.Length == 0)
                    {
                        return null;
                    }

                    if (tokens[0].Type == JTokenType.String)
                    {
                        var result = new LogObjectResult
                        {
                            ResultType = LogObjectResultType.Hashes,
                            Hashes = new Hash[tokens.Length]
                        };
                        for (var i = 0; i < tokens.Length; i++)
                        {
                            result.Hashes[i] = tokens[i].Value<string>();
                        }

                        return result;
                    }
                    else
                    {
                        var result = new LogObjectResult
                        {
                            ResultType = LogObjectResultType.LogObjects,
                            LogObjects = new FilterLogObject[tokens.Length]
                        };
                        for (var i = 0; i < tokens.Length; i++)
                        {
                            result.LogObjects[i] = tokens[i].ToObject<FilterLogObject>();
                        }

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception deserializing json value for {nameof(DefaultBlockParameter)}: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Unexpected json value for {nameof(DefaultBlockParameter)}: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, LogObjectResult value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteToken(JsonToken.Null);
            }

            try
            {
                writer.WriteStartArray();
                if (value.ResultType == LogObjectResultType.LogObjects)
                {
                    if (value.LogObjects?.Length > 0)
                    {
                        foreach (var log in value.LogObjects)
                        {
                            JToken.FromObject(log).WriteTo(writer);
                        }
                    }
                }
                else
                {
                    if (value.Hashes?.Length > 0)
                    {
                        foreach (var hash in value.Hashes)
                        {
                            writer.WriteToken(JsonToken.String, hash.GetHexString(hexPrefix: true));
                        }
                    }
                }

                writer.WriteEndArray();
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value for {nameof(LogObjectResult)}: '{value.ResultType}'", ex);
            }
        }
    }
}
