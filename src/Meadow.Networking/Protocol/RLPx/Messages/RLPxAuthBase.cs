using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using Meadow.Networking.Protocol.RLPx.Messages;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    /*
    * References:
    * https://github.com/ethereum/devp2p/blob/master/rlpx.md
    * https://github.com/ethereum/EIPs/blob/master/EIPS/eip-8.md
    * https://github.com/ethereum/homestead-guide/blob/master/old-docs-for-reference/go-ethereum-wiki.rst/RLPx-Encryption.rst
    * */

    /// <summary>
    /// Represents the base for an RLPx authentication packet, in order t
    /// </summary>
    public abstract class RLPxAuthBase : IRLPxMessage
    {
        #region Constants
        protected const int NONCE_SIZE = 32;
        #endregion

        #region Fields
        protected static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        #endregion

        #region Properties
        public byte[] R { get; protected set; }
        public byte[] S { get; protected set; }
        public byte V { get; protected set; }
        public byte[] PublicKey { get; protected set; }
        public byte[] Nonce { get; set; }
        #endregion

        #region Functions
        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] data);

        /// <summary>
        /// Recovers the ephemeral key used to sign the transformed nonce in the authentication data.
        /// Throws an exception if the recovered key is invalid, or could not be recovered.
        /// </summary>
        /// <param name="receiverPrivateKey">The private key of the receiver used to generate the shared secret.</param>
        /// <returns>Returns the remote ephemeral public key for the keypair which signed this authentication data.</returns>
        public virtual EthereumEcdsa RecoverRemoteEphemeralKey(EthereumEcdsa receiverPrivateKey)
        {
            // Create an EC provider with the given public key.
            EthereumEcdsa publicKey = EthereumEcdsa.Create(PublicKey, EthereumEcdsaKeyType.Public);

            // Generate the shared secret using ECDH between our local private key and this remote public key
            byte[] ecdhKey = receiverPrivateKey.ComputeECDHKey(publicKey);

            // Obtain our transformed nonce data.
            byte[] transformedNonceData = GetTransformedNonce(ecdhKey);

            // We want our signature in r,s,v format.
            BigInteger ecdsa_r = BigIntegerConverter.GetBigInteger(R, false, 32);
            BigInteger ecdsa_s = BigIntegerConverter.GetBigInteger(S, false, 32);
            byte recoveryId = EthereumEcdsa.GetRecoveryIDFromV(V);

            // Recover the public key from the data provided.
            EthereumEcdsa remoteEphemeralPublickey = EthereumEcdsa.Recover(transformedNonceData, recoveryId, ecdsa_r, ecdsa_s);

            // Verify the key is valid

            // Return the ephemeral key
            return remoteEphemeralPublickey;
        }

        public virtual void Sign(EthereumEcdsa localPrivateKey, EthereumEcdsa ephemeralPrivateKey, EthereumEcdsa receiverPublicKey, uint? chainID = null)
        {
            // Generate the shared secret using ECDH between our local private key and this remote public key
            byte[] ecdhKey = localPrivateKey.ComputeECDHKey(receiverPublicKey);

            // Generate a nonce for our authentication message if the one provided is null.
            if (Nonce == null)
            {
                Nonce = new byte[NONCE_SIZE];
                _randomNumberGenerator.GetBytes(Nonce);
            }
            else if (Nonce.Length != NONCE_SIZE)
            {
                // Throw an exception if an invalid nonce was provided.
                throw new ArgumentException($"Invalid size nonce provided for RLPx session when signing authentication message. Should be {NONCE_SIZE} bytes but was {Nonce.Length}.");
            }

            // Obtain our transformed nonce data.
            byte[] transformedNonceData = GetTransformedNonce(ecdhKey);

            // Sign the transformed data.
            var signature = ephemeralPrivateKey.SignData(transformedNonceData);

            // We want our signature in r,s,v format.
            R = BigIntegerConverter.GetBytes(signature.r, 32);
            S = BigIntegerConverter.GetBytes(signature.s, 32);
            V = EthereumEcdsa.GetVFromRecoveryID(chainID, signature.RecoveryID);

            // Set our local public key and the public key hash.
            PublicKey = localPrivateKey.ToPublicKeyArray(false, true);
        }

        private byte[] GetTransformedNonce(byte[] ecdhKey)
        {
            // Xor the nonce and shared secret (this will be used to sign, and we will provide the receiver with the nonce so they can verify the signed data too).
            byte[] transformedNonceData = new byte[Nonce.Length];
            for (int i = 0; i < ecdhKey.Length; i++)
            {
                transformedNonceData[i] = (byte)(ecdhKey[i] ^ Nonce[i]);
            }

            // Return the transformed nonce data.
            return transformedNonceData;
        }
        #endregion
    }
}
