using Meadow.Core.EthTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Meadow.Core.AbiEncoding
{

    public static class AbiTypeMap
    {
        /// <summary>
        /// Map of all finite solidity type names and their corresponding C# type.
        /// Includes all the static / elementary types, as well as the explicit dynamic
        /// types 'string' and 'bytes'.
        /// 
        /// Does not include the array or tuple types; i.e.: &lt;type&gt;[M], &lt;type&gt;[], or (T1,T2,...,Tn)
        /// 
        /// Note: All possible values of the C# types do not neccessarily fit into the corresponding
        /// solidity types. For example a UInt32 C# type is used for a UInt24 solidity type.
        /// Integer over/underflows are checked at runtime during encoding.
        /// 
        /// ByteSize is zero for dynamic types.
        /// </summary>
        static readonly ReadOnlyDictionary<string, AbiTypeInfo> _finiteTypes;

        /// <summary>
        /// Cache of solidity types parsed during runtime; eg: arrays, tuples
        /// </summary>
        static readonly ConcurrentDictionary<string, AbiTypeInfo> _cachedTypes = new ConcurrentDictionary<string, AbiTypeInfo>();

        static AbiTypeMap()
        {
            // elementary types
            var dict = new Dictionary<string, AbiTypeInfo>
            {
                // equivalent to uint8 restricted to the values 0 and 1
                ["bool"] = new AbiTypeInfo("bool", typeof(bool), 1, elementaryBaseType: SolidityTypeElementaryBase.Bool),
                
                // 20 bytes
                ["address"] = new AbiTypeInfo("address", typeof(Address), 20, elementaryBaseType: SolidityTypeElementaryBase.Address),
                
                // dynamic sized unicode string assumed to be UTF - 8 encoded.
                ["string"] = new AbiTypeInfo("string", typeof(string), 1, SolidityTypeCategory.String),
                
                // dynamic sized byte sequence
                ["bytes"] = new AbiTypeInfo("bytes", typeof(IEnumerable<byte>), 1, SolidityTypeCategory.Bytes)
            };

            // fixed sized bytes elementary types
            for (var i = 1; i <= UInt256.SIZE; i++)
            {
                dict["bytes" + i] = new AbiTypeInfo(
                    "bytes" + i, 
                    typeof(IEnumerable<byte>), 
                    primitiveTypeByteSize: i,
                    SolidityTypeCategory.Elementary,
                    SolidityTypeElementaryBase.Bytes,
                    arrayTypeLength: i);
            }

            // signed and unsigned integer type of M bits, 0 < M <= 256, M % 8 == 0
            AddIntRange<sbyte, byte>(1, 1);
            AddIntRange<short, ushort>(2, 2);
            AddIntRange<int, uint>(3, 4);
            AddIntRange<long, ulong>(5, 8);
            AddIntRange<BigInteger, UInt256>(9, 32);

            void AddIntRange<TIntType, TUIntType>(int byteStart, int byteEnd)
            {
                for (var i = byteStart; i <= byteEnd; i++)
                {
                    var bits = i * 8;
                    string typeSigned = "int" + bits;
                    string typeUnsigned = "uint" + bits;
                    dict.Add(typeSigned, new AbiTypeInfo(typeSigned, typeof(TIntType), i, elementaryBaseType: SolidityTypeElementaryBase.Int));
                    dict.Add(typeUnsigned, new AbiTypeInfo(typeUnsigned, typeof(TUIntType), i, elementaryBaseType: SolidityTypeElementaryBase.UInt));
                }
            }

            _finiteTypes = new ReadOnlyDictionary<string, AbiTypeInfo>(dict);
        }

        public static string SolidityTypeToClrTypeString(string name)
        {
            var info = GetSolidityTypeInfo(name);
            return info.ClrTypeName;
        }

        static readonly char[] SquareBracketChars = new[] { '[', ']' };

        static readonly string[] SquareBracketString = new[] { "][" };

        static int[] ParseArrayDimensionSizes(string brackets)
        {
            var parts = brackets.Substring(1, brackets.Length - 2).Split(SquareBracketString, StringSplitOptions.None);
            var result = new int[parts.Length];
            for (var i = 0; i < result.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result[i] = int.Parse(parts[i], CultureInfo.InvariantCulture);
                }
            }

            return result;
        }

        public static AbiTypeInfo GetSolidityTypeInfo(string name)
        {
            var arrayBracket = name.IndexOf('[');
            if (arrayBracket > 0)
            {
                if (_cachedTypes.TryGetValue(name, out var t))
                {
                    return t;
                }

                var bracketPart = name.Substring(arrayBracket);
                int arraySize = 0;
                var typeCategory = SolidityTypeCategory.DynamicArray;

                int[] arrayDimensionSizes = null;

                // if a fixed array length has been set, ex: uint64[10]
                if (bracketPart.Length > 2)
                {
                    if (bracketPart.Contains("]["))
                    {
                        arrayDimensionSizes = ParseArrayDimensionSizes(bracketPart);
                        if (arrayDimensionSizes[0] > 0)
                        {
                            arraySize = arrayDimensionSizes[0];
                            typeCategory = SolidityTypeCategory.FixedArray;
                        }
                    }
                    else
                    {
                        // parse the number within the square brackets
                        var sizeStr = bracketPart.Substring(1, bracketPart.Length - 2);
                        arraySize = int.Parse(sizeStr, CultureInfo.InvariantCulture);
                        typeCategory = SolidityTypeCategory.FixedArray;
                    }
                }

                var baseName = name.Substring(0, arrayBracket);
                if (_finiteTypes.TryGetValue(baseName, out var baseInfo))
                {
                    var arrayType = typeof(IEnumerable<>).MakeGenericType(baseInfo.ClrType);
                    var info = new AbiTypeInfo(name, arrayType, baseInfo.PrimitiveTypeByteSize, typeCategory, SolidityTypeElementaryBase.None, arraySize, baseInfo, arrayDimensionSizes);
                    _cachedTypes[name] = info;
                    return info;
                }
            }
            else if (_finiteTypes.TryGetValue(name, out var t))
            {
                return t;
            }
    
            throw new ArgumentException("Unexpected solidity ABI type: " + name, nameof(name));

        }

    

    }




}
