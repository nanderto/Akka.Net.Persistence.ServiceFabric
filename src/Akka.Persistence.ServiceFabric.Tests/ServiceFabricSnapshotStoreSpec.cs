using System;
using System.Configuration;
using Akka.Persistence.TestKit.Snapshot;

namespace Akka.Persistence.ServiceFabric.Tests
{
    using System.Diagnostics;

    public class ServiceFabricSnapshotStoreSpec : SnapshotStoreSpec
    {
        //private static readonly MongoDbRunner Runner = MongoDbRunner.Start(ConfigurationManager.AppSettings[0]);

        private static readonly string SpecConfig = @"
        akka.persistence {
            publish-plugin-commands = on
            snapshot-store {
                plugin = ""akka.persistence.snapshot-store.servicefabric""
                servicefabric {
                    class = ""Akka.Persistence.ServiceFabric.Tests.Mocks.MockServiceFabricSnapshotStore, Akka.Persistence.ServiceFabric.Tests""
                    # Dispatcher for the plugin actor.
                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                }
            }
            journal {
                plugin = ""akka.persistence.journal.servicefabricjournal""
                servicefabricjournal { 
                    class = ""Akka.Persistence.ServiceFabric.Tests.Mocks.MockServiceFabricJournal, Akka.Persistence.ServiceFabric.Tests"" 
                    plugin-dispatcher = ""akka.actor.default-dispatcher"" 
                    key-prefix = ""akka:persistence:journal"" 
                } 
            }
        }";


        public ServiceFabricSnapshotStoreSpec() : base(SpecConfig, "ServiceFabricSnapshotStoreSpec")
        {
            ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ServiceFabricJournalSpec).Name);
            Initialize();
        }

        //private static string CreateSpecConfig()
        //{
        //    return SpecConfig.Replace("<ConnectionString>", Runner.ConnectionString + "akkanet");
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    //new MongoClient(Runner.ConnectionString)
        //    //    .GetDatabase("akkanet")
        //    //    .DropCollectionAsync("SnapshotStore").Wait();

        //    base.Dispose(disposing);
        //}
    }
}
