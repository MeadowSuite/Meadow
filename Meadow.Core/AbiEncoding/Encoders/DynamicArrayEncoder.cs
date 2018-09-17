using Meadow.Core.EthTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{
    public class DynamicArrayEncoder<TItem> : AbiTypeEncoder<IEnumerable<TItem>>
    {
        IAbiTypeEncoder<TItem> _itemEncoder;

        public DynamicArrayEncoder(IAbiTypeEncoder<TItem> itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public override void SetTypeInfo(AbiTypeInfo info)
        {
            _info = info;
            if (_info.Category != SolidityTypeCategory.DynamicArray)
            {
                throw UnsupportedTypeException();
            }
        }

        public override int GetEncodedSize()
        {
            int len = _itemEncoder.GetEncodedSize() * _val.Count();
            return (UInt256.SIZE * 2) + len;
        }

        public override int GetPackedEncodedSize()
        {
            int len = _itemEncoder.GetPackedEncodedSize() * _val.Count();
            return len;
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.EncodePacked(ref buffer);
            }
        }

        public override void Encode(ref AbiEncodeBuffer buff)
        {
            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();
            try
            {
                // write data offset position into header
                int offset = buff.HeadLength + buff.DataAreaCursorPosition;
                uintEncoder.Encode(buff.HeadCursor, offset);
                buff.IncrementHeadCursor(UInt256.SIZE);

                // write array item count into data buffer
                int len = _val.Count();
                uintEncoder.Encode(buff.DataAreaCursor, len);
                buff.IncrementDataCursor(UInt256.SIZE);

                // write payload into data buffer
                var payloadBuffer = new AbiEncodeBuffer(buff.DataAreaCursor, Enumerable.Repeat(_info.ArrayItemInfo, len).ToArray());
                foreach (var item in _val)
                {
                    _itemEncoder.SetValue(item);
                    _itemEncoder.Encode(ref payloadBuffer);
                    buff.IncrementDataCursor(_itemEncoder.GetEncodedSize());
                }
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }
        }

        public override void Decode(ref AbiDecodeBuffer buff, out IEnumerable<TItem> val)
        {
            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();

            try
            {
                // Read the next header int which is the offset to the start of the data
                // in the data payload area.
                uintEncoder.Decode(buff.HeadCursor, out int startingPosition);

                // The first int in our offset of data area is the length of the rest of the payload.
                var encodedLength = buff.Buffer.Slice(startingPosition, UInt256.SIZE);
                uintEncoder.Decode(encodedLength, out int itemCount);

                var payloadOffset = startingPosition + UInt256.SIZE;
                var payload = buff.Buffer.Slice(payloadOffset, buff.Buffer.Length - payloadOffset);
                var payloadBuffer = new AbiDecodeBuffer(payload, Enumerable.Repeat(_info.ArrayItemInfo, itemCount).ToArray());
                var items = new TItem[itemCount];
                for (var i = 0; i < itemCount; i++)
                {
                    _itemEncoder.Decode(ref payloadBuffer, out var item);
                    items[i] = item;
                }

                val = items;

                buff.IncrementHeadCursor(UInt256.SIZE);
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }
        }
    }

}
