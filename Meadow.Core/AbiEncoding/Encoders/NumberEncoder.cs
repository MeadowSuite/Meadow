using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Meadow.Core.EthTypes;
using System.Collections.Concurrent;
using System.Globalization;
using System.ComponentModel;

namespace Meadow.Core.AbiEncoding.Encoders
{

    public abstract class NumberEncoder<TInt> : AbiTypeEncoder<TInt> where TInt :
#if LANG_7_3
        unmanaged
#else
        struct
#endif
    {
        protected static Dictionary<string, (int ByteSize, BigInteger MaxValue)> _unsignedTypeSizes
            = new Dictionary<string, (int ByteSize, BigInteger MaxValue)>();

        protected static Dictionary<string, (int ByteSize, BigInteger MaxValue, BigInteger MinValue)> _signedTypeSizes
            = new Dictionary<string, (int ByteSize, BigInteger MaxValue, BigInteger MinValue)>();

        static NumberEncoder()
        {
            for (var i = 1; i <= UInt256.SIZE; i++)
            {
                var bitSize = i * 8;
                var maxIntValue = BigInteger.Pow(2, bitSize);
                _unsignedTypeSizes.Add("uint" + bitSize, (i, maxIntValue));
                _signedTypeSizes.Add("int" + bitSize, (i, (maxIntValue / 2) + 1, -maxIntValue / 2));
            }
        }

        public override int GetEncodedSize() => UInt256.SIZE;
        public override int GetPackedEncodedSize() => _info.PrimitiveTypeByteSize;

        protected abstract bool Signed { get; }
        protected abstract BigInteger AsBigInteger { get; }

        public BigInteger MaxValue => Signed ? _signedTypeSizes[_info.SolidityName].MaxValue : _unsignedTypeSizes[_info.SolidityName].MaxValue;
        public BigInteger MinValue => Signed ? _signedTypeSizes[_info.SolidityName].MinValue : 0;

        public override void SetValue(object val)
        {
            if (val is TInt num)
            {
                SetValue(num);
            }
            else if (val is string str)
            {
                str = str.Trim();
                BigInteger bigInt;

                if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = HexUtil.HexToBytes(str);
                    Array.Reverse(bytes);
                    if (Signed)
                    {
                        bigInt = new BigInteger(bytes);
                    }
                    else
                    {
                        bigInt = new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());
                    }
                }
                else
                {
                    bigInt = BigInteger.Parse(str, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
                }

                SetValue(TypeConversion.ConvertValue<TInt>(bigInt));
            }
            else
            {
                switch (val)
                {
                    case byte u8:
                    case sbyte s8:
                    case ushort u16:
                    case short s16:
                    case uint u32:
                    case int s32:
                    case ulong u64:
                    case long s64:
                    case float f32 when Math.Floor(f32) == f32:
                    case double f64 when Math.Floor(f64) == f64:
                    case decimal d128 when Math.Floor(d128) == d128:
                    case UInt256 u256:
                    case BigInteger bi:
                        SetValue(TypeConversion.ConvertValue<TInt>(val));
                        break;
                    default:
                        ThrowInvalidTypeException(val);
                        break;
                }
            }
        }

        public override void SetValue(in TInt val)
        {
            base.SetValue(val);
            var bigInt = AsBigInteger;
            if (bigInt > MaxValue)
            {
                throw IntOverflow();
            }

            if (Signed && bigInt < MinValue)
            {
                throw IntUnderflow();
            }
        }

        public override void EncodePacked(ref Span<byte> buffer)
        {
            var byteSize = _info.PrimitiveTypeByteSize;
            Span<TInt> valSpan = stackalloc TInt[1] 
            {
                _val
            };
            Span<byte> byteView = MemoryMarshal.AsBytes(valSpan);

            var encodedSize = _info.PrimitiveTypeByteSize;

            if (BitConverter.IsLittleEndian)
            {
                var indexOffset = encodedSize - 1;

                for (var i = 0; i < byteSize; i++)
                {
                    buffer[indexOffset - i] = byteView[i];
                }
            }
            else
            {
                for (var i = 0; i < byteSize; i++)
                {
                    buffer[i] = byteView[i];
                }
            }

            buffer = buffer.Slice(encodedSize);
        }

        public override void Encode(ref AbiEncodeBuffer buffer)
        {
            Encode(buffer.HeadCursor);
            buffer.IncrementHeadCursor(UInt256.SIZE);
        }

        protected void Encode(Span<byte> buffer)
        {
            var byteSize = _info.PrimitiveTypeByteSize;
            Span<TInt> valSpan = stackalloc TInt[1]
            {
                _val
            };
            Span<byte> byteView = MemoryMarshal.AsBytes(valSpan);

            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < byteSize; i++)
                {
                    buffer[31 - i] = byteView[i];
                }
            }
            else
            {
                for (var i = 0; i < byteSize; i++)
                {
                    buffer[31 - byteSize + i] = byteView[i];
                }
            }

            if (Signed)
            {
                // pad two's complement encoding into larger type by checking if most significant bit is set
                var isNeg = (byteView[byteSize - 1] & (1 << 7)) != 0;
                if (isNeg)
                {
                    for (var i = 0; i < 32 - byteSize; i++)
                    {
                        buffer[i] = 0xFF;
                    }
                }
            }
        }

        protected Exception IntOverflow()
        {
            return new OverflowException($"Max value for type '{_info}' is {MaxValue}, was given {_val}");
        }

        protected Exception IntUnderflow()
        {
            return new OverflowException($"Min value for type '{_info}' is {MinValue}, was given {_val}");
        }

        public override void Decode(ref AbiDecodeBuffer buff, out TInt val)
        {
            Decode(buff.HeadCursor, out val);
            buff.IncrementHeadCursor(UInt256.SIZE);
        }

        public void Decode(ReadOnlySpan<byte> buffer, out TInt val)
        {
            Span<TInt> num = new TInt[1];
            Span<byte> byteView = MemoryMarshal.AsBytes(num);

            var byteSize = _info.PrimitiveTypeByteSize;
            var padSize = UInt256.SIZE - byteSize;
            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < byteSize; i++)
                {
                    byteView[byteSize - i - 1] = buffer[i + padSize];
                }
            }
            else
            {
                for (var i = 0; i < byteSize; i++)
                {
                    byteView[i] = buffer[i + padSize];
                }
            }

            if (Signed)
            {
                // pad two's complement encoding into larger type by checking if most significant bit is set
                var isNeg = (byteView[byteSize - 1] & (1 << 7)) != 0;
                if (isNeg)
                {
                    for (var i = byteSize; i < byteView.Length; i++)
                    {
                        byteView[i] = 0xFF;
                    }
                }
            }

#if ZERO_BYTE_CHECKS
            // data validity check: should be padded with zero-bytes
            for (var i = 0; i < padSize; i++)
            {
                if (buffer[i] != 0 && buffer[i] != 0xFF)
                {
                    throw new ArgumentException($"Invalid {_info.SolidityName} input data; should be {byteSize} bytes, left-padded with {UInt256.SIZE - byteSize} zero-bytes; received: " + buffer.Slice(0, 32).ToHexString());
                }
            }
#endif

            val = num[0];
        }
    }


    public class Int8Encoder : NumberEncoder<sbyte>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;

        public override void SetValue(in sbyte val)
        {
            _val = val;
        }
    }

    public class UInt8Encoder : NumberEncoder<byte>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;

        public override void SetValue(in byte val)
        {
            _val = val;
        }
    }

    public class Int16Encoder : NumberEncoder<short>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt16Encoder : NumberEncoder<ushort>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int32Encoder : NumberEncoder<int>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt32Encoder : NumberEncoder<uint>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int64Encoder : NumberEncoder<long>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
    }

    public class UInt64Encoder : NumberEncoder<ulong>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => _val;
    }

    public class Int256Encoder : NumberEncoder<BigInteger>
    {
        protected override bool Signed => true;
        protected override BigInteger AsBigInteger => _val;
        
        public override void EncodePacked(ref Span<byte> buffer)
        {
            var bigInt = AsBigInteger;
            Span<byte> bytes = bigInt.ToByteArray();
            bytes.Reverse();

            var len = _info.PrimitiveTypeByteSize;

            // strip leading zeros
            while (bytes.Length > len && bytes[0] == 0)
            {
                bytes = bytes.Slice(1);
            }

            var padding = len - bytes.Length;
            bytes.CopyTo(buffer.Slice(padding));
            if (bigInt.Sign < 0)
            {
                for (var i = 0; i < padding; i++)
                {
                    buffer[i] = 0xFF;
                }
            }

            buffer = buffer.Slice(len);
        }

        public override void Encode(ref AbiEncodeBuffer buff)
        {
            // Get the bytes for our integer as a 256-bit integer, as it should
            // have all those leading bits set, regardless of actual integer size.
            Span<byte> arr = BigIntegerConverter.GetBytes(AsBigInteger, UInt256.SIZE);

            // Copy it to our output buffer.
            arr.CopyTo(buff.HeadCursor);

            // Advance our cursor
            buff.IncrementHeadCursor(UInt256.SIZE);
        }

        public override void Decode(ref AbiDecodeBuffer buff, out BigInteger val)
        {
            // Copy our read only span into a normal span (only the bits our integer needs).
            int size = _info.PrimitiveTypeByteSize;
            Span<byte> spanCopy = new byte[size];
            buff.HeadCursor.Slice(buff.HeadCursor.Length - size).CopyTo(spanCopy);

            // Read our big integer from our data
            val = BigIntegerConverter.GetBigInteger(spanCopy, true, size);

            // Advance our cursor.
            buff.IncrementHeadCursor(UInt256.SIZE);
        }
    }

    public class UInt256Encoder : NumberEncoder<UInt256>
    {
        protected override bool Signed => false;
        protected override BigInteger AsBigInteger => (BigInteger)_val;

        public static readonly ObjectPool<UInt256Encoder> UncheckedEncoders = new ObjectPool<UInt256Encoder>(() => 
        {
            var inst = new UInt256Encoder();
            inst.SetTypeInfo(AbiTypeMap.GetSolidityTypeInfo("uint256"));
            return inst;
        });

        public override void SetValue(object val)
        {
            if (val is UInt256 num)
            {
                SetValue(num);
            }
            else if (val is string numStr)
            {
                SetValue((UInt256)numStr);
            }
            else
            {
                switch (val)
                {
                    case byte b:
                        SetValue(b);
                        break;
                    case sbyte b:
                        SetValue(b);
                        break;
                    case ushort b:
                        SetValue(b);
                        break;
                    case short b:
                        SetValue(b);
                        break;
                    case uint b:
                        SetValue(b);
                        break;
                    case int b:
                        SetValue(b);
                        break;
                    case ulong b:
                        SetValue(b);
                        break;
                    case long b:
                        SetValue(b);
                        break;
                    case float b when Math.Floor(b) == b:
                        SetValue(b);
                        break;
                    case double b when Math.Floor(b) == b:
                        SetValue(b);
                        break;
                    case decimal b when Math.Floor(b) == b:
                        SetValue(b);
                        break;
                    case UInt256 b:
                        SetValue(b);
                        break;
                    case BigInteger b:
                        SetValue((UInt256)b);
                        break;
                    default:
                        ThrowInvalidTypeException(val);
                        break;
                }
            }
        }

        public override void SetValue(in UInt256 val)
        {
            // Skip unnecessary bounds check on max uint256 value.
            // An optimization only for this common type at the moment.
            if (_info.SolidityName == "uint256")
            {
                _val = val;
            }
            else
            {
                base.SetValue(val);
            }
        }

        /// <summary>
        /// Encodes a solidity 'uint256' (with no overflow checks since its the max value)
        /// </summary>
        public void Encode(ref AbiEncodeBuffer buff, in UInt256 val)
        {
            var encoder = UncheckedEncoders.Get();
            try
            {
                encoder._val = val;
                encoder.Encode(ref buff);
            }
            finally
            {
                UncheckedEncoders.Put(encoder);
            }
        }

        public void Encode(Span<byte> data, in UInt256 val)
        {
            var encoder = UncheckedEncoders.Get();
            try
            {
                encoder._val = val;
                encoder.Encode(data);
            }
            finally
            {
                UncheckedEncoders.Put(encoder);
            }
        }

        public void Decode(ReadOnlySpan<byte> buffer, out int val)
        {
            Decode(buffer, out UInt256 num);
            val = (int)num;
        }

        public void Decode(ref AbiDecodeBuffer buff, out int val)
        {
            Decode(ref buff, out UInt256 num);
            val = (int)num;
        }

    }


}
