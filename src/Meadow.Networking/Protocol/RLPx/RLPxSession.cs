using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.Utils;
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
    public class RLPxSession
    {
        #region Constants
        private const int NONCE_SIZE = 32;
        #endregion

        #region Fields
        private static RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();
        #endregion

        #region Properties
        public EthereumEcdsa LocalPrivateKey { get; }
        public EthereumEcdsa EphemeralPrivateKey { get; }
        #endregion

        #region Constructor
        public RLPxSession(EthereumEcdsa localPrivateKey, EthereumEcdsa ephemeralPrivateKey)
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
        }
        #endregion

        #region Functions

        #endregion
    }
}
