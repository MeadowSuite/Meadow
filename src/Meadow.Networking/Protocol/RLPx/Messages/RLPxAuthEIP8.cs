using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.RlpEncoding;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx.Messages
{
    public class RLPxAuthEIP8 : RLPxAuthBase
    {
        #region Properties
        public BigInteger Version { get; set; }
        #endregion

        #region Constructor
        public RLPxAuthEIP8()
        {

        }

        public RLPxAuthEIP8(byte[] data)
        {
            Deserialize(data);
        }
        #endregion

        #region Functions
        public override bool Deserialize(byte[] data)
        {
            // Decode our RLP item from data.
            RLPList rlpList = null;

            try
            {
                rlpList = (RLPList)RLP.Decode(data);
            }
            catch
            {
                return false;
            }

            // Verify the sizes of all components.
            if (!rlpList.Items[0].IsByteArray)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's first item (signature) was not a byte array.");
                return false;
            }

            if (((byte[])rlpList.Items[0]).Length != EthereumEcdsa.SIGNATURE_RSV_SIZE)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's first item (signature) was the incorrect size.");
                return false;
            }

            if (!rlpList.Items[1].IsByteArray)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's second item (public key) was not a byte array.");
                return false;
            }

            if (((byte[])rlpList.Items[1]).Length != EthereumEcdsa.PUBLIC_KEY_SIZE)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's second item (public key) was the incorrect size.");
                return false;
            }

            if (!rlpList.Items[2].IsByteArray)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's third item (nonce) was not a byte array.");
                return false;
            }

            if (((byte[])rlpList.Items[2]).Length != NONCE_SIZE)
            {
                //throw new ArgumentException("RLPx EIP8 Authentication packet's third item (nonce) was the incorrect size.");
                return false;
            }

            // Obtain all components.
            Memory<byte> signature = rlpList.Items[0];
            R = signature.Slice(0, 32).ToArray();
            S = signature.Slice(32, 32).ToArray();
            V = signature.Span[64];
            PublicKey = rlpList.Items[1];
            Nonce = rlpList.Items[2];

            // Decode version if it's available.
            if (rlpList.Items.Count >= 4)
            {
                Version = RLP.ToInteger((RLPByteArray)rlpList.Items[3], 32, false);
            }

            // Return a success status
            return true;
        }

        public override byte[] Serialize()
        {
            // Create an RLP item to contain all of our data
            RLPList rlpList = new RLPList();
            rlpList.Items.Add(R.Concat(S, new byte[] { V }));
            rlpList.Items.Add(PublicKey);
            rlpList.Items.Add(Nonce);
            rlpList.Items.Add(RLP.FromInteger(Version, 32, true));

            // Serialize our RLP data
            return RLP.Encode(rlpList);
        }
        #endregion

    }
}
