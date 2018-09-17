using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public static class BouncyCastleExtensions
{
    #region Functions
    public static Org.BouncyCastle.Math.BigInteger ToBouncyCastleBigInteger(this BigInteger bigInteger)
    {
        return new Org.BouncyCastle.Math.BigInteger(1, BigIntegerConverter.GetBytes(bigInteger));
    }

    public static BigInteger ToNumericsBigInteger(this Org.BouncyCastle.Math.BigInteger bcBigInteger)
    {
        return BigIntegerConverter.GetBigInteger(bcBigInteger.ToByteArrayUnsigned());
    }
    #endregion
}