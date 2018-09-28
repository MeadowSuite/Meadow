using Meadow.Core.EthTypes;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.Core.AbiEncoding.Encoders
{

    public class StringEncoder : AbiTypeEncoder<string>
    {
        // utf-8 encoded and this value is interpreted as of bytes type and encoded further.
        // Note that the length used in this subsequent encoding is the number of bytes of 
        // the utf-8 encoded string, not its number of characters.

        static readonly Encoding UTF8 = new UTF8Encoding(false, false);

        public override int GetEncodedSize()
        {
            var len = UTF8.GetByteCount(_val);
            int padded = PadLength(len, UInt256.SIZE);
            return (UInt256.SIZE * 2) + padded;
        }

        public override int GetPackedEncodedSize()
        {
            var len = UTF8.GetByteCount(_val);
            return len;
        }

        public override void SetValue(object val)
        {
            if (!(val is string))
            {
                ThrowInvalidTypeException(val);
            }

            base.SetValue(val);
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            Span<byte> utf8 = UTF8.GetBytes(_val);
            utf8.CopyTo(buffer);
            buffer = buffer.Slice(utf8.Length);
        }

        public override void Encode(ref AbiEncodeBuffer buff)
        {
            Span<byte> utf8 = UTF8.GetBytes(_val);

            var uintEncoder = UInt256Encoder.UncheckedEncoders.Get();

            try
            {
                // write data offset position into header
                int offset = buff.HeadLength + buff.DataAreaCursorPosition;
                uintEncoder.Encode(buff.HeadCursor, offset);
                buff.IncrementHeadCursor(UInt256.SIZE);

                // write payload len into data buffer
                int len = utf8.Length;
                uintEncoder.Encode(buff.DataAreaCursor, len);
                buff.IncrementDataCursor(UInt256.SIZE);

                // write payload into data buffer
                utf8.CopyTo(buff.DataAreaCursor);
                int padded = PadLength(len, UInt256.SIZE);
                buff.IncrementDataCursor(padded);
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }

        }

        public override void Decode(ref AbiDecodeBuffer buff, out string val)
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
                var encodedString = buff.Buffer.Slice(startingPosition + UInt256.SIZE, byteLen);
                var bytes = new byte[byteLen];
                encodedString.CopyTo(bytes);
                val = UTF8.GetString(bytes);
                int bodyLen = PadLength(bytes.Length, UInt256.SIZE);

                buff.IncrementHeadCursor(UInt256.SIZE);
            }
            finally
            {
                UInt256Encoder.UncheckedEncoders.Put(uintEncoder);
            }

        }
    }

}
