using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.EVM.Data_Types.Trees.Comparer
{
    public class ArrayComparer<T> : EqualityComparer<T>, IEqualityComparer<T>
    {
        public override bool Equals(T left, T right)
        {
            // Cast our values into arrays.
            Array leftArray = left as Array;
            Array rightArray = right as Array;

            // If the objects are the same, return true.
            if (left.Equals(right))
            {
                return true;
            }

            // If the lengths are not equal.
            if (leftArray.Length != rightArray.Length)
            {
                return false;
            }

            // Verify each underlying subelement matches.
            for (int i = 0; i < leftArray.Length; i++)
            {
                if (!leftArray.GetValue(i).Equals(rightArray.GetValue(i)))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode(T obj)
        {
            // Not the best hash algorithm, but we expect certain types (ex: byte[]) will have collisions anyways
            // since hashcodes are only 32-bit integers yet the data can exceed 32-bits. Dictionaries use Equals()
            // as a fallback in case of collisions, so we don't need to worry about this hash algorithm being too poor.
            // This just needed to be overrode to not use reference comparison/hashing. Although, with larger databases,
            // and many operations, more unique hash codes will be desirable to avoid compares later. A simple xor cipher should do.
            Array objArray = obj as Array;
            int hash = objArray.Length;
            for (int i = 0; i < objArray.Length; i++)
            {
                int shift = ((i % 4) * 8);
                hash = hash + (i << shift);
                hash = hash ^ (objArray.GetValue(i).GetHashCode() << shift);
            }

            return hash;
        }
    }
}
