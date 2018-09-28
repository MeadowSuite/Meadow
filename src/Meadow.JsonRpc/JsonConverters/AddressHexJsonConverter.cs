using Newtonsoft.Json;
using Meadow.Core.Utils;
using System;
using Meadow.Core.EthTypes;

namespace Meadow.JsonRpc.JsonConverters
{
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
