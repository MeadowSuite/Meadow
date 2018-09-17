using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using SolcNet.DataDescription.Output;
using System.Linq;
using Meadow.Core.Cryptography;

namespace Meadow.Core.AbiEncoding
{
    /// <summary>
    /// The first four bytes of the call data for a function call specifies the function to be called. 
    /// It is the first (left, high-order in big-endian) four bytes of the Keccak (SHA-3) hash of the 
    /// signature of the function. The signature is defined as the canonical expression of the basic 
    /// prototype, i.e. the function name with the parenthesised list of parameter types. Parameter 
    /// types are split by a single comma - no spaces are used.
    /// <see href="https://solidity.readthedocs.io/en/v0.4.23/abi-spec.html#function-selector"/>
    /// </summary>
    public static class AbiSignature
    {

        static readonly Encoding UTF8 = new UTF8Encoding(false);

        /// <summary>
        /// Creates the 4 byte function selector from a function signature string
        /// </summary>
        /// <param name="functionSignature">Function signature, ex: "baz(uint32,bool)"</param>
        /// <param name="hexPrefix">True to prepend the hex string with "0x"</param>
        /// <returns>8 character lowercase hex string (from first 4 bytes of the sha3 hash of utf8 encoded function signature)</returns>
        public static string GetMethodIDHex(string functionSignature, bool hexPrefix = false)
        {
            var bytes = UTF8.GetBytes(functionSignature);
            var hash = KeccakHash.ComputeHash(bytes).Slice(0, 4);
            string funcSignature = HexUtil.GetHexFromBytes(hash, hexPrefix: hexPrefix);
            return funcSignature;
        }

        public static ReadOnlyMemory<byte> GetMethodID(string functionSignature)
        {
            var bytes = UTF8.GetBytes(functionSignature);
            var mem = new Memory<byte>(new byte[4]);
            KeccakHash.ComputeHash(bytes).Slice(0, 4).CopyTo(mem.Span);
            return mem;
        }

        public static void GetMethodID(Span<byte> buffer, string functionSignature)
        {
            var bytes = UTF8.GetBytes(functionSignature);
            KeccakHash.ComputeHash(bytes).Slice(0, 4).CopyTo(buffer);
        }

        public static string GetFullSignature(this Abi abiItem)
        {
            string typesString;
            if (abiItem.Inputs == null || abiItem.Inputs.Length == 0)
            {
                typesString = string.Empty;
            }
            else
            {
                var types = new string[abiItem.Inputs.Length];
                for (var i = 0; i < types.Length; i++)
                {
                    types[i] = abiItem.Inputs[i].Type;
                }

                typesString = string.Join(",", types);
            }

            return $"{abiItem.Name}({typesString})";
        }

        public static string GetSignatureHash(this Abi abi, bool hexPrefix = false)
        {
            string str = GetFullSignature(abi);
            var bytes = UTF8.GetBytes(str);
            var hash = KeccakHash.ComputeHash(bytes).ToHexString(hexPrefix);
            return hash;
        }
    }
}
