using Akka.Actor;
using Akka.Persistence;
using Akka.Persistence.ServiceFabric.Journal;
using Akka.Persistence.ServiceFabric.Snapshot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaPersistence.Actors
{
    public class Counter : ReceivePersistentActor
    {
        public class GetCount { }

        private int counter;

        private CounterState State = new CounterState();

        private int _msgsSinceLastSnapshot = 0;

        public Counter()
        {
            Recover<Evt>(evt =>
            {
                State.Update(evt);
            });

            Recover<string>(s =>
            {
                State.Update(new Evt(s));
            });

            Recover<SnapshotOffer>(offer => {
                var counterState =  offer.Snapshot as CounterState;
                if (counterState != null)
                {
                    State = counterState;
                }
            });

            Command<string>(s =>
            {
                var evt = new Evt(s);

                Persist<Evt>(evt, evnt =>
                {
                    ++counter;

                    //var evt = new Evt(str);
                    State.Update(evnt);

                    if (++_msgsSinceLastSnapshot % 10 == 0)
                    {
                        //time to save a snapshot
                        SaveSnapshot(State.Copy());
                    }
                });
            });

            Command<GetCount>(get => Sender.Tell(State.Count));

            Command<SaveSnapshotSuccess>(success =>
            {
                ServiceEventSource.Current.Message($"Saved snapshot");
                DeleteMessages(success.Metadata.SequenceNr);
            });

            Command<SaveSnapshotFailure>(failure => {
                // handle snapshot save failure...
                ServiceEventSource.Current.Message($"Snapshot failure");
            });

        }

        public override string PersistenceId
        {
            get
            {
                return "counter";
            }
        }
    }

    internal class CounterState
    {
        private long count = 0L;
        
        public long Count
        {
            get { return count; }
            set { count = value; }
        }

        public CounterState(long count)
        {
            this.Count = count;
        }

        public CounterState() : this(0)
        {
        }

        public CounterState Copy()
        {
            return new CounterState(count);
        }

        public void Update(Evt evt)
        {
            ++Count;
        }
    }

    public class Evt
    {
        public Evt(string data)
        {
            Data = data;
        }

        public string Data { get; }

    }

    public class Cmd
    {
        public Cmd(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

}
