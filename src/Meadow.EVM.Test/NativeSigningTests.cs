using Meadow.Core.Cryptography;
using Meadow.Core.Utils;
using Meadow.EVM.Configuration;
using Meadow.EVM.Data_Types;
using Meadow.EVM.Data_Types.Addressing;
using Meadow.EVM.Data_Types.State;
using Meadow.EVM.Data_Types.Transactions;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Meadow.EVM.Test
{
    public class NativeSigningTests
    {

        [Fact]
        public void SigningTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                Span<byte> signature = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                Span<byte> messageHash = new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f };
                Span<byte> secretKey = "e815acba8fcf085a0b4141060c13b8017a08da37f2eb1d6a5416adbb621560ef".HexToBytes();

                bool result = secp256k1.SignRecoverable(signature, messageHash, secretKey);
                Assert.True(result);

                // Recover the public key
                Span<byte> publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
                result = secp256k1.Recover(publicKeyOutput, signature, messageHash);
                Assert.True(result);

                // Serialize the public key
                Span<byte> serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                result = secp256k1.PublicKeySerialize(serializedKey, publicKeyOutput);
                Assert.True(result);

                // Slice off any prefix.
                serializedKey = serializedKey.Slice(serializedKey.Length - Secp256k1.PUBKEY_LENGTH);

                Assert.Equal("0x3a2361270fb1bdd220a2fa0f187cc6f85079043a56fb6a968dfad7d7032b07b01213e80ecd4fb41f1500f94698b1117bc9f3335bde5efbb1330271afc6e85e92", serializedKey.ToHexString(true), true);

                // Verify we could obtain the correct sender from the signature.
                Span<byte> senderAddress = KeccakHash.ComputeHash(serializedKey).Slice(KeccakHash.HASH_SIZE - Address.ADDRESS_SIZE);
                Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", senderAddress.ToArray().ToHexString(true), true);

                // Verify it works with variables generated from our managed code.
                BigInteger ecdsa_r = BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637", CultureInfo.InvariantCulture);
                BigInteger ecdsa_s = BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683", CultureInfo.InvariantCulture);
                byte recoveryId = 1;

                byte[] ecdsa_r_bytes = BigIntegerConverter.GetBytes(ecdsa_r);
                byte[] ecdsa_s_bytes = BigIntegerConverter.GetBytes(ecdsa_s);
                signature = ecdsa_r_bytes.Concat(ecdsa_s_bytes);

                // Allocate memory for the signature and create a serialized-format signature to deserialize into our native format (platform dependent, hence why we do this).
                Span<byte> serializedSignature = ecdsa_r_bytes.Concat(ecdsa_s_bytes);
                signature = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                result = secp256k1.RecoverableSignatureParseCompact(signature, serializedSignature, recoveryId);
                if (!result)
                {
                    throw new Exception("Unmanaged EC library failed to parse serialized signature.");
                }

                // Recover the public key
                publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
                result = secp256k1.Recover(publicKeyOutput, signature, messageHash);
                Assert.True(result);


                // Serialize the public key
                serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                result = secp256k1.PublicKeySerialize(serializedKey, publicKeyOutput);
                Assert.True(result);

                // Slice off any prefix.
                serializedKey = serializedKey.Slice(serializedKey.Length - Secp256k1.PUBKEY_LENGTH);

                // Assert our key
                Assert.Equal("0x3a2361270fb1bdd220a2fa0f187cc6f85079043a56fb6a968dfad7d7032b07b01213e80ecd4fb41f1500f94698b1117bc9f3335bde5efbb1330271afc6e85e92", serializedKey.ToHexString(true), true);

                //senderAddress = EthereumEcdsa.Recover(messageHash.ToArray(), recoveryId, ecdsa_r, ecdsa_s).GetPublicKeyHash();
                //senderAddress = senderAddress.Slice(KeccakHash.HASH_SIZE - Address.ADDRESS_SIZE);
                //Assert.Equal("0x75c8aa4b12bc52c1f1860bc4e8af981d6542cccd", senderAddress.ToArray().ToHexString(true), true);
            }
        }
    }
}
