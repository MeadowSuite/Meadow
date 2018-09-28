using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Meadow.Core
{
    public enum SolidityType
    {
        /// <summary>
        /// Dynamic sized unicode string assumed to be UTF-8 encoded.
        /// </summary>
        [EnumMember(Value = "string")]
        String,

        /// <summary>
        /// Dynamic sized byte sequence.
        /// </summary>
        [EnumMember(Value = "bytes")]
        Bytes,

        [EnumMember(Value = "address")]
        Address,

        /// <summary>
        /// Equivalent to uint8 restricted to the values 0 and 1.
        /// </summary>
        [EnumMember(Value = "bool")]
        Bool,

        [EnumMember(Value = "bytes1")]
        Bytes1,

        [EnumMember(Value = "bytes2")]
        Bytes2,

        [EnumMember(Value = "bytes3")]
        Bytes3,

        [EnumMember(Value = "bytes4")]
        Bytes4,

        [EnumMember(Value = "bytes5")]
        Bytes5,

        [EnumMember(Value = "bytes6")]
        Bytes6,

        [EnumMember(Value = "bytes7")]
        Bytes7,

        [EnumMember(Value = "bytes8")]
        Bytes8,

        [EnumMember(Value = "bytes9")]
        Bytes9,

        [EnumMember(Value = "bytes10")]
        Bytes10,

        [EnumMember(Value = "bytes11")]
        Bytes11,

        [EnumMember(Value = "bytes12")]
        Bytes12,

        [EnumMember(Value = "bytes13")]
        Bytes13,

        [EnumMember(Value = "bytes14")]
        Bytes14,

        [EnumMember(Value = "bytes15")]
        Bytes15,

        [EnumMember(Value = "bytes16")]
        Bytes16,

        [EnumMember(Value = "bytes17")]
        Bytes17,

        [EnumMember(Value = "bytes18")]
        Bytes18,

        [EnumMember(Value = "bytes19")]
        Bytes19,

        [EnumMember(Value = "bytes20")]
        Bytes20,

        [EnumMember(Value = "bytes21")]
        Bytes21,

        [EnumMember(Value = "bytes22")]
        Bytes22,

        [EnumMember(Value = "bytes23")]
        Bytes23,

        [EnumMember(Value = "bytes24")]
        Bytes24,

        [EnumMember(Value = "bytes25")]
        Bytes25,

        [EnumMember(Value = "bytes26")]
        Bytes26,

        [EnumMember(Value = "bytes27")]
        Bytes27,

        [EnumMember(Value = "bytes28")]
        Bytes28,

        [EnumMember(Value = "bytes29")]
        Bytes29,

        [EnumMember(Value = "bytes30")]
        Bytes30,

        [EnumMember(Value = "bytes31")]
        Bytes31,

        [EnumMember(Value = "bytes32")]
        Bytes32,

        [EnumMember(Value = "uint8")]
        UInt8,

        [EnumMember(Value = "uint16")]
        UInt16,

        [EnumMember(Value = "uint24")]
        UInt24,

        [EnumMember(Value = "uint32")]
        UInt32,

        [EnumMember(Value = "uint40")]
        UInt40,

        [EnumMember(Value = "uint48")]
        UInt48,

        [EnumMember(Value = "uint56")]
        UInt56,

        [EnumMember(Value = "uint64")]
        UInt64,

        [EnumMember(Value = "uint72")]
        UInt72,

        [EnumMember(Value = "uint80")]
        UInt80,

        [EnumMember(Value = "uint88")]
        UInt88,

        [EnumMember(Value = "uint96")]
        UInt96,

        [EnumMember(Value = "uint104")]
        UInt104,

        [EnumMember(Value = "uint112")]
        UInt112,

        [EnumMember(Value = "uint120")]
        UInt120,

        [EnumMember(Value = "uint128")]
        UInt128,

        [EnumMember(Value = "uint136")]
        UInt136,

        [EnumMember(Value = "uint144")]
        UInt144,

        [EnumMember(Value = "uint152")]
        UInt152,

        [EnumMember(Value = "uint160")]
        UInt160,

        [EnumMember(Value = "uint168")]
        UInt168,

        [EnumMember(Value = "uint176")]
        UInt176,

        [EnumMember(Value = "uint184")]
        UInt184,

        [EnumMember(Value = "uint192")]
        UInt192,

        [EnumMember(Value = "uint200")]
        UInt200,

        [EnumMember(Value = "uint208")]
        UInt208,

        [EnumMember(Value = "uint216")]
        UInt216,

        [EnumMember(Value = "uint224")]
        UInt224,

        [EnumMember(Value = "uint232")]
        UInt232,

        [EnumMember(Value = "uint240")]
        UInt240,

        [EnumMember(Value = "uint248")]
        UInt248,

        [EnumMember(Value = "uint256")]
        UInt256,

        [EnumMember(Value = "int8")]
        Int8,

        [EnumMember(Value = "int16")]
        Int16,

        [EnumMember(Value = "int24")]
        Int24,

        [EnumMember(Value = "int32")]
        Int32,

        [EnumMember(Value = "int40")]
        Int40,

        [EnumMember(Value = "int48")]
        Int48,

        [EnumMember(Value = "int56")]
        Int56,

        [EnumMember(Value = "int64")]
        Int64,

        [EnumMember(Value = "int72")]
        Int72,

        [EnumMember(Value = "int80")]
        Int80,

        [EnumMember(Value = "int88")]
        Int88,

        [EnumMember(Value = "int96")]
        Int96,

        [EnumMember(Value = "int104")]
        Int104,

        [EnumMember(Value = "int112")]
        Int112,

        [EnumMember(Value = "int120")]
        Int120,

        [EnumMember(Value = "int128")]
        Int128,

        [EnumMember(Value = "int136")]
        Int136,

        [EnumMember(Value = "int144")]
        Int144,

        [EnumMember(Value = "int152")]
        Int152,

        [EnumMember(Value = "int160")]
        Int160,

        [EnumMember(Value = "int168")]
        Int168,

        [EnumMember(Value = "int176")]
        Int176,

        [EnumMember(Value = "int184")]
        Int184,

        [EnumMember(Value = "int192")]
        Int192,

        [EnumMember(Value = "int200")]
        Int200,

        [EnumMember(Value = "int208")]
        Int208,

        [EnumMember(Value = "int216")]
        Int216,

        [EnumMember(Value = "int224")]
        Int224,

        [EnumMember(Value = "int232")]
        Int232,

        [EnumMember(Value = "int240")]
        Int240,

        [EnumMember(Value = "int248")]
        Int248,

        [EnumMember(Value = "int256")]
        Int256,
    }
}
