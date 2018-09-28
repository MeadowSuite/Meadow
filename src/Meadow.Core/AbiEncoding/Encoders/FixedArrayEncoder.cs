using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{
    public class FixedArrayEncoder<TItem> : AbiTypeEncoder<IEnumerable<TItem>>
    {
        IAbiTypeEncoder<TItem> _itemEncoder;

        public FixedArrayEncoder(IAbiTypeEncoder<TItem> itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public override void SetTypeInfo(AbiTypeInfo info)
        {
            _info = info;
            if (_info.Category != SolidityTypeCategory.FixedArray)
            {
                throw UnsupportedTypeException();
            }
        }

        public override int GetEncodedSize()
        {
            int len = _itemEncoder.GetEncodedSize() * _info.ArrayLength;
            return len;
        }

        public override int GetPackedEncodedSize()
        {
            int len = _itemEncoder.GetPackedEncodedSize() * _info.ArrayLength;
            return len;
        }

        void ValidateArrayLength()
        {
            var itemCount = _val.Count();
            if (itemCount != _info.ArrayLength)
            {
                throw new ArgumentOutOfRangeException($"Fixed size array type '{_info.SolidityName}' needs exactly {_info.ArrayLength} items, was given {itemCount}");
            }
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            ValidateArrayLength();
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.EncodePacked(ref buffer);
            }
        }

        public override void Encode(ref AbiEncodeBuffer buffer)
        {
            ValidateArrayLength();
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.Encode(ref buffer);
            }
        }

        public override void Decode(ref AbiDecodeBuffer buff, out IEnumerable<TItem> val)
        {
            var items = new TItem[_info.ArrayLength];
            for (var i = 0; i < items.Length; i++)
            {
                _itemEncoder.Decode(ref buff, out var item);
                items[i] = item;
            }

            val = items;
        }

    }

}
