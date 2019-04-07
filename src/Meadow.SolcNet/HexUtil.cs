using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SolcNet
{
    static class HexUtil
    {

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
        static byte HexCharToByte(in char c)
        {
            var b = c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0');
            return (byte)b;
        }

        /// <summary>
        /// Expected str to already be stripped of hex prefix, 
        /// and expects bytes to already be allocated to the correct size
        /// </summary>
        static void HexToSpan(ReadOnlySpan<char> hexStr, Span<byte> bytes)
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
