using Meadow.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace Meadow.JsonRpc.JsonConverters
{
    public class StructArrayHexConverter<TElement> : JsonConverter<TElement[]> where TElement :
#if LANG_7_3
        unmanaged
#else
        struct
#endif
    {
        public override TElement[] ReadJson(JsonReader reader, Type objectType, TElement[] existingValue, bool hasExistingValue, JsonSerializer serializer)
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

                    Span<byte> val = hex.HexToSpan();
                    var elementSize = Marshal.SizeOf<TElement>();
                    var arr = new TElement[val.Length / elementSize];
                    var byteSpan = MemoryMarshal.Cast<TElement, byte>(arr);
                    val.CopyTo(byteSpan);
                    return arr;
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, TElement[] value, JsonSerializer serializer)
        {
            try
            {
                if (value == null)
                {
                    writer.WriteToken(JsonToken.Null);
                }
                else
                {
                    var bytes = MemoryMarshal.Cast<TElement, byte>(value);
                    var hexStr = bytes.ToHexString(hexPrefix: true);
                    writer.WriteToken(JsonToken.String, hexStr);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{value}'", ex);
            }
        }
    }
}
