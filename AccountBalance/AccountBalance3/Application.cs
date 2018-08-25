﻿using System;
using EventStore.ClientAPI;
using ReactiveDomain;
using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;

namespace AccountBalance3 {
    public class Application : IDisposable {
        private IStreamStoreConnection _conn;
        private IRepository _repo;
        private BalanceReadModel _readModel;

        private readonly Guid _accountId = Guid.Parse("06AC5641-EDE6-466F-9B37-DD8304D05A84");

        public void Bootstrap() {
            var esConnection = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@localhost:1113");
            _conn = new EventStoreConnectionWrapper(esConnection);
            esConnection.Connected += (_, __) => Console.WriteLine("Connected");
            esConnection.ConnectAsync().Wait();
            IStreamNameBuilder namer = new PrefixedCamelCaseStreamNameBuilder();
            IEventSerializer ser = new JsonMessageSerializer();
            _repo = new StreamStoreRepository(namer, _conn, ser);
            try {
                _repo.Save(new Account(_accountId));
            }
            catch (Exception e) {
            }
            IListener listener = new StreamListener("Account", _conn, namer, ser);
            _readModel = new BalanceReadModel(() => listener, _accountId);
        }

        public void Run() {
            string[] cmd;
            do {

                cmd = Console.ReadLine().Split(' ');
                Account acct;
                switch (cmd[0].ToLower())
                {
                    case "credit":
                        acct = _repo.GetById<Account>(_accountId);
                        acct.Credit(uint.Parse(cmd[1]));
                        _repo.Save(acct);
                        Console.WriteLine($"got credit {cmd[1]}");
                        break;
                    case "debit":
                        try
                        {

                            acct = _repo.GetById<Account>(_accountId);
                            acct.Debit(uint.Parse(cmd[1]));
                            _repo.Save(acct);
                            Console.WriteLine($"got debit {cmd[1]}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        break;
                }

            } while (cmd[0].ToLower() != "exit");
        }

        public void Dispose()
        {
            _readModel?.Dispose();
            _conn?.Dispose();
        }
    }
}