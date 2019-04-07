using System;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using SolcNet.DataDescription.Parsing;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

namespace SolcNet.DataDescription.Input
{
    [JsonConverter(typeof(OutputTypes.JsonEnumConverter))]
    [Flags]
    public enum OutputType : long
    {
        /// <summary>ABI</summary>
        [EnumMember(Value = "abi")]
        Abi = 1L << 0,

        /// <summary>AST of all source files</summary>
        [EnumMember(Value = "ast")]
        Ast = 1L << 1,

        /// <summary>legacy AST of all source files</summary>
        [EnumMember(Value = "legacyAST")]
        LegacyAst = 1L << 2,

        /// <summary>Developer documentation (natspec)</summary>
        [EnumMember(Value = "devdoc")]
        DevDoc = 1L << 3,

        /// <summary>User documentation (natspec)</summary>
        [EnumMember(Value = "userdoc")]
        UserDoc = 1L << 4,

        /// <summary>Metadata</summary>
        [EnumMember(Value = "metadata")]
        Metadata = 1L << 5,

        /// <summary>New assembly format before desugaring</summary>
        [EnumMember(Value = "ir")]
        IR = 1L << 6,

        /// <summary>All evm related targets</summary>
        [EnumMember(Value = "evm")]
        Evm = 1L << 7,

        /// <summary>New assembly format after desugaring</summary>
        [EnumMember(Value = "evm.assembly")]
        EvmAssembly = 1L << 8,

        /// <summary>Old-style assembly format in JSON</summary>
        [EnumMember(Value = "evm.legacyAssembly")]
        EvmLegacyAssembly = 1L << 9,

        /// <summary>All bytecode related targets</summary>
        [EnumMember(Value = "evm.bytecode")]
        EvmBytecode = 1L << 10,

        /// <summary>Bytecode object</summary>
        [EnumMember(Value = "evm.bytecode.object")]
        EvmBytecodeObject = 1L << 11,

        /// <summary>Opcodes list</summary>
        [EnumMember(Value = "evm.bytecode.opcodes")]
        EvmBytecodeOpcodes = 1L << 12,

        /// <summary>Source mapping (useful for debugging)</summary>
        [EnumMember(Value = "evm.bytecode.sourceMap")]
        EvmBytecodeSourceMap = 1L << 13,

        /// <summary>Link references (if unlinked object)</summary>
        [EnumMember(Value = "evm.bytecode.linkReferences")]
        EvmBytecodeLinkReferences = 1L << 14,

        /// <summary>Deployed bytecode (has the same options as evm.bytecode)</summary>
        [EnumMember(Value = "evm.deployedBytecode")]
        EvmDeployedBytecode = 1L << 15,

        /// <summary>Bytecode object</summary>
        [EnumMember(Value = "evm.deployedBytecode.object")]
        EvmDeployedBytecodeObject = 1L << 16,

        /// <summary>Opcodes list</summary>
        [EnumMember(Value = "evm.deployedBytecode.opcodes")]
        EvmDeployedBytecodeOpcodes = 1L << 17,

        /// <summary>Source mapping (useful for debugging)</summary>
        [EnumMember(Value = "evm.deployedBytecode.sourceMap")]
        EvmDeployedBytecodeSourceMap = 1L << 18,

        /// <summary>Link references (if unlinked object)</summary>
        [EnumMember(Value = "evm.deployedBytecode.linkReferences")]
        EvmDeployedBytecodeLinkReferences = 1L << 19,

        /// <summary>The list of function hashes</summary>
        [EnumMember(Value = "evm.methodIdentifiers")]
        EvmMethodIdentifiers = 1L << 20,

        /// <summary>Function gas estimates</summary>
        [EnumMember(Value = "evm.gasEstimates")]
        EvmGasEstimates = 1L << 21,

        /// <summary>All eWASM related targets</summary>
        [EnumMember(Value = "ewasm")]
        Ewasm = 1L << 22,

        /// <summary>eWASM S-expressions format (not supported atm)</summary>
        [EnumMember(Value = "ewasm.wast")]
        EwasmWast = 1L << 23,

        /// <summary>eWASM binary format (not supported atm)</summary>
        [EnumMember(Value = "ewasm.wasm")]
        EwasmWasm = 1L << 24
    }

    public static class OutputTypes
    {
        public static readonly OutputType[] All;

        public static readonly Dictionary<OutputType, string> CustomTypes = new Dictionary<OutputType, string>();
        static int NEXT_ID;

        static OutputTypes()
        {
            All = Enum.GetValues(typeof(OutputType)).Cast<OutputType>().ToArray();
            long maxEnum = All.Cast<long>().Max();
            while ((1L << NEXT_ID) <= maxEnum)
            {
                NEXT_ID++;
            }
        }

        public static OutputType[] GetItems(OutputType outputType)
        {
            List<OutputType> types = new List<OutputType>();

            foreach(var ot in All)
            {
                if ((outputType & ot) == ot)
                {
                    types.Add(ot);
                }
            }
            foreach(var item in CustomTypes)
            {
                if ((outputType & item.Key) == item.Key)
                {
                    types.Add(item.Key);
                }
            }
            return types.ToArray();
        }

        public static OutputType FromString(string str)
        {
            foreach(var item in CustomTypes)
            {
                if (item.Value == str)
                {
                    return item.Key;
                }
            }
            var enumID = Interlocked.Increment(ref NEXT_ID);
            OutputType enumVal = (OutputType)(1L << enumID);
            CustomTypes.Add(enumVal, str);
            return enumVal;
        }

        public class JsonEnumConverter : JsonConverter<OutputType>
        {
            public override OutputType ReadJson(JsonReader reader, Type objectType, OutputType existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var str = reader.Value.ToString();
                if (Enum.TryParse<OutputType>(str, ignoreCase: true, out var result))
                {
                    return result;
                }
                return FromString(str);
            }

            public override void WriteJson(JsonWriter writer, OutputType value, JsonSerializer serializer)
            {
                string enumStrVal;
                if (Enum.IsDefined(typeof(OutputType), value))
                {
                    var type = value.GetType();
                    enumStrVal = type.GetField(Enum.GetName(type, value)).GetCustomAttribute<EnumMemberAttribute>().Value;
                }
                else
                {
                    enumStrVal = CustomTypes[value];
                }
                writer.WriteToken(JsonToken.String, enumStrVal);
            }
        }
    }
    



}
