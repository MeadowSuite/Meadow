using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static class DeconstructExtensions
    {
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> p, 
            out TKey key,
            out TValue value)
        {
            key = p.Key;
            value = p.Value;
        }

    }

    public static class ListExtensions
    {
        public static void AddRange<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }
    }

    public static class SpanExtensions
    {
        public static Span<byte> AsByteSpan<TFrom>(this TFrom[] arr) where TFrom :
#if LANG_7_3
            unmanaged
#else
            struct
#endif
        {
            return MemoryMarshal.Cast<TFrom, byte>(arr.AsSpan());
        }
    }
}
