using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
using Meadow.Networking.Cryptography;
using Meadow.Networking.Protocol.RLPx.Messages;
using System;
using System.Collections.Generic;
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
        #endregion

        #region Fields
        private RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        private bool _usingEip8Auth;
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

        public EthereumEcdsa RemoteEphermalPublicKey { get; private set; }

        /// <summary>
        /// Indicates whether or not this session is using EIP8. (For the initiator, this means creating EIP8 auth, for receiver, this means we received an EIP8 auth).
        /// </summary>
        public bool UsingEIP8Authentication
        {
            get
            {
                return _usingEip8Auth;
            }
            set
            {
                // If this session is of the role of initiator, we can configure if we want to send an EIP8 packet.
                // Receiver processes any kind it gets.
                if (Role == RLPxSessionRole.Initiator)
                {
                    _usingEip8Auth = value;
                }
            }
        }
        /*
        * TODO: Move this and UsingEIP8Authentication into network configuration.
        */
        public uint? ChainId { get; }
        #endregion

        #region Constructor
        public RLPxSession(RLPxSessionRole role, EthereumEcdsa localPrivateKey, EthereumEcdsa ephemeralPrivateKey)
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
                throw new Exception("RLPx Authentication data should only be created by the initiator.");
            }

            // Create a new authentication message based off of our configured setting.
            RLPxAuthBase authenticationMessage = null;
            if (UsingEIP8Authentication)
            {
                // Create our EIP8 authentication message.
                authenticationMessage = new RLPxAuthEIP8()
                {
                    Nonce = nonce,
                    Version = 4,
                };
            }
            else
            {
                // Create our standard authentication message.
                authenticationMessage = new RLPxAuthStandard()
                {
                     Nonce = nonce,
                };
            }

            // Sign the authentication message
            authenticationMessage.Sign(LocalPrivateKey, EphemeralPrivateKey, receiverPublicKey, ChainId);

            // Serialize the authentication data
            byte[] serializedData = authenticationMessage.Serialize();

            // Encrypt the data accordingly (standard uses no shared mac data, EIP8 has 2 bytes which prefix the resulting encrypted data).
            if (UsingEIP8Authentication)
            {
                // Generate two bytes of random mac data.
                byte[] sharedMacData = new byte[2];
                _randomNumberGenerator.GetBytes(sharedMacData);

                // Encrypt the serialized data with the shared mac data, and return the result prefixed with it.
                byte[] encryptedSerializedData = Ecies.Encrypt(receiverPublicKey, serializedData, sharedMacData);
                return sharedMacData.Concat(encryptedSerializedData);
            }
            else
            {
                // Encrypt it using ECIES and return it without any shared mac data.
                return Ecies.Encrypt(receiverPublicKey, serializedData, null);
            }
        }

        /// <summary>
        /// Verifies the provided encrypted serialized authentication data received from the initiator.
        /// If verification fails, an appropriate exception will be thrown. If no exception is thrown,
        /// the verification has succeeded.
        /// </summary>
        /// <param name="authenticationData">The encrypted serialized authentication data to verify.</param>
        public void VerifyAuthentication(byte[] authenticationData)
        {
            // Verify this is the responder role.
            if (Role != RLPxSessionRole.Responder)
            {
                throw new Exception("RLPx Authentication data should only be verified by the responder.");
            }

            // Try to deserialize the data as a standard authentication packet.
            // The data is currently encrypted serialized data, so it will also need to be decrypted.
            RLPxAuthBase authenticationMessage = null;
            try
            {
                // The authentication data can simply be decrypted without any shared mac.
                byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, authenticationData, null);

                // Try to parse the decrypted authentication data.
                authenticationMessage = new RLPxAuthStandard(decryptedAuthData);
                _usingEip8Auth = false;
            }
            catch (Exception authStandardEx)
            {
                try
                {
                    // EIP8 has the first two bytes as the shared mac data, and the remainder is the data to decrypt.
                    Memory<byte> authDataMem = authenticationData;

                    // Split the shared mac from the authentication data
                    byte[] decryptedAuthData = Ecies.Decrypt(LocalPrivateKey, authDataMem.Slice(2), authDataMem.Slice(0, 2));

                    // Try to parse the decrypted authentication data.
                    authenticationMessage = new RLPxAuthEIP8(decryptedAuthData);
                    _usingEip8Auth = true;
                }
                catch (Exception authEip8Ex)
                {
                    string exceptionMessage = "Could not deserialize RLPx Authentication data in either standard or EIP8 format.\r\n";
                    exceptionMessage += $"Standard Authentication Error: {authStandardEx.Message}\r\n";
                    exceptionMessage += $"EIP8 Authentication Error: {authEip8Ex.Message}";
                    throw new Exception(exceptionMessage);
                }
            }

            // Try to recover the public key (with the "receiver" private key, which in this case is our local private key, since our role is responder).
            // If this fails, it will throw an exception as we expect this method to if any failure occurs.
            (EthereumEcdsa remoteEphermalPublicKey, uint? chainId) = authenticationMessage.RecoverDataFromSignature(LocalPrivateKey);
            RemoteEphermalPublicKey = remoteEphermalPublicKey;

            // TODO: Verify the chain id.
        }
        #endregion
    }
}
