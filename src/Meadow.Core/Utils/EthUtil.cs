using Meadow.Core.EthTypes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class EthUtil
    {
        public static readonly UInt256 ONE_ETHER_IN_WEI = (UInt256)BigInteger.Pow(10, 18);
    }
}
