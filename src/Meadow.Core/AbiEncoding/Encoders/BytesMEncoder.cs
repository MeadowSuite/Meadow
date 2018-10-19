using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using Meadow.Core.EthTypes;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{
    public class BytesMEncoder : AbiTypeEncoder<IEnumerable<byte>>, IAbiTypeEncoder<byte[]>
    {
        public override void SetTypeInfo(AbiTypeInfo info)
        {
            _info = info;
            if (info.Category != SolidityTypeCategory.Elementary || info.ElementaryBaseType != SolidityTypeElementaryBase.Bytes)
            {
                throw UnsupportedTypeException();
            }
        }

        public void SetValue(in byte[] val)
        {
            base.SetValue(val);
        }

        public override void SetValue(object val)
        {
            switch (val)
            {
                case IEnumerable<byte> e:
                    base.SetValue(e);
                    break;
                case string str:
                    base.SetValue(HexUtil.HexToBytes(str));
                    break;
                case byte n:
                    base.SetValue(new byte[] { n });
                    break;
                case sbyte n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case short n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case ushort n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case int n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case uint n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case long n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case ulong n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                case UInt256 n:
                    base.SetValue(HexConverter.GetHexFromInteger(n).HexToBytes());
                    break;
                default:
                    ThrowInvalidTypeException(val);
                    break;
            }
        }

        public override int GetEncodedSize() => UInt256.SIZE;
        public override int GetPackedEncodedSize() => _info.PrimitiveTypeByteSize;

        void ValidateArrayLength()
        {
            var itemCount = _val.Count();
            if (itemCount != _info.PrimitiveTypeByteSize)
            {
                throw new ArgumentOutOfRangeException($"Type '{_info.SolidityName}' needs exactly {_info.PrimitiveTypeByteSize} items, was given {itemCount}");
            }
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            Span<byte> dataSpan = _val.ToArray();
            dataSpan.CopyTo(buffer);
            buffer = buffer.Slice(dataSpan.Length);
        }

        public override void Encode(ref AbiEncodeBuffer buff)
        {
            // bytes<M>: enc(X) is the sequence of bytes in X padded with trailing
            // zero-bytes to a length of 32 bytes.
            int i = 0;
            foreach (byte b in _val)
            {
                buff.HeadCursor[i++] = b;
            }

            buff.IncrementHeadCursor(UInt256.SIZE);
        }

        public override void Decode(ref AbiDecodeBuffer buff, out IEnumerable<byte> val)
        {
            Decode(ref buff, out byte[] result);
            val = result;
        }

        public void Decode(ref AbiDecodeBuffer buff, out byte[] val)
        {
            var bytes = new byte[_info.PrimitiveTypeByteSize];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = buff.HeadCursor[i];
            }

#if ZERO_BYTE_CHECKS
            // data validity check: all bytes after the fixed M amount should be zero
            for (var i = bytes.Length; i < UInt256.SIZE; i++)
            {
                if (buff.HeadCursor[i] != 0)
                {
                    throw new ArgumentException($"Invalid {_info.SolidityName} input data; should be {_info.PrimitiveTypeByteSize} bytes padded {UInt256.SIZE - _info.PrimitiveTypeByteSize} zero-bytes; received: " + buff.HeadCursor.Slice(0, 32).ToHexString());
                }
            }
#endif

            val = bytes;
            buff.IncrementHeadCursor(UInt256.SIZE);
        }

    }

}
