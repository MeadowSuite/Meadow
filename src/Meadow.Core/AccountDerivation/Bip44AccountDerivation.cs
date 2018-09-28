using Meadow.Core.AccountDerivation.BIP32;
using Meadow.Core.AccountDerivation.BIP39;
using System;
using System.Globalization;

namespace Meadow.Core.AccountDerivation
{
    /// <summary>
    /// Multi-Account Hierarchy for Deterministic Wallets.
    /// https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki
    /// </summary>
    public class Bip44AccountDerivation : IAccountDerivation
    {
        // the HD path without the last component (the account index).
        readonly string _pathPrefix;

        readonly ExtendedKey _extendedKey;

        public MnemonicPhrase _phrase;

        /// <summary>
        /// Returns the mnemonic phrase string used.
        /// </summary>
        public string MnemonicPhrase => _phrase.MnemonicString;

        public Bip44AccountDerivation(MnemonicPhrase mnemonicPhrase, uint coinType, uint account = 0, uint change = 0, string password = null)
        {
            var coinTypeIndex = coinType.ToString(CultureInfo.InvariantCulture);
            var accountIndex = account.ToString(CultureInfo.InvariantCulture);
            var changeIndex = change.ToString(CultureInfo.InvariantCulture);
            _pathPrefix = $"m/44'/{coinTypeIndex}'/{accountIndex}'/{changeIndex}/";

            _phrase = mnemonicPhrase;
            _extendedKey = new ExtendedKey(mnemonicPhrase.DeriveKeySeed(password));
        }

        public byte[] GeneratePrivateKey(uint accountIndex)
        {
            // Obtain our indexed key path for this mnemonic.
            string indexedKeyPath = _pathPrefix + accountIndex.ToString(CultureInfo.InvariantCulture);

            // Obtain our indexed key
            ExtendedKey indexedKey = _extendedKey.GetChildKey(new KeyPath(indexedKeyPath));

            // Obtain our private key
            byte[] privateKey = indexedKey.InternalKey.ToPrivateKeyArray();

            return privateKey;
        }

    }
}