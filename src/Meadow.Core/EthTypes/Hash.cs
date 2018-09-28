using Newtonsoft.Json;
using Meadow.Core.Utils;
using System;
using System.Runtime.InteropServices;

namespace Meadow.Core.EthTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Hash : IEquatable<Hash>
    {
        public const int SIZE = 32;

        public static readonly Hash Zero = default;

        // parts are big-endian
        readonly ulong _p1;
        readonly ulong _p2;
        readonly ulong _p3;
        readonly ulong _p4;

        public Span<byte> GetSpan() => MemoryMarshal.AsBytes(new Span<ulong>(new[] { _p1, _p2, _p3, _p4 }));
        public byte[] GetBytes() => GetSpan().ToArray();
        public string GetHexString(bool hexPrefix = true) => HexConverter.GetHex<Hash>(this, hexPrefix: hexPrefix);

        public Hash(string hexString)
        {
            if (hexString.Length == (SIZE * 2) + 2)
            {
                if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    hexString = hexString.Substring(2);
                }
            }

            if (hexString.Length != SIZE * 2)
            {
                throw new ArgumentException($"Hash hex string should be {SIZE * 2} chars long, or {(SIZE * 2) + 2} with a 0x prefix, was given " + hexString.Length, nameof(hexString));
            }

            Span<byte> bytes = HexUtil.HexToBytes(hexString);
            var uintView = MemoryMarshal.Cast<byte, ulong>(bytes);
            _p1 = uintView[0];
            _p2 = uintView[1];
            _p3 = uintView[2];
            _p4 = uintView[3];
        }

        public Hash(byte[] bytes)
        {
            if (bytes.Length != SIZE)
            {
                throw new ArgumentException("Byte arrays for hash should be 32 bytes long, was given " + bytes.Length, nameof(bytes));
            }

            var uintView = MemoryMarshal.Cast<byte, ulong>(bytes);
            _p1 = uintView[0];
            _p2 = uintView[1];
            _p3 = uintView[2];
            _p4 = uintView[3];
        }

        public string ToString(bool hexPrefix = true) => GetHexString(hexPrefix);
        public override string ToString() => GetHexString();

        public override int GetHashCode() => (_p1, _p2, _p3, _p4).GetHashCode();

        public override bool Equals(object obj) => obj is Hash addr ? Equals(addr) : false;

        public bool Equals(Hash other)
        {
            return other._p1 == _p1 && other._p2 == _p2 && other._p3 == _p3 && other._p4 == _p4;
        }

        public static bool operator ==(Hash a, Hash b) => a.Equals(b);
        public static bool operator !=(Hash a, Hash b) => !a.Equals(b);

        public static explicit operator Hash(byte[] value) => new Hash(value);
        public static implicit operator Hash(string value) => HexConverter.HexToValue<Hash>(value);
        public static implicit operator string(Hash value) => value.GetHexString();
    }



}
