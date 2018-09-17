using Meadow.Core.Utils;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Meadow.Core.EthTypes;

namespace Meadow.Core.AbiEncoding.Encoders
{
    public class AddressEncoder : AbiTypeEncoder<Address>
    {
        public override int GetEncodedSize() => UInt256.SIZE;

        public override int GetPackedEncodedSize() => Address.SIZE;

        static readonly byte[] ZEROx12 = Enumerable.Repeat((byte)0, 12).ToArray();

        public override void SetValue(object val)
        {
            switch (val)
            {
                case Address addr:
                    SetValue(addr);
                    break;
                case string str:
                    SetValue(new Address(str));
                    break;
                case byte[] bytes:
                    SetValue(new Address(bytes));
                    break;
                default:
                    ThrowInvalidTypeException(val);
                    break;
            }

        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            MemoryMarshal.Write(buffer, ref _val);
            buffer = buffer.Slice(Address.SIZE);
        }

        // encoded the same way as an uint160
        public override void Encode(ref AbiEncodeBuffer buffer)
        {
            MemoryMarshal.Write(buffer.HeadCursor.Slice(12), ref _val);
            buffer.IncrementHeadCursor(UInt256.SIZE);
        }

        public override void Decode(ref AbiDecodeBuffer buff, out Address val)
        {

#if ZERO_BYTE_CHECKS
            // data validity check: 20 address bytes should be left-padded with 12 zero-bytes
            if (!buff.HeadCursor.Slice(0, 12).SequenceEqual(ZEROx12))
            {
                throw new ArgumentException("Invalid address input data; should be 20 address bytes, left-padded with 12 zero-bytes; received: " + buff.HeadCursor.Slice(0, UInt256.SIZE).ToHexString());
            }
#endif
            val = MemoryMarshal.Read<Address>(buff.HeadCursor.Slice(12));
            buff.IncrementHeadCursor(UInt256.SIZE);
        }
    }

}
