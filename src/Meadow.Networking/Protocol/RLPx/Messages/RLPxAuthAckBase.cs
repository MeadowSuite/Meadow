using Meadow.Core.Cryptography.Ecdsa;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    /// <summary>
    /// Represents the base for an RLPx authentication acknowledge data, exposing common variables among all authentication types.
    /// </summary>
    public abstract class RLPxAuthAckBase : IRLPxMessage
    {
        #region Fields
        protected static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        #endregion

        #region Properties
        public byte[] EphemeralPublicKey { get; set; }
        public byte[] Nonce { get; set; }
        #endregion

        #region Functions
        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] data);
        protected void VerifyProperties()
        {
            // If our nonce is null, generate a new one
            Nonce = Nonce ?? RLPxSession.GenerateNonce();

            // Verify the ephemeral public key is not null and is the correct size.
            if (EphemeralPublicKey?.Length != EthereumEcdsa.PUBLIC_KEY_SIZE)
            {
                throw new ArgumentException("Could not construct RLPx auth-ack because the provided ephemeral public key is not the correct size.");
            }

            // Verify the nonce is not null and is the correct size.
            if (Nonce.Length != RLPxSession.NONCE_SIZE)
            {
                throw new ArgumentException("Could not construct RLPx auth-ack because the provided nonce is not the correct size.");
            }
        }
        #endregion
    }
}
