using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.RlpEncoding;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    public class RLPxAuthAckEIP8 : RLPxAuthAckBase
    {
        #region Properties
        public BigInteger Version { get; set; }
        #endregion

        #region Constructor
        public RLPxAuthAckEIP8()
        {
        }

        public RLPxAuthAckEIP8(byte[] serializedData)
        {
            Deserialize(serializedData);
        }
        #endregion

        #region Functions
        public override void Deserialize(byte[] data)
        {
            // Decode our RLP item from data.
            RLPList rlpList = (RLPList)RLP.Decode(data);

            // Verify the sizes of all components.
            if (!rlpList.Items[0].IsByteArray)
            {
                throw new ArgumentException("RLPx EIP8 auth-ack packet's first item (ephemeral public key) was not a byte array.");
            }
            else if (((byte[])rlpList.Items[0]).Length != EthereumEcdsa.PUBLIC_KEY_SIZE)
            {
                throw new ArgumentException("RLPx EIP8 auth-ack packet's first item (ephemeral public key) was not the correct size.");
            }
            else if (!rlpList.Items[1].IsByteArray)
            {
                throw new ArgumentException("RLPx EIP8 auth-ack packet's second item (nonce) was not a byte array.");
            }
            else if (((byte[])rlpList.Items[1]).Length != RLPxSession.NONCE_SIZE)
            {
                throw new ArgumentException("RLPx EIP8 auth-ack packet's second item (nonce) was not the correct size.");
            }
            else if (rlpList.Items.Count >= 3 && !rlpList.Items[2].IsByteArray)
            {
                throw new ArgumentException("RLPx EIP8 auth-ack packet's third item (version) was not a byte array.");
            }

            // Obtain all components.
            EphemeralPublicKey = rlpList.Items[0];
            Nonce = rlpList.Items[1];

            // Decode version if it's available.
            if (rlpList.Items.Count >= 3)
            {
                Version = RLP.ToInteger((RLPByteArray)rlpList.Items[2], 32, false);
            }
        }

        public override byte[] Serialize()
        {
            // Verify our underlying properties
            VerifyProperties();

            // Create an RLP item to contain all of our data
            RLPList rlpList = new RLPList();
            rlpList.Items.Add(EphemeralPublicKey);
            rlpList.Items.Add(Nonce);
            rlpList.Items.Add(RLP.FromInteger(Version, 32, true));

            // Serialize our RLP data
            return RLP.Encode(rlpList);
        }
        #endregion
    }
}
