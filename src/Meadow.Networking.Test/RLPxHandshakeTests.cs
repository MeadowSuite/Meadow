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
            RLPxSession initiatorSession = new RLPxSession(RLPxSessionRole.Initiator, initiatorKeyPair, initiatorEphemeralKeyPair, useEip8);
            RLPxSession responderSession = new RLPxSession(RLPxSessionRole.Responder, responderKeyPair, responderEphemeralKeypair);

            // Create authentication data (initiator) (should work)
            byte[] authData = initiatorSession.CreateAuthentiation(responderKeyPair);

            // Create authentication data (responder) (should fail, responder should only be receiving auth data, not creating it).
            Assert.Throws<Exception>(() => { responderSession.CreateAuthentiation(initiatorKeyPair); });

            // Verify the authentication data (responder) (should work)
            responderSession.VerifyAuthentication(authData);

            // Verify the authentication data (initiator) (should fail, responder should only be creating auth data, not verifying/receiving it).
            Assert.Throws<Exception>(() => { initiatorSession.VerifyAuthentication(authData); });

            // After verification, the responder session should have set it's EIP8 status accordingly based off of what was received.
            Assert.Equal(useEip8, responderSession.UsingEIP8Authentication);

            // Create an authentication acknowledgement (responder) (should work)
            byte[] authAckData = responderSession.CreateAuthenticationAcknowledgement();

            // Create an authentication acknowledgement (initiator) (should fail, initiator should only receiving auth-ack)
            Assert.Throws<Exception>(() => { initiatorSession.CreateAuthenticationAcknowledgement(); });

            // Verify the authentication acknowledgement (initiator) (should work)
            initiatorSession.VerifyAuthenticationAcknowledgement(authAckData);

            // Verify the authentication acknowledgement (responder) (should fail, responder should not be verifying/receiving auth-ack)
            Assert.Throws<Exception>(() => { responderSession.VerifyAuthenticationAcknowledgement(authAckData); });
        }

        [Fact(Skip = "Tests data that is not exposed. Can be tested by uncommenting the commented code and exposing access to the relevant properties.")]
        public void ProvidedHandshakeData()
        {
            // Define our nonces
            byte[] initiatorNonce = "abfee9cf900416b3c08c33e640739d5f27320ec140f024c07587a01bf2118910".HexToBytes();
            byte[] responderNonce = "312d4b3f55134880afc60f1a605dbd89dfbc8bc275e0f110221c5b09a23e382e".HexToBytes();

            // Create the initiator and responder keypairs
            EthereumEcdsa initiatorKeyPair = EthereumEcdsa.Create("549f2abf6141a90ff2d21d4a7d8f297d6629d52c279890343840b94100cfe873".HexToBytes(), EthereumEcdsaKeyType.Private);
            EthereumEcdsa initiatorEphemeralKeyPair = EthereumEcdsa.Create("42fe8795681de5ea19ea164348e1f0321cf2ae03c0731785a0753a503be0a91f".HexToBytes(), EthereumEcdsaKeyType.Private);
            EthereumEcdsa responderKeyPair = EthereumEcdsa.Create("259e1377ac0022c638b951c64446964c03b2743edd977953c83b72889493c2c4".HexToBytes(), EthereumEcdsaKeyType.Private);
            EthereumEcdsa responderEphemeralKeypair = EthereumEcdsa.Create("7ad9d85886f3ad51d231dc0a3fe346ffc0aedddcbc1d910a23f7d8215dc4f6f1".HexToBytes(), EthereumEcdsaKeyType.Private);

            // Initiate RLPx sessions for each role.
            RLPxSession initiatorSession = new RLPxSession(RLPxSessionRole.Initiator, initiatorKeyPair, initiatorEphemeralKeyPair);
            RLPxSession responderSession = new RLPxSession(RLPxSessionRole.Responder, responderKeyPair, responderEphemeralKeypair);

            // Create authentication data (initiator) (should work)
            byte[] authData = initiatorSession.CreateAuthentiation(responderKeyPair, (byte[])initiatorNonce.Clone());

            // Verify the authentication data (responder) (should work)
            responderSession.VerifyAuthentication(authData);

            // Create an authentication acknowledgement (responder) (should work)
            byte[] authAckData = responderSession.CreateAuthenticationAcknowledgement((byte[])responderNonce.Clone());

            // Verify the authentication acknowledgement (initiator) (should work)
            initiatorSession.VerifyAuthenticationAcknowledgement(authAckData);

            // Set our auth and auth-ack packets (UNCOMMENT/COMMENT THIS ACCORDINGLY FOR TESTING, AS DESCRIBED IN THE SKIP MESSAGE MARKING THIS TEST).
            //initiatorSession.AuthData = "04ce3672878bc585003184ae8397e7ee3484beb2edadcdb167eca1c34d8369022ce2bda5f02d165c64ea86e12712124759e57d5cac57e2c2b5dbf3b9273b8b1b51c4b879deadc142e5aba12505e307bebf043e0a39153590cd6d388e97997e1fb11e4aaf96084ead8b39f1f2f0992c0e5048219c00e7ca3729ea598ddeeef00ba61b1f6807f500c16eaedf2be25d9715cc664c56c65c1ad4878b564d245ff7b2a416d03ad40b67d7ad859a51be44634925a44f54158dcb23cf4ee7f4c6312cc4315b358be1976d7e0639b3276df741d17510ab7f8804a38a81114584c1320c9f8f55ae2dbd16e14752a5dc96a91af8bffef75e0bb69754d2e4c08e7f3f4aacb15492fa543378cf3839f1be272ad63ce9d874a253e3af358a0dd8d22122137af955e1e6d011f2c0b1792ce24466579e89fb6f44".HexToBytes();
            //initiatorSession.AuthAckData = "040be304b52267f5f834b9491dd96beac89195495bbe5fae78cb82cb1bee5906c595af9a56038813f724c7936c492a3ee1f3dfa1dd826249d88acc4f4b0f2ce4f2f48faef6f34a64cbb85b13d138f82b5d447958e6b7c278b748510602dc1043f026f771ca17b9763e14dee7f6e3902d3eeb0f7f8b07da71ccb4107e8cfe53d65ccafbc70b65a0c9ef590c26d207866f9e6d3b0f9ab16cea65e93674b4f155a901e312b83dbb51aed6695afac10ec9b7ea0d1c846310a16a6c45ba2473f85d56894259caf3c425e44d9a90072e049920a6b3".HexToBytes();
            //responderSession.AuthData = initiatorSession.AuthData;
            //responderSession.AuthAckData = initiatorSession.AuthAckData;

            // Derive our resulting data
            initiatorSession.DeriveEncryptionParameters();
            responderSession.DeriveEncryptionParameters();

            // Verify properties
            Assert.Equal(initiatorNonce, initiatorSession.InitiatorNonce);
            Assert.Equal(responderNonce, initiatorSession.ResponderNonce);
            Assert.Equal(initiatorNonce, responderSession.InitiatorNonce);
            Assert.Equal(responderNonce, responderSession.ResponderNonce);

            // Get our initial message authentication codes
            Assert.Equal("a459dc46f089e803a641d02a495965a5fa73e183a2fe5d938f598ff3fd7196d4", initiatorSession.EgressMac.Hash.ToHexString(false));
            Assert.Equal("cc4e5144935e90fade946a394df07c293916c51fb4f283f2b0570732a7f474b4", initiatorSession.IngressMac.Hash.ToHexString(false));
            Assert.Equal("a459dc46f089e803a641d02a495965a5fa73e183a2fe5d938f598ff3fd7196d4", responderSession.IngressMac.Hash.ToHexString(false));
            Assert.Equal("cc4e5144935e90fade946a394df07c293916c51fb4f283f2b0570732a7f474b4", responderSession.EgressMac.Hash.ToHexString(false));
        }
    }
}
