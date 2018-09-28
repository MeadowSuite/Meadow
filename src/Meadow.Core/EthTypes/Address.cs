using Newtonsoft.Json;
using SolcNet;
using Meadow.Core.Utils;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Meadow.Core.Cryptography;

namespace Meadow.Core.EthTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Address : IEquatable<Address>
    {
        public const int SIZE = 20;

        public static readonly Address Zero = default;

        // parts are big-endian
        readonly uint _p1;
        readonly uint _p2;
        readonly uint _p3;
        readonly uint _p4;
        readonly uint _p5;

        public Span<byte> GetSpan() => MemoryMarshal.AsBytes(new Span<Address>(new[] { this }));


        public byte[] GetBytes() => GetSpan().ToArray();
        public string GetHexString(bool hexPrefix = true) => HexConverter.GetHex<Address>(this, hexPrefix: hexPrefix);

        public Address(string hexString)
        {
            if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }

            // special handling for default/empty address 0x0
            bool allZeros = true;
            for (var i = 0; i < hexString.Length; i++)
            {
                if (hexString[i] != '0')
                {
                    allZeros = false;
                    break;
                }
            }

            if (allZeros)
            {
                _p1 = 0;
                _p2 = 0;
                _p3 = 0;
                _p4 = 0;
                _p5 = 0;
                return;
            }

            if (hexString.Length != 40)
            {
                throw new ArgumentException("Address hex string should be 40 chars long, or 42 with a 0x prefix, was given " + hexString.Length, nameof(hexString));
            }

            Span<byte> bytes = HexUtil.HexToBytes(hexString);
            if (!ValidChecksum(hexString))
            {
                throw new ArgumentException("Address does not pass mixed-case checksum validation, https://github.com/ethereum/EIPs/blob/master/EIPS/eip-55.md");
            }

            var uintView = MemoryMarshal.Cast<byte, uint>(bytes);
            _p1 = uintView[0];
            _p2 = uintView[1];
            _p3 = uintView[2];
            _p4 = uintView[3];
            _p5 = uintView[4];
        }

        public Address(Span<byte> bytes)
        {
            if (bytes.Length != SIZE)
            {
                throw new ArgumentException("Byte arrays for addresses should be 20 bytes long, was given " + bytes.Length, nameof(bytes));
            }

            var uintView = MemoryMarshal.Cast<byte, uint>(bytes);
            _p1 = uintView[0];
            _p2 = uintView[1];
            _p3 = uintView[2];
            _p4 = uintView[3];
            _p5 = uintView[4];
        }

        public string ToString(bool hexPrefix = true) => GetHexString(hexPrefix);
        public override string ToString() => GetHexString();

        public override int GetHashCode() => (_p1, _p2, _p3, _p4, _p5).GetHashCode();

        public override bool Equals(object obj) => obj is Address addr ? Equals(addr) : false;

        public bool Equals(Address other)
        {
            return other._p1 == _p1 && other._p2 == _p2 && other._p3 == _p3 && other._p4 == _p4 && other._p5 == _p5;
        }

        public static bool operator ==(Address a, Address b) => a.Equals(b);
        public static bool operator !=(Address a, Address b) => !a.Equals(b);

        public static explicit operator Address(byte[] value) => new Address(value);
        public static implicit operator Address(string value) => new Address(value);
        public static implicit operator string(Address value) => value.GetHexString();

        /// <summary>
        /// https://github.com/ethereum/EIPs/blob/master/EIPS/eip-55.md
        /// </summary>
        public string ToStringWithChecksum()
        {
            Span<uint> buffer = stackalloc uint[10];
            buffer[0] = _p1;
            buffer[1] = _p2;
            buffer[2] = _p3;
            buffer[3] = _p4;
            buffer[4] = _p5;

            Span<byte> addrBytes = MemoryMarshal.AsBytes(buffer);
            Span<char> addrHexBytesPrefixed = stackalloc char[42];
            addrHexBytesPrefixed[0] = '0';
            addrHexBytesPrefixed[1] = 'x';

            Span<char> addrHexChars = addrHexBytesPrefixed.Slice(2);
            HexUtil.WriteBytesIntoHexString(addrBytes.Slice(0, 20), addrHexChars);

            for (var i = 0; i < 40; i++)
            {
                addrBytes[i] = (byte)addrHexChars[i];
            }

            KeccakHash.ComputeHash(addrBytes.Slice(0, 40), addrBytes.Slice(0, 32));

            for (var i = 0; i < 40; i++)
            {
                char inspectChar = addrHexChars[i];

                // skip check if character is a number
                if (inspectChar > 64)
                {
                    // get character casing flag
                    var c = i % 2 == 0 ? addrBytes[i / 2] >> 4 : addrBytes[i / 2] & 0x0F;
                    bool upperFlag = c >= 8;
                    if (upperFlag)
                    {
                        addrHexChars[i] = (char)(inspectChar - 32);
                    }
                }
            }

            return addrHexBytesPrefixed.ToString();
        }

        /// <summary>
        /// https://github.com/ethereum/EIPs/blob/master/EIPS/eip-55.md
        /// </summary>
        public static bool ValidChecksum(string addressHexStr)
        {
            bool foundUpper = false, foundLower = false;

            foreach (var c in addressHexStr)
            {
                foundUpper |= c > 64 && c < 71;
                foundLower |= c > 96 && c < 103;
                if (foundUpper && foundLower)
                {
                    break;
                }
            }

            if (!(foundUpper && foundLower))
            {
                return true;
            }

            // get lowercase utf16 buffer
            Span<byte> addr = stackalloc byte[80];

            var addrSpan = addressHexStr.AsSpan();
            if (addrSpan[0] == '0' && addrSpan[1] == 'x')
            {
                addrSpan = addrSpan.Slice(2);
            }

            if (addrSpan.Length != 40)
            {
                throw new ArgumentException("Address hex string should be 40 chars long, or 42 with a 0x prefix, was given " + addressHexStr.Length, nameof(addressHexStr));
            }

            addrSpan.ToLowerInvariant(MemoryMarshal.Cast<byte, char>(addr));

            // inline buffer conversion from utf16 to ascii
            for (var i = 0; i < 40; i++)
            {
                addr[i] = addr[i * 2];
            }

            // get hash of ascii hex
            KeccakHash.ComputeHash(addr.Slice(0, 40), addr.Slice(0, 32));

            for (var i = 0; i < 40; i++)
            {
                char inspectChar = addrSpan[i];

                // skip check if character is a number
                if (inspectChar > 64)
                {
                    // get character casing flag
                    var c = i % 2 == 0 ? addr[i / 2] >> 4 : addr[i / 2] & 0x0F;
                    bool upperFlag = c >= 8;

                    // verify character is uppercase, otherwise bad checksum
                    if (upperFlag && inspectChar > 96)
                    {
                        return false;
                    }

                    // verify character is lowercase
                    else if (!upperFlag && inspectChar < 97)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

    }
    


}
