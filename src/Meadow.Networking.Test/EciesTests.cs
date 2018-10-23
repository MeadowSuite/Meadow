using Meadow.Core.Cryptography.Ecdsa;
using System;
using System.Collections.Generic;
using System.Text;
using Meadow.Core.AccountDerivation;
using Xunit;

namespace Meadow.Networking.Test
{
    public class EciesTests
    {
        [Fact]
        public void EncryptTest()
        {
            Ecies.Encrypt(EthereumEcdsa.Generate(), new byte[ushort.MaxValue], null);
        }
    }
}
