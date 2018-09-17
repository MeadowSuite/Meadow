using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class RLPTests
    {
        [Fact]
        public void RLPBasicStringList()
        {
            // Source of test data: http://hidskes.com/blog/2014/04/02/ethereum-building-blocks-part-1-rlp/
            byte[] expected = new byte[] { 200, 131, 99, 97, 116, 131, 100, 111, 103 };

            byte[] result = RLP.Encode(new RLPList(new List<RLPItem>()
            {
                new RLPByteArray(System.Text.ASCIIEncoding.ASCII.GetBytes("cat")),
                new RLPByteArray(System.Text.ASCIIEncoding.ASCII.GetBytes("dog"))
            }));

            Assert.True(expected.ValuesEqual(result));
        }

        [Fact]
        public void RLPLongerStringList()
        {
            // Source of test data: http://hidskes.com/blog/2014/04/02/ethereum-building-blocks-part-1-rlp/
            byte[] expected = new byte[] 
            {
                248, 144, 152, 116, 104, 105, 115, 32, 105, 115, 32, 97, 32, 118,
                101, 114, 121, 32, 108, 111, 110, 103, 32, 108, 105, 115, 116, 158,
                121, 111, 117, 32, 110, 101, 118, 101, 114, 32, 103, 117, 101, 115,
                115, 32, 104, 111, 119, 32, 108, 111, 110, 103, 32, 105, 116, 32, 105,
                115, 169, 105, 110, 100, 101, 101, 100, 44, 32, 104, 111, 119, 32, 100,
                105, 100, 32, 121, 111, 117, 32, 107, 110, 111, 119, 32, 105, 116, 32,
                119, 97, 115, 32, 116, 104, 105, 115, 32, 108, 111, 110, 103, 173, 103,
                111, 111, 100, 32, 106, 111, 98, 44, 32, 116, 104, 97, 116, 32, 73, 32,
                99, 97, 110, 32, 116, 101, 108, 108, 32, 121, 111, 117, 32, 105, 110,
                32, 104, 111, 110, 101, 115, 116, 108, 121, 121, 121, 121, 121
            };

            byte[] result = RLP.Encode(new RLPList(
                "this is a very long list",
                "you never guess how long it is",
                "indeed, how did you know it was this long",
                "good job, that I can tell you in honestlyyyyy"));

            Assert.True(expected.ValuesEqual(result));
        }

        private void RLPStringEncodeDecodeTest(string s)
        {
            RLPItem s1 = s;
            byte[] encoded = RLP.Encode(s);
            RLPItem s2 = RLP.Decode(encoded);
            string ss1 = s1;
            string ss2 = s2;
            if (s.Length == 0)
            {
                Assert.Null((string)s2);
            }
            else
            {
                Assert.Equal((string)s1, (string)s2);
            }
        }

        [Fact]
        public void RLPEncodeDecodeLongString()
        {
            RLPStringEncodeDecodeTest("ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|ABCDEFGHIJKLMNOPQRSTUVWXYZ|");
            RLPStringEncodeDecodeTest("");
            RLPStringEncodeDecodeTest("a");
            RLPStringEncodeDecodeTest("abcd");
            RLPStringEncodeDecodeTest("123456789012345678901234567890123456789012345678901234"); // 54 characters
            RLPStringEncodeDecodeTest("1234567890123456789012345678901234567890123456789012345"); // 55 characters, right at the limit when the format changes
            RLPStringEncodeDecodeTest("12345678901234567890123456789012345678901234567890123456"); // 56 characters
        }

        private void RLPListEncodeDecodeTest(int items)
        {
            RLPList list = new RLPList();
            for (int i = 0; i < items; i++)
            {
                if (i % 2 == 0)
                {
                    list.Items.Add(new byte[] { (byte)(i % 256) });
                }
                else
                {
                    list.Items.Add(new RLPList());
                }
            }

            byte[] encoded = RLP.Encode(list);
            RLPList list2 = (RLPList)RLP.Decode(encoded);

            Assert.Equal(list.Items.Count, list2.Items.Count);

            for (int i = 0; i < list.Items.Count; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.IsType<RLPByteArray>(list2.Items[i]);
                    RLPByteArray b1 = ((RLPByteArray)list.Items[i]);
                    RLPByteArray b2 = ((RLPByteArray)list2.Items[i]);
                    Assert.True(MemoryExtensions.SequenceEqual(b1.Data.Span, b2.Data.Span));
                }
                else
                {
                    Assert.IsType<RLPList>(list2.Items[i]);
                }
            }
        }

        [Fact]
        public void RLPTestLists()
        {
            RLPListEncodeDecodeTest(0);
            RLPListEncodeDecodeTest(1);
            RLPListEncodeDecodeTest(54);
            RLPListEncodeDecodeTest(55); // 55 entries right at the limit when the format changes
            RLPListEncodeDecodeTest(56);
            RLPListEncodeDecodeTest(1000);
        }

        [Fact]
        public void ImplicitEncoding_NegativeInt()
        {
            Assert.ThrowsAny<Exception>(() => RLP.Encode(-1234));
        }

        [Fact]
        public void ImplicitEncoding()
        {

#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable SA1115 // Parameter should follow comma
#pragma warning disable CA1825 // Avoid zero-length array allocations.
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
            var item = RLP.Encode(

                // string
                "First item",

                // byte array
                "0x7cafb9c3de0fd02142754ce648ba7db04e4c161e".HexToBytes(),

                // single byte
                (byte)0xf5,

                // integer
                2147483648,

                // empty array
                new RLPItem[] { },

                // array of mixed types
                new RLPItem[] 
                {
                    "hello world",
                    123456
                }
            );

#pragma warning restore SA1114 // Parameter list should follow declaration
#pragma warning restore SA1115 // Parameter should follow comma
#pragma warning restore CA1825 // Avoid zero-length array allocations.
#pragma warning restore SA1118 // Parameter should not span multiple lines
#pragma warning restore SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly

            var result = item.ToHexString(hexPrefix: true);

            var expected = "0xf8398a4669727374206974656d947cafb9c3de0fd02142754ce648ba7db04e4c161e81f58480000000c0d08b68656c6c6f20776f726c648301e240";
            Assert.Equal(expected, result);
        }
    }
}
