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


namespace Akka.Persistence.ServiceFabric.Tests
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
            //var config = ConfigurationFactory.ParseString(@"
            //    akka {
            //        persistence {
            //            journal {
            //                # Path to the journal plugin to be used
            //                plugin = ""akka.persistence.journal.servicefabricjournal""

            //                # In-memory journal plugin.
            //                servicefabricjournal {
            //                    # Class name of the plugin.
            //                    class = ""Akka.Persistence.ServiceFabric.Journal.ServiceFabricJournal, Akka.Persistence.ServiceFabric""
            //                    # Dispatcher for the plugin actor.
            //                    plugin-dispatcher = ""akka.actor.default-dispatcher""
            //                }
            //            }
            //            snapshot-store {
            //                plugin = ""akka.persistence.snapshot-store.servicefabric""
            //                servicefabric {
            //                    class = ""Akka.Persistence.ServiceFabric.Snapshot.ServiceFabricSnapshotStore, Akka.Persistence.ServiceFabric""
            //                    # Dispatcher for the plugin actor.
            //                    plugin-dispatcher = ""akka.actor.default-dispatcher""
            //                }
            //            }
            //        }  
            //    }");
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
    }
}
