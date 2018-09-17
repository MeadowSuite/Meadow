using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// Disabled, currently unused

/*
namespace Meadow.Core.EthTypes
{
    public interface IFixedN
    {
        int Decimals { get; }
    }

    /// <summary>
    /// Where M represents the number of bits taken by the type
    /// and N represents how many decimal points are available.
    /// M must be divisible by 8 and goes from 8 to 256 bits. 
    /// N must be between 0 and 80, inclusive. ufixed and fixed 
    /// are aliases for ufixed128x19 and fixed128x19, respectively.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Fixed<M, N> where M : struct where N : struct, IFixedN
    {
        readonly M Value;

        public int Decimals => default(N).Decimals;


    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UFixed<M, N> where M : struct where N : struct, IFixedN
    {
        readonly M Value;

        public int Decimals => default(N).Decimals;


    }
}
*/