using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class HexEncodingTests
    {
        [Fact]
        public void Decode()
        {
            var hexString = "d52828f9194553d3c96ca255140ed7b223b4edbe686d5be594fcc984f3e2e2d1fc5779027451e033e6264db1708fdc12ad7629d902372c68cd43651d7bcee0e689d158bc94d53ed1d3bad84120a9740420b7d77cb908cec42b113530ecc3b7174666279e";
            var decoded = HexUtil.HexToBytes(hexString);
            var recoded = HexUtil.GetHexFromBytes(decoded);
            Assert.Equal(hexString, recoded);

            var bytes = new byte[100];
            new Random().NextBytes(bytes);
            var encoded = HexUtil.GetHexFromBytes(bytes);
        }

        [Fact]
        public void Decode_0xPrefix()
        {
            var hexString = "0xd52828f9194553d3c96ca255140ed7b223b4edbe686d5be594fcc984f3e2e2d1fc5779027451e033e6264db1708fdc12ad7629d902372c68cd43651d7bcee0e689d158bc94d53ed1d3bad84120a9740420b7d77cb908cec42b113530ecc3b7174666279e";
            var decoded = HexUtil.HexToBytes(hexString);
            var recoded = HexUtil.GetHexFromBytes(decoded, hexPrefix: true);
            Assert.Equal(hexString, recoded);
        }

        [Fact]
        public void Decode_Uppercase()
        {
            var hexString = "D52828F9194553D3C96CA255140ED7B223B4EDBE686D5BE594FCC984F3E2E2D1FC5779027451E033E6264DB1708FDC12AD7629D902372C68CD43651D7BCEE0E689D158BC94D53ED1D3BAD84120A9740420B7D77CB908CEC42B113530ECC3B7174666279E";
            var decoded = HexUtil.HexToBytes(hexString);
            var recoded = HexUtil.GetHexFromBytes(decoded);
            Assert.Equal(hexString.ToLowerInvariant(), recoded);
        }

        [Fact]
        public void Extension_String()
        {
            var hexString = "d52828f9194553d3c96ca255140ed7b223b4edbe686d5be594fcc984f3e2e2d1fc5779027451e033e6264db1708fdc12ad7629d902372c68cd43651d7bcee0e689d158bc94d53ed1d3bad84120a9740420b7d77cb908cec42b113530ecc3b7174666279e";
            var decoded = hexString.HexToBytes();
            var recoded = HexUtil.GetHexFromBytes(decoded);
            Assert.Equal(hexString, recoded);
        }

        [Fact]
        public void Extension_Bytes()
        {
            var hexString = "d52828f9194553d3c96ca255140ed7b223b4edbe686d5be594fcc984f3e2e2d1fc5779027451e033e6264db1708fdc12ad7629d902372c68cd43651d7bcee0e689d158bc94d53ed1d3bad84120a9740420b7d77cb908cec42b113530ecc3b7174666279e";
            var bytes = HexUtil.HexToBytes(hexString);
            var encoded = bytes.ToHexString();
            Assert.Equal(hexString, encoded);
        }
    }
}
