using System;
using Newtonsoft.Json;
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
                : base(source)
            {
                AccountId = accountId;
            }

            [JsonConstructor]
            public AccountCreated(
                Guid accountId,
                CorrelationId correlationId,
                SourceId sourceId)
                : base(correlationId, sourceId)
            {
                AccountId = accountId;
            }
        }

        public class DebitAccount : Command
        {
            public readonly Guid AccountId;
            public readonly uint Amount;

            public DebitAccount(
                Guid accountId,
                uint amount,
                CorrelatedMessage source)
                : base(source)
            {
                AccountId = accountId;
                Amount = amount;
            }
        }

        public class Debit : Event
        {
            public readonly uint Amount;

            public Debit(
                uint amount,
                CorrelatedMessage source)
                : base(source)
            {
                Amount = amount;
            }

            [JsonConstructor]
            public Debit(
                uint amount,
                CorrelationId correlationId,
                SourceId sourceId)
                : base(correlationId, sourceId)
            {
                Amount = amount;
            }
        }

        public class CreditAccount : Command
        {
            public readonly Guid AccountId;
            public readonly uint Amount;

            public CreditAccount(
                Guid accountId,
                uint amount,
                CorrelatedMessage source)
                : base(source)
            {
                AccountId = accountId;
                Amount = amount;
            }
        }

        public class Credit : Event
        {
            public readonly uint Amount;

            public Credit(
                uint amount,
                CorrelatedMessage source)
                : base(source)
            {
                Amount = amount;
            }

            [JsonConstructor]
            public Credit(
                uint amount,
                CorrelationId correlationId,
                SourceId sourceId)
                : base(correlationId, sourceId)
            {
                Amount = amount;
            }
        }
    }
}