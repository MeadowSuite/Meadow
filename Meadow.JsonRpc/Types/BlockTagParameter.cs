using Meadow.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Meadow.JsonRpc.Types
{
    public enum BlockParameterType
    {
        [EnumMember(Value = "latest")]
        Latest,

        [EnumMember(Value = "earliest")]
        Earliest,

        [EnumMember(Value = "pending")]
        Pending,

        /// <summary>
        /// Use <see cref="DefaultBlockParameter.BlockNumber"/> for this parameter type
        /// </summary>
        BlockNumber
    }

    [JsonConverter(typeof(DefaultBlockParameterConverter))]
    public class DefaultBlockParameter
    {
        public static readonly DefaultBlockParameter Default = BlockParameterType.Latest;

        public BlockParameterType ParameterType { get; set; }

        /// <summary>
        /// Only set if <see cref="ParameterType"/> is set to <see cref="BlockParameterType.BlockNumber"/>
        /// </summary>
        public ulong? BlockNumber { get; set; }

        public DefaultBlockParameter() { }

        public DefaultBlockParameter(ulong blockNumber)
        {
            ParameterType = BlockParameterType.BlockNumber;
            BlockNumber = blockNumber;
        }

        public DefaultBlockParameter(BlockParameterType blockParameterType)
        {
            ParameterType = blockParameterType;
        }

        public override string ToString()
        {
            if (ParameterType == BlockParameterType.BlockNumber)
            {
                return HexConverter.GetHexFromInteger(BlockNumber.Value, hexPrefix: true);
            }
            else
            {
                return ParameterType.GetMemberValue();
            }
        }

        public static implicit operator DefaultBlockParameter(BlockParameterType paramType) => new DefaultBlockParameter(paramType);
        public static implicit operator DefaultBlockParameter(ulong blockNumber) => new DefaultBlockParameter(blockNumber);

        public static implicit operator BlockParameterType(DefaultBlockParameter param) => param.ParameterType;
    }

    class DefaultBlockParameterConverter : JsonConverter<DefaultBlockParameter>
    {
        public override DefaultBlockParameter ReadJson(JsonReader reader, Type objectType, DefaultBlockParameter existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            try
            {
                if (reader.Value is string str)
                {
                    if (str == BlockParameterType.Earliest.GetMemberValue())
                    {
                        return new DefaultBlockParameter(BlockParameterType.Earliest);
                    }

                    if (str == BlockParameterType.Latest.GetMemberValue())
                    {
                        return new DefaultBlockParameter(BlockParameterType.Latest);
                    }

                    if (str == BlockParameterType.Pending.GetMemberValue())
                    {
                        return new DefaultBlockParameter(BlockParameterType.Pending);
                    }

                    var blockNum = HexConverter.HexToInteger<ulong>(str);
                    return new DefaultBlockParameter(blockNum);
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Exception deserializing json value for {nameof(DefaultBlockParameter)}: '{reader.Value}'", ex);
            }

            throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Unexpected json value for {nameof(DefaultBlockParameter)}: '{reader.Value}'");
        }

        public override void WriteJson(JsonWriter writer, DefaultBlockParameter value, JsonSerializer serializer)
        {
            try
            {
                if (value == null)
                {
                    writer.WriteToken(JsonToken.Null);
                }
                else if (value == BlockParameterType.BlockNumber)
                {
                    if (!value.BlockNumber.HasValue)
                    {
                        throw new ArgumentException("Block number must be set");
                    }

                    var numHex = HexConverter.GetHexFromInteger(value.BlockNumber.Value, hexPrefix: true);
                    writer.WriteToken(JsonToken.String, numHex);
                }
                else
                {
                    writer.WriteToken(JsonToken.String, value.ParameterType.GetMemberValue());
                }
            }
            catch (Exception ex)
            {
                throw new JsonRpcErrorException(JsonRpcErrorCode.ParseError, $"Error serializing json value for {nameof(DefaultBlockParameter)}: '{value}'", ex);
            }
        }
    }
}
