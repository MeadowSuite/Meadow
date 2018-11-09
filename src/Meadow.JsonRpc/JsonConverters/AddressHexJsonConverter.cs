using Newtonsoft.Json;
using Meadow.Core.Utils;
using System;
using Meadow.Core.EthTypes;
using Newtonsoft.Json.Linq;

namespace Meadow.JsonRpc.JsonConverters
{
    public class AddressHexArrayJsonConverter : JsonConverter<Address[]>
    {
        public override Address[] ReadJson(JsonReader reader, Type objectType, Address[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var arr = JArray.Load(reader);
                    var result = new Address[arr.Count];
                    for (var i = 0; i < result.Length; i++)
                    {
                        result[i] = arr[i].Value<string>();
                    }

                    return result;
                }
                else if (reader.TokenType == JsonToken.String)
                {
                    return new Address[] { (string)reader.Value };
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, Address[] value, JsonSerializer serializer)
        {
            try
            {
                writer.WriteStartArray();

                foreach (var addr in value)
                {
                    string val = HexConverter.GetHex(addr, hexPrefix: true);
                    writer.WriteToken(JsonToken.String, val);
                }

                writer.WriteEndArray();
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{value}'", ex);
            }
        }
    }

    public class AddressHexJsonConverter : JsonConverter<Address>
    {
        public override Address ReadJson(JsonReader reader, Type objectType, Address existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.Value == null)
                {
                    return default;
                }

                if (reader.Value is string hex)
                {
                    if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        objectType = Nullable.GetUnderlyingType(objectType);
                    }

                    return HexConverter.HexToValue<Address>(hex);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, Address value, JsonSerializer serializer)
        {
            try
            {
                string val = HexConverter.GetHex(value, hexPrefix: true);
                writer.WriteToken(JsonToken.String, val);
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{value}'", ex);
            }
        }
    }
}
