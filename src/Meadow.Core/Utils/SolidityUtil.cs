using Meadow.Core.AbiEncoding;
using Meadow.Core.Cryptography;
using Meadow.Core.Cryptography.Ecdsa;
using Meadow.Core.EthTypes;
using Meadow.Core.RlpEncoding;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Meadow.Core.Utils
{

    public static class SolidityUtil
    {



        /// <summary>
        /// Returns a Keccak256 hash of the given message, using the prefix from eth_sign: "\u0019Ethereum Signed Message:\n{message.Length}".
        /// Message string is UTF8 decoded.
        /// </summary>
        public static byte[] HashPersonalMessage(string message)
        {
            return HashPersonalMessage(StringUtil.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Returns a Keccak256 hash of the given message, using the prefix from eth_sign: "\u0019Ethereum Signed Message:\n{message.Length}".
        /// </summary>
        public static byte[] HashPersonalMessage(Span<byte> messageBytes)
        {
            const string PREFIX = "\u0019Ethereum Signed Message:\n";
            string prefixedMessage = PREFIX + messageBytes.Length.ToString(CultureInfo.InvariantCulture);
            byte[] buffer = new byte[StringUtil.UTF8.GetByteCount(prefixedMessage) + messageBytes.Length];
            StringUtil.UTF8.GetBytes(prefixedMessage, 0, prefixedMessage.Length, buffer, 0);
            messageBytes.CopyTo(buffer.AsSpan(prefixedMessage.Length));
            var resultBuffer = new byte[KeccakHash.HASH_SIZE];
            KeccakHash.ComputeHash(buffer, resultBuffer);
            return resultBuffer;
        }

        /// <summary>
        /// Similar to the "web3.utils.soliditySha3" <see href="https://web3js.readthedocs.io/en/1.0/web3-utils.html#soliditysha3" />.
        /// Calculates the keccak256 hash of the input parameters the same way solidity would. Values are ABI encoded in non-standard packed mode.
        /// </summary>
        public static byte[] PackAndHash(params (AbiTypeInfo SolidityType, object Value)[] values)
        {
            byte[] data = AbiPack(values);
            byte[] hashedData = KeccakHash.ComputeHashBytes(data);
            return hashedData;
        }

        /// <summary>
        /// Performs an ECDSA sign on the given 32-byte message hash.
        /// Returns the signature in r,s,v format. Use <see cref="SignatureToRpcFormat(byte[], byte[], byte)"/> to convert into the serialized byte array format.
        /// </summary>       
        /// <param name="messageHash">The 32 byte message hash. Usually the output from <see cref="HashPersonalMessage(string)"/> or <see cref="KeccakHash.ComputeHash(Span{byte})"/></param>
        /// <param name="signerPrivateKeyHex">Hex encoded private key to sign with.</param>
        public static (byte[] R, byte[] S, byte V) ECSign(byte[] messageHash, string signerPrivateKeyHex)
        {
            return ECSign(messageHash, signerPrivateKeyHex.HexToBytes());
        }

        /// <summary>
        /// Performs an ECDSA sign on the given 32-byte message hash.
        /// Returns the signature in r,s,v format. Use <see cref="SignatureToRpcFormat(byte[], byte[], byte)"/> to convert into the serialized byte array format.
        /// </summary>       
        /// <param name="messageHash">The 32 byte message hash. Usually the output from <see cref="HashPersonalMessage(string)"/> or <see cref="KeccakHash.ComputeHash(Span{byte})"/></param>
        /// <param name="signerPrivateKey">The private key to sign with.</param>
        public static (byte[] R, byte[] S, byte V) ECSign(byte[] messageHash, byte[] signerPrivateKey)
        {
            if (messageHash.Length != 32)
            {
                throw new ArgumentException($"The provided message hash is not the expected length of 32 bytes.", nameof(messageHash));
            }

            EthereumEcdsa keypair = EthereumEcdsa.Create(signerPrivateKey, EthereumEcdsaKeyType.Private);

            // Sign the hash
            var signature = keypair.SignData(messageHash);

            // We want our result in r,s,v format.
            byte[] r = BigIntegerConverter.GetBytes(signature.r);
            byte[] s = BigIntegerConverter.GetBytes(signature.s);
            byte v = EthereumEcdsa.GetVFromRecoveryID(null, signature.RecoveryID);

            return (r, s, v);
        }

        /// <summary>
        /// Recovers the public key from the message hash and signature.
        /// </summary>
        /// <param name="signatureHex">Hex encoded signature of the serialized format (used by the eth_sign RPC method).</param>
        public static byte[] ECRecover(byte[] messageHash, string signatureHex)
        {
            return ECRecover(messageHash, signatureHex.HexToBytes());
        }

        /// <summary>
        /// Recovers the public key from the message hash and signature.
        /// </summary>
        /// <param name="signature">The signature into the serialized format (used by the eth_sign RPC method).</param>
        public static byte[] ECRecover(byte[] messageHash, byte[] signature)
        {
            var sig = SignatureFromRpcFormat(signature);
            return ECRecover(messageHash, sig);
        }

        /// <summary>
        /// Recovers the public key from the message hash and signature.
        /// </summary>
        public static byte[] ECRecover(byte[] messageHash, (byte[] R, byte[] S, byte V) signature)
        {
            return ECRecover(messageHash, signature.R, signature.S, signature.V);
        }

        /// <summary>
        /// Recovers the public key from the message hash and signature.
        /// </summary>
        public static byte[] ECRecover(byte[] messageHash, byte[] r, byte[] s, byte v)
        {
            BigInteger rInt = BigIntegerConverter.GetBigInteger(r);
            BigInteger sInt = BigIntegerConverter.GetBigInteger(s);

            // Verify we have a low r, s, and a valid v.
            if (rInt >= Secp256k1Curve.N)
            {
                throw new ArgumentException("Failed 'r' verification", nameof(r));
            }

            if (sInt >= Secp256k1Curve.N)
            {
                throw new ArgumentException("Failed 's' verification", nameof(s));
            }

            if (v < 27 || v > 28)
            {
                throw new ArgumentException("Failed 'v' verification", nameof(v));
            }

            // Obtain our recovery id from v.
            byte recoveryID = EthereumEcdsa.GetRecoveryIDFromV(v);

            // Try to get an address from this. If it fails, it will throw an exception.
            var key = EthereumEcdsa.Recover(messageHash, recoveryID, rInt, sInt);
            byte[] senderAddress = key.ToPublicKeyArray(compressed: false, slicedPrefix: true);
            return senderAddress;
        }

        /// <summary>
        /// Serializes the signature into the format of the eth_sign RPC method.
        /// </summary>
        public static byte[] SignatureToRpcFormat((byte[] R, byte[] S, byte V) signature)
        {
            return SignatureToRpcFormat(signature.R, signature.S, signature.V);
        }

        /// <summary>
        /// Serializes the signature into the format of the eth_sign RPC method.
        /// </summary>
        public static byte[] SignatureToRpcFormat(byte[] r, byte[] s, byte v)
        {
            var result = new byte[65];
            r.CopyTo(result, 32 - r.Length);
            s.CopyTo(result, 64 - s.Length);
            result[64] = v;
            return result;
        }

        /// <summary>
        /// Create a signature object from a serialized / RPC signature (the format of the eth_sign RPC method).
        /// </summary>
        /// <param name="signatureHex">Hex encoded signature</param>
        public static (byte[] R, byte[] S, byte V) SignatureFromRpcFormat(string signatureHex)
        {
            return SignatureFromRpcFormat(signatureHex.HexToBytes());
        }

        /// <summary>
        /// Create a signature object from a serialized / RPC signature (the format of the eth_sign RPC method).
        /// </summary>
        public static (byte[] R, byte[] S, byte V) SignatureFromRpcFormat(byte[] signature)
        {
            var r = new byte[32];
            var s = new byte[32];
            Buffer.BlockCopy(signature, 0, r, 0, 32);
            Buffer.BlockCopy(signature, 32, s, 0, 32);
            var v = signature[64];

            // Support old format of eth_sign
            if (v < 27)
            {
                v += 27;
            }

            return (r, s, v);
        }

        /// <summary>
        /// Returns the ethereum address from a public key. Accepts uncompressed and compressed formats unless sanitize is set to false.
        /// </summary>
        public static Address PublicKeyToAddress(byte[] publicKey, bool sanitize = true)
        {
            if (sanitize)
            {
                var ecdsa = EthereumEcdsa.Create(publicKey, EthereumEcdsaKeyType.Public);
                return EcdsaKeyPairToAddress(ecdsa);
            }
            else
            {
                var publicKeyHash = KeccakHash.ComputeHash(publicKey);
                var addressBytes = publicKeyHash.Slice(12, 20);
                var address = new Address(addressBytes);
                return address;
            }
        }

        /// <summary>
        /// Returns the ethereum address from a private key.
        /// </summary>
        /// <param name="privateKeyHex">Hex encoded private key.</param>
        public static Address PrivateKeyToAddress(string privateKeyHex)
        {
            return PrivateKeyToAddress(privateKeyHex.HexToBytes());
        }

        /// <summary>
        /// Returns the ethereum address from a private key.
        /// </summary>
        public static Address PrivateKeyToAddress(byte[] privateKey)
        {
            var ecdsa = EthereumEcdsa.Create(privateKey, EthereumEcdsaKeyType.Private);
            return EcdsaKeyPairToAddress(ecdsa);
        }


        public static Address EcdsaKeyPairToAddress(this EthereumEcdsa ecdsaKeyPair)
        {
            var verifiedPublicKey = ecdsaKeyPair.ToPublicKeyArray(compressed: false, slicedPrefix: true);
            return PublicKeyToAddress(verifiedPublicKey, sanitize: false);
        }

        /// <summary>
        /// Resizes a byte array to the given size, left padded with leading zero-bytes.
        /// Returns the existing array if length is already of the given size, otherwise creates a new array.
        /// </summary>
        /// <param name="value">The array to resize.</param>
        /// <param name="length">The total length of the new array.</param>
        public static byte[] PadLeft(byte[] value, int length)
        {
            if (value.Length == length)
            {
                return value;
            }

            if (value.Length > length)
            {
                throw new ArgumentException($"Input value is already greater in size '{value.Length}' than the given length '{length}'.");
            }

            var newValue = new byte[length];
            Buffer.BlockCopy(value, 0, newValue, length - value.Length, value.Length);
            return newValue;
        }

        /// <summary>
        /// Resizes a byte span to the given size, left padded with leading zero-bytes.
        /// Returns the existing array if length is already of the given size, otherwise creates a new array.
        /// </summary>
        /// <param name="value">The array to resize.</param>
        /// <param name="length">The total length of the new array.</param>
        public static Span<byte> PadLeft(Span<byte> value, int length)
        {
            if (value.Length == length)
            {
                return value;
            }

            if (value.Length > length)
            {
                throw new ArgumentException($"Input value is already greater in size '{value.Length}' than the given length '{length}'.");
            }

            Span<byte> newValue = new byte[length];
            value.CopyTo(newValue.Slice(length - value.Length));
            return newValue;
        }

        /// <param name="byteLength">The length of bytes (not string length)</param>
        public static string PadLeft(string hexString, int byteLength, bool includeHexPrefix = true)
        {
            var value = hexString.HexToBytes();
            var result = PadLeft(value, byteLength);
            var hex = result.ToHexString(hexPrefix: includeHexPrefix);
            return hex;
        }

        /// <summary>
        /// Encodes values with non-standard packed mode. Commonly used on data before hashing/signing.
        /// See <see href="https://solidity.readthedocs.io/en/v0.4.24/abi-spec.html#non-standard-packed-mode"/>
        /// This method only supports static solidity types defined in <see cref="SolidityType"/>. 
        /// To specify any type as a string use <see cref="AbiPack" />.
        /// </summary>
        public static byte[] AbiPackTypes(params (SolidityType SolidityType, object Value)[] values)
        {
            var typeStrings = values.Select(v => ((AbiTypeInfo)v.SolidityType, v.Value)).ToArray();
            return AbiPack(typeStrings);
        }

        /// <summary>
        /// Encodes values with non-standard packed mode. Commonly used on data before hashing/signing.
        /// See <see href="https://solidity.readthedocs.io/en/v0.4.24/abi-spec.html#non-standard-packed-mode"/>
        /// </summary>
        /// <param name="values">Strings and <see cref="SolidityType"/> can be used as <see cref="AbiTypeInfo"/> variables.</param>
        public static byte[] AbiPack(params (AbiTypeInfo SolidityType, object Value)[] values)
        {
            var encoders = GetEncoders(values);
            return EncoderUtil.EncodePacked(encoders);
        }

        /// <summary>
        /// Encodes values as solidity types in ABI format which is used for: function parameters, return values, and event arguments.
        /// This method only supports static solidity types defined in <see cref="SolidityType"/>. 
        /// To specify any type as a string use <see cref="AbiEncode" />.
        /// </summary>
        public static byte[] AbiEncodeTypes(params (SolidityType SolidityType, object Value)[] values)
        {
            var typeStrings = values.Select(v => ((AbiTypeInfo)v.SolidityType, v.Value)).ToArray();
            return AbiEncode(typeStrings);
        }

        /// <summary>
        /// Encodes values as solidity types in ABI format which is used for: function parameters, return values, and event arguments.
        /// </summary>        
        /// <param name="values">Strings and <see cref="SolidityType"/> can be used as <see cref="AbiTypeInfo"/> variables.</param>
        public static byte[] AbiEncode(params (AbiTypeInfo SolidityType, object Value)[] values)
        {
            var encoders = GetEncoders(values);
            return EncoderUtil.Encode(encoders);
        }

        /// <summary>
        /// Encodes values as solidity types in ABI format which is used for: function parameters, return values, and event arguments.
        /// </summary>        
        /// <param name="solidityType">Strings and <see cref="SolidityType"/> can be used as <see cref="AbiTypeInfo"/> variables.</param>
        public static byte[] AbiEncode(AbiTypeInfo solidityType, object value)
        {
            var encoders = GetEncoders((solidityType, value));
            return EncoderUtil.Encode(encoders);
        }

        public static T AbiDecode<T>(SolidityType solidityType, Span<byte> data)
        {
            return AbiDecode<T>(EnumExtensions.GetMemberValue(solidityType), data);
        }

        public static T AbiDecode<T>(string solidityType, Span<byte> data)
        {
            var obj = AbiDecode(solidityType, data);
            return TypeConversion.ConvertValue<T>(obj);
        }

        public static object AbiDecode(SolidityType solidityType, Span<byte> data)
        {
            return AbiDecode(EnumExtensions.GetMemberValue(solidityType), data);
        }

        public static object AbiDecode(AbiTypeInfo solidityType, Span<byte> data)
        {
            var encoder = EncoderFactory.LoadEncoder(solidityType);
            var buffer = new AbiDecodeBuffer(data, encoder.TypeInfo);
            encoder.DecodeObject(ref buffer, out var result);
            return result;
        }

        static IAbiTypeEncoder[] GetEncoders(params (AbiTypeInfo SolidityType, object Value)[] values)
        {
            var encoders = new IAbiTypeEncoder[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var abiEncoder = EncoderFactory.LoadEncoder(values[i].SolidityType);
                abiEncoder.SetValue(values[i].Value);
                encoders[i] = abiEncoder;
            }

            return encoders;
        }

    }
}
