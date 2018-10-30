using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using Meadow.Networking.Cryptography;
using Meadow.Networking.Protocol.RLPx.Messages;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Meadow.Networking.Protocol.RLPx
{
    /*
     * References:
     * https://github.com/ethereum/devp2p/blob/master/rlpx.md
     * https://github.com/ethereum/EIPs/blob/master/EIPS/eip-8.md
     * https://github.com/ethereum/homestead-guide/blob/master/old-docs-for-reference/go-ethereum-wiki.rst/RLPx-Encryption.rst
     * */

    /// <summary>
    /// Represents the state of an RLPx session (local/remote keys, authentication, acknowledgements, etc).
    /// </summary>
    public class RLPxSession
    {
        #region Constants
        public const int MAX_SUPPORTED_VERSION = 4;
        public const int NONCE_SIZE = 32;

        private const int AUTH_STANDARD_ENCRYPTED_SIZE = 307;
        private const int AUTH_ACK_STANDARD_ENCRYPTED_SIZE = 210;
        #endregion

        #region Fields
        private static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        #endregion

        #region Properties
        /// <summary>
        /// Indicates the role of this current RLPx session object/user.
        /// </summary>
        public RLPxSessionRole Role { get; }
        /// <summary>
        /// The private key of this user, used to sign 
        /// </summary>
        public EthereumEcdsa LocalPrivateKey { get; }
        public EthereumEcdsa EphemeralPrivateKey { get; }

        public EthereumEcdsa RemotePublicKey { get; private set; }
        public EthereumEcdsa RemoteEphermalPublicKey { get; private set; }
        public BigInteger RemoteVersion { get; private set; }

        public byte[] AuthData { get; private set; }
        public byte[] AuthAckData { get; private set; }

        /// <summary>
        /// The nonce value chosen by the initiator.
        /// </summary>
        public byte[] InitiatorNonce { get; private set; }
        /// <summary>
        /// The nonce value chosen by the responder.
        /// </summary>
        public byte[] ResponderNonce { get; private set; }

        public byte[] Token { get; private set; }
        public byte[] AesSecret { get; private set; }
        public byte[] MacSecret { get; private set; }
        /// <summary>
        /// Message authentication code that is updated as encrypted data is sent.
        /// </summary>
        public byte[] EgressMac { get; private set; }
        /// <summary>
        /// Message authentication code that is updated as encrypted data is received.
        /// </summary>
        public byte[] IngressMac { get; private set; }

        /// <summary>
        /// Indicates whether or not this session is using EIP8. (For the initiator, this means creating EIP8 auth, for receiver, this means we received an EIP8 auth).
        /// </summary>
        public bool UsingEIP8Authentication { get; private set; }
        /*
        * TODO: Move this and UsingEIP8Authentication into network configuration.
        */
        public uint? ChainId { get; }
        #endregion

        #region Constructor
        public RLPxSession(RLPxSessionRole role, EthereumEcdsa localPrivateKey, EthereumEcdsa ephemeralPrivateKey, bool usingEIP8 = false)
        {
            // Verify the private key is not null
            if (localPrivateKey == null)
            {
                throw new ArgumentNullException("Provided local private key for RLPx session was null.");
            }

            // Verify the private key is a private key
            if (localPrivateKey.KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("Provided local private key for RLPx session was not a valid private key.");
            }

            // Set the local private key
            LocalPrivateKey = localPrivateKey;

            // Verify the ephemeral private key is a private key
            if (ephemeralPrivateKey != null && ephemeralPrivateKey.KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("Provided local private key for RLPx session was not a valid private key.");
            }

            // Set the ephemeral private key.
            EphemeralPrivateKey = ephemeralPrivateKey ?? EthereumEcdsa.Generate();

            // Set the role
            Role = role;

            // Set the auth type if we are initiator.
            if (Role == RLPxSessionRole.Initiator)
            {
                UsingEIP8Authentication = usingEIP8;
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Creates authentication data to send to the responder.
        /// </summary>
        /// <param name="receiverPublicKey">The responder/receiver's public key.</param>
        /// <param name="nonce">The nonce to use for the authentication data. If null, new data is generated.</param>
        /// <returns>Returns the encrypted serialized authentication data.</returns>
        public byte[] CreateAuthentiation(EthereumEcdsa receiverPublicKey, byte[] nonce = null)
        {
            // Verify this is the initiator role.
            if (Role != RLPxSessionRole.Initiator)
            {
                throw new Exception("RLPx auth data should only be created by the initiator.");
            }

            // If the nonce is null, generate a new one.
            nonce = nonce ?? GenerateNonce();

            // Set our initiator nonce
            InitiatorNonce = nonce;

            // Set the remote public key
            RemotePublicKey = receiverPublicKey;

            // Create a new authentication message based off of our configured setting.
            RLPxAuthBase authMessage = null;
            if (UsingEIP8Authentication)
            {
                // Create our EIP8 authentication message.
                authMessage = new RLPxAuthEIP8()
                {
                    Nonce = InitiatorNonce,
                    Version = MAX_SUPPORTED_VERSION,
                };
            }
            else
            {
                // Create our standard authentication message.
                authMessage = new RLPxAuthStandard()
                {
                    Nonce = InitiatorNonce,
                };
            }

            // Sign the authentication message
            authMessage.Sign(LocalPrivateKey, EphemeralPrivateKey, RemotePublicKey, ChainId);

            // Serialize the authentication data
            byte[] serializedData = authMessage.Serialize();

            // Encrypt the data accordingly (standard uses no shared mac data, EIP8 has 2 bytes which prefix the resulting encrypted data).
            if (UsingEIP8Authentication)
            {
                // Obtain two bytes of mac data by EIP8 standards (big endian 16-bit unsigned integer equal to the size of the resulting ECIES encrypted data).
                // We can calculate this as the amount of overhead data ECIES adds is static in size.
                byte[] sharedMacData = BigIntegerConverter.GetBytes(serializedData.Length + Ecies.ECIES_ADDITIONAL_OVERHEAD, 2);

                // Encrypt the serialized data with the shared mac data, and prefix the result with it.
                byte[] encryptedSerializedData = Ecies.Encrypt(RemotePublicKey, serializedData, sharedMacData);
                AuthData = sharedMacData.Concat(encryptedSerializedData);
            }
            else
            {
                // Encrypt it using ECIES without any shared mac data.
                AuthData = Ecies.Encrypt(RemotePublicKey, serializedData, null);
            }

            // Return the auth data
            return AuthData;
        }

        /// <summary>
        /// Verifies the provided encrypted serialized authentication data received from the initiator.
        /// If verification fails, an appropriate exception will be thrown. If no exception is thrown,
        /// the verification has succeeded.
        /// </summary>
        /// <param name="authData">The encrypted serialized authentication data to verify.</param>
        public void VerifyAuthentication(byte[] authData)
        {
            // Verify this is the responder role.
            if (Role != RLPxSessionRole.Responder)
            {
                throw new Exception("RLPx auth data should only be verified by the responder.");
            }

            // Try to deserialize the data as a standard authentication packet.
            // The data is currently encrypted serialized data, so it will also need to be decrypted.
            RLPxAuthBase authenticationMessage = null;
            try
            {
                // Set the auth data
                AuthData = authData.Slice(0, AUTH_STANDARD_ENCRYPTED_SIZE);

                // The authentication data can simply be decrypted without any shared mac.
                byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, AuthData, null);

                // Try to parse the decrypted authentication data.
                authenticationMessage = new RLPxAuthStandard(decryptedAuthData);
                UsingEIP8Authentication = false;

                // Set the remote version
                RemoteVersion = MAX_SUPPORTED_VERSION;
            }
            catch (Exception authStandardEx)
            {
                try
                {
                    // EIP8 has a uint16 denoting encrypted data size, and the data to decrypt follows.
                    Memory<byte> authDataMem = authData;
                    Memory<byte> sharedMacDataMem = authDataMem.Slice(0, 2);
                    ushort encryptedSize = (ushort)BigIntegerConverter.GetBigInteger(sharedMacDataMem.Span, false, sharedMacDataMem.Length);
                    Memory<byte> encryptedData = authDataMem.Slice(2, encryptedSize);

                    // Set our auth data
                    AuthData = authDataMem.Slice(0, sharedMacDataMem.Length + encryptedSize).ToArray();

                    // Split the shared mac from the authentication data
                    byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, encryptedData, sharedMacDataMem);

                    // Try to parse the decrypted EIP8 authentication data.
                    RLPxAuthEIP8 authEip8Message = new RLPxAuthEIP8(decryptedAuthData);
                    UsingEIP8Authentication = true;

                    // Set the generic authentication data object.
                    authenticationMessage = authEip8Message;

                    // Set the remote version
                    RemoteVersion = authEip8Message.Version;
                }
                catch (Exception authEip8Ex)
                {
                    string exceptionMessage = "Could not deserialize RLPx auth data in either standard or EIP8 format.\r\n";
                    exceptionMessage += $"Standard Auth Error: {authStandardEx.Message}\r\n";
                    exceptionMessage += $"EIP8 Auth Error: {authEip8Ex.Message}";
                    throw new Exception(exceptionMessage);
                }
            }

            // Set the initiator nonce
            InitiatorNonce = authenticationMessage.Nonce;

            // Set the remote public key.
            RemotePublicKey = EthereumEcdsa.Create(authenticationMessage.PublicKey, EthereumEcdsaKeyType.Public);

            // Try to recover the public key (with the "receiver" private key, which in this case is our local private key, since our role is responder).
            // If this fails, it will throw an exception as we expect this method to if any failure occurs.
            (EthereumEcdsa remoteEphermalPublicKey, uint? chainId) = authenticationMessage.RecoverDataFromSignature(LocalPrivateKey);
            RemoteEphermalPublicKey = remoteEphermalPublicKey;

            // TODO: Verify the chain id.
        }

        /// <summary>
        /// Creates authentication acknowledgement data to send to the initiator, signalling that their authentication 
        /// data was successfully verified, and providing the initiator with information that they have already provided
        /// to the responder (so they can both derive the same shared secrets).
        /// </summary>
        /// <param name="nonce">The nonce to use for the authentication data. If null, new data is generated.</param>
        /// <returns>Returns the encrypted serialized authentication acknowledgement data.</returns>
        public byte[] CreateAuthenticationAcknowledgement(byte[] nonce = null)
        {
            // Verify this is the responder role.
            if (Role != RLPxSessionRole.Responder)
            {
                throw new Exception("RLPx auth-ack data should only be created by the responder.");
            }

            // If the nonce is null, generate a new one.
            if (nonce == null)
            {
                nonce = new byte[NONCE_SIZE];
                _randomNumberGenerator.GetBytes(nonce);
            }

            // Set the responder nonce
            ResponderNonce = nonce ?? GenerateNonce();

            // If we are using EIP8
            RLPxAuthAckBase authAckMessage = null;
            if (UsingEIP8Authentication)
            {
                // We use EIP8 authentication acknowledgement
                authAckMessage = new RLPxAuthAckEIP8()
                {
                    EphemeralPublicKey = EphemeralPrivateKey.ToPublicKeyArray(false, true),
                    Nonce = ResponderNonce,
                    Version = MAX_SUPPORTED_VERSION,
                };
            }
            else
            {
                // We use standard authentication acknowledgement
                authAckMessage = new RLPxAuthAckStandard()
                {
                    EphemeralPublicKey = EphemeralPrivateKey.ToPublicKeyArray(false, true),
                    Nonce = ResponderNonce,
                    TokenFound = false, // TODO: Check for a saved session key from before, and set this accordingly.
                };
            }

            // Serialize the authentication-acknowledgement data
            byte[] serializedData = authAckMessage.Serialize();

            // Encrypt the data accordingly (standard uses no shared mac data, EIP8 has 2 bytes which prefix the resulting encrypted data).
            if (UsingEIP8Authentication)
            {
                // Obtain two bytes of mac data by EIP8 standards (big endian 16-bit unsigned integer equal to the size of the resulting ECIES encrypted data).
                // We can calculate this as the amount of overhead data ECIES adds is static in size.
                byte[] sharedMacData = BigIntegerConverter.GetBytes(serializedData.Length + Ecies.ECIES_ADDITIONAL_OVERHEAD, 2);

                // Encrypt the serialized data with the shared mac data, prefix the result with it.
                byte[] encryptedSerializedData = Ecies.Encrypt(RemotePublicKey, serializedData, sharedMacData);
                AuthAckData = sharedMacData.Concat(encryptedSerializedData);
            }
            else
            {
                // Encrypt it using ECIES without any shared mac data.
                AuthAckData = Ecies.Encrypt(RemotePublicKey, serializedData, null);
            }

            // Return the auth-ack data
            return AuthAckData;
        }

        /// <summary>
        /// Verifies the provided encrypted serialized authentication acknowledgement data received from the initiator.
        /// If verification fails, an appropriate exception will be thrown. If no exception is thrown, the verification
        /// has succeeded.
        /// </summary>
        /// <param name="authAckData">The encrypted serialized authentication acknowledgement data to verify.</param>
        public void VerifyAuthenticationAcknowledgement(byte[] authAckData)
        {
            // Verify this is the initiator role.
            if (Role != RLPxSessionRole.Initiator)
            {
                throw new Exception("RLPx auth-ack data should only be verified by the initiator.");
            }

            // Try to deserialize the data as a standard authentication packet.
            // The data is currently encrypted serialized data, so it will also need to be decrypted.
            RLPxAuthAckBase authAckMessage = null;
            try
            {
                // Set the auth-ack data
                AuthAckData = authAckData.Slice(0, AUTH_ACK_STANDARD_ENCRYPTED_SIZE);

                // The authentication data can simply be decrypted without any shared mac.
                byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, AuthAckData, null);

                // Try to parse the decrypted authentication data.
                authAckMessage = new RLPxAuthAckStandard(decryptedAuthData);
                UsingEIP8Authentication = false;

                // Set the remote version
                RemoteVersion = MAX_SUPPORTED_VERSION;
            }
            catch (Exception authAckStandardEx)
            {
                try
                {
                    // EIP8 has a uint16 denoting encrypted data size, and the data to decrypt follows.
                    Memory<byte> authAckDataMem = authAckData;
                    Memory<byte> sharedMacDataMem = authAckDataMem.Slice(0, 2);
                    ushort encryptedSize = (ushort)BigIntegerConverter.GetBigInteger(sharedMacDataMem.Span, false, sharedMacDataMem.Length);
                    Memory<byte> encryptedData = authAckDataMem.Slice(2, encryptedSize);

                    // Set our auth-ack data
                    AuthAckData = authAckDataMem.Slice(0, sharedMacDataMem.Length + encryptedSize).ToArray();

                    // Split the shared mac from the authentication data
                    byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, encryptedData, sharedMacDataMem);

                    // Try to parse the decrypted EIP8 authentication data.
                    RLPxAuthAckEIP8 authEip8Message = new RLPxAuthAckEIP8(decryptedAuthData);
                    UsingEIP8Authentication = true;

                    // Set the generic authentication data object.
                    authAckMessage = authEip8Message;

                    // Set the remote version
                    RemoteVersion = authEip8Message.Version;
                }
                catch (Exception authAckEip8Ex)
                {
                    string exceptionMessage = "Could not deserialize RLPx auth-ack data in either standard or EIP8 format.\r\n";
                    exceptionMessage += $"Standard Auth-Ack Error: {authAckStandardEx.Message}\r\n";
                    exceptionMessage += $"EIP8 Auth-Ack Error: {authAckEip8Ex.Message}";
                    throw new Exception(exceptionMessage);
                }
            }

            // Set the responder nonce
            ResponderNonce = authAckMessage.Nonce;

            // Set the remote public key.
            RemoteEphermalPublicKey = EthereumEcdsa.Create(authAckMessage.EphemeralPublicKey, EthereumEcdsaKeyType.Public);
        }

        public void DeriveEncryptionParameters()
        {
            // Verify we have all required information
            if (AuthData == null || AuthAckData == null || RemoteEphermalPublicKey == null || InitiatorNonce == null || ResponderNonce == null)
            {
                throw new Exception("RLPx deriving encryption information failed: Handshaking has not successfully completed yet.");
            }

            // Generate the ecdh key with both ephemeral keys
            byte[] ecdhEphemeralKey = EphemeralPrivateKey.ComputeECDHKey(RemoteEphermalPublicKey);

            // Generate a shared secret: Keccak256(ecdhEphemeralKey || Keccak256(ResponderNonce || InitiatorNonce))
            byte[] combinedNonceHash = KeccakHash.ComputeHashBytes(ResponderNonce.Concat(InitiatorNonce));
            byte[] sharedSecret = KeccakHash.ComputeHashBytes(ecdhEphemeralKey.Concat(combinedNonceHash));

            // Derive the token as a hash of the shared secret.
            Token = KeccakHash.ComputeHashBytes(sharedSecret);

            // Derive AES secret: Keccak256(ecdhEphemeralKey || sharedSecret)
            AesSecret = KeccakHash.ComputeHashBytes(ecdhEphemeralKey.Concat(sharedSecret));

            // Derive Mac secret: Keccak256(ecdhEphemeralKey || AesSecret)
            MacSecret = KeccakHash.ComputeHashBytes(ecdhEphemeralKey.Concat(AesSecret));

            // TODO: Derive ingress + egress mac. (Requires Keccak implementation that can update)
        }

        public static byte[] GenerateNonce()
        {
            // Create a new nonce buffer
            byte[] nonce = new byte[NONCE_SIZE];

            // Fill it with data accordingly
            _randomNumberGenerator.GetBytes(nonce);

            // Return the nonce
            return nonce;
        }
        #endregion
    }
}
