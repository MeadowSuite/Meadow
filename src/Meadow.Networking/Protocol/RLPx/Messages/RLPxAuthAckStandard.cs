using Meadow.Core.Cryptography.Ecdsa;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    public class RLPxAuthAckStandard : RLPxAuthAckBase
    {
        #region Constants
        private const int STANDARD_AUTH_ACK_SIZE = EthereumEcdsa.PUBLIC_KEY_SIZE + RLPxSession.NONCE_SIZE + 1;
        #endregion

        #region Properties
        public bool TokenFound { get; set; }
        #endregion

        #region Constructor
        public RLPxAuthAckStandard()
        {
        }

        public RLPxAuthAckStandard(byte[] serializedData)
        {
            Deserialize(serializedData);
        }
        #endregion

        #region Functions
        public override void Deserialize(byte[] data)
        {
            // Verify the size of the data
            if (data.Length != STANDARD_AUTH_ACK_SIZE)
            {
                throw new ArgumentException("Could not deserialize RLPx auth-ack data because the provided serialized data is the incorrect size.");
            }

            // Copy the components out of the data buffer.
            Memory<byte> dataMem = data;
            int offset = 0;
            EphemeralPublicKey = dataMem.Slice(offset, EthereumEcdsa.PUBLIC_KEY_SIZE).ToArray();
            offset += EphemeralPublicKey.Length;
            Nonce = dataMem.Slice(offset, RLPxSession.NONCE_SIZE).ToArray();
            offset += Nonce.Length;
            TokenFound = (dataMem.Span[offset++] != 0);
        }

        public override byte[] Serialize()
        {
            // Verify the properties of our object.
            VerifyProperties();

            // We serialize our data in the following format: PublicKey || Nonce || TokenFound
            byte[] result = new byte[STANDARD_AUTH_ACK_SIZE];

            // Copy the data into the resulting buffer.
            int offset = 0;
            Array.Copy(EphemeralPublicKey, 0, result, offset, EphemeralPublicKey.Length);
            offset += EphemeralPublicKey.Length;
            Array.Copy(Nonce, 0, result, offset, Nonce.Length);
            offset += Nonce.Length;
            result[offset++] = (byte)(TokenFound ? 1 : 0);

            // Verify the data size
            if (offset != result.Length)
            {
                throw new ArgumentException("Could not serialize RLPx auth-ack data because the resulting size was invalid.");
            }

            // Return the resulting data.
            return result;
        }
        #endregion
    }
}
