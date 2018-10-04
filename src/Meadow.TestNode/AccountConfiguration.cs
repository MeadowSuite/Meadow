using Meadow.Core.AccountDerivation;

namespace Meadow.TestNode
{
    public class AccountConfiguration
    {
        /// <summary>
        /// The number of accounts to generate.
        /// Defaults to 100.
        /// </summary>
        public int AccountGenerationCount { get; set; } = 100;

        /// <summary>
        /// The balance (in ether) that accounts should be initially given.
        /// Defaults to 2000.
        /// </summary>
        public decimal DefaultAccountEtherBalance { get; set; } = 2000;

        /// <summary>
        /// The specification/format to use for generating accounts from the seed.
        /// Defaults to <see cref="Bip44AccountDerivation"/>
        /// </summary>
        public IAccountDerivation AccountDerivationMethod { get; set; }

    }
}
