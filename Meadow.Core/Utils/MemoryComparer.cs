using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Meadow.Core.Utils
{
    public class MemoryComparer<T> : IEqualityComparer<Memory<T>> where T :
#if LANG_7_3
        unmanaged, 
#else
        struct,
#endif
        IEquatable<T>
    {
        public bool Equals(Memory<T> x, Memory<T> y)
        {
            return MemoryExtensions.SequenceEqual(x.Span, y.Span);
        }

        public int GetHashCode(Memory<T> obj)
        {
            var byteSpan = obj is Memory<byte> m ? m.Span : MemoryMarshal.AsBytes(obj.Span);

            // Most key entries are 32 bytes, so use inline variables and generic hashcode combine method as
            // fastest path.
            if (byteSpan.Length == 32)
            {
                var span = MemoryMarshal.Cast<T, ulong>(obj.Span);
                return (span[0], span[1], span[2], span[3]).GetHashCode();
            }

            // 4 bytes sizes are also common, so optimize for these. 
            else if (byteSpan.Length == 4)
            {
                return MemoryMarshal.Cast<T, int>(obj.Span)[0].GetHashCode();
            }

            // Otherwise iterate through the array 4 bytes at a time.
            else
            {
                var hashCode = default(HashCode);

                var span = MemoryMarshal.Cast<T, uint>(obj.Span);
                for (var i = 0; i < span.Length; i++)
                {
                    hashCode.Add(span[i]);
                }

                // Hash in any remainder bytes (only ever up to 3 bytes)
                var remLen = byteSpan.Length % sizeof(uint);
                if (remLen > 0)
                {
                    var remainder = byteSpan.Slice(byteSpan.Length - remLen);
                    for (var i = 0; i < remainder.Length; i++)
                    {
                        hashCode.Add(remainder[i]);
                    }
                }

                return hashCode.ToHashCode();
            }
            
        }

    }
}
