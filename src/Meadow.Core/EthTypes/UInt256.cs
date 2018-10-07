using Meadow.Core.Utils;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Meadow.Core.EthTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UInt256 : IComparable<UInt256>, IEquatable<UInt256>
    {
        public const int SIZE = 32;

        public static readonly UInt256 MaxValue = new UInt256(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        public static readonly UInt256 MinValue = 0;
        public static readonly UInt256 Zero = 0;

        public static readonly BigInteger MaxValueAsBigInt = MaxValue.ToBigInteger();

        // parts are big-endian
        readonly ulong _part1;
        readonly ulong _part2;
        readonly ulong _part3;
        readonly ulong _part4;

        public UInt256(Span<byte> arr) : this()
        {
            if (arr.Length < 32)
            {
                Span<byte> newArr = new byte[32];
                arr.CopyTo(newArr);
                arr = newArr;
            }

            var pos = 0;
            FromByteArray(arr, ref pos, ref this);
        }

        public UInt256(UInt256 uint256) : this(uint256._part1, uint256._part2, uint256._part3, uint256._part4)
        {

        }

        public UInt256(BigInteger value) : this(BigIntegerConverter.GetBytes(value).Reverse().ToArray())
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public UInt256(int value) : this()
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            unchecked
            {
                _part1 = (ulong)value;
            }
        }

        public UInt256(long value) : this()
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            unchecked
            {
                _part1 = (ulong)value;
            }
        }

        public UInt256(uint value) : this()
        {
            _part1 = value;
        }

        public UInt256(ulong value) : this()
        {
            _part1 = value;
        }

        public UInt256(ulong part1, ulong part2, ulong part3, ulong part4)
        {
            _part1 = part1;
            _part2 = part2;
            _part3 = part3;
            _part4 = part4;
        }

        public UInt256(ref ulong part1, ref ulong part2, ref ulong part3, ref ulong part4)
        {
            _part1 = part1;
            _part2 = part2;
            _part3 = part3;
            _part4 = part4;
        }

        public static UInt256 DivRem(UInt256 dividend, UInt256 divisor, out UInt256 remainder)
        {
            BigInteger remainderBigInt;
            var result = new UInt256(BigInteger.DivRem(dividend.ToBigInteger(), divisor.ToBigInteger(), out remainderBigInt));
            remainder = new UInt256(remainderBigInt);
            return result;
        }

        public static UInt256 Pow(UInt256 value, int exponent) => new UInt256(BigInteger.Pow(value.ToBigInteger(), exponent));

        public static double Log(UInt256 value, double baseValue) => BigInteger.Log(value.ToBigInteger(), baseValue);

        /// <summary>
        /// Divides with a precision of 28 digits <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/decimal"/>
        /// </summary>
        public static UInt256 DivideRounded(UInt256 dividend, UInt256 divisor)
        {
            // Perform rounded integer division on large number
            return (dividend / divisor) + ((dividend % divisor) / (divisor / 2));
        }

        public static explicit operator decimal(UInt256 value) => (decimal)value.ToBigInteger();
        public static explicit operator double(UInt256 value) => (double)value.ToBigInteger();
        public static explicit operator float(UInt256 value) => (float)value.ToBigInteger();
        public static explicit operator ulong(UInt256 value) => (ulong)value.ToBigInteger();
        public static explicit operator long(UInt256 value) => (long)value.ToBigInteger();
        public static explicit operator uint(UInt256 value) => (uint)value.ToBigInteger();
        public static explicit operator int(UInt256 value) => (int)value.ToBigInteger();
        public static explicit operator ushort(UInt256 value) => (ushort)value.ToBigInteger();
        public static explicit operator short(UInt256 value) => (short)value.ToBigInteger();
        public static explicit operator byte(UInt256 value) => (byte)value.ToBigInteger();
        public static explicit operator sbyte(UInt256 value) => (sbyte)value.ToBigInteger();
        public static explicit operator BigInteger(UInt256 value) => value.ToBigInteger();
        public static explicit operator UInt256(BigInteger value) => new UInt256(value);
        public static explicit operator UInt256(string value) => TryParse(value, out var result) ? result : FromHexString(value);

        public static implicit operator UInt256(byte value) => new UInt256(value);
        public static implicit operator UInt256(int value) => new UInt256(value);
        public static implicit operator UInt256(long value) => new UInt256(value);
        public static implicit operator UInt256(sbyte value) => new UInt256(value);
        public static implicit operator UInt256(short value) => new UInt256(value);
        public static implicit operator UInt256(uint value) => new UInt256(value);
        public static implicit operator UInt256(ulong value) => new UInt256(value);
        public static implicit operator UInt256(ushort value) => new UInt256(value);

        public static implicit operator UInt256(float value) => FromFloatingType(value);
        public static implicit operator UInt256(double value) => FromFloatingType(value);
        public static implicit operator UInt256(decimal value) => FromDecimalType(value);

        static UInt256 FromFloatingType(double floatingNum)
        {
            if (double.IsPositiveInfinity(floatingNum))
            {
                throw new NotFiniteNumberException($"Cannot convert +Infinity into {nameof(UInt256)}");
            }

            if (double.IsNegativeInfinity(floatingNum))
            {
                throw new NotFiniteNumberException($"Cannot convert -Infinity into {nameof(UInt256)}");
            }

            if (double.IsNaN(floatingNum))
            {
                throw new NotFiniteNumberException($"Cannot convert NaN into {nameof(UInt256)}");
            }

            if (floatingNum < 0)
            {
                throw new OverflowException($"Cannot convert negative number \"{floatingNum.ToString(CultureInfo.InvariantCulture)}\" into {nameof(UInt256)}");
            }

            var hasFractionalPart = (floatingNum - Math.Round(floatingNum) != 0);
            if (hasFractionalPart)
            {
                throw new ArithmeticException($"Cannot convert a fractional number \"{floatingNum.ToString(CultureInfo.InvariantCulture)}\" into a {nameof(UInt256)}");
            }

            var exponentialNotationStr = floatingNum.ToString("E", CultureInfo.InvariantCulture);
            var bigInt = BigInteger.Parse(exponentialNotationStr, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            if (bigInt > MaxValueAsBigInt)
            {
                throw new OverflowException($"Number \"{floatingNum.ToString(CultureInfo.InvariantCulture)}\" is larger than {MaxValue} (the max size of a {nameof(UInt256)})");
            }

            return new UInt256(bigInt);
        }

        static UInt256 FromDecimalType(decimal decimalNum)
        {
            if (decimalNum < 0)
            {
                throw new OverflowException($"Cannot convert negative number \"{decimalNum.ToString(CultureInfo.InvariantCulture)}\" into {nameof(UInt256)}");
            }

            var bigInt = new BigInteger(decimalNum);
            var hasFractionalPart = (decimalNum - ((decimal)bigInt) != 0);
            if (hasFractionalPart)
            {
                throw new ArithmeticException($"Cannot convert a fractional number \"{decimalNum.ToString(CultureInfo.InvariantCulture)}\" into a {nameof(UInt256)}");
            }

            if (bigInt > MaxValueAsBigInt)
            {
                throw new OverflowException($"Number \"{decimalNum.ToString(CultureInfo.InvariantCulture)}\" is larger than {MaxValue} (the max size of a {nameof(UInt256)})");
            }

            return new UInt256(bigInt);
        }

        public static Boolean operator !=(UInt256 left, UInt256 right) => !(left == right);
        public static Boolean operator ==(UInt256 left, UInt256 right) => (left._part1 == right._part1) && (left._part2 == right._part2) && (left._part3 == right._part3) && (left._part4 == right._part4);
        public static UInt256 operator %(UInt256 dividend, UInt256 divisor) => new UInt256(dividend.ToBigInteger() % divisor.ToBigInteger());
        public static UInt256 operator +(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() + right.ToBigInteger());
        public static UInt256 operator -(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() - right.ToBigInteger());
        public static UInt256 operator *(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() * right.ToBigInteger());
        public static UInt256 operator /(UInt256 dividend, UInt256 divisor) => new UInt256(dividend.ToBigInteger() / divisor.ToBigInteger());
        public static UInt256 operator ~(UInt256 value) => new UInt256(~value._part1, ~value._part2, ~value._part3, ~value._part4);
        public static UInt256 operator >>(UInt256 value, int shift) => new UInt256(value.ToBigInteger() >> shift);
        public static UInt256 operator <<(UInt256 value, int shift) => new UInt256(value.ToBigInteger() << shift);
        public static UInt256 operator |(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() | right.ToBigInteger());
        public static UInt256 operator &(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() & right.ToBigInteger());
        public static UInt256 operator ^(UInt256 left, UInt256 right) => new UInt256(left.ToBigInteger() ^ right.ToBigInteger());
        public static UInt256 operator ++(UInt256 value) => value + 1;
        public static UInt256 operator --(UInt256 value) => value - 1;

        public static Boolean operator >(UInt256 left, UInt256 right)
        {
            return left.ToBigInteger() > right.ToBigInteger();
        }

        public static Boolean operator >=(UInt256 left, UInt256 right)
        {
            return left.ToBigInteger() >= right.ToBigInteger();
        }

        public static Boolean operator <(UInt256 left, UInt256 right)
        {
            return left.ToBigInteger() < right.ToBigInteger();
        }

        public static Boolean operator <=(UInt256 left, UInt256 right)
        {
            return left.ToBigInteger() <= right.ToBigInteger();
        }

        public static UInt256 Parse(string value, IFormatProvider provider) => new UInt256(BigInteger.Parse("0" + value, provider));
        public static UInt256 Parse(string value, NumberStyles style) => new UInt256(BigInteger.Parse("0" + value, style, CultureInfo.InvariantCulture));
        public static UInt256 Parse(string value, NumberStyles style, IFormatProvider provider) => new UInt256(BigInteger.Parse("0" + value, style, provider));

        /// <summary>
        /// Parses a string of a positive numeric value. 
        /// Can be a integer without commas or decimals. 
        /// Can be a exponential notation, examples: "45e10", "1.23e5", "24E18", "24e+18". 
        /// Underscores and spaces are ignored. 
        /// </summary>
        public static UInt256 Parse(string numericString)
        {
            return TryParseInternal(numericString, out var result, out var ex) ? result : throw ex;
        }

        /// <summary>
        /// Parses a hex string as bytes. The '0x' prefix is optional.
        /// Examples: "0x12a05f200", "12a05f200", "0xff", "ff", "0xcded53d631ce4a38a1f90d59e5f2f9c023cd28c64aa66488e9462cc4a64a032f"
        /// </summary>
        public static UInt256 ParseHex(string hexString)
        {
            return HexConverter.HexToInteger<UInt256>(hexString);
        }

        /// <summary>
        /// Parses a string of a positive numeric value. 
        /// Can be a integer (no decimals). 
        /// Can be a exponential notation, examples: "45e10", "1.23e5", "24E18", "24e+18". 
        /// Commas, underscores, spaces are ignored. 
        /// </summary>
        public static bool TryParse(string numericString, out UInt256 val)
        {
            return TryParseInternal(numericString, out val, out _);
        }

        static bool TryParseInternal(string numericString, out UInt256 val, out Exception ex)
        {
            // Check if the input has a hex prefix
            var trimmed = numericString.Trim().Replace("_", "").Replace(" ", "");
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                ex = new ArgumentException($"Input value contains a hex string prefix. Did you mean to use '{nameof(FromHexString)}'?");
                val = default;
                return false;
            }

            BigInteger bigInt;

            // Check if the input is a number in exponential notation
            var exponentialIndex = trimmed.IndexOfAny(new[] { 'e', 'E' });
            if (exponentialIndex > 0 && exponentialIndex == trimmed.LastIndexOfAny(new[] { 'e', 'E' }))
            {
                if (!BigInteger.TryParse(trimmed, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out bigInt))
                {
                    ex = new ArgumentException("Input value is in an invalid exponential notation.");
                    val = default;
                    return false;
                }
            }
            else
            {
                // Check if the value has a decimal
                if (trimmed.Contains('.'))
                {
                    ex = new ArgumentException("Input value should not contain demicals or thousands seperator characters.");
                    val = default;
                    return false;
                }

                // Try to parse the integer
                if (!BigInteger.TryParse(trimmed, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out bigInt))
                {
                    ex = new ArgumentException("Input value could not be parsed as an integer.");
                    val = default;
                    return false;
                }
            }

            // Disallow negatives
            if (bigInt.Sign < 0)
            {
                ex = new ArgumentException("Input value should not be negative.");
                val = default;
                return false;
            }

            // Check the the number fits into 32 bytes
            Span<byte> bytes = bigInt.ToByteArray();
            while (bytes.Length > SIZE && bytes[bytes.Length - 1] == 0)
            {
                bytes = bytes.Slice(0, bytes.Length - 1);
            }

            if (bytes.Length > SIZE)
            {
                ex = new ArgumentException($"Integer is too large. Max size is {SIZE} bytes. Input value requires {bytes.Length} bytes.");
                val = default;
                return false;
            }

            ex = null;
            val = new UInt256(bytes);
            return true;
        }

        /// <summary>
        /// Alias for <see cref="Parse(string)"/>
        /// </summary>
        public static UInt256 FromString(string numericString)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            return Parse(numericString);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        /// <summary>
        /// Alias for <see cref="ParseHex(string)"/>
        /// </summary>
        public static UInt256 FromHexString(string hexString)
        {
            return ParseHex(hexString);
        }

        public int CompareTo(UInt256 other)
        {
            if (this == other)
            {
                return 0;
            }

            if (this < other)
            {
                return -1;
            }

            if (this > other)
            {
                return +1;
            }

            throw new Exception();
        }

        public override Boolean Equals(Object obj)
        {
            return obj is UInt256 other ? Equals(other) : false;
        }

        public bool Equals(UInt256 other)
        {
            return (other._part1 == _part1) && (other._part2 == _part2) && (other._part3 == _part3) && (other._part4 == _part4);
        }

        public override int GetHashCode()
        {
            return (_part1, _part2, _part3, _part4).GetHashCode();
        }

        public BigInteger ToBigInteger()
        {
            var buffer = new byte[33];
            int position = 0;
            ToByteArray(buffer, ref position, ref this);
            return new BigInteger(buffer);
        }

        public static void FromByteArray(ReadOnlySpan<byte> buffer, out UInt256 num)
        {
            num = MemoryMarshal.Read<UInt256>(buffer);
        }

        public static void FromByteArray(Span<byte> buffer, ref int position, ref UInt256 num)
        {
            if (buffer.Length - position < 32)
            {
                throw new ArgumentOutOfRangeException();
            }

            num = MemoryMarshal.Read<UInt256>(buffer.Slice(position));

            position += 32;
        }

        public static void ToByteArray(Span<byte> buffer, ref int position, ref UInt256 num)
        {
            if (buffer.Length - position < 32)
            {
                throw new ArgumentOutOfRangeException();
            }

            MemoryMarshal.Write(buffer.Slice(position), ref num);

            position += 8;
        }

        public byte[] ToByteArray()
        {
            int position = 0;
            var buffer = new byte[32];
            ToByteArray(buffer, ref position, ref this);
            return buffer;
        }

        public static void ToByteArraySafe(byte[] buffer, ref int position, ref UInt256 num)
        {
            byte[] Order(byte[] value)
            {
                return BitConverter.IsLittleEndian ? value : value.Reverse().ToArray();
            }

            Buffer.BlockCopy(Order(BitConverter.GetBytes(num._part1)), 0, buffer, position, 8);
            Buffer.BlockCopy(Order(BitConverter.GetBytes(num._part2)), 0, buffer, position += 8, 8);
            Buffer.BlockCopy(Order(BitConverter.GetBytes(num._part3)), 0, buffer, position += 8, 8);
            Buffer.BlockCopy(Order(BitConverter.GetBytes(num._part4)), 0, buffer, position += 8, 8);
            position += 8;
        }

        public byte[] ToByteArraySafe()
        {
            var buffer = new byte[32];
            int pos = 0;
            ToByteArraySafe(buffer, ref pos, ref this);
            return buffer;
        }

        public override string ToString()
        {
            return ToBigInteger().ToString(CultureInfo.InvariantCulture);
        }

        public string ToHexString(bool hexPrefix = true)
        {
            return HexConverter.GetHexFromInteger(this, hexPrefix);
        }

    }

}