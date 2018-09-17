using System;

namespace Meadow.Core.AbiEncoding
{

    class AbiTypeEncoderUnboxer<TItem> : IAbiTypeEncoder<TItem>
    {
        IAbiTypeEncoder _encoder;
        public AbiTypeEncoderUnboxer(IAbiTypeEncoder encoder)
        {
            _encoder = encoder;
        }

        public AbiTypeInfo TypeInfo => _encoder.TypeInfo;

        public void Decode(ref AbiDecodeBuffer buff, out TItem val)
        {
            _encoder.DecodeObject(ref buff, out object objectVal);
            val = (TItem)objectVal;
        }

        public void DecodeObject(ref AbiDecodeBuffer buff, out object val)
        {
            _encoder.DecodeObject(ref buff, out val);
        }

        public void Encode(ref AbiEncodeBuffer buffer)
        {
            _encoder.Encode(ref buffer);
        }

        public void EncodePacked(ref Span<byte> buffer)
        {
            _encoder.EncodePacked(ref buffer);
        }

        public int GetEncodedSize()
        {
            return _encoder.GetEncodedSize();
        }

        public int GetPackedEncodedSize()
        {
            return _encoder.GetPackedEncodedSize();
        }

        public void SetTypeInfo(AbiTypeInfo info)
        {
            _encoder.SetTypeInfo(info);
        }

        public void SetValue(in TItem val)
        {
            _encoder.SetValue(val);
        }

        public void SetValue(object val)
        {
            _encoder.SetValue(val);
        }
    }

}
