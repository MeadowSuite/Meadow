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
            var items = new object[TypeInfo.ArrayLength];
            for (var i = 0; i < items.Length; i++)
            {
                _itemEncoder.DecodeObject(ref buff, out var item);
                items[i] = item;
            }

            val = items;
        }

        void ValidateArrayLength()
        {
            var itemCount = _val.Count();
            if (itemCount != TypeInfo.ArrayLength)
            {
                throw new ArgumentOutOfRangeException($"Fixed size array type '{TypeInfo.SolidityName}' needs exactly {TypeInfo.ArrayLength} items, was given {itemCount}");
            }
        }

        public void Encode(ref AbiEncodeBuffer buffer)
        {
            ValidateArrayLength();
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.Encode(ref buffer);
            }
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
            int len = _itemEncoder.GetEncodedSize() * TypeInfo.ArrayLength;
            return len;
        }

        public int GetPackedEncodedSize()
        {
            int len = _itemEncoder.GetPackedEncodedSize() * TypeInfo.ArrayLength;
            return len;
        }



    }

}
