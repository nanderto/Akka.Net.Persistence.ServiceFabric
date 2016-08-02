using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Akka.Configuration;
using Akka.Actor;
using AkkaPersistence.Actors;
using Akka.Persistence.ServiceFabric;
using Akka.Persistence.ServiceFabric.Snapshot;
using Microsoft.ServiceFabric.Data;
using PersistenceExample;

namespace AkkaPersistence
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class AkkaPersistence : AkkaStatefulService
    {
        public AkkaPersistence(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        //protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        //{
        //    return new ServiceReplicaListener[0];
        //} 

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.
            var config = ConfigurationFactory.ParseString(@"
                akka {
                    persistence {
                        journal {
                            # Path to the journal plugin to be used
                            plugin = ""akka.persistence.journal.servicefabricjournal""

                            # In-memory journal plugin.
                            servicefabricjournal {
                                # Class name of the plugin.
                                class = ""Akka.Persistence.ServiceFabric.Journal.ServiceFabricJournal, Akka.Persistence.ServiceFabric""
                                # Dispatcher for the plugin actor.
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                            }
                        }
                        snapshot-store {
                            plugin = ""akka.persistence.snapshot-store.servicefabric""
                            servicefabric {
                                class = ""Akka.Persistence.ServiceFabric.Snapshot.ServiceFabricSnapshotStore, Akka.Persistence.ServiceFabric""
                                # Dispatcher for the plugin actor.
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                            }
                        }
                    }  
                }");

            //Create a reliable conter and set it to 0
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            using (var tx = StateManager.CreateTransaction())
            {
                var ret = await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);
                await tx.CommitAsync();
            }

            // Create a new actor system (a container for your actors)
            var system = Akka.Actor.ActorSystem.Create("MySystem", config);

            //SqlServerPersistence.Init(system);

            //BasicUsage(system);

            //FailingActorExample(system);

            //SnapshotedActor(system);

            //ViewExample(system);

            //AtLeastOnceDelivery(system);

            //await IncrementCounter(system, cancellationToken);

            await IncrementCounter2(system, cancellationToken, this.StateManager);
        }

        private static async Task IncrementCounter(ActorSystem system, CancellationToken cancellationToken)
        {
            // Create your actor and get a reference to it.
            // This will be an "ActorRef", which is not a
            // reference to the actual actor instance
            // but rather a client or proxy to it.
            var logger = system.ActorOf<LoggerActor>("Startup");
            //var logger2 = system.ActorOf<LoggerActor>("Startup2");
            // Send a message to the actor
            logger.Tell("First Message");
            //logger2.Tell("First Message to Logger 2");
            var counter = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                logger.Tell("Increment");

                if (counter % 20 == 0)
                {
                    logger.Tell("Boom");
                }

                if (counter % 5 == 0)
                {
                    var result = await logger.Ask("Whats the counter now");
                    var currentCount = result != null && int.Parse(result.ToString()) > 0 ? result.ToString() : "Value does not exist.";
                    ServiceEventSource.Current.Message($"Current Counter Value: {currentCount}");

                    var messages = (List<string>)await logger.Ask("Get Messages");
                    WriteOutMessages(messages);

                    messages = (List<string>)await logger.Ask(new LoggerActor.GetMessages());
                    WriteOutMessages(messages);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                ++counter;
            }
        }

        private static void WriteOutMessages(List<string> messages)
        {
            if (messages != null)
            {
                foreach (var item in messages)
                {
                    ServiceEventSource.Current.Message($"Message in saved state is: {item}");
                }
            }
        }


        private static async Task IncrementCounter2(ActorSystem system, CancellationToken cancellationToken, IReliableStateManager stateManager)
        {
            // Create your actor and get a reference to it.
            // This will be an "ActorRef", which is not a
            // reference to the actual actor instance
            // but rather a client or proxy to it.
            var Counter = system.ActorOf<Counter>("Startup");
            //var logger2 = system.ActorOf<LoggerActor>("Startup2");
            // Send a message to the actor
            //Counter.Tell("First Message");
            //logger2.Tell("First Message to Logger 2");
            var counter = 0;

            var myDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();



                var result = await Counter.Ask(new Counter.GetCount());
                var currentCount = result != null && int.Parse(result.ToString()) > 0 ? result.ToString() : "Value does not exist.";
                ServiceEventSource.Current.Message($"Current Counter Value: {currentCount}");
                Counter.Tell("Increment");

                if (counter % 5 == 0)
                {

                }


                using (var tx = stateManager.CreateTransaction())
                {
                    var reliableResult = await myDictionary.TryGetValueAsync(tx, "Counter");
                    var reliableCounter = reliableResult.HasValue ? reliableResult.Value.ToString() : "Value does not exist.";
                    ServiceEventSource.Current.Message($"Current Reliable Counter Value: {reliableCounter}");
                    var ret = await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);
                    //ServiceEventSource.Current.Message($"Ret: {ret}");
                    await tx.CommitAsync();
                }

                ServiceEventSource.Current.Message($"Current loop Counter Value: {counter}");

                ++counter;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private static void AtLeastOnceDelivery(ActorSystem system)
        {
            ServiceEventSource.Current.Message("\n--- AT LEAST ONCE DELIVERY EXAMPLE ---\n");
            var delivery = system.ActorOf(Props.Create(() => new DeliveryActor()), "delivery");

            var deliverer = system.ActorOf(Props.Create(() => new AtLeastOnceDeliveryExampleActor(delivery.Path)));
            delivery.Tell("start");
            deliverer.Tell(new Message("foo"));


            System.Threading.Thread.Sleep(1000); //making sure delivery stops before send other commands
            delivery.Tell("stop");

            deliverer.Tell(new Message("bar"));

            ServiceEventSource.Current.Message("\nSYSTEM: Throwing exception in Deliverer\n");
            deliverer.Tell("boom");
            System.Threading.Thread.Sleep(1000);

            deliverer.Tell(new Message("bar1"));
            ServiceEventSource.Current.Message("\nSYSTEM: Enabling confirmations in 3 seconds\n");

            System.Threading.Thread.Sleep(3000);
            ServiceEventSource.Current.Message("\nSYSTEM: Enabled confirmations\n");
            delivery.Tell("start");

        }

        private static void ViewExample(ActorSystem system)
        {
            ServiceEventSource.Current.Message("\n--- PERSISTENT VIEW EXAMPLE ---\n");
            var pref = system.ActorOf(Props.Create<ViewExampleActor>());
            var view = system.ActorOf(Props.Create<ExampleView>());

            system.Scheduler.ScheduleTellRepeatedly(TimeSpan.Zero, TimeSpan.FromSeconds(2), pref, "scheduled", ActorRefs.NoSender);
            system.Scheduler.ScheduleTellRepeatedly(TimeSpan.Zero, TimeSpan.FromSeconds(5), view, "snap", ActorRefs.NoSender);
        }

        private static void SnapshotedActor(ActorSystem system)
        {
            ServiceEventSource.Current.Message("\n--- SNAPSHOTED ACTOR EXAMPLE ---\n");
            var pref = system.ActorOf(Props.Create<SnapshotedExampleActor>(), "snapshoted-actor");

            // send two messages (a, b) and persist them
            pref.Tell("a");
            pref.Tell("b");

            // make a snapshot: a, b will be stored in durable memory
            pref.Tell("snap");

            // send next two messages - those will be cleared, since MemoryJournal is not "persistent"
            pref.Tell("c");
            pref.Tell("d");

            // print internal actor's state
            pref.Tell("print");

            // result after first run should be like:

            // Current actor's state: d, c, b, a

            // after second run:

            // Offered state (from snapshot): b, a      - taken from the snapshot
            // Current actor's state: d, c, b, a, b, a  - 2 last messages loaded from the snapshot, rest send in this run

            // after third run:

            // Offered state (from snapshot): b, a, b, a        - taken from the snapshot
            // Current actor's state: d, c, b, a, b, a, b, a    - 4 last messages loaded from the snapshot, rest send in this run

            // etc...
        }

        private static void FailingActorExample(ActorSystem system)
        {
            ServiceEventSource.Current.Message("\n--- FAILING ACTOR EXAMPLE ---\n");
            var pref = system.ActorOf(Props.Create<ExamplePersistentFailingActor>(), "failing-actor");

            pref.Tell("a");
            pref.Tell("print");
            // restart and recovery
            pref.Tell("boom");
            pref.Tell("print");
            pref.Tell("b");
            pref.Tell("print");
            pref.Tell("c");
            pref.Tell("print");

            // Will print in a first run (i.e. with empty journal):

            // Received: a
            // Received: a, b
            // Received: a, b, c
        }

        private static void BasicUsage(ActorSystem system)
        {
            ServiceEventSource.Current.Message("--- BASIC EXAMPLE ---");
            // create a persistent actor, using LocalSnapshotStore and MemoryJournal
            var aref = system.ActorOf(Props.Create<ExamplePersistentActor>(), "basic-actor");

            // all commands are stacked in internal actor's state as a list
            aref.Tell(new Command("foo"));
            aref.Tell(new Command("baz"));
            aref.Tell(new Command("bar"));

            // save current actor state using LocalSnapshotStore (it will be serialized and stored inside file on example bin/snapshots folder)
            aref.Tell("snap");

            // add one more message, this one is not snapshoted and won't be persisted (because of MemoryJournal characteristics)
            aref.Tell(new Command("buzz"));

            // print current actor state
            aref.Tell("print");

            for (int i = 0; i < 10; i++)
            {
                Task.Delay(1000);
            }

            aref.GracefulStop(TimeSpan.FromSeconds(10));
            // on first run displayed state should be: 

            // buzz-3, bar-2, baz-1, foo-0 
            // (numbers denotes current actor's sequence numbers for each stored event)

            // on the second run: 

            // buzz-6, bar-5, baz-4, foo-3, bar-2, baz-1, foo-0
            // (sequence numbers are continuously increasing taken from last snapshot, 
            // also buzz-3 event isn't present since it's has been called after snapshot request,
            // and MemoryJournal will destroy stored events on program stop)

            // on next run's etc...
        }
    }
}
