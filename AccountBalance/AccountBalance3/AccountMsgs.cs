using System;
using ReactiveDomain.Messaging;

namespace AccountBalance3 {
    public class AccountMsgs
    {
        public class AccountCreated : Message
        {
            public readonly Guid Id;

            public AccountCreated(Guid id)
            {
                Id = id;
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