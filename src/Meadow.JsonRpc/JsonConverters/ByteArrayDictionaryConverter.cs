using Meadow.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.JsonRpc.JsonConverters
{

    public class ByteArrayDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Obtain the underlying type
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                objectType = Nullable.GetUnderlyingType(objectType);
            }

            // Verify it is a supported type.
            var canConvert = objectType == typeof(Dictionary<Memory<byte>, byte[]>);
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
                // Check if it's null, if so, return null
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                else if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new ArgumentException($"Invalid token type detected in {nameof(ByteArrayDictionaryConverter)}.");
                }

                // Determine the type
                if (objectType == typeof(Dictionary<Memory<byte>, byte[]>))
                {
                    // Create our dictionary
                    Dictionary<Memory<byte>, byte[]> result = new Dictionary<Memory<byte>, byte[]>(new MemoryComparer<byte>());

                    // Loop for all child tokens
                    byte[] key = null;
                    while (reader.Read())
                    {
                        // If this is an end object, break out
                        if (reader.TokenType == JsonToken.EndObject)
                        {
                            break;
                        }
                        else if (reader.TokenType == JsonToken.PropertyName)
                        {
                            // If we are reading a property name, set our key in our memory.
                            key = ((string)reader.Value).HexToBytes();
                        }
                        else if (reader.TokenType == JsonToken.String)
                        {
                            // If we are reading a string type, it's the value.

                            // Verify our key isn't null
                            if (key == null)
                            {
                                throw new ArgumentException($"Missing property name for value when deserializing in {nameof(ByteArrayDictionaryConverter)}.");
                            }

                            // Obtain our value.
                            byte[] value = ((string)reader.Value).HexToBytes();

                            // Add it to our lookup.
                            result.Add(key, value);

                            // Clear our key
                            key = null;
                        }
                    }

                    return result;
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
                // Mark the start of the dictionary.
                writer.WriteStartObject();

                // Write dictionary key-value pairs.
                if (value is Dictionary<Memory<byte>, byte[]>)
                {
                    var lookup = (Dictionary<Memory<byte>, byte[]>)value;
                    foreach (var keyValuePair in lookup)
                    {
                        // Obtain hex strings of the key/value.
                        var keyHexStr = keyValuePair.Key.ToHexString(hexPrefix: true);
                        var valueHexStr = keyValuePair.Value.ToHexString(hexPrefix: true);

                        // Write the property out.
                        writer.WritePropertyName(keyHexStr);
                        writer.WriteValue(valueHexStr);
                    }
                }

                // Mark the end of the dictionary.
                writer.WriteEndObject();
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception serializing json value: '{value}'", ex);
            }
        }
    }
}