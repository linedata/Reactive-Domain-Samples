using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;

namespace AccountBalance3
{
    class AccountSvc :
        IDisposable,
        IHandleCommand<AccountMsgs.CreateAccount>
    {
        private readonly IDispatcher _dispatcher;
        private readonly IRepository _repo;

        public AccountSvc(
            IDispatcher dispatcher,
            IRepository repo)
        {
            _dispatcher = dispatcher;
            _repo = repo;

            _dispatcher.Subscribe<AccountMsgs.CreateAccount>(this);
        }

        public CommandResponse Handle(AccountMsgs.CreateAccount command)
        {
            if (_repo.TryGetById<Account>(command.AccountId, out var account))
                return command.Fail(new Exception($"Account with ID '{command.AccountId}' already exists."));
            _repo.Save(new Account(command.AccountId, command));
            return command.Succeed();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _dispatcher.Unsubscribe<AccountMsgs.CreateAccount>(this);
            }
            _disposed = true;
        }
    }
}
