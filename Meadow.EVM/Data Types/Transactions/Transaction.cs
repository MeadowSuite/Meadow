using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.RlpEncoding;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.EVM.Definitions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Transactions;

namespace Meadow.EVM.Data_Types.Transactions
{
    public class Transaction : IRLPSerializable
    {
        #region Fields
        private (byte cache_v, BigInteger cache_r, BigInteger cache_s, byte[] hash, Address sender) _cachedSender;
        #endregion

        #region Properties
        /// <summary>
        /// Used to prevent replay attacks and etc.
        /// </summary>
        public BigInteger Nonce { get; private set; }
        /// <summary>
        /// Indicates the price of gas at the time of this transaction.
        /// </summary>
        public BigInteger GasPrice { get; private set; }
        /// <summary>
        /// The amount of gas provided to be charged for the transaction, where any remainder should be refunded if all succeeds.
        /// </summary>
        public BigInteger StartGas { get; private set; }
        /// <summary>
        /// The address to receive this transaction and process it (could cause code to execute at that address).
        /// </summary>
        public Address To { get; private set; } 
        /// <summary>
        /// The amount of ether to send to the receiving address.
        /// </summary>
        public BigInteger Value { get; private set; }
        /// <summary>
        /// Call specific data
        /// </summary>
        public byte[] Data { get; private set; }

        // The components below are ECDSA components that let us get the public key.
        public byte ECDSA_v { get; private set; }
        public BigInteger ECDSA_r { get; private set; }
        public BigInteger ECDSA_s { get; private set; }

        /// <summary>
        /// An optional ChainID which is embedded in our ECDSA v parameter.
        /// </summary>
        public EthereumChainID? ChainID
        {
            get
            {
                // Chain ID is embedded in v.

                // If it's a 27 or 28, it's the old protocol, it has no embedded chain ID.
                if (ECDSA_v == 27 || ECDSA_v == 28)
                {
                    return null;
                }

                // If r = 0 and s = 0, this is the null address and we treat v as the chain outright.
                else if (ECDSA_r == 0 & ECDSA_s == 0)
                {
                    return (EthereumChainID)ECDSA_v;
                }

                // Otherwise we have v = (ChainID * 2) + RecoveryID + 35. (We can presume recovery ID is 0, 1)
                else
                {
                    return (EthereumChainID)((ECDSA_v - 35 - EthereumEcdsa.GetRecoveryIDFromV(ECDSA_v)) / 2);
                }
            }
        }

        /// <summary>
        /// The amount of gas we need to pay at the beginning of transaction application.
        /// </summary>
        public BigInteger BaseGasCost
        {
            get
            {
                // We count the zero bytes we have in our data
                BigInteger zeroBytes = 0;
                if (Data != null)
                {
                    for (int i = 0; i < Data.Length; i++)
                    {
                        if (Data[i] == 0)
                        {
                            zeroBytes++;
                        }
                    }
                }

                // And calculate the non zero byte count in our data.
                BigInteger nonZeroBytes = Data.Length - zeroBytes;

                // Based off the amount of zero and non zero bytes, we can return an intrinsic gas used.
                return GasDefinitions.GAS_TRANSACTION + (GasDefinitions.GAS_TRANSACTION_DATA_ZERO * zeroBytes) + (GasDefinitions.GAS_TRANSACTION_DATA_NON_ZERO * nonZeroBytes);
            }
        }
        #endregion

        #region Constructor
        public Transaction() { }
        public Transaction(BigInteger nonce, BigInteger gasPrice, BigInteger startGas, Address to, BigInteger value, byte[] data)
        {
            Nonce = nonce;
            GasPrice = gasPrice;
            StartGas = startGas;
            To = to;
            Value = value;
            Data = data;
        }

        public Transaction(BigInteger nonce, BigInteger gasPrice, BigInteger startGas, Address to, BigInteger value, byte[] data, byte ecdsa_v, BigInteger ecdsa_r, BigInteger ecdsa_s) : this(nonce, gasPrice, startGas, to, value, data)
        {
            ECDSA_v = ecdsa_v;
            ECDSA_r = ecdsa_r;
            ECDSA_s = ecdsa_s;
        }

        public Transaction(RLPItem rlpTransaction)
        {
            Deserialize(rlpTransaction);
        }
        #endregion

        #region Functions


        public byte[] GetHash()
        {
            // Obtain a hash of our transaction entirely
            return KeccakHash.ComputeHashBytes(RLP.Encode(Serialize()));
        }

        public Address GetSenderAddress()
        {
            // Obtain our transaction hash
            byte[] transactionHash = TransactionUtil.GetUnsignedHash(Nonce, GasPrice, StartGas, new Core.EthTypes.Address(To.ToByteArray()), Value, Data, (uint?)ChainID);

            // Check if we have a cached sender.
            if (_cachedSender.sender != null)
            {
                // Verify all parameters
                if (_cachedSender.cache_v == ECDSA_v && _cachedSender.cache_r == ECDSA_r && _cachedSender.cache_s == ECDSA_s && _cachedSender.hash.ValuesEqual(transactionHash))
                {
                    return _cachedSender.sender;
                }
            }

            // Verify our key parts are valid
            if (ECDSA_r == 0 && ECDSA_s == 0)
            {
                return Address.NULL_ADDRESS;
            }
            else if (ECDSA_r >= Secp256k1Curve.N || ECDSA_s >= Secp256k1Curve.N || ECDSA_r == 0 || ECDSA_s == 0)
            {
                throw new ArgumentException("Invalid ECDSA signature component, either the r or s value.");
            }

            try
            {
                // Using our hash, v, r, and s, we can now recover our public key.
                byte recoveryID = EthereumEcdsa.GetRecoveryIDFromV(ECDSA_v);
                byte[] publicKeyHash = EthereumEcdsa.Recover(transactionHash, recoveryID, ECDSA_r, ECDSA_s).GetPublicKeyHash();

                // If our hash is null, throw na exception.
                if (publicKeyHash == null)
                {
                    throw new ArgumentException();
                }

                // Obtain the address portion from the last bytes of our hash.
                _cachedSender = (ECDSA_v, ECDSA_r, ECDSA_s, transactionHash, new Address(publicKeyHash));
                return _cachedSender.sender;
            }
            catch
            {
                // An exception was caught, we failed our signature.
                throw new TransactionException("Signature failed on transaction.");
            }
        }

        public void Sign(EthereumEcdsa privateKey, EthereumChainID? chainID = null)
        {
            var sig = TransactionUtil.Sign(privateKey, Nonce, GasPrice, StartGas, new Core.EthTypes.Address(To.ToByteArray()), Value, Data, (uint?)chainID);
            ECDSA_r = sig.R;
            ECDSA_s = sig.S;
            ECDSA_v = sig.V;
        }
        #endregion

        #region RLP Serialization
        /// <summary>
        /// Serializes the transaction into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP transaction.</returns>
        public RLPItem Serialize()
        {
            return TransactionUtil.Serialize(ECDSA_v, ECDSA_r, ECDSA_s, Nonce, GasPrice, StartGas, new Core.EthTypes.Address(To.ToByteArray()), Value, Data);
        }

        /// <summary>
        /// Deserializes the given RLP serialized transaction and sets all values accordingly.
        /// </summary>
        /// <param name="item">The RLP item to deserialize and obtain values from.</param>
        public void Deserialize(RLPItem item)
        {
            // Verify this is a list
            if (!item.IsList)
            {
                throw new ArgumentException();
            }

            // Verify it has 9 items.
            RLPList rlpTransaction = (RLPList)item;
            if (rlpTransaction.Items.Count != 9)
            {
                throw new ArgumentException();
            }

            // Verify the types of all items
            for (int i = 0; i < rlpTransaction.Items.Count; i++)
            {
                if (!rlpTransaction.Items[i].IsByteArray)
                {
                    throw new ArgumentException();
                }
            }

            // Verify our address is the correct length
            if (((RLPByteArray)rlpTransaction.Items[3]).Data.Length != Address.ADDRESS_SIZE)
            {
                throw new ArgumentException();
            }

            // Set our values
            Nonce = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[0]);
            GasPrice = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[1]);
            StartGas = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[2]);
            To = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[3], Address.ADDRESS_SIZE);
            Value = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[4]);
            Data = ((RLPByteArray)rlpTransaction.Items[5]).Data.ToArray();
            ECDSA_v = ((byte)rlpTransaction.Items[6]);
            ECDSA_r = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[7]);
            ECDSA_s = RLP.ToInteger((RLPByteArray)rlpTransaction.Items[8]);
        }
        #endregion
    }
}
