using Meadow.Core.AbiEncoding.Encoders;
using Meadow.Core.EthTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Meadow.Core.AbiEncoding
{
    public static class DecoderFactory
    {
        public static DecodeDelegate<TItem[]> GetMultiDimArrayDecoder<TItem>(AbiTypeInfo solidityType)
        {
            IAbiTypeEncoder encoder;
            IAbiTypeEncoder itemEncoder;

            if (solidityType.ArrayItemInfo.ElementaryBaseType == SolidityTypeElementaryBase.Bytes)
            {
                itemEncoder = new BytesMEncoder();
            }
            else
            {
                // TODO: define all multi-dim array encoder runtime matches
                throw new NotImplementedException();
            }

            itemEncoder.SetTypeInfo(solidityType.ArrayItemInfo);

            switch (solidityType.Category)
            {
                case SolidityTypeCategory.DynamicArray:
                    encoder = new DynamicArrayEncoderNonGeneric(itemEncoder);
                    break;
                case SolidityTypeCategory.FixedArray:
                    encoder = new FixedArrayEncoderNonGeneric(itemEncoder);
                    break;
                default:
                    throw new ArgumentException($"Encoder factory for array types was called with a type '{solidityType.Category}'");
            }

            encoder.SetTypeInfo(solidityType);

            void Decode(AbiTypeInfo st, ref AbiDecodeBuffer buff, out TItem[] val)
            {
                encoder.DecodeObject(ref buff, out var objectVal);
                var objectArray = (object[])objectVal;
                val = objectArray.Select(v => (TItem)v).ToArray();
            }

            return Decode;
        }

        public static DecodeDelegate<TItem[]> GetArrayDecoder<TItem>(IAbiTypeEncoder<TItem> itemEncoder)
        {
            void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out TItem[] val)
            {
                DecoderFactory.Decode(solidityType, ref buff, out val, itemEncoder);
            }

            return Decode;
        }

        public static void Decode<TItem>(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out TItem[] val, IAbiTypeEncoder<TItem> itemEncoder)
        {
            Decode(solidityType, ref buff, out IEnumerable<TItem> items, itemEncoder);
            val = items is TItem[] arr ? arr : items.ToArray();
        }

        public static void Decode<TItem>(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out IEnumerable<TItem> val, IAbiTypeEncoder<TItem> itemEncoder)
        {
            AbiTypeEncoder<IEnumerable<TItem>> encoder;
            switch (solidityType.Category)
            {
                case SolidityTypeCategory.DynamicArray:
                    encoder = new DynamicArrayEncoder<TItem>(itemEncoder);
                    break;
                case SolidityTypeCategory.FixedArray:
                    encoder = new FixedArrayEncoder<TItem>(itemEncoder);
                    break;
                default:
                    throw new ArgumentException($"Encoder factory for array types was called with a type '{solidityType.Category}'");

            }

            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out byte[] val)
        {
            Decode(solidityType, ref buff, out IEnumerable<byte> bytes);
            val = bytes is byte[] arr ? arr : bytes.ToArray();
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out IEnumerable<byte> val)
        {
            switch (solidityType.Category)
            {
                case SolidityTypeCategory.Bytes:
                    {
                        var encoder = new BytesEncoder();
                        encoder.SetTypeInfo(solidityType);
                        encoder.Decode(ref buff, out val);
                        break;
                    }

                case SolidityTypeCategory.DynamicArray:
                    {
                        var encoder = new DynamicArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(solidityType);
                        encoder.Decode(ref buff, out val);
                        break;
                    }

                case SolidityTypeCategory.FixedArray:
                    {
                        var encoder = new FixedArrayEncoder<byte>(new UInt8Encoder());
                        encoder.SetTypeInfo(solidityType);
                        encoder.Decode(ref buff, out val);
                        break;
                    }

                case SolidityTypeCategory.Elementary when solidityType.ElementaryBaseType == SolidityTypeElementaryBase.Bytes:
                    {
                        var encoder = new BytesMEncoder();
                        encoder.SetTypeInfo(solidityType);
                        encoder.Decode(ref buff, out val);
                        break;
                    }

                default:
                    throw new ArgumentException($"Encoder factor method for byte arrays called with type '{solidityType.Category}'");
            }
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out string val)
        {
            var encoder = new StringEncoder();
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out Address val)
        {
            var encoder = new AddressEncoder();
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out Hash val)
        {
            var encoder = new BytesMEncoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out var bytes);
            val = new Hash(bytes is byte[] b ? b : bytes.ToArray());
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out bool val)
        {
            var encoder = new BoolEncoder();
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out sbyte val)
        {
            var encoder = new Int8Encoder();
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out byte val)
        {
            var encoder = new UInt8Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out short val)
        {
            var encoder = new Int16Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out ushort val)
        {
            var encoder = new UInt16Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out int val)
        {
            var encoder = new Int32Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out uint val)
        {
            var encoder = new UInt32Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out long val)
        {
            var encoder = new Int64Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out ulong val)
        {
            var encoder = new UInt64Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out BigInteger val)
        {
            var encoder = new Int256Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

        public static void Decode(AbiTypeInfo solidityType, ref AbiDecodeBuffer buff, out UInt256 val)
        {
            NumberEncoder<UInt256> encoder = new UInt256Encoder();
            encoder.SetTypeInfo(solidityType);
            encoder.Decode(ref buff, out val);
        }

    }

    public delegate void DecodeDelegate<TOut>(AbiTypeInfo typeInfo, ref AbiDecodeBuffer buff, out TOut result);

}
