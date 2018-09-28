using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class BaseEncoding
    {
        public static class CHAR_SETS
        {
            public const string BASE36 = "0123456789abcdefghijklmnopqrstuvwxyz";
        }

        public static string ToBaseString(this byte[] bytes, string charSet, char paddingChar = '0')
        {
            BigInteger dividend = new BigInteger(bytes);
            BigInteger bigInt = new BigInteger(charSet.Length);

            int resultLength = (int)Math.Ceiling(bytes.Length * 8 / Math.Log(charSet.Length, 2));
            char[] chars = new char[resultLength];

            int index = 0;
            while (!dividend.IsZero)
            {
                dividend = BigInteger.DivRem(dividend, bigInt, out var remainder);
                int digitIndex = Math.Abs((int)remainder);
                chars[index] = charSet[digitIndex];
                index++;
            }

            if (index < chars.Length)
            {
                chars[index] = paddingChar;
            }

            return new string(chars);
        }

    }
}
