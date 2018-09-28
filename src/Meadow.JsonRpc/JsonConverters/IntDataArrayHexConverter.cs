using Meadow.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.JsonRpc.JsonConverters
{

    public class IntDataArrayHexConverter : JsonConverter
    {
        static readonly HashSet<Type> _supportedTypes = new HashSet<Type>
        {
            typeof(byte[]),
            typeof(short[]),
            typeof(ushort[]),
            typeof(int[]),
            typeof(uint[]),
            typeof(long[]),
            typeof(ulong[])
        };

        public override bool CanConvert(Type objectType)
        {
            // Obtain the underlying type
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                objectType = Nullable.GetUnderlyingType(objectType);
            }

            // Verify it is a supported type.
            var canConvert = _supportedTypes.Contains(objectType);
            if (!canConvert)
            {
                return false;
            }

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

                    Span<byte> val = hex.HexToSpan();
                    var elementType = objectType.GetElementType();
                    var elementSize = Marshal.SizeOf(elementType);
                    var arr = Array.CreateInstance(elementType, val.Length / elementSize);

                    switch (arr)
                    {
                        case byte[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case sbyte[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case short[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case ushort[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case int[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case uint[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case long[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        case ulong[] d:
                            val.CopyTo(d.AsByteSpan());
                            break;
                        default:
                            throw new Exception("Unexpected type: " + arr.GetType());
                    }

                    return arr;
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception parsing json value: '{reader.Value}'");
        }

        Span<byte> AsByteSpan(object value)
        {
            switch (value)
            {
                case byte[] d:
                    return new Span<byte>(d);
                case sbyte[] d:
                    return MemoryMarshal.AsBytes((Span<sbyte>)d);
                case short[] d:
                    return MemoryMarshal.AsBytes((Span<short>)d);
                case ushort[] d:
                    return MemoryMarshal.AsBytes((Span<ushort>)d);
                case int[] d:
                    return MemoryMarshal.AsBytes((Span<int>)d);
                case uint[] d:
                    return MemoryMarshal.AsBytes((Span<uint>)d);
                case long[] d:
                    return MemoryMarshal.AsBytes((Span<long>)d);
                case ulong[] d:
                    return MemoryMarshal.AsBytes((Span<ulong>)d);
                default:
                    throw new Exception("Unexpected type: " + value.GetType());
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                if (value == null)
                {
                    writer.WriteToken(JsonToken.Null);
                }
                else
                {
                    var bytes = AsByteSpan(value);
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
