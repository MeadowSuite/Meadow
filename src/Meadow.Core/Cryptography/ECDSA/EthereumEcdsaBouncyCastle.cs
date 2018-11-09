using Meadow.Core.AccountDerivation;
using Meadow.Core.Utils;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Meadow.Core.Cryptography.Ecdsa
{
    /// <summary>
    /// ECDSA cryptographic provider that accomodates for Ethereum signing standards, using the managed BouncyCastle library.
    /// </summary>
    public class EthereumEcdsaBouncyCastle : EthereumEcdsa
    {
        /// <summary>
        /// Algorithm to use in Bouncy Castle's ECDSA providers.
        /// </summary>
        private const string ALGORITHM = "EC";

        // Managed
        /// <summary>
        /// Random secure data provider for cryptographic operations.
        /// </summary>
        private static SecureRandom _secureRandom;

        /// <summary>
        /// Public key parameters for ECDSA, always available.
        /// </summary>
        public ECPublicKeyParameters PublicKey { get; private set; }

        /// <summary>
        /// Private key parameters for ECDSA, available if this is a private key instance only.
        /// </summary>
        public ECPrivateKeyParameters PrivateKey { get; private set; }

        static EthereumEcdsaBouncyCastle()
        {
            // Initialize a new secure random provider.
            _secureRandom = new SecureRandom();
        }

        public EthereumEcdsaBouncyCastle(Memory<byte> key, EthereumEcdsaKeyType keyType)
        {
            // Set the key type
            KeyType = keyType;

            // Instantiate our key depending on what type of key it is.
            if (KeyType == EthereumEcdsaKeyType.Public)
            {
                // If it's only 64 bytes, we need to add our prefix. (Source: https://en.bitcoin.it/wiki/Elliptic_Curve_Digital_Signature_Algorithm)
                if (key.Length == PUBLIC_KEY_SIZE)
                {
                    key = new byte[] { 0x4 }.Concat(key.ToArray());
                }

                // Obtain our public key parameters by using the provided quotient.
                ECPoint q = Secp256k1Curve.Parameters.Curve.DecodePoint(key.ToArray());
                PublicKey = new ECPublicKeyParameters(ALGORITHM, q, Secp256k1Curve.DomainParameters);
            }
            else
            {
                // Obtain our private key parameters 
                Org.BouncyCastle.Math.BigInteger keyInt = new Org.BouncyCastle.Math.BigInteger(1, key.ToArray());
                PrivateKey = new ECPrivateKeyParameters(ALGORITHM, keyInt, Secp256k1Curve.DomainParameters);

                // Obtain Q from our private key.
                ECPoint q = Secp256k1Curve.Parameters.G.Multiply(PrivateKey.D);
                PublicKey = new ECPublicKeyParameters(ALGORITHM, q, Secp256k1Curve.DomainParameters);
            }
        }

        static EthereumEcdsaBouncyCastle GenerateSingle(uint accountIndex, IAccountDerivation accountFactory)
        {
            var privateKey = accountFactory.GeneratePrivateKey(accountIndex);
            var keyBigInt = BigIntegerConverter.GetBigInteger(privateKey, signed: false, byteCount: PRIVATE_KEY_SIZE);
            keyBigInt = Secp256k1Curve.EnforceLowS(keyBigInt);

            // Return our private key instance.
            return new EthereumEcdsaBouncyCastle(privateKey, EthereumEcdsaKeyType.Private);
        }

        public static new EthereumEcdsaBouncyCastle Generate(IAccountDerivation accountFactory)
        {
            return GenerateSingle(0, accountFactory);
        }

        public static new IEnumerable<EthereumEcdsaBouncyCastle> Generate(int count, IAccountDerivation accountFactory)
        {
            for (uint i = 0; i < count; i++)
            {
                yield return GenerateSingle(i, accountFactory);
            }
        }

        /// <summary>
        /// Creates an ECDSA instance by recovering a public key given a hash, recovery ID, and r and s components of the resulting signature of the hash. Throws an exception if recovery is not possible.
        /// </summary>
        /// <param name="hash">The hash of the data which was signed.</param>
        /// <param name="recoveryId">The recovery ID of ECDSA during signing.</param>
        /// <param name="ecdsa_r">The r component of the ECDSA signature for the provided hash.</param>
        /// <param name="ecdsa_s">The s component of the ECDSA signature for the provided hash.</param>
        /// <returns>Returns the quotient/public key which was used to sign this hash.</returns>
        public static new EthereumEcdsaBouncyCastle Recover(Span<byte> hash, byte recoveryId, BigInteger ecdsa_r, BigInteger ecdsa_s)
        {
            // Source: http://www.secg.org/sec1-v2.pdf (Section 4.1.6 - Public Key Recovery Operation)

            // Recovery ID must be between 0 and 4 (0 and 1 is all that should be used, but we support multiple cases in case)
            if (recoveryId < 0 || recoveryId > 3)
            {
                throw new ArgumentException($"ECDSA public key recovery must have a v parameter between [0, 3]. Value provided is {recoveryId.ToString(CultureInfo.InvariantCulture)}");
            }

            // NOTES:
            // First bit of recoveryID being set means y is odd, otherwise it is even.
            // The second bit indicates which item of the two to choose.

            // If the hash is null, we'll assume it's a zero length byte array
            if (hash == null)
            {
                hash = Array.Empty<byte>();
            }

            // Obtain our elliptic curve parameters
            Org.BouncyCastle.Math.BigInteger r = ecdsa_r.ToBouncyCastleBigInteger();
            Org.BouncyCastle.Math.BigInteger s = ecdsa_s.ToBouncyCastleBigInteger();
            Org.BouncyCastle.Math.BigInteger j = Org.BouncyCastle.Math.BigInteger.ValueOf((long)recoveryId >> 1);
            Org.BouncyCastle.Math.BigInteger x = j.Multiply(Secp256k1Curve.Parameters.N).Add(r);

            // Verify our x coordinate is less than our curve's modulo p (aka curve Q)
            if (Secp256k1Curve.Parameters.Curve.Field.Characteristic.CompareTo(x) <= 0)
            {
                throw new ArgumentException("ECDSA signature's X coordinate cannot exceeded the modulo divisor.");
            }

            // To obtain our curve point R, we decode it with an extra byte for Y descriptor which mentions if Y is even or odd.
            int curveLength = X9IntegerConverter.GetByteLength(Secp256k1Curve.Parameters.Curve);
            byte[] xdata = X9IntegerConverter.IntegerToBytes(x, curveLength + 1);
            xdata[0] = (byte)(0x2 | (recoveryId & 1));
            ECPoint r1 = Secp256k1Curve.Parameters.Curve.DecodePoint(xdata);

            // nR should be infinity.
            if (!r1.Multiply(Secp256k1Curve.Parameters.N).IsInfinity)
            {
                throw new ArgumentException("ECDSA's nR should be the point at infinity.");
            }

            // We obtain an integer representation of our hash.
            Org.BouncyCastle.Math.BigInteger e = new Org.BouncyCastle.Math.BigInteger(1, hash.ToArray());

            // Next we'll want the multiplicative inverse of r (~r)
            Org.BouncyCastle.Math.BigInteger rInverse = r.ModInverse(Secp256k1Curve.Parameters.N);

            // Next we get the additive inverse of our hash, subtracting it from zero, and bounding it accordingly.
            Org.BouncyCastle.Math.BigInteger eAddInverse = Org.BouncyCastle.Math.BigInteger.Zero.Subtract(e).Mod(Secp256k1Curve.Parameters.N);

            // Using the inverse of r we have, we can multiply it by s to get our ~r * s.
            Org.BouncyCastle.Math.BigInteger rsInverse = rInverse.Multiply(s).Mod(Secp256k1Curve.Parameters.N);

            // Using the inverse of r we have, and the inverse of e, we can have ~r * -e
            Org.BouncyCastle.Math.BigInteger reInverse = rInverse.Multiply(eAddInverse).Mod(Secp256k1Curve.Parameters.N);

            // Q = ((~r * s) * R) + ((~r * -e) * G) => (~r * sR) + (~r * -eG) => ~r (sR - eG)
            ECPoint q = ECAlgorithms.SumOfTwoMultiplies(Secp256k1Curve.Parameters.G, reInverse, r1, rsInverse).Normalize();

            // Obtain our public key from this
            return new EthereumEcdsaBouncyCastle(Secp256k1Curve.Parameters.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger()).GetEncoded(false), EthereumEcdsaKeyType.Public);

        }

        /// <summary>
        /// Obtains the binary data representation of our public key.
        /// </summary>
        /// <returns>Returns a binary data representation of the public key.</returns>
        public override byte[] ToPublicKeyArray(bool compressed = false, bool slicedPrefix = true)
        {
            // Throw an error if trying to slice prefix off of compressed public key
            if (compressed && slicedPrefix)
            {
                throw new ArgumentException("Should not be slicing the prefix off of a compressed public key, as compressed keys solely include X and Y is derived using the prefix.");
            }

            // Obtain our bytes for Q.
            byte[] q = Secp256k1Curve.Parameters.Curve.CreatePoint(PublicKey.Q.XCoord.ToBigInteger(), PublicKey.Q.YCoord.ToBigInteger()).GetEncoded(compressed);

            // Slice the prefix off of it.
            if (slicedPrefix)
            {
                q = q.Slice(1);
            }

            return q;
        }

        /// <summary>
        /// Obtains the binary data representation of our private key.
        /// </summary>
        /// <returns>Returns a binary data representation of the private key.</returns>
        public override byte[] ToPrivateKeyArray()
        {
            // Verify we have a private key.
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw _notPrivateKeyException;
            }

            // Return D.
            return PrivateKey.D.ToByteArray();
        }

        /// <summary>
        /// Verifies a hash was signed correctly given the r and s signature components.
        /// </summary>
        /// <param name="hash">The hash which was signed.</param>
        /// <param name="r">The ECDSA signature component r.</param>
        /// <param name="s">The ECDSA signature component s.</param>
        /// <returns>Returns a boolean indicating whether the data was properly signed.</returns>
        public override bool VerifyData(Span<byte> hash, BigInteger r, BigInteger s)
        {
            // Initialize a bouncy castle ECDSA signer.
            ECDsaSigner provider = new ECDsaSigner();
            provider.Init(false, PublicKey);

            // Verify our R and S signature components given the hash we signed.
            return provider.VerifySignature(hash.ToArray(), r.ToBouncyCastleBigInteger(), s.ToBouncyCastleBigInteger());
        }


        /// <summary>
        /// Signs given data and returns the r and s components of the ECDSA signature, along with a recovery ID to recover the public key given the original signed message and the returned components.
        /// </summary>
        /// <param name="hash">The hash to be signed.</param>
        /// <returns>Returns r and s components of an ECDSA signature, along with a recovery ID to recover the signers public key given the original signed message and r, s.</returns>
        public override (byte RecoveryID, BigInteger r, BigInteger s) SignData(Span<byte> hash)
        {
            // Verify we have a private key.
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw _notPrivateKeyException;
            }

            // Initialize our crypto provider.
            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(true, PrivateKey);

            // Obtain our signature, rs[0] ("r") and rs[1] ("s")
            Org.BouncyCastle.Math.BigInteger[] rs = signer.GenerateSignature(hash.ToArray());

            // We want to make sure we enforce a low S value
            rs[1] = Secp256k1Curve.EnforceLowS(rs[1]);

            // We need to return a valid recovery ID for this signature. We do this by trying all of our 4 possible recovery IDs to make sure the public key hash recovered is the same as ours.

            // We start by obtaining our current public key hash
            byte[] actualPublicKeyHash = GetPublicKeyHash();

            // Next we try of our potential recovery IDs until we can obtain the matching public key from the signature.
            // 2, 3 are usually not used and 0, 1 denote odd or even Y, which can be figured out.
            for (byte recoveryID = 0; recoveryID < 4; recoveryID++)
            {
                // We wrap this in a try in case, as we know one of these IDs will work.
                try
                {
                    EthereumEcdsa possibleMatch = Recover(hash, recoveryID, rs[0].ToNumericsBigInteger(), rs[1].ToNumericsBigInteger());
                    if (actualPublicKeyHash.ValuesEqual(possibleMatch.GetPublicKeyHash()))
                    {
                        return (recoveryID, rs[0].ToNumericsBigInteger(), rs[1].ToNumericsBigInteger());
                    }
                }
                catch { continue; }
            }

            // If we couldn't find a recovery ID, we throw an exception.
            throw new Exception("Could not obtain a valid Recovery ID for the signature.");
        }

        /// <summary>
        /// Computes a shared secret among two keys using ECDH. Assumes this instance is of the private key, and requires a public key as input.
        /// </summary>
        /// <param name="publicKey">The public key to compute a shared secret for, using this current private key.</param>
        /// <returns>Returns a computed shared secret using this private key with the provided public key. Throws an exception if this instance is not a private key and the provided argument is not a public key.</returns>
        public override byte[] ComputeECDHKey(EthereumEcdsa publicKey)
        {
            // Verify the types of keys
            if (KeyType != EthereumEcdsaKeyType.Private)
            {
                throw new ArgumentException("Could not calculate ECDH shared secret because called upon key was not a private key.");
            }

            // If this public key isn't already this same managed type object, create one.
            if (!(publicKey is EthereumEcdsaBouncyCastle))
            {
                publicKey = new EthereumEcdsaBouncyCastle(publicKey.ToPublicKeyArray(), EthereumEcdsaKeyType.Public);
            }

            // Obtain the public key parameters from the managed public key
            ECPublicKeyParameters pubKeyParams = ((EthereumEcdsaBouncyCastle)publicKey).PublicKey;

            // Create an ECDH provider
            ECDHBasicAgreement ecdhAgreement = new ECDHBasicAgreement();
            ecdhAgreement.Init(PrivateKey);

            // Calculate the agreement
            BigInteger agreementInt = ecdhAgreement.CalculateAgreement(pubKeyParams).ToNumericsBigInteger();

            // Obtain a data representation of the agreement.
            byte[] sharedSecret = BigIntegerConverter.GetBytes(agreementInt, ECDH_SHARED_SECRET_SIZE);

            // Return the computed shared secret.
            return sharedSecret;
        }
    }
}
