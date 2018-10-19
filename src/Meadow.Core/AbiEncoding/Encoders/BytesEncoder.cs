using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Core.AbiEncoding.Encoders
{

    public class BytesEncoder : AbiTypeEncoder<IEnumerable<byte>>, IAbiTypeEncoder<byte[]>
    {
        public override void SetTypeInfo(AbiTypeInfo info)
        {
            _info = info;
            if (info.Category != SolidityTypeCategory.Bytes)
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


        public override int GetEncodedSize()
        {
            // 32 byte length prefix + byte array length + 32 byte remainder padding
            var len = _val.Count();
            return (UInt256.SIZE * 2) + PadLength(len, UInt256.SIZE);
        }

        public override int GetPackedEncodedSize() => _val.Count();

        public override void EncodePacked(ref Span<byte> buffer)
        {
            Span<byte> dataSpan = _val.ToArray();
            dataSpan.CopyTo(buffer);
            buffer = buffer.Slice(dataSpan.Length);
        }
        

        public override void Encode(ref AbiEncodeBuffer buff)
        {
            // bytes, of length k(which is assumed to be of type uint256):
            // enc(X) = enc(k) pad_right(X), i.e.the number of bytes is encoded as a uint256 
            // followed by the actual value of X as a byte sequence, followed  by the minimum
            // number of zero-bytes such that len(enc(X)) is a multiple of 32.
            // write length prefix

            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();
            
            try
            {

                // write data offset position into header
                int offset = buff.HeadLength + buff.DataAreaCursorPosition;
                uintEncoder.Encode(buff.HeadCursor, offset);
                buff.IncrementHeadCursor(UInt256.SIZE);

                // write payload len into data buffer          
                int len = _val.Count();
                uintEncoder.Encode(buff.DataAreaCursor, len);
                buff.IncrementDataCursor(UInt256.SIZE);

                // write payload into data buffer
                int i = 0;
                foreach (byte b in _val)
                {
                    buff.DataAreaCursor[i++] = b;
                }

                int padded = PadLength(len, UInt256.SIZE);
                buff.IncrementDataCursor(padded);
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }
        }

        public override void Decode(ref AbiDecodeBuffer buff, out IEnumerable<byte> val)
        {
            Decode(ref buff, out byte[] result);
            val = result;
        }

        public void Decode(ref AbiDecodeBuffer buff, out byte[] val)
        {
            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();
            try
            {
                // Read the next header int which is the offset to the start of the data
                // in the data payload area.
                uintEncoder.Decode(buff.HeadCursor, out int startingPosition);

                // The first int in our offset of data area is the length of the rest of the payload.
                var encodedLength = buff.Buffer.Slice(startingPosition, UInt256.SIZE);
                uintEncoder.Decode(encodedLength, out int byteLen);

                // Read the actual payload from the data area
                var payloadOffset = startingPosition + UInt256.SIZE;
                var payload = buff.Buffer.Slice(payloadOffset, byteLen);
                var bytes = payload.ToArray();
                int bodyLen = PadLength(bytes.Length, UInt256.SIZE);
                val = bytes;

#if ZERO_BYTE_CHECKS
                // data validity check: should be right-padded with zero bytes
                for (var i = payloadOffset + byteLen; i < payloadOffset + bodyLen; i++)
                {
                    if (buff.Buffer[i] != 0)
                    {
                        throw new ArgumentException($"Invalid bytes input data; should be {bytes.Length} bytes of data followed by {bodyLen - bytes.Length} zero-bytes");
                    }
                }
#endif

                buff.IncrementHeadCursor(UInt256.SIZE);
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }
        }

    }

}
