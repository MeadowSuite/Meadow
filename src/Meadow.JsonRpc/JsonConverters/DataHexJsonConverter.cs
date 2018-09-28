using Newtonsoft.Json;
using Meadow.Core.Utils;
using System;
using Meadow.Core.EthTypes;
using System.Linq;

namespace Meadow.JsonRpc.JsonConverters
{
    public class DataHexCompactedJsonConverter : JsonConverter<Data[]>
    {
        public override Data[] ReadJson(JsonReader reader, Type objectType, Data[] existingValue, bool hasExistingValue, JsonSerializer serializer)
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

                    var bytes = hex.HexToSpan();
                    if (bytes.Length % Data.SIZE != 0)
                    {
                        throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json Data[], byte length must be divisble by {Data.SIZE} but it is {bytes.Length} bytes; value: '{reader.Value}'");
                    }

                    var dataItems = new Data[bytes.Length / Data.SIZE];
                    for (var i = 0; i < dataItems.Length; i++)
                    {
                        dataItems[i] = new Data(bytes.Slice(i * Data.SIZE, Data.SIZE));
                    }

                    return dataItems;
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, Data[] value, JsonSerializer serializer)
        {
            Span<byte> bytes = new byte[value.Length * Data.SIZE];
            for (var i = 0; i < value.Length; i++)
            {
                value[i].GetSpan().CopyTo(bytes.Slice(i * Data.SIZE));
            }

            var hexStr = bytes.ToHexString(hexPrefix: true);
            writer.WriteToken(JsonToken.String, hexStr);
        }
    }

    public class DataHexJsonConverter : JsonConverter<Data>
    {
        public override Data ReadJson(JsonReader reader, Type objectType, Data existingValue, bool hasExistingValue, JsonSerializer serializer)
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

                    return HexConverter.HexToValue<Data>(hex);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, Data value, JsonSerializer serializer)
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
