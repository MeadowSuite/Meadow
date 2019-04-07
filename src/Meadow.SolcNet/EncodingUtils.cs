using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SolcNet
{
    public static class EncodingUtils
    {
        private static readonly Encoding UTF8_ENCODING = new UTF8Encoding(false, false);

        public static unsafe IntPtr StringToUtf8(string str)
        {
            if (str == null)
            {
                return IntPtr.Zero;
            }

            int length = UTF8_ENCODING.GetByteCount(str);
            var buffer = (byte*)Marshal.AllocHGlobal(length + 1);

            if (length > 0)
            {
                fixed (char* pValue = str)
                {
                    UTF8_ENCODING.GetBytes(pValue, str.Length, buffer, length);
                }
            }

            buffer[length] = 0;

            return new IntPtr(buffer);
        }

        public static unsafe string Utf8ToString(IntPtr utf8)
        {
            if (utf8 == IntPtr.Zero)
            {
                return null;
            }

            var pNativeData = (byte*)utf8;

            var start = pNativeData;
            byte* walk = start;

            // Find the end of the string
            while (*walk != 0)
            {
                walk++;
            }

            if (walk == start)
            {
                return String.Empty;
            }

            return new String((sbyte*)pNativeData, 0, (int)(walk - start), UTF8_ENCODING);
        }

        public static ValueTuple<T1, T2, T3>[] Flatten<T1, T2, T3>(this Dictionary<T1, Dictionary<T2, T3>> dicts)
        {
            return FlattenNestedDictionaries(dicts);
        }

        public static ValueTuple<T1, T2, T3>[] FlattenNestedDictionaries<T1, T2, T3>(Dictionary<T1, Dictionary<T2, T3>> dicts)
        {
            var items = new List<(T1, T2, T3)>();
            foreach (var kp in dicts)
            {
                foreach (var c in kp.Value)
                {
                    items.Add((kp.Key, c.Key, c.Value));
                }
            }

            return items.ToArray();
        }

        public static byte[] HexToBytes(string str)
        {
            ReadOnlySpan<char> strSpan = str.AsSpan();
            if (strSpan.Length > 1)
            {
                if (strSpan[0] == '0' && (strSpan[1] == 'x' || strSpan[1] == 'X'))
                {
                    strSpan = strSpan.Slice(2);
                }
            }

            var byteArr = new byte[strSpan.Length / 2];
            if (byteArr.Length == 0)
            {
                return byteArr;
            }

            Span<byte> bytes = byteArr;
            HexToSpan(strSpan, bytes);
            return byteArr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte HexCharToByte(in char c)
        {
            var b = c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0');
            return (byte)b;
        }

        /// <summary>
        /// Expected str to already be stripped of hex prefix, 
        /// and expects bytes to already be allocated to the correct size
        /// </summary>
        private static void HexToSpan(ReadOnlySpan<char> hexStr, Span<byte> bytes)
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
                    cursor[i] = (byte)((HexCharToByte(hexStr[i * 2]) << 4) | HexCharToByte(hexStr[i * 2 + 1]));
                }
            }
        }
    }
}
