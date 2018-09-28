using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Meadow.Core.Utils
{
    /// <summary>
    /// Helper class for encoding and decoding between hex strings and bytes arrays/spans.
    /// </summary>
    public static class HexUtil
    {

        /// <summary>
        /// Returns single lowercase hex character for given byte
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetHexCharFromByte(in byte b)
        {
            return (char)(b > 9 ? b + 0x57 : b + 0x30);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte HexCharToByte(in char c)
        {
            var b = c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0');
            return (byte)b;
        }

        public static void StripHexPrefix(ref ReadOnlySpan<char> str)
        {
            if (str.Length > 1)
            {
                if (str[0] == '0' && (str[1] == 'x' || str[1] == 'X'))
                {
                    str = str.Slice(2);
                }
            }
        }

        public static byte[] HexToBytes(string str)
        {
            ReadOnlySpan<char> strSpan = str.AsSpan();
            StripHexPrefix(ref strSpan);
            var byteArr = new byte[(strSpan.Length / 2) + (strSpan.Length % 2)];
            if (byteArr.Length == 0)
            {
                return byteArr;
            }

            Span<byte> bytes = byteArr;
            HexToSpan(strSpan, bytes);
            return byteArr;
        }

        public static ReadOnlyMemory<byte> HexToMemory(string str)
        {
            return HexToBytes(str);
        }


        /// <summary>
        /// Expected str to already be stripped of hex prefix, 
        /// and expects bytes to already be allocated to the correct size
        /// </summary>
        public static void HexToSpan(ReadOnlySpan<char> hexStr, Span<byte> bytes)
        {
            // Special case for compact single char hex format.
            // For example 0xf should be read the same as 0x0f
            if (hexStr.Length == 1)
            {
                bytes[0] = HexCharToByte(hexStr[0]);
            }
            else
            {
                Span<byte> cursor = bytes;

                if (hexStr.Length % 2 == 1)
                {
                    cursor[0] = HexCharToByte(hexStr[0]);
                    cursor = cursor.Slice(1);
                    hexStr = hexStr.Slice(1);

                }

                for (var i = 0; i < cursor.Length; i++)
                {
                    cursor[i] = (byte)((HexCharToByte(hexStr[i * 2]) << 4) | HexCharToByte(hexStr[(i * 2) + 1]));
                }
            }
        }

        public static string GetHexFromBytes(byte[] bytes, bool hexPrefix = false)
        {
            Span<byte> span = bytes;
            return GetHexFromBytes(span, hexPrefix);
        }


        public static string GetHexFromBytes(bool hexPrefix = false, params ReadOnlyMemory<byte>[] bytes)
        {
            var byteLen = 0;
            foreach (var mem in bytes)
            {
                byteLen += mem.Length;
            }

            Span<char> charArr = stackalloc char[(byteLen * 2) + (hexPrefix ? 2 : 0)];
            Span<char> c = charArr;
            if (hexPrefix)
            {
                c[0] = '0';
                c[1] = 'x';
                c = c.Slice(2);
            }

            foreach (var mem in bytes)
            {
                var span = mem.Span;
                WriteBytesIntoHexString(span, c);
                c = c.Slice(span.Length * 2);
            }

            return charArr.ToString();
        }

        public static string GetHexFromBytes(ReadOnlySpan<byte> bytes, bool hexPrefix = false)
        {
            if (hexPrefix && bytes.Length == 0)
            {
                return "0x";
            }

            Span<char> charArr = stackalloc char[(bytes.Length * 2) + (hexPrefix ? 2 : 0)];
            Span<char> c = charArr;
            if (hexPrefix)
            {
                c[0] = '0';
                c[1] = 'x';
                c = c.Slice(2);
            }

            WriteBytesIntoHexString(bytes, c);
            return charArr.ToString();
        }

        /// <summary>
        /// Expects target Span`char` to already be allocated to correct size
        /// </summary>
        /// <param name="bytes">Bytes to read</param>
        /// <param name="c">An already allocated char buffer to write hex into</param>
        public static void WriteBytesIntoHexString(ReadOnlySpan<byte> bytes, Span<char> c)
        {
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = bytes[i];
                c[i * 2] = GetHexCharFromByte((byte)(index >> 4));
                c[(i * 2) + 1] = GetHexCharFromByte((byte)(index & 0xF));
            }
        }

    }
}
