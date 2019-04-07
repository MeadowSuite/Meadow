using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SolcNet
{
    public static class EncodingUtils
    {
        readonly static Encoding UTF8_ENCODING = new UTF8Encoding(false, false);

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

    }
}
