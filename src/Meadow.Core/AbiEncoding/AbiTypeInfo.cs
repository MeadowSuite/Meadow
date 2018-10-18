using Meadow.Core.Utils;
using System;

namespace Meadow.Core.AbiEncoding
{
    /// <summary>
    /// Can be implicitly created from a <see cref="string"/> (e.g: "address"), or a <see cref="SolidityType"/> (e.g.: <see cref="SolidityType.String"/>
    /// </summary>
    public class AbiTypeInfo : IEquatable<AbiTypeInfo>
    {
        /// <summary>
        /// Original type string from the json ABI.
        /// </summary>
        public readonly string SolidityName;

        /// <summary>
        /// The corresponding C# type. For solidiy array types this is an IEnumerable&lt;TBaseType&gt;.
        /// </summary>
        public readonly Type ClrType;

        /// <summary>
        /// The ClrType.FullName (for caching).
        /// </summary>
        public readonly string ClrTypeName;

        /// <summary>
        /// For static sized types this is the size of the entire type, 
        /// otherwise is the size of the base type for an array/dynamic type.
        /// The solidity types 'string' and 'bytes' are considered dynamic arrays
        /// of bytes where this base size is 1.
        /// </summary>
        public readonly int PrimitiveTypeByteSize;

        public readonly SolidityTypeCategory Category;

        public readonly SolidityTypeElementaryBase ElementaryBaseType = SolidityTypeElementaryBase.None;

        /// <summary>
        /// If the type is a dynamic or fixed array type
        /// </summary>
        public bool IsArrayType =>
            Category == SolidityTypeCategory.DynamicArray ||
            Category == SolidityTypeCategory.FixedArray;

        /// <summary>
        /// If the type is of "bytes" or "bytes&lt;M&gt;"
        /// </summary>
        public bool IsSpecialBytesType =>
            Category == SolidityTypeCategory.Bytes ||
            ElementaryBaseType == SolidityTypeElementaryBase.Bytes;

        public bool IsStaticType => Category == SolidityTypeCategory.Elementary;
        public bool IsDynamicType => !IsStaticType;

        /// <summary>
        /// The elementary/base value of an array time. Null for non-array types.
        /// </summary>
        public readonly AbiTypeInfo ArrayItemInfo;

        /// <summary>
        /// The size of each dimension for multi-dimensional array types. Dynamic sizes are represented as 0.
        /// For example the type uint256[3][][6] would be represented as [3, 0, 6].
        /// Null for non-multi-dimensional types.
        /// </summary>
        public readonly int[] ArrayDimensionSizes;

        public AbiTypeInfo(string solidityName, Type clrType, int primitiveTypeByteSize,
            SolidityTypeCategory category = SolidityTypeCategory.Elementary,
            SolidityTypeElementaryBase elementaryBaseType = SolidityTypeElementaryBase.None,
            AbiTypeInfo arrayItemInfo = null, int[] arrayDimensionSizes = null)
        {
            SolidityName = solidityName;
            ClrType = clrType;
            ClrTypeName = ClrType.FullName;
            PrimitiveTypeByteSize = primitiveTypeByteSize;
            Category = category;
            ElementaryBaseType = elementaryBaseType;
            ArrayItemInfo = arrayItemInfo;
            ArrayDimensionSizes = arrayDimensionSizes;
        }

        public static AbiTypeInfo Create(string solidityName)
        {
            return AbiTypeMap.GetSolidityTypeInfo(solidityName);
        }

        public static bool operator ==(AbiTypeInfo b1, AbiTypeInfo b2)
        {
            return b1?.SolidityName == b2?.SolidityName;
        }

        public static bool operator !=(AbiTypeInfo b1, AbiTypeInfo b2)
        {
            return !(b1 == b2);
        }

        public bool Equals(AbiTypeInfo other)
        {
            return other?.SolidityName == SolidityName;
        }

        public override bool Equals(object obj)
        {
            return obj is AbiTypeInfo info ? Equals(info) : false;
        }

        public override int GetHashCode()
        {
            return SolidityName.GetHashCode();
        }

        public override string ToString()
        {
            return SolidityName;
        }

        public static implicit operator AbiTypeInfo(string solidityType)
        {
            return Create(solidityType);
        }

        public static implicit operator AbiTypeInfo(SolidityType solidityType)
        {
            return Create(solidityType.GetMemberValue());
        }

        public static implicit operator string(AbiTypeInfo solidityType)
        {
            return solidityType.SolidityName;
        }
    }

    public enum SolidityTypeElementaryBase
    {
        None,
        Int,
        UInt,
        Bytes,
        Address,
        Bool
    }

    public enum SolidityTypeCategory
    {
        /// <summary>
        /// Static / base / primitive type, eg: uint16, address, bool, etc..
        /// </summary>
        Elementary,

        /// <summary>
        /// An static/fixed sized array type
        /// </summary>
        FixedArray,

        /// <summary>
        /// A dynamic/variably sized array type
        /// </summary>
        DynamicArray,

        /// <summary>
        /// Special encoded dynamic length string
        /// </summary>
        String,

        /// <summary>
        /// Special dynamic length byte array
        /// </summary>
        Bytes
    }



}
