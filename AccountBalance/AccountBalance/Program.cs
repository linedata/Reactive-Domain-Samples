using System;
using System.Text;
using EventStore.ClientAPI;
using ReactiveDomain;
using ReactiveDomain.EventStore;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using EventData = EventStore.ClientAPI.EventData;
using ExpectedVersion = EventStore.ClientAPI.ExpectedVersion;
using StreamPosition = EventStore.ClientAPI.StreamPosition;

namespace AccountBalance {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            IEventStoreConnection conn = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@localhost:1113");
            conn.Connected += (_, __) => Console.WriteLine("Connected");
            conn.ConnectAsync().Wait();

            conn.AppendToStreamAsync("Test", ExpectedVersion.Any, new[] {
                new EventData(
                    Guid.NewGuid(),
                    "TestEvent",
                    false,
                    Encoding.UTF8.GetBytes("Test event data"),
                    Encoding.UTF8.GetBytes("Test event Metadata"))
            });

            Console.WriteLine("Event Written");

            var evt = conn.ReadStreamEventsForwardAsync("Test", StreamPosition.Start, 1, false).Result;
            Console.WriteLine(Encoding.UTF8.GetString(evt.Events[0].Event.Data));
            Console.ReadKey();
            return;
            IStreamStoreConnection streamConn = new EventStoreConnectionWrapper(conn);
            IStreamNameBuilder nameBuilder = new PrefixedCamelCaseStreamNameBuilder();
            IEventSerializer serializer = new JsonMessageSerializer();
            IRepository repo = new StreamStoreRepository(nameBuilder, streamConn, serializer);
            repo.Save(new Account(Guid.NewGuid()));

            IListener myListener = new StreamListener("Account Listener", streamConn, nameBuilder, serializer);
        }

        class accountCreated : Message
        {
            public readonly Guid Id;

            public accountCreated(Guid id) {
                Id = id;
            }
        }
        class Account : EventDrivenStateMachine {

            public Account(Guid id) {
                setup();
                Raise(new accountCreated(id));
            }

            private int bal;
            private void setup() {
                Register<credit>(evt => bal += 1);
                Register<accountCreated>( evt => Id = evt.Id);
            }


        }

        class credit : Message {

        }
        class myReadModel : 
                ReadModelBase,
                IHandle<credit>
        {
            public myReadModel(Func<IListener> listener, Guid accountGuid) : base(listener) {
                Start<Account>();
                this.EventStream.Subscribe<credit>(this);
            }

            public void Handle(credit message) {
                throw new NotImplementedException();
            }
        }
    }
}
