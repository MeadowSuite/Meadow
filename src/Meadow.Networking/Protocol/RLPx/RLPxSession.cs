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

        public byte[] InitiatorNonce { get; private set; }
        public byte[] ResponderNonce { get; private set; }

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
                // Generate two bytes of random mac data.
                byte[] sharedMacData = new byte[2];
                _randomNumberGenerator.GetBytes(sharedMacData);

                // Encrypt the serialized data with the shared mac data, and return the result prefixed with it.
                byte[] encryptedSerializedData = Ecies.Encrypt(RemotePublicKey, serializedData, sharedMacData);
                return sharedMacData.Concat(encryptedSerializedData);
            }
            else
            {
                // Encrypt it using ECIES and return it without any shared mac data.
                return Ecies.Encrypt(RemotePublicKey, serializedData, null);
            }
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
                // The authentication data can simply be decrypted without any shared mac.
                byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, authData, null);

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
                    // EIP8 has the first two bytes as the shared mac data, and the remainder is the data to decrypt.
                    Memory<byte> authDataMem = authData;

                    // Split the shared mac from the authentication data
                    byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, authDataMem.Slice(2), authDataMem.Slice(0, 2));

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

                // Encrypt the serialized data with the shared mac data, and return the result prefixed with it.
                byte[] encryptedSerializedData = Ecies.Encrypt(RemotePublicKey, serializedData, sharedMacData);
                return sharedMacData.Concat(encryptedSerializedData);
            }
            else
            {
                // Encrypt it using ECIES and return it without any shared mac data.
                return Ecies.Encrypt(RemotePublicKey, serializedData, null);
            }
        }

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
                // The authentication data can simply be decrypted without any shared mac.
                byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, authAckData, null);

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
                    // EIP8 has the first two bytes as the shared mac data, and the remainder is the data to decrypt.
                    Memory<byte> authDataMem = authAckData;
                    Memory<byte> sharedMacDataMem = authDataMem.Slice(0, 2);
                    BigInteger encryptedSize = BigIntegerConverter.GetBigInteger(sharedMacDataMem.Span, false, sharedMacDataMem.Length);
                    Memory<byte> encryptedData = authDataMem.Slice(2);

                    // Verify the shared mac data represents the total size of the signed data.
                    if (encryptedSize != encryptedData.Length)
                    {
                        throw new Exception("RLPx auth-ack data had invalid shared mac data. It should describe the size of the ECIES encrypted buffer.");
                    }

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
