using Meadow.Core.AccountDerivation.BIP32;
using Meadow.Core.AccountDerivation.BIP39;
using Meadow.Core.EthTypes;
using Meadow.Core.Utils;
using System;
using System.Globalization;
using Xunit;

namespace Meadow.Core.Test
{
    public class BipTests
    {
        [Fact]
        public void BIP39_GenerateMnemonic()
        {
            // Generate 10 different phrases.
            for (int i = 0; i < 10; i++)
            {
                // Generate a mnemonic and verify it.
                MnemonicPhrase mnemonic = new MnemonicPhrase(WordListLanguage.English, null);
                Assert.True(mnemonic.Verify());

                // Parse the generated mnemonic from a string and verify it.
                MnemonicPhrase mnemonicCopy = new MnemonicPhrase(mnemonic.MnemonicString);
                Assert.True(mnemonicCopy.Verify());
            }
        }

        [Fact]
        public void BIP39_TestSeeds()
        {
            // Create a mnemonic of size 0x10 and verify it.
            MnemonicPhrase mnemonic1 = new MnemonicPhrase(WordListLanguage.English, new byte[0x10]);
            Assert.True(mnemonic1.Verify());

            // Derive seed from our data.
            byte[] seed = mnemonic1.DeriveKeySeed("testTestTest!!!");
            Assert.Equal("A424DD4B5DBEB58B44B36DB439FEC78D77853563234FFEE8C2912C4BD32D29DF2C5828728FDC6EF77AEC1CB7271A9D7C04D86A6642B1B0A2D6A9048CC636755A", seed.ToHexString(), StringComparer.InvariantCultureIgnoreCase);

            // Create a mnemonic of size 0x20 and verify it.
            MnemonicPhrase mnemonic2 = new MnemonicPhrase(WordListLanguage.English, new byte[0x20]);
            Assert.True(mnemonic2.Verify());

            // Derive seed from our data.
            seed = mnemonic2.DeriveKeySeed("totallyDifferentPassword?");
            Assert.Equal("95DC7BE1A5362EF01D970BE636665276F15946F7C77D46A54423A1C068B435981369F879DB50E6B1D3291A4821F8F78FF433E97AE850F8244770A22A772479A2", seed.ToHexString(), StringComparer.InvariantCultureIgnoreCase);

            // Create a mnemonic from a mnemonic string.
            MnemonicPhrase mnemonic3 = new MnemonicPhrase("abandon math mimic master filter design carbon cactus bachelor bag speed print guess session goat acquire captain olive best smooth joy erode able despair");
            Assert.True(mnemonic3.Verify());

            // Derive seed from our data.
            seed = mnemonic3.DeriveKeySeed("staticMnemonicStringPassword...#*($10@");
            Assert.Equal("78A7C9E99AF640D818EB5D990E1C2F0BF91E68C20BED59E5EBA38A483B1D1ECBB2ED8DF31DED0164048A23F30EBF7E6B6F2A8D06584741D36DD36D4177DC04F6", seed.ToHexString(), StringComparer.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void BIP32_Test()
        {
            // Obtain a static seed.
            byte[] seed = "3E6888C1747ED36CA68AAE2FD8F3A46E8AD2AE9BD545FA7F5AABD00FB14198F4B13A2D07DD7C720A57EF0BDC293EE245C28D8B7CA68029F50B58F8D7DCCBA15F".HexToBytes();

            // Derive an extended key from this
            ExtendedKey key = new ExtendedKey(seed);

            // Derive private key -> child private key -> child private key -> child hardened private key.
            var derived = key.GetChildKey(20).GetChildKey(40).GetChildKey(0x80000020);
            Assert.Equal("D26812D4F2E44A2E663B00314167DE30DF0DA771D45E85977C7AC2CA33291183", derived.InternalKey.ToPrivateKeyArray().ToHexString(), StringComparer.InvariantCultureIgnoreCase);

            // Derive private key -> public key -> child public key.
            var derivedPubKeyLater = derived.GetExtendedPublicKey().GetChildKey(30);
            Assert.Equal("0302BF19F1F90603E6DEE1630AB997B31742043CCFA7452A94BA1B686DCDE19A88", derivedPubKeyLater.InternalKey.ToPublicKeyArray(true, false).ToHexString(), StringComparer.InvariantCultureIgnoreCase);

            // Public keys cannot derive hardened children (private key must be known).
            Assert.ThrowsAny<ArgumentException>(() => { derivedPubKeyLater.GetChildKey(0x80000020); });

            // Rebuild our key using a child and our get parent key function.
            var immediateChildKey = key.GetChildKey(20);
            var recreatedKey = immediateChildKey.GetParentPrivateKey(key.GetExtendedPublicKey());

            // Verify our rebuilt key.
            Assert.Equal(key.InternalKey.ToPrivateKeyArray().ToHexString(), recreatedKey.InternalKey.ToPrivateKeyArray().ToHexString());
        }

        [Fact]
        public void BIP44_Mnemonic_HDKeys_Test()
        {
            // Obtain our master key from a mnemonic.
            MnemonicPhrase mnemonicPhrase = new MnemonicPhrase("benefit already weapon attract visit kiss favorite blouse matter impulse noodle earth");
            ExtendedKey extendedKey = new ExtendedKey(mnemonicPhrase.DeriveKeySeed());

            // Define the key path
            string keyPath = "m/44'/60'/0'/0/account_index";

            // Define our expected results.
            string[] expectedResultAddresses =
            {
                "0x02032d303958511779aAFEE078434b2413d24bC6",
                "0x768aB38a2f1a91234cee8A18714d73C665903c5a",
                "0x21891E5f8c42a4a3B65114De8C8D33102fbD8b8e",
                "0x48B187AA0D5ef22a52c2213aDb27E554d065f08C",
                "0x10Fdd87FCFf55653854B8eE94a70E0F365E55EC2",
                "0x219ef73681A4Cf864E23467c4EcBBf070215372d",
                "0xCE7d33a554f15B41504b17022A38cB7038b74B1f",
                "0x5942181baD62B159072db7538CCA4F70B5Cdb6D6",
                "0xa7925fAfF5402cE3f9D40CF2335B87377FaE1118",
                "0x63526C9C7137d83cd49DB6231c6383C99e5949e8",
            };

            // Verify all of our addresses.
            for (int i = 0; i < expectedResultAddresses.Length; i++)
            {
                // Obtain our indexed key path for this mnemonic.
                string indexedKeyPath = keyPath.Replace("account_index", i.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);

                // Obtain our indexed key
                ExtendedKey indexedKey = extendedKey.GetChildKey(new KeyPath(indexedKeyPath));

                // Obtain our public key hash
                byte[] addressBytes = indexedKey.InternalKey.GetPublicKeyHash();
                addressBytes = addressBytes.Slice(addressBytes.Length - Address.SIZE);

                // Verify our address matches our expected result.
                Assert.Equal(expectedResultAddresses[i], addressBytes.ToHexString(true), StringComparer.InvariantCultureIgnoreCase);
            }
        }
    }
}
