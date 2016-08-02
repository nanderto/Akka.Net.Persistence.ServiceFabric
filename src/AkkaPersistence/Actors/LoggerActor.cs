using Akka.Actor;
using Akka.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaPersistence.Actors
{
    public class LoggerActor :  ReceivePersistentActor
    {
        public class GetMessages { }

        private List<string> messages = new List<string>(); //INTERNAL STATE

        private int _msgsSinceLastSnapshot = 0;

        private int counter;

        public LoggerActor()
        {

            Recover<string>(str =>
            {
                messages.Add(str);
            });

            Recover<SnapshotOffer>(offer => {
                var snapshotMessages = offer.Snapshot as List<string>;
                if (snapshotMessages != null) // null check
                    messages = (List<string>)snapshotMessages.Concat(messages);
            });

            Command<string>(str => Persist(str, s => 
            {
                if (str == "Boom")
                {
                    ServiceEventSource.Current.Message($"Throwing an exception in Logging actor {str}!!!");
                    throw new Exception("Controlled Devestation");
                }

                if (str == "Whats the counter now")
                {
                    Sender.Tell(counter, Self);
                }


                if (str == "Get Messages")
                {
                    Sender.Tell(messages, Self);
                }

                if (str == "Increment")
                {
                    ++counter;
                    messages.Add($"Counter has been {str}ed to {counter}");
                }
                else
                {
                    messages.Add(str); //add msg to in-memory event store after persisting
                }
                
                if (++_msgsSinceLastSnapshot % 10 == 0)
                {
                    //time to save a snapshot
                    SaveSnapshot(messages);
                }

                ServiceEventSource.Current.Message($"Received in Logging actor {str}");

            }));

            Command<SaveSnapshotSuccess>(success => 
            {
                ServiceEventSource.Current.Message($"Saved snapshot");
                // soft-delete the journal up until the sequence # at
                // which the snapshot was taken
                DeleteMessages(success.Metadata.SequenceNr);
            });

            Command<SaveSnapshotFailure>(failure => {
                // handle snapshot save failure...
                ServiceEventSource.Current.Message($"Snapshot failure");
            });

           // IReadOnlyList<string> readOnlyList = new List<string>(messages);
            Command<GetMessages>(get => Sender.Tell(new List<string>(messages)));


            //Receive<string>(str => messages.Add(str));
            //Receive<GetMessages>(get => Sender.Tell(new IReadOnlyList<string>(messages));
        }

        public LoggerActor(string message)
        {
            this.message = message;
        }

        public string message { get; private set; }

        private string persistenceId = string.Empty;
        public override string PersistenceId
        {
            get
            {
                if(string.IsNullOrEmpty(persistenceId))
                {
                    persistenceId = Guid.NewGuid().ToString();
                }

                return persistenceId;
            }
        }    
    }
}
