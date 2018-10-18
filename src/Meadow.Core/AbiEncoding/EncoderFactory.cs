using Meadow.Core.AbiEncoding.Encoders;
using Meadow.Core.EthTypes;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Meadow.Core.AbiEncoding
{
    /// <summary>
    /// Generic-overloaded methods for easy access to typed encoders from generated contracts.
    /// Trade off of: Lots of code here for less code in the generated contracts, and less 
    /// dynamic runtime type checking.
    /// </summary>
    public static class EncoderFactory
    {
        // TODO: if we use the t4 generated UInt<M> types, use a t4 generator to create the corresponding LoadEncoder methods here...

        public static IAbiTypeEncoder<TItem> LoadMultiDimArrayEncoder<TItem>(string solidityType, in TItem val)
        {
            var info = AbiTypeMap.GetSolidityTypeInfo(solidityType);
            IAbiTypeEncoder itemEncoder;
            if (info.ArrayItemInfo.ElementaryBaseType == SolidityTypeElementaryBase.Bytes)
            {
                itemEncoder = new BytesMEncoder();
            }
            else
            {
                // TODO: implement multi-dim array runtime encoder matching
                throw new NotImplementedException();
            }

            itemEncoder.SetTypeInfo(info.ArrayItemInfo);
            IAbiTypeEncoder encoder;
            switch (info.Category)
            {
                case SolidityTypeCategory.FixedArray:
                    encoder = new FixedArrayEncoderNonGeneric(itemEncoder);
                    break;
                case SolidityTypeCategory.DynamicArray:
                    encoder = new DynamicArrayEncoderNonGeneric(itemEncoder);
                    break;
                default:
                    throw new ArgumentException($"Encoder factory for array types was called with a type '{info.Category}'");
            }

            encoder.SetTypeInfo(info);
            encoder.SetValue(val);
            return new AbiTypeEncoderUnboxer<TItem>(encoder);
        }

        public static IAbiTypeEncoder<IEnumerable<TItem>> LoadEncoder<TItem>(string solidityType, in IEnumerable<TItem> val, IAbiTypeEncoder<TItem> itemEncoder)
        {
            var info = AbiTypeMap.GetSolidityTypeInfo(solidityType);
            IAbiTypeEncoder<IEnumerable<TItem>> encoder;
            switch (info.Category)
            {
                case SolidityTypeCategory.FixedArray:
                    encoder = new FixedArrayEncoder<TItem>(itemEncoder);
                    break;
                case SolidityTypeCategory.DynamicArray:
                    encoder = new DynamicArrayEncoder<TItem>(itemEncoder);
                    break;
                default:
                    throw new ArgumentException($"Encoder factory for array types was called with a type '{info.Category}'");
            }

            encoder.SetTypeInfo(info);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<byte[]> LoadEncoder(string solidityType, in byte[] val)
        {
            var info = AbiTypeMap.GetSolidityTypeInfo(solidityType);
            switch (info.Category)
            {
                case SolidityTypeCategory.Bytes:
                    {
                        var encoder = new BytesEncoder();
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.Elementary when info.ElementaryBaseType == SolidityTypeElementaryBase.Bytes:
                    {
                        var encoder = new BytesMEncoder();
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.DynamicArray:
                    {
                        var encoder = new DynamicArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.FixedArray:
                    {
                        var encoder = new FixedArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                default:
                    throw new ArgumentException($"Encoder factor method for byte arrays called with type '{info.Category}'");
            }
        }

        public static IAbiTypeEncoder<IEnumerable<byte>> LoadEncoder(string solidityType, in IEnumerable<byte> val)
        {
            var info = AbiTypeMap.GetSolidityTypeInfo(solidityType);
            switch (info.Category)
            {
                case SolidityTypeCategory.Bytes:
                    {
                        var encoder = new BytesEncoder();
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.Elementary when info.ElementaryBaseType == SolidityTypeElementaryBase.Bytes:
                    {
                        var encoder = new BytesMEncoder();
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.DynamicArray:
                    {
                        var encoder = new DynamicArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                case SolidityTypeCategory.FixedArray:
                    {
                        var encoder = new FixedArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(info);
                        encoder.SetValue(val);
                        return encoder;
                    }

                default:
                    throw new ArgumentException($"Encoder factor method for byte arrays called with type '{info.Category}'");
            }

        }

        public static IAbiTypeEncoder<string> LoadEncoder(string solidityType, in string val)
        {
            var encoder = new StringEncoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<Address> LoadEncoder(string solidityType, in Address val)
        {
            var encoder = new AddressEncoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<bool> LoadEncoder(string solidityType, in bool val)
        {
            var encoder = new BoolEncoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<sbyte> LoadEncoder(string solidityType, in sbyte val)
        {
            var encoder = new Int8Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<byte> LoadEncoder(string solidityType, in byte val)
        {
            var encoder = new UInt8Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<short> LoadEncoder(string solidityType, in short val)
        {
            var encoder = new Int16Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<ushort> LoadEncoder(string solidityType, in ushort val)
        {
            var encoder = new UInt16Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<int> LoadEncoder(string solidityType, in int val)
        {
            var encoder = new Int32Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<uint> LoadEncoder(string solidityType, in uint val)
        {
            var encoder = new UInt32Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<long> LoadEncoder(string solidityType, in long val)
        {
            var encoder = new Int64Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<ulong> LoadEncoder(string solidityType, in ulong val)
        {
            var encoder = new UInt64Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<BigInteger> LoadEncoder(string solidityType, in BigInteger val)
        {
            var encoder = new Int256Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        public static IAbiTypeEncoder<UInt256> LoadEncoder(string solidityType, in UInt256 val)
        {
            var encoder = new UInt256Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }

        static IAbiTypeEncoder GetEncoder(AbiTypeInfo solidityType)
        {
            switch (solidityType.Category)
            {
                case SolidityTypeCategory.String:
                    return new StringEncoder();

                case SolidityTypeCategory.Bytes:
                    return new BytesEncoder();

                case SolidityTypeCategory.DynamicArray:
                    return new DynamicArrayEncoderNonGeneric(LoadEncoder(solidityType.ArrayItemInfo));

                case SolidityTypeCategory.FixedArray:
                    return new FixedArrayEncoderNonGeneric(LoadEncoder(solidityType.ArrayItemInfo));

                case SolidityTypeCategory.Elementary:
                    switch (solidityType.ElementaryBaseType)
                    {
                        case SolidityTypeElementaryBase.Address:
                            return new AddressEncoder();

                        case SolidityTypeElementaryBase.Bool:
                            return new BoolEncoder();

                        case SolidityTypeElementaryBase.Bytes:
                            return new BytesMEncoder();

                        case SolidityTypeElementaryBase.UInt:
                            switch (solidityType.PrimitiveTypeByteSize)
                            {
                                case var size when size > 8:
                                    return new UInt256Encoder();
                                case var size when size > 4:
                                    return new UInt64Encoder();
                                case var size when size > 2:
                                    return new UInt32Encoder();
                                case var size when size > 1:
                                    return new UInt16Encoder();
                                case 1:
                                    return new UInt8Encoder();
                                default:
                                    throw new ArgumentException($"Unexpected type byte size '{solidityType.PrimitiveTypeByteSize}' for type '{solidityType.SolidityName}'");
                            }

                        case SolidityTypeElementaryBase.Int:
                            switch (solidityType.PrimitiveTypeByteSize)
                            {
                                case var size when size > 8:
                                    return new Int256Encoder();
                                case var size when size > 4:
                                    return new Int64Encoder();
                                case var size when size > 2:
                                    return new Int32Encoder();
                                case var size when size > 1:
                                    return new Int16Encoder();
                                case 1:
                                    return new Int8Encoder();
                                default:
                                    throw new ArgumentException($"Unexpected type byte size '{solidityType.PrimitiveTypeByteSize}' for type '{solidityType.SolidityName}'");
                            }

                        default:
                            throw new ArgumentException($"Unexpected elementary type '{solidityType.ElementaryBaseType}'");
                    }

                default:
                    throw new ArgumentException($"Unexpected solidity type category {solidityType.Category}");
            }

        }

        public static IAbiTypeEncoder LoadEncoder(AbiTypeInfo solidityType)
        {
            var encoder = GetEncoder(solidityType);
            encoder.SetTypeInfo(solidityType);
            return encoder;
        }

        public static IAbiTypeEncoder LoadEncoderNonGeneric(AbiTypeInfo solidityType, object val)
        {
            var encoder = GetEncoder(solidityType);
            encoder.SetTypeInfo(solidityType);
            encoder.SetValue(val);
            return encoder;
        }
    }

}
