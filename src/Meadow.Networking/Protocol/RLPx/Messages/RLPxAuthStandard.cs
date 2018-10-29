using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    public class RLPxAuthStandard : RLPxAuthBase
    {
        #region Constants
        private const int STANDARD_AUTH_SIZE = EthereumEcdsa.SIGNATURE_RSV_SIZE + KeccakHash.HASH_SIZE + EthereumEcdsa.PUBLIC_KEY_SIZE + NONCE_SIZE + 1; // R (32) || S (32) || V (1) || EphermalPublicKeyHash (32) || PublicKey (64) || Nonce (32) || UseSessionToken (1)
        #endregion

        #region Properties
        public byte[] EphermalPublicKeyHash { get; private set; }
        public bool UseSessionToken { get; set; }
        #endregion

        #region Constructor
        public RLPxAuthStandard()
        {
        }

        public RLPxAuthStandard(byte[] serializedData)
        {
            Deserialize(serializedData);
        }
        #endregion

        #region Functions
        public override (EthereumEcdsa remoteEphemeralPublicKey, uint? chainId) RecoverDataFromSignature(EthereumEcdsa receiverPrivateKey)
        {
            // Obtain the remote ephemeral key with our base method.
            (EthereumEcdsa remoteEphemeralKey, uint? chainId) = base.RecoverDataFromSignature(receiverPrivateKey);

            // Next we hash the public key to verify it matches our ephemeral public key hash.
            byte[] remoteEphemeralKeyHash = remoteEphemeralKey.GetPublicKeyHash();

            // Verify the public key
            if (!EphermalPublicKeyHash.SequenceEqual(remoteEphemeralKeyHash))
            {
                throw new ArgumentException("Recovered ephemeral key from authentication data did not match the provided ephemeral public key hash.");
            }

            // Return the key
            return (remoteEphemeralKey, chainId);
        }

        public override void Sign(EthereumEcdsa localPrivateKey, EthereumEcdsa ephemeralPrivateKey, EthereumEcdsa remotePublicKey, uint? chainId = null)
        {
            // Sign the data with the base method
            base.Sign(localPrivateKey, ephemeralPrivateKey, remotePublicKey, chainId);

            // Set our ephermal public key hash.
            EphermalPublicKeyHash = ephemeralPrivateKey.GetPublicKeyHash();
        }

        public override void Deserialize(byte[] data)
        {
            // Verify the size of the data
            if (data.Length != STANDARD_AUTH_SIZE)
            {
                throw new ArgumentException("Could not deserialize RLPx Authentication data because the provided serialized data is the incorrect size.");
            }

            // Copy the components out of the data buffer.
            Memory<byte> dataMem = data;
            int offset = 0;
            R = dataMem.Slice(offset, 32).ToArray();
            offset += R.Length;
            S = dataMem.Slice(offset, 32).ToArray();
            offset += S.Length;
            V = dataMem.Span[offset++];
            EphermalPublicKeyHash = dataMem.Slice(offset, KeccakHash.HASH_SIZE).ToArray();
            offset += EphermalPublicKeyHash.Length;
            PublicKey = dataMem.Slice(offset, EthereumEcdsa.PUBLIC_KEY_SIZE).ToArray();
            offset += PublicKey.Length;
            Nonce = dataMem.Slice(offset, NONCE_SIZE).ToArray();
            offset += Nonce.Length;
            UseSessionToken = (dataMem.Span[offset++] != 0);
        }

        public override byte[] Serialize()
        {
            // We serialize our data in the following format:
            byte[] result = new byte[STANDARD_AUTH_SIZE];

            // Copy the data into the resulting buffer.
            int offset = 0;
            Array.Copy(R, 0, result, offset, R.Length);
            offset += R.Length;
            Array.Copy(S, 0, result, offset, S.Length);
            offset += S.Length;
            result[offset++] = V;
            Array.Copy(EphermalPublicKeyHash, 0, result, offset, EphermalPublicKeyHash.Length);
            offset += EphermalPublicKeyHash.Length;
            Array.Copy(PublicKey, 0, result, offset, PublicKey.Length);
            offset += PublicKey.Length;
            Array.Copy(Nonce, 0, result, offset, Nonce.Length);
            offset += Nonce.Length;
            result[offset++] = (byte)(UseSessionToken ? 1 : 0);

            // Verify the data size
            if (offset != result.Length)
            {
                throw new ArgumentException("Could not serialize RLPx Authentication data because the resulting size was invalid.");
            }

            // Return the resulting data.
            return result;
        }
        #endregion

    }
}
