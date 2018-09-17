using Newtonsoft.Json;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using Meadow.Core.EthTypes;

namespace Meadow.JsonRpc.JsonConverters
{

    public class JsonRpcHexConverter : JsonConverter
    {
        static readonly HashSet<Type> _supportedTypes = new HashSet<Type>
        {
            typeof(Address),
            typeof(Hash),
            typeof(Data),
            typeof(UInt256),
            typeof(ulong),
            typeof(long),
            typeof(uint),
            typeof(int),
            typeof(ushort),
            typeof(short),
            typeof(byte),
            typeof(sbyte),
            typeof(byte[])
        };

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                objectType = Nullable.GetUnderlyingType(objectType);
            }

            var canConvert = _supportedTypes.Contains(objectType);
            return canConvert;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.Value == null)
                {
                    return null;
                }

                if (reader.Value is string hex)
                {
                    if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        objectType = Nullable.GetUnderlyingType(objectType);
                    }

                    return HexConverter.HexToObject(objectType, hex);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                string val = HexConverter.GetHexFromObject(value, hexPrefix: true);
                writer.WriteToken(JsonToken.String, val);
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{value}'", ex);
            }
        }
    }
}
