using Meadow.Core.EthTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{
    // TODO: attempt to share code between this untyped encoder and the typed/generic encoder

    public class DynamicArrayEncoderNonGeneric : IAbiTypeEncoder
    {
        public AbiTypeInfo TypeInfo { get; private set; }

        IEnumerable<object> _val;
        IAbiTypeEncoder _itemEncoder;

        public DynamicArrayEncoderNonGeneric(IAbiTypeEncoder itemEncoder)
        {
            _itemEncoder = itemEncoder;
        }

        public int GetEncodedSize()
        {
            int len = _itemEncoder.GetEncodedSize() * _val.Count();
            return (UInt256.SIZE * 2) + len;
        }

        public int GetPackedEncodedSize()
        {
            int len = _itemEncoder.GetPackedEncodedSize() * _val.Count();
            return len;
        }

        public void DecodeObject(ref AbiDecodeBuffer buff, out object val)
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
                var payloadBuffer = new AbiDecodeBuffer(payload, Enumerable.Repeat(TypeInfo.ArrayItemInfo, itemCount).ToArray());
                var items = new object[itemCount];
                for (var i = 0; i < itemCount; i++)
                {
                    _itemEncoder.DecodeObject(ref payloadBuffer, out var item);
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

        public void EncodePacked(ref Span<byte> buffer)
        {
            foreach (var item in _val)
            {
                _itemEncoder.SetValue(item);
                _itemEncoder.EncodePacked(ref buffer);
            }
        }

        public void Encode(ref AbiEncodeBuffer buffer)
        {
            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();
            try
            {
                // write data offset position into header
                int offset = buffer.HeadLength + buffer.DataAreaCursorPosition;
                uintEncoder.Encode(buffer.HeadCursor, offset);
                buffer.IncrementHeadCursor(UInt256.SIZE);

                // write array item count into data buffer
                int len = _val.Count();
                uintEncoder.Encode(buffer.DataAreaCursor, len);
                buffer.IncrementDataCursor(UInt256.SIZE);

                // write payload into data buffer
                var payloadBuffer = new AbiEncodeBuffer(buffer.DataAreaCursor, Enumerable.Repeat(TypeInfo.ArrayItemInfo, len).ToArray());
                foreach (var item in _val)
                {
                    _itemEncoder.SetValue(item);
                    _itemEncoder.Encode(ref payloadBuffer);
                    buffer.IncrementDataCursor(_itemEncoder.GetEncodedSize());
                }
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }
        }

        public void SetTypeInfo(AbiTypeInfo info)
        {
            if (info.Category != SolidityTypeCategory.DynamicArray)
            {
                throw EncoderUtil.CreateUnsupportedTypeEncodingException(TypeInfo);
            }

            TypeInfo = info;
        }

        public void SetValue(object val) => _val = (val as IEnumerable).Cast<object>();

    }

}
