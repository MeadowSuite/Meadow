using Meadow.Core.Utils;
using System;
using System.Linq;
using Meadow.Core.EthTypes;
using System.ComponentModel;
using System.Numerics;

namespace Meadow.Core.AbiEncoding.Encoders
{
    // Encoded the same as an uint8, where 1 is used for true and 0 for false

    public class BoolEncoder : AbiTypeEncoder<bool>
    {
        public override int GetEncodedSize() => UInt256.SIZE;
        public override int GetPackedEncodedSize() => 1;

        public override void SetValue(object val)
        {
            if (val is bool boolean)
            {
                SetValue(boolean);
            }
            else if (val is string str)
            {
                str = str.Trim();
                if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    SetValue(false);
                }
                else if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    SetValue(true);
                }
                else if (str == "0")
                {
                    SetValue(false);
                }
                else if (str == "1")
                {
                    SetValue(true);
                }
                else
                {
                    ThrowInvalidTypeException(val);
                }
            }
            else
            {
                switch (val)
                {
                    case byte b:
                        SetValue(b != 0);
                        break;
                    case sbyte b:
                        SetValue(b != 0);
                        break;
                    case ushort b:
                        SetValue(b != 0);
                        break;
                    case short b:
                        SetValue(b != 0);
                        break;
                    case uint b:
                        SetValue(b != 0);
                        break;
                    case int b:
                        SetValue(b != 0);
                        break;
                    case ulong b:
                        SetValue(b != 0);
                        break;
                    case long b:
                        SetValue(b != 0);
                        break;
                    case UInt256 b:
                        SetValue(b != 0);
                        break;
                    case BigInteger b:
                        SetValue(b != 0);
                        break;
                    default:
                        ThrowInvalidTypeException(val);
                        break;
                }
            }
        }

        public override void Encode(ref AbiEncodeBuffer buffer)
        {
            buffer.HeadCursor[UInt256.SIZE - 1] = _val ? (byte)1 : (byte)0;
            buffer.IncrementHeadCursor(UInt256.SIZE);
        }

        static readonly byte[] ZEROx31 = Enumerable.Repeat((byte)0, UInt256.SIZE - 1).ToArray();

        public override void Decode(ref AbiDecodeBuffer buff, out bool val)
        {
            // Input data validity check: last byte should be either 0 or 1.
            switch (buff.HeadCursor[31])
            {
                case 0:
                    val = false;
                    break;
                case 1:
                    val = true;
                    break;
                default:
                    throw Error(buff.HeadCursor);

            }

#if ZERO_BYTE_CHECKS
            // Input data validity check: all but the last byte should be zero.
            // Span<byte>.SequenceEquals should use the fast native memory slab comparer.
            if (!buff.HeadCursor.Slice(0, UInt256.SIZE - 1).SequenceEqual(ZEROx31))
            {
                throw Error(buff.HeadCursor.Slice(0, UInt256.SIZE - 1));
            }
#endif

            buff.IncrementHeadCursor(UInt256.SIZE);

            Exception Error(ReadOnlySpan<byte> payload)
            {
                return new ArgumentException("Invalid boolean input data; should be 31 zeros followed by a 1 or 0; received: " + payload.Slice(0, UInt256.SIZE).ToHexString());
            }
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            buffer[0] = _val ? (byte)1 : (byte)0;
            buffer = buffer.Slice(1);
        }



    }


}
