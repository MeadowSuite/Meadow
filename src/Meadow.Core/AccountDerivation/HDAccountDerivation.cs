using Meadow.Core.AccountDerivation.BIP39;

namespace Meadow.Core.AccountDerivation
{
    /// <summary>
    /// Acount derivation using BIP44 with Ethereum's SLIP44 registered coin index "60".
    /// </summary>
    public class HDAccountDerivation : Bip44AccountDerivation
    {
        public HDAccountDerivation(MnemonicPhrase mnemonicPhrase) 
            : base(mnemonicPhrase, coinType: 60)
        {

        }


        public HDAccountDerivation(string mnemonicPhrase)
            : this(new MnemonicPhrase(mnemonicPhrase)) { }

        public static HDAccountDerivation Create(WordListLanguage language = WordListLanguage.English)
        {
            return new HDAccountDerivation(new MnemonicPhrase(language));
        }

    }

}
