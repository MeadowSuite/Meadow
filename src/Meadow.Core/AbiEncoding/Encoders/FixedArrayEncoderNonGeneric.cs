using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{    
    // TODO: attempt to share code between this untyped encoder and the typed/generic encoder

    public class FixedArrayEncoderNonGeneric : IAbiTypeEncoder
    {
        IAbiTypeEncoder _itemEncoder;
        IEnumerable<object> _val;

        public FixedArrayEncoderNonGeneric(IAbiTypeEncoder itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public AbiTypeInfo TypeInfo { get; private set; }

        public void SetTypeInfo(AbiTypeInfo info)
        {
            if (info.Category != SolidityTypeCategory.FixedArray)
            {
                throw EncoderUtil.CreateUnsupportedTypeEncodingException(info);
            }

            TypeInfo = info;
        }

        public void SetValue(object val) => _val = (val as IEnumerable).Cast<object>();

        public void DecodeObject(ref AbiDecodeBuffer buff, out object val)
        {
            var items = ArrayExtensions.CreateJaggedArray(TypeInfo.ArrayItemInfo.ClrType, TypeInfo.ArrayDimensionSizes);

            void DecodeArray(ref AbiDecodeBuffer buffer, Array resultOutput, int[] arrayDimensionSizes, int i)
            {
                if (i < arrayDimensionSizes.Length - 1)
                {
                    foreach (var subItem in resultOutput)
                    {
                        DecodeArray(ref buffer, (Array)subItem, arrayDimensionSizes, i + 1);
                    }
                }

                if (i == arrayDimensionSizes.Length - 1)
                {
                    for (var resultIndex = 0; resultIndex < resultOutput.Length; resultIndex++)
                    {
                        _itemEncoder.DecodeObject(ref buffer, out var item);
                        resultOutput.SetValue(item, resultIndex);
                    }
                }
            }

            DecodeArray(ref buff, (Array)items, TypeInfo.ArrayDimensionSizes, 0);

            val = items;
        }

        void ValidateArrayLength()
        {
            void Validate(IEnumerable<object> val, int[] arrayDimensionSizes, int i)
            {
                var actualCount = val.Count();
                if (actualCount != arrayDimensionSizes[i])
                {
                    throw new ArgumentOutOfRangeException($"Fixed size array type '{TypeInfo.SolidityName}' needs exactly {arrayDimensionSizes[i]} items, was given {actualCount}");

                }

                if (i < arrayDimensionSizes.Length - 1)
                {
                    foreach (var subItem in val)
                    {
                        Validate((subItem as IEnumerable).Cast<object>(), arrayDimensionSizes, i + 1);
                    }
                }
            }

            Validate(_val, TypeInfo.ArrayDimensionSizes, 0);
        }

        public void Encode(ref AbiEncodeBuffer buffer)
        {
            ValidateArrayLength();

            void EncodeValue(ref AbiEncodeBuffer buff, IEnumerable< object> val, int[] arrayDimensionSizes, int i)
            {
                if (i < arrayDimensionSizes.Length - 1)
                {
                    foreach (var subItem in val)
                    {
                        EncodeValue(ref buff, (subItem as IEnumerable).Cast<object>(), arrayDimensionSizes, i + 1);
                    }
                }

                if (i == arrayDimensionSizes.Length - 1)
                {
                    foreach (var item in val)
                    {
                        _itemEncoder.SetValue(item);
                        _itemEncoder.Encode(ref buff);
                    }
                }
            }

            EncodeValue(ref buffer, _val, TypeInfo.ArrayDimensionSizes, 0);
        }

        public void EncodePacked(ref Span<byte> buffer)
        {
            ValidateArrayLength();
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.EncodePacked(ref buffer);
            }
        }

        public int GetEncodedSize()
        {
            int totalArraySize = 1;
            foreach (var dim in TypeInfo.ArrayDimensionSizes)
            {
                totalArraySize *= dim;
            }

            int len = _itemEncoder.GetEncodedSize() * totalArraySize;
            return len;
        }

        public int GetPackedEncodedSize()
        {
            int totalArraySize = 1;
            foreach (var dim in TypeInfo.ArrayDimensionSizes)
            {
                totalArraySize *= dim;
            }

            int len = _itemEncoder.GetPackedEncodedSize() * totalArraySize;
            return len;
        }



    }

}
