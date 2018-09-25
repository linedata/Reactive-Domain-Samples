//#define RD_PLAIN
//#define RD_DOMAIN
//#define RD_GOSSIPARRAY
//#define RD_GOSSIPARRAYONE
#define RD_GOSSIPTCP

using System;
using System.Net;
using EventStore.ClientAPI.Exceptions;
using ReactiveDomain;
using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ExpectedVersion = EventStore.ClientAPI.ExpectedVersion;

namespace AccountBalance3
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Application Starting...");
            var app = new Application();
            app.Bootstrap();
            app.Run();
            Console.ReadLine();
        }
    }

    public class Application
    {
        private IRepository repo;
        private readonly Guid _accountId = Guid.NewGuid(); //Parse("06AC5641-EDE6-466F-9B37-DD8304D05A84");
        private BalanceReadModel _readModel;

        private const string Username = "admin";
        private const string Password = "changeit";
        private const string Server = "127.0.0.1";
        private const int TcpPort = 1113;
        private const int HttpPort = 2113;
        private const string DnsName = "eventstore";

        //10.117.10.0, 10.117.10.123 and 10.117.8.141 are test IPs. Your IPs will vary
        private const string IpOne = "10.117.10.0";
        private const string IpTwo = "10.117.10.123";
        private const string IpThree = "10.117.8.141";
        private readonly IPAddress[] _ipAddressArray = 
            { IPAddress.Parse(IpOne), IPAddress.Parse(IpTwo), IPAddress.Parse(IpThree) };
        private readonly int[] _portArray = { HttpPort, HttpPort, HttpPort };

        // Other strings for EventStoreConnection.Create
        private string _gossipSeeds = $"GossipSeeds={IpOne}:{HttpPort},{IpTwo}:{HttpPort},{IpThree}:{HttpPort};";
        private string _serverConn = $"ConnectTo=tcp://{Username}:{Password}@{Server}:{TcpPort}";
        private string _dnsConn = $"ConnectTo=discover://{Username}:{Password}@{DnsName}:{HttpPort}";

        public void Bootstrap() {
            var eventStoreLoader = new EventStoreLoader();
            IStreamStoreConnection esConnection = null;
            var creds = new ReactiveDomain.UserCredentials(username: Username, password: Password);

            try
            {

#if RD_PLAIN
                eventStoreLoader.Connect(creds, IPAddress.Parse(IpOne), TcpPort);
#endif
#if RD_DOMAIN
                eventStoreLoader.Connect(creds, DnsName, HttpPort);
#endif
#if RD_GOSSIPARRAY
                eventStoreLoader.Connect(creds, _ipAddressArray, _portArray);
#endif
#if RD_GOSSIPARRAYONE
                eventStoreLoader.Connect(creds, _ipAddressArray, new [] {HttpPort});
#endif
#if RD_GOSSIPTCP
                eventStoreLoader.Connect(creds, _ipAddressArray, HttpPort);
#endif

            }
            catch (EventStoreConnectionException exception)
            {
                ConsoleLog.ErrorLog(exception.Message);
                Environment.Exit(1);
            }
            finally
            {
                esConnection = eventStoreLoader.Connection;
                ConsoleLog.InfoLog($"Successful connection to ES server {Server}:{TcpPort}");
            }

            esConnection.Connected += (_, __) => Console.WriteLine("Connected");
            IStreamNameBuilder nameBuilder = new PrefixedCamelCaseStreamNameBuilder();
            IEventSerializer serializer = new JsonMessageSerializer();
            repo = new StreamStoreRepository(nameBuilder, esConnection, serializer);
            IListener listener = new StreamListener("Account", esConnection, nameBuilder, serializer);
            _readModel = new BalanceReadModel(() => listener, _accountId);

            Account acct = null;
            try
            {
                repo.Save(new Account(_accountId));
                Console.WriteLine($"Account: {_accountId} created");
            }
            catch (Exception e)
            {
                ConsoleLog.ErrorLog($"Exception: {e.Message}");
            }
        }

        public void Run()
        {
            var cmd = new[] {""};
            Account acct;
            do
            {
                Console.WriteLine("Available Commands:");
                Console.WriteLine("\tcredit value\n\tdebit value\n\texit");
                cmd = Console.ReadLine().Split(' ');
                if(cmd.Length < 2)
                    continue;
                switch (cmd[0].ToLower())
                {
                    case "credit":
                        acct = repo.GetById<Account>(_accountId);
                        acct.Credit(uint.Parse(cmd[1]));
                        repo.Save(acct);
                        ConsoleLog.InfoLog($"got credit {cmd[1]}");
                        break;
                    case "debit":
                        try
                        {

                            acct = repo.GetById<Account>(_accountId);
                            acct.Debit(uint.Parse(cmd[1]));
                            repo.Save(acct);
                            ConsoleLog.InfoLog($"got debit {cmd[1]}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                        break;
                }

            } while (cmd[0].ToLower() != "exit");
        }

        /// <summary>
        /// Make the connection to the eventstore - singleton
        /// </summary>
        /// <returns>IStreamStoreConnection new connection</returns>
        internal IStreamStoreConnection Connect(string connectionString)
        {
            var eventStoreLoader = new EventStoreLoader();
            IStreamStoreConnection connection = null;

            try
            {
                // EventStoreLoader.Connect() creates an EventStoreConnectionWrapper and calls its Connect() function.
                // We can get the wrapper through the Loader Connection. Exposing the connection with local property.
                eventStoreLoader.Connect(
                    credentials: new ReactiveDomain.UserCredentials(username: Username, password: Password),
                    server: IPAddress.Parse(Server),
                    tcpPort: TcpPort);
            }
            catch (EventStoreConnectionException exception)
            {
                connection = null;
                ConsoleLog.ErrorLog(exception.Message);
            }
            finally
            {
                connection = eventStoreLoader.Connection;
                ConsoleLog.InfoLog($"Successful connection to ES server {Server}:{TcpPort}");
            }
            return connection;
        }
    }

    public class BalanceReadModel :
        ReadModelBase,
        IHandle<Credit>,
        IHandle<Debit>
    {
        public BalanceReadModel(Func<IListener> listener, Guid accountId) : base("Balance", listener)
        {
            EventStream.Subscribe<Credit>(this);
            EventStream.Subscribe<Debit>(this);
            Start<Account>(accountId);
        }

        private int balance;

        public void Handle(Credit message)
        {
            balance += (int) message.Amount;
            redraw();

        }

        public void Handle(Debit message)
        {
            balance -= (int) message.Amount;
            redraw();
        }

        private void redraw()
        {
            //Console.Clear();
            Console.WriteLine($"balance = {balance}");
        }
    }

    public class Account : EventDrivenStateMachine
    {
        private long _balance;

        public Account()
        {
            setup();
        }

        public Account(Guid id) : this()
        {

            Raise(new AccountCreated(id));
        }

        class MySecretEvent : Message
        {
        }

        public void setup()
        {
            Register<AccountCreated>(evt => Id = evt.Id);
            Register<Debit>(Apply);
            Register<Credit>(Apply);
        }

        private void Apply(Debit @event)
        {
            _balance -= @event.Amount;
        }

        private void Apply(Credit @event)
        {
            _balance += @event.Amount;
        }

        public void Credit(uint amount)
        {
            //nothing to check
            Raise(new Credit(amount));
        }

        public void Debit(uint amount)
        {
            ReactiveDomain.Util.Ensure.Nonnegative(_balance - amount, "Balance");

            Raise(new Debit(amount));
        }
    }

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
        public uint Amount;

        public Debit(uint amount)
        {
            Amount = amount;
        }
    }

    public class Credit : Message
    {
        public uint Amount;

        public Credit(uint amount)
        {
            Amount = amount;
        }
    }

    public static class ConsoleLog
    {
        public static void ErrorLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ResetColor();
        }
        public static void InfoLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Info: {message}");
            Console.ResetColor();
        }
    }
}
