using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Networking.Protocol.RLPx.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using Meadow.Core.Utils;
using Xunit;

namespace Meadow.Networking.Test
{
    public class RLPxAuthTests
    {
        [Fact]
        public void ValidSignAndRecoverStandard()
        {
            // Generate all needed keypairs.
            EthereumEcdsa localPrivateKey = EthereumEcdsa.Generate();
            EthereumEcdsa ephemeralPrivateKey = EthereumEcdsa.Generate();
            EthereumEcdsa receiverPrivateKey = EthereumEcdsa.Generate();

            // Create an RLPx auth packet and sign it.
            RLPxAuthStandard authPacket = new RLPxAuthStandard();
            authPacket.Sign(localPrivateKey, ephemeralPrivateKey, receiverPrivateKey, null);

            // Serialize and deserialize it.
            byte[] serializedData = authPacket.Serialize();
            RLPxAuthStandard deserializedPacket = new RLPxAuthStandard(serializedData);

            // Verify all matches between our serialized/deserialized object data.
            Assert.Equal(authPacket.EphermalPublicKeyHash.ToHexString(), deserializedPacket.EphermalPublicKeyHash.ToHexString());
            Assert.Equal(authPacket.Nonce.ToHexString(), deserializedPacket.Nonce.ToHexString());
            Assert.Equal(authPacket.PublicKey.ToHexString(), deserializedPacket.PublicKey.ToHexString());
            Assert.Equal(authPacket.R.ToHexString(), deserializedPacket.R.ToHexString());
            Assert.Equal(authPacket.S.ToHexString(), deserializedPacket.S.ToHexString());
            Assert.Equal(authPacket.V, deserializedPacket.V);
            Assert.Equal(authPacket.UseSessionToken, deserializedPacket.UseSessionToken);

            // Try to recover the public key
            EthereumEcdsa recoveredEphemeralPublicKey = deserializedPacket.RecoverRemoteEphemeralKey(receiverPrivateKey);

            // Verify our public key hashes match
            Assert.Equal(ephemeralPrivateKey.GetPublicKeyHash().ToHexString(), recoveredEphemeralPublicKey.GetPublicKeyHash().ToHexString());
        }

        [Fact]
        public void ValidSignAndRecoverEip8()
        {
            // Generate all needed keypairs.
            EthereumEcdsa localPrivateKey = EthereumEcdsa.Generate();
            EthereumEcdsa ephemeralPrivateKey = EthereumEcdsa.Generate();
            EthereumEcdsa receiverPrivateKey = EthereumEcdsa.Generate();

            // Create an RLPx auth packet and sign it.
            RLPxAuthEIP8 authPacket = new RLPxAuthEIP8();
            authPacket.Sign(localPrivateKey, ephemeralPrivateKey, receiverPrivateKey, null);

            // Serialize and deserialize it.
            byte[] serializedData = authPacket.Serialize();
            RLPxAuthEIP8 deserializedPacket = new RLPxAuthEIP8(serializedData);

            // Verify all matches between our serialized/deserialized object data.
            Assert.Equal(authPacket.Nonce.ToHexString(), deserializedPacket.Nonce.ToHexString());
            Assert.Equal(authPacket.PublicKey.ToHexString(), deserializedPacket.PublicKey.ToHexString());
            Assert.Equal(authPacket.R.ToHexString(), deserializedPacket.R.ToHexString());
            Assert.Equal(authPacket.S.ToHexString(), deserializedPacket.S.ToHexString());
            Assert.Equal(authPacket.V, deserializedPacket.V);

            // Try to recover the public key
            EthereumEcdsa recoveredEphemeralPublicKey = deserializedPacket.RecoverRemoteEphemeralKey(receiverPrivateKey);

            // Verify our public key hashes match
            Assert.Equal(ephemeralPrivateKey.GetPublicKeyHash().ToHexString(), recoveredEphemeralPublicKey.GetPublicKeyHash().ToHexString());
        }
    }
}
