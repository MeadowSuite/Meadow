using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.Core.Utils
{
    /// <summary>
    /// Hex encoding extensions methods for strings and byte arrays/spans.
    /// </summary>
    public static class HexExtensions
    {
        public static ReadOnlyMemory<byte> HexToReadOnlyMemory(this string hexString)
        {
            return HexUtil.HexToMemory(hexString);
        }

        public static byte[] HexToBytes(this string hexString)
        {
            return HexUtil.HexToBytes(hexString);
        }

        public static Span<byte> HexToSpan(this string hexString)
        {
            return HexUtil.HexToBytes(hexString);
        }

        public static ReadOnlySpan<byte> HexToReadOnlySpan(this string hexString)
        {
            return HexUtil.HexToBytes(hexString);
        }

        public static string ToHexString(this ReadOnlySpan<byte> bytes, bool hexPrefix = false)
        {
            return HexUtil.GetHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string ToHexString(this Memory<byte> bytes, bool hexPrefix = false)
        {
            return HexUtil.GetHexFromBytes(bytes.Span, hexPrefix: hexPrefix);
        }

        public static string ToHexString(this Span<byte> bytes, bool hexPrefix = false)
        {
            return HexUtil.GetHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string ToHexString(this byte[] bytes, bool hexPrefix = false)
        {
            return HexUtil.GetHexFromBytes(bytes, hexPrefix: hexPrefix);
        }

        public static string ToHexString(this IEnumerable<byte> bytes, bool hexPrefix = false)
        {
            return HexUtil.GetHexFromBytes(bytes.ToArray(), hexPrefix: hexPrefix);
        }
    }
}
