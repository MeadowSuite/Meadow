using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Meadow.Core.Utils;
using Xunit;

namespace Meadow.EVM.Test
{
    public class RIPEMD160
    {
        void DoHash(string expectedHex, string textInput)
        {
            var hashAlgo = new RIPEMD160Managed();
            var res = hashAlgo.ComputeHash(Encoding.ASCII.GetBytes(textInput));
            Assert.Equal(expectedHex, res.ToHexString(), ignoreCase: true);
        }

        [Fact]
        public void Test()
        {
            DoHash("9c1185a5c5e9fc54612808977ee8f548b2258d31", "");
            DoHash("0bdc9d2d256b3ee9daae347be6f4dc835a467ffe", "a");
            DoHash("8eb208f7e05d987a9b044a8e98c6b087f15a0bfc", "abc");
            DoHash("5d0689ef49d2fae572b881b123a85ffa21595f36", "message digest");
            DoHash("f71c27109c692c1b56bbdceb5b9d2865b3708dbc", "abcdefghijklmnopqrstuvwxyz");
            DoHash("12a053384a9c0c88e405a06c27dcf49ada62eb2b", "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq");
            DoHash("b0e20b6e3116640286ed3a87a5713079b21f5189", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        }



    }
}
