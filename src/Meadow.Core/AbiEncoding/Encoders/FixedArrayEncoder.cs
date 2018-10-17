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
            if (_info.ArrayDimensionSizes.Length != 1)
            {
                throw new NotImplementedException();
            }

            int len = _itemEncoder.GetEncodedSize() * _info.ArrayDimensionSizes[0];
            return len;
        }

        public override int GetPackedEncodedSize()
        {
            if (_info.ArrayDimensionSizes.Length != 1)
            {
                throw new NotImplementedException();
            }

            int len = _itemEncoder.GetPackedEncodedSize() * _info.ArrayDimensionSizes[0];
            return len;
        }

        void ValidateArrayLength()
        {
            var itemCount = _val.Count();
            if (_info.ArrayDimensionSizes.Length != 1)
            {
                throw new NotImplementedException();
            }

            var expectedCount = _info.ArrayDimensionSizes[0];
            if (itemCount != expectedCount)
            {
                throw new ArgumentOutOfRangeException($"Fixed size array type '{_info.SolidityName}' needs exactly {expectedCount} items, was given {itemCount}");
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
            if (_info.ArrayDimensionSizes.Length != 1)
            {
                throw new NotImplementedException();
            }

            var items = new TItem[_info.ArrayDimensionSizes[0]];
            for (var i = 0; i < items.Length; i++)
            {
                _itemEncoder.Decode(ref buff, out var item);
                items[i] = item;
            }

            val = items;
        }

    }

}
