using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Networking.Protocol.RLPx.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using Meadow.Core.Utils;
using Xunit;
using Meadow.Networking.Protocol.RLPx;

namespace Meadow.Networking.Test
{
    public class RLPxHandshakeTests
    {
        [Fact]
        public void AuthValidSignAndRecoverStandard()
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
            EthereumEcdsa recoveredEphemeralPublicKey = deserializedPacket.RecoverDataFromSignature(receiverPrivateKey).remoteEphemeralPublicKey;

            // Verify our public key hashes match
            Assert.Equal(ephemeralPrivateKey.GetPublicKeyHash().ToHexString(), recoveredEphemeralPublicKey.GetPublicKeyHash().ToHexString());
        }

        [Fact]
        public void AuthValidSignAndRecoverEip8()
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
            EthereumEcdsa recoveredEphemeralPublicKey = deserializedPacket.RecoverDataFromSignature(receiverPrivateKey).remoteEphemeralPublicKey;

            // Verify our public key hashes match
            Assert.Equal(ephemeralPrivateKey.GetPublicKeyHash().ToHexString(), recoveredEphemeralPublicKey.GetPublicKeyHash().ToHexString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FullHandshakeStandard(bool useEip8)
        {
            // Create the initiator and responder keypairs
            EthereumEcdsa initiatorKeyPair = EthereumEcdsa.Generate();
            EthereumEcdsa initiatorEphemeralKeyPair = EthereumEcdsa.Generate();
            EthereumEcdsa responderKeyPair = EthereumEcdsa.Generate();
            EthereumEcdsa responderEphemeralKeypair = EthereumEcdsa.Generate();

            // Initiate RLPx sessions for each role.
            RLPxSession initiatorSession = new RLPxSession(RLPxSessionRole.Initiator, initiatorKeyPair, initiatorEphemeralKeyPair);
            initiatorSession.UsingEIP8Authentication = useEip8;
            RLPxSession responderSession = new RLPxSession(RLPxSessionRole.Responder, responderKeyPair, responderEphemeralKeypair);

            // Create authentication data (initiator) (should work)
            byte[] authenticationData = initiatorSession.CreateAuthentiation(responderKeyPair);

            // Create authentication data (responder) (should fail, responder should only be receiving auth data, not creating it).
            Assert.Throws<Exception>(() => { responderSession.CreateAuthentiation(initiatorKeyPair); });

            // Verify the authentication data (responder) (should work)
            responderSession.VerifyAuthentication(authenticationData);

            // Verify the authentication data (initiator) (should fail, responder should only be creating auth data, not verifying/receiving it).
            Assert.Throws<Exception>(() => { initiatorSession.VerifyAuthentication(authenticationData); });

            // After verification, the responder session should have set it's EIP8 status accordingly based off of what was received.
            Assert.Equal(useEip8, responderSession.UsingEIP8Authentication);
        }
    }
}
