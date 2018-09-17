using System;

namespace Meadow.Core.AccountDerivation
{
    public delegate byte[] GenerateAccountKeyDelegate(uint accountIndex);

    public interface IAccountDerivation
    {
        byte[] GeneratePrivateKey(uint accountIndex);
    }

    public abstract class AccountDerivationBase : IAccountDerivation
    {
        public virtual byte[] Seed { get; set; }

        public abstract byte[] GeneratePrivateKey(uint accountIndex);

        class AccountDerivationFactory : AccountDerivationBase
        {
            GenerateAccountKeyDelegate _generateAccount;

            public AccountDerivationFactory(GenerateAccountKeyDelegate generateAccount)
            {
                _generateAccount = generateAccount;
            }

            public override byte[] GeneratePrivateKey(uint accountIndex)
            {
                return _generateAccount(accountIndex);
            }
        }

        public static IAccountDerivation Create(GenerateAccountKeyDelegate generateAccount)
        {
            return new AccountDerivationFactory(generateAccount);
        }
    }



}
