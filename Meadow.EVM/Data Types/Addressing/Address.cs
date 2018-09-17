using Meadow.Core.Cryptography;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.EVM.Data_Types.Addressing
{
    public class Address
    {
        #region Constants
        public const int ADDRESS_SIZE = 20;
        #endregion

        #region Fields
        private static BigInteger? _address_max;
        private BigInteger _internalAddress;
        #endregion

        #region Properties
        /// <summary>
        /// Represents the zero address.
        /// </summary>
        public static Address ZERO_ADDRESS
        {
            get
            {
                return (BigInteger)(0);
            }
        }

        /// <summary>
        /// Represents the create contract address.
        /// </summary>
        public static Address CREATE_CONTRACT_ADDRESS
        {
            get
            {
                return (BigInteger)(0);
            }
        }

        /// <summary>
        /// Represents a null address.
        /// </summary>
        public static Address NULL_ADDRESS
        {
            get
            {
                return (BigInteger)(-1);
            }
        }

        /// <summary>
        /// Represents the maximum value an address can have.
        /// </summary>
        public static BigInteger ADDRESS_MAX
        {
            get
            {
                if (_address_max == null)
                {
                    _address_max = 1;
                    _address_max <<= 160;
                    _address_max -= 1;
                }

                return (BigInteger)_address_max;
            }
        }
        #endregion

        #region Constructor
        public Address(byte[] address)
        {
            SetAddress(BigIntegerConverter.GetBigInteger(address));
        }

        public Address(Span<byte> address)
        {
            SetAddress(BigIntegerConverter.GetBigInteger(address));
        }

        public Address(string address)
        {
            SetAddress(BigIntegerConverter.GetBigInteger(address.HexToBytes()));
        }

        public Address(BigInteger address)
        {
            // Set our address accordingly.
            SetAddress(address);
        }
        #endregion

        #region Functions
        private void SetAddress(BigInteger address)
        {
            // If our address is above the maximum, we'll want to remove those bits.
            if (address > ADDRESS_MAX)
            {
                address %= (ADDRESS_MAX + 1);
            }

            // Set the address
            _internalAddress = address;
        }

        public BigInteger ToBigInteger()
        {
            return _internalAddress;
        }

        public byte[] ToByteArray()
        {
            // Obtain the amount of bytes that constitute an address from the least significant bit side.
            return BigIntegerConverter.GetBytes(_internalAddress, ADDRESS_SIZE);
        }

        public override string ToString()
        {
            return "0x" + ToByteArray().ToHexString(false);
        }

        public static Address MakeContractAddress(Address sender, BigInteger nonce)
        {
            // Create an RLP list with the address and nonce
            RLPList list = new RLPList(RLP.FromInteger(sender, ADDRESS_SIZE), RLP.FromInteger(nonce));
            var hash = KeccakHash.ComputeHash(RLP.Encode(list)).Slice(EVMDefinitions.WORD_SIZE - ADDRESS_SIZE);
            return new Address(hash);
        }

        #endregion

        #region Operators
        public static bool operator <(Address emp1, Address emp2)
        {
            return emp1.ToBigInteger() < emp2.ToBigInteger();
        }

        public static bool operator >(Address emp1, Address emp2)
        {
            return emp1.ToBigInteger() > emp2.ToBigInteger();
        }

        public static bool operator ==(Address emp1, Address emp2)
        {
            if (object.ReferenceEquals(emp1, null) != object.ReferenceEquals(emp2, null))
            {
                return false;
            }

            if (object.ReferenceEquals(emp1, null) && object.ReferenceEquals(emp2, null))
            {
                return true;
            }

            return emp1.ToBigInteger() == emp2.ToBigInteger();
        }

        public static bool operator !=(Address emp1, Address emp2)
        {
            if (object.ReferenceEquals(emp1, null) != object.ReferenceEquals(emp2, null))
            {
                return true;
            }

            if (object.ReferenceEquals(emp1, null) && object.ReferenceEquals(emp2, null))
            {
                return false;
            }

            return emp1.ToBigInteger() != emp2.ToBigInteger();
        }

        public static bool operator <=(Address emp1, Address emp2)
        {
            return emp1.ToBigInteger() <= emp2.ToBigInteger();
        }

        public static bool operator >=(Address emp1, Address emp2)
        {
            return emp1.ToBigInteger() >= emp2.ToBigInteger();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Address))
            {
                return false;
            }

            return this == (Address)obj;

        }

        public override int GetHashCode()
        {
            return _internalAddress.GetHashCode();
        }

        public static implicit operator Address(BigInteger address)
        {
            return new Address(address);
        }

        public static implicit operator Address(string address)
        {
            return new Address(address);
        }

        public static implicit operator BigInteger(Address address)
        {
            return address.ToBigInteger();
        }
        #endregion
    }
}
