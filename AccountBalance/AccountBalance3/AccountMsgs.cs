using System;
using ReactiveDomain.Messaging;

namespace AccountBalance3 {
    public class AccountMsgs
    {
        public class CreateAccount : Command
        {
            public readonly Guid AccountId;

            public CreateAccount(
                Guid accountId,
                CorrelatedMessage source)
                : base(source)
            {
                AccountId = accountId;
            }
        }

        public class AccountCreated : Event
        {
            public readonly Guid AccountId;

            public AccountCreated(
                Guid accountId,
                CorrelatedMessage source)
                : base(source ?? NewRoot())
            {
                AccountId = accountId;
            }
        }
        public class Debit : Message
        {
            public readonly uint Amount;
            public Debit(uint amount)
            {
                Amount = amount;
            }
        }

        public class Credit : Message
        {
            public readonly uint Amount;

            public Credit(uint amount)
            {
                Amount = amount;
            }
        }
    }
}