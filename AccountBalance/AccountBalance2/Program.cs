using System;
using System.Security.Principal;
using EventStore.ClientAPI;
using ReactiveDomain;
using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace AccountBalance2
{
    class Program
    {

        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            var app = new Application();
            app.Bootstrap();
            app.Run();
            Console.ReadLine();
        }
    }

    public class Application
    {
        private IStreamStoreConnection conn;
        private IRepository repo;
        private Guid _accountId = Guid.Parse("06AC5641-EDE6-466F-9B37-DD8304D05A84");
        private BalanceReadModel _readModel;
        public void Bootstrap() {
            IEventStoreConnection esConnection = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@localhost:1113");
            conn = new EventStoreConnectionWrapper(esConnection);
            esConnection.Connected += (_, __) => Console.WriteLine("Connected");
            esConnection.ConnectAsync().Wait();
            IStreamNameBuilder namer = new PrefixedCamelCaseStreamNameBuilder();
            IEventSerializer ser = new JsonMessageSerializer();
            repo = new StreamStoreRepository(namer,conn,ser);
            Account acct = null;
            try {
                repo.Save(new Account(_accountId));
            }
            catch (Exception e) {
            }
            IListener listener = new StreamListener("Account",conn,namer,ser);
            _readModel = new BalanceReadModel( ()=> listener, _accountId);
        }
        public void Run() {
            var cmd = new []{""};
            Account acct;
            do {

                cmd =  Console.ReadLine().Split(' ');
                switch (cmd[0].ToLower()) {
                    case "credit":
                        acct = repo.GetById<Account>(_accountId);    
                        acct.Credit(uint.Parse(cmd[1]));
                        repo.Save(acct);
                        Console.WriteLine($"got credit {cmd[1]}");
                        break;
                    case "debit":
                        acct = repo.GetById<Account>(_accountId);
                        acct.Debit(uint.Parse(cmd[1]));
                        repo.Save(acct);
                        Console.WriteLine($"got debit {cmd[1]}");
                        break;
                }
                
            } while (cmd[0].ToLower() != "exit");
        }
    }

    public class BalanceReadModel : 
        ReadModelBase,
        IHandle<Credit>,
        IHandle<Debit>
    {
        public BalanceReadModel(Func<IListener> listener, Guid accountId):base(listener) {
            EventStream.Subscribe<Credit>(this);
            EventStream.Subscribe<Debit>(this);
            Start<Account>(accountId);
        }

        private int balance;

        public void Handle(Credit message) {
            balance += (int)message.Amount;
            redraw();

        }
        public void Handle(Debit message){
            balance -= (int)message.Amount;
            redraw();
        }

        private void redraw() {
            Console.Clear();
            Console.WriteLine($"ballance = { balance}");
        }
    }
    public class Account : EventDrivenStateMachine
    {
         public Account() {
         setup();   
        }
        public Account(Guid id):this() {
            
            Raise(new AccountCreated(id));
        }

        public void setup() {
            Register<AccountCreated>( evt => Id = evt.Id);
        }

        public void Credit(uint amount) {
            //nothing to check
            Raise(new Credit(amount));
        }

        public void Debit(uint amount) {
            //nothing to check
            Raise(new Debit(amount));
        }
    }
    public class AccountCreated:Message{
        public readonly Guid Id;

        public AccountCreated(Guid id) {
            Id = id;
        }
    }

    public class Debit : Message
        {
            public uint Amount;
            public Debit(uint amount) {
                Amount = amount;
            }
        }

        public class Credit : Message
        {
            public uint Amount;

            public Credit(uint amount) {
                Amount = amount;
            }
        }
    
}
