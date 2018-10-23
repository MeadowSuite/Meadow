using Meadow.Core.Cryptography.Ecdsa;
using System;

namespace Meadow.Networking
{
    /// <summary>
    /// Implements the Elliptic Curve Integrated Encryption Scheme ("ECIES"), an asymmetrical encryption scheme.
    /// </summary>
    public abstract class Ecies
    {
        #region Properties

        #endregion

        #region Functions
        public static byte[] Encrypt(EthereumEcdsa receiverPublicKey, byte[] data, byte[] sharedMacData)
        {
            // Split our data into its individual components.
            var test = Secp256k1Curve.DomainParameters.Curve.FieldSize;

            return null;
        }

        public static byte[] Decrypt(EthereumEcdsa privateKey, byte[] encryptedData, byte[] sharedMacData)
        {
            // Verify our provided key type
            if (privateKey.KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("ECIES could not decrypt data because the provided key was not a private key.");
            }

            return null;
        }
        #endregion
    }
}
