using Meadow.Core.EthTypes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class AddressTests
    {
        [Fact]
        public void ZeroAddress()
        {
            Address addr1 = "0x0";
            Address addr2 = "0";

            Assert.Equal(Address.Zero, addr1);
            Assert.Equal(Address.Zero, addr2);
        }

        [Fact]
        public void AddressToString()
        {
            var str = "0xb60e8dd61c5d32be8058bb8eb970870f07233155";
            Address addr = str;
            Assert.Equal(str, addr.ToString());
            Assert.True(Address.ValidChecksum(str));
        }

        [Fact]
        public void Checksum()
        {
            var str = "0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            Address addr = str;
            Assert.True(Address.ValidChecksum(str));
        }

        [Fact]
        public void InvalidChecksum()
        {
            var str = "0xfb6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            Address addr;
            Assert.Throws<ArgumentException>(() => addr = str);
            Assert.False(Address.ValidChecksum(str));
        }

        [Fact]
        public void ToChecksum()
        {
            var str = "0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            Address addr = str;
            var result = addr.ToStringWithChecksum();
            Assert.Equal(str, result);
        }
    }
}
