using Meadow.Core.EthTypes;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Meadow.Core.Utils.HexUtil;

namespace Meadow.Core.Utils
{
    /// <summary>
    /// Helper class for converting struct and class objects to and from hex.
    /// </summary>
    public static class HexConverter
    {

        public static string GetHexFromObject(object val, bool hexPrefix = false)
        {
            switch (val)
            {
                case byte v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case sbyte v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case short v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case ushort v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case int v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case uint v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case long v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case ulong v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case UInt256 v: return GetHexFromInteger(v, hexPrefix: hexPrefix);
                case Address v: return GetHex<Address>(v, hexPrefix: hexPrefix);
                case Hash v: return GetHex<Hash>(v, hexPrefix: hexPrefix);
                case Data v: return GetHex<Data>(v, hexPrefix: hexPrefix);
                case byte[] v: return GetHexFromBytes(v, hexPrefix: hexPrefix);
                default:
                    throw new Exception($"Converting type '{val.GetType()}' to hex is not supported");
            }
        }

        public static string GetHexFromInteger(in byte s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(byte)];
            bytes[0] = s;
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in sbyte s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(sbyte)];
            bytes[0] = unchecked((byte)s);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in short s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in ushort s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in int s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in uint s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in long s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in ulong s, bool hexPrefix = false)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, s);
            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string GetHexFromInteger(in UInt256 s, bool hexPrefix = false)
        {
            Span<UInt256> span = stackalloc UInt256[1]
            {
                s
            };
            Span<byte> bytes = MemoryMarshal.AsBytes(span);
            if (BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }

            StripLeadingZeroBytes(ref bytes);
            return GetZeroStrippedHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        static void StripLeadingZeroBytes(ref Span<byte> span)
        {
            int startIndex = 0;
            for (; startIndex < span.Length && span[startIndex] == 0x0; startIndex++) { }
            if (startIndex != 0)
            {
                span = span.Slice(startIndex);
            }
        }


        static string GetZeroStrippedHexFromBytes(ReadOnlySpan<byte> bytes, bool hexPrefix)
        {
            // Strips a leading 0 hex char from a hex sequence.
            // Example: 0x0fff -> 0xfff
            // This is a stupid work-around for geth which throws exceptions trying to parse an integer from (valid) hex string
            // that begins has a single 0 after the 0x prefix (example: 0x05).
            // Except for the case of 0x0 which is valid.
            var hex = GetHexFromBytes(bytes, hexPrefix: hexPrefix);
            if (hexPrefix)
            {
                if (hex.Length > 3 && hex[2] == '0')
                {
                    hex = "0x" + hex.Substring(3);
                }
                else if (hex.Length == 2)
                {
                    hex = "0x0";
                }
            }
            else
            {
                if (hex.Length > 1 && hex[0] == '0')
                {
                    hex = hex.Substring(1);
                }
            }

            return hex;
        }


        public static string GetHex<T>(in T s, bool hexPrefix = false) where T : unmanaged
        {
            Span<T> span = stackalloc T[1]
            {
                s
            };
            return GetHexFromBytes(MemoryMarshal.AsBytes(span), hexPrefix: hexPrefix);
        }

        public static string GetHexFromStruct<T>(in T s, bool hexPrefix = false) where T : unmanaged
        {
            Span<T> span = stackalloc T[1]
            {
                s
            };
            return GetHexFromBytes(MemoryMarshal.AsBytes(span), hexPrefix: hexPrefix);
        }

        static readonly ConcurrentDictionary<Type, MethodInfo> ParseHexGenericCache = new ConcurrentDictionary<Type, MethodInfo>();

        static readonly HashSet<Type> _bigEndianCheckTypes = new HashSet<Type>
        {
            typeof(ushort), typeof(short),
            typeof(uint), typeof(int),
            typeof(ulong), typeof(long),
            typeof(UInt256)
        };

        public static object HexToObject(Type targetType, string str)
        {
            if (targetType == typeof(byte[]))
            {
                return str.HexToBytes();
            }

            if (!targetType.IsValueType)
            {
                throw new ArgumentException($"Type '{targetType}' is not a struct", nameof(targetType));
            }
            
            MethodInfo methodInfo;

            if (!ParseHexGenericCache.TryGetValue(targetType, out methodInfo))
            {
                if (_bigEndianCheckTypes.Contains(targetType))
                {
                    methodInfo = typeof(HexConverter).GetMethod(nameof(HexToInteger), BindingFlags.Static | BindingFlags.Public);
                    methodInfo = methodInfo.MakeGenericMethod(targetType);
                }
                else
                {
                    methodInfo = typeof(HexConverter).GetMethod(nameof(HexToValue), BindingFlags.Static | BindingFlags.Public);
                    methodInfo = methodInfo.MakeGenericMethod(targetType);
                }

                ParseHexGenericCache[targetType] = methodInfo;
            }

            return methodInfo.Invoke(null, new object[] { str });
        }

        public static TInt HexToInteger<TInt>(string str) where TInt : unmanaged
        {
            ReadOnlySpan<char> strSpan = str.AsSpan();
            StripHexPrefix(ref strSpan);
            int byteLen = (strSpan.Length / 2) + (strSpan.Length % 2);
            int typeSize = Unsafe.SizeOf<TInt>();

            if (typeSize < byteLen)
            {
                throw new ArgumentException($"Target type '{typeof(TInt)}' is {Unsafe.SizeOf<TInt>()} bytes but was given {byteLen} bytes of input");
            }

            Span<TInt> holder = stackalloc TInt[1];
            Span<byte> bytes = MemoryMarshal.AsBytes(holder);
            HexToSpan(strSpan, bytes.Slice(typeSize - byteLen));

            switch (bytes.Length)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 32:
                    if (BitConverter.IsLittleEndian)
                    {
                        bytes.Reverse();
                    }

                    return holder[0];
                default:
                    throw new ArgumentException($"Unexpected input length {bytes.Length}");
            }
        }

        public static T HexToValue<T>(string str) where T : unmanaged
        {
            if (_bigEndianCheckTypes.Contains(typeof(T)))
            {
                throw new ArgumentException($"Integer types should be parsed with {nameof(HexToInteger)}");
            }

            var strSpan = str.AsSpan();

            StripHexPrefix(ref strSpan);

            var byteLen = (strSpan.Length / 2) + (strSpan.Length % 2);
            var tSize = Unsafe.SizeOf<T>();
            if (byteLen > tSize)
            {
                var overSize = (byteLen - tSize) * 2;
                for (var i = 0; i < overSize; i++)
                {
                    if (strSpan[i] != '0')
                    {
                        throw new ArgumentException($"Cannot fit {byteLen} bytes from hex string into {tSize} byte long type {typeof(T)}");

                    }
                }

                strSpan = strSpan.Slice(overSize);
            }
            else if (byteLen < tSize)
            {
                var underSize = (tSize * 2) - strSpan.Length;
                Span<char> paddedStr = new char[tSize * 2];
                for (var i = 0; i < underSize; i++)
                {
                    paddedStr[i] = '0';
                }

                strSpan.CopyTo(paddedStr.Slice(underSize));
                strSpan = paddedStr;
            }

            Span<T> span = stackalloc T[1];
            HexToSpan(strSpan, MemoryMarshal.AsBytes(span));
            return span[0];
        }



    }
}
