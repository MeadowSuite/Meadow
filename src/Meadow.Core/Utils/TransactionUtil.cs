using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.EthTypes;
using Meadow.Core.RlpEncoding;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class TransactionUtil
    {
        /// <summary>
        /// Signs transaction data and returns the raw byte array. Typically used for eth_sendRawTransaction.
        /// </summary>
        public static byte[] SignRawTransaction(EthereumEcdsa privateKey, BigInteger nonce, BigInteger gasPrice, BigInteger gasLimit, Address? to, BigInteger value, byte[] data, uint? chainID)
        {
            var sig = Sign(privateKey, nonce, gasPrice, gasLimit, to, value, data, chainID);
            var serialized = Serialize(sig.V, sig.R, sig.S, nonce, gasPrice, gasLimit, to, value, data);
            byte[] bytes = RLP.Encode(serialized);
            return bytes;
        }

        public static (BigInteger R, BigInteger S, byte V) Sign(EthereumEcdsa privateKey, BigInteger nonce, BigInteger gasPrice, BigInteger gasLimit, Address? to, BigInteger value, byte[] data, uint? chainID)
        {
            // Obtain our transaction hash to sign.
            byte[] hash = GetUnsignedHash(nonce, gasPrice, gasLimit, to, value, data, chainID);

            // Sign our data
            (byte RecoveryID, BigInteger r, BigInteger s) signature = privateKey.SignData(hash);

            // Set our r and s components.
            var r = signature.r;
            var s = signature.s;

            // Obtain our v parameter from recovery ID and set it
            var v = EthereumEcdsa.GetVFromRecoveryID(chainID, signature.RecoveryID);

            return (r, s, v);
        }

        public static byte[] GetUnsignedHash(BigInteger nonce, BigInteger gasPrice, BigInteger gasLimit, Address? to, BigInteger value, byte[] data, uint? chainID)
        {
            // Spurious Dragon introduced an update to deter replay attacks where v = CHAIN_ID * 2 + 35 or v = CHAIN_ID * 2 + 36.
            if (chainID != null)
            {
                // Ethereum uses this to deter replay attacks by embedding the network/chain ID in the hashed data.
                return KeccakHash.ComputeHashBytes(RLP.Encode(Serialize((byte)chainID, 0, 0, nonce, gasPrice, gasLimit, to, value, data))); // we serialize with our network ID.
            }

            // Pre-Spurious Dragon, we simply hash all main properties except v, r, s.
            return KeccakHash.ComputeHashBytes(RLP.Encode(Serialize(null, null, null, nonce, gasPrice, gasLimit, to, value, data)));
        }


        /// <summary>
        /// Serializes the transaction into an RLP item for encoding.
        /// </summary>
        /// <returns>Returns a serialized RLP transaction.</returns>
        public static RLPItem Serialize(byte? v, BigInteger? r, BigInteger? s, BigInteger nonce, BigInteger gasPrice, BigInteger gasLimit, Address? to, BigInteger value, byte[] data)
        {
            // We create a new RLP list that constitute this transaction.
            RLPList rlpTransaction = new RLPList();

            // Add our values
            rlpTransaction.Items.Add(RLP.FromInteger(nonce, 32, true));
            rlpTransaction.Items.Add(RLP.FromInteger(gasPrice, 32, true));
            rlpTransaction.Items.Add(RLP.FromInteger(gasLimit, 32, true));

            if (to != null)
            {
                rlpTransaction.Items.Add(new RLPByteArray(to.Value.GetBytes()));
            }
            else
            {
                rlpTransaction.Items.Add(new RLPByteArray(Array.Empty<byte>()));
            }

            rlpTransaction.Items.Add(RLP.FromInteger(value, 32, true));
            rlpTransaction.Items.Add(data);
            if (v != null)
            {
                rlpTransaction.Items.Add(v);
            }

            if (r != null)
            {
                rlpTransaction.Items.Add(RLP.FromInteger(r.Value, 32, true));
            }

            if (s != null)
            {
                rlpTransaction.Items.Add(RLP.FromInteger(s.Value, 32, true));
            }

            // Return our rlp log item.
            return rlpTransaction;
        }

    }
}
