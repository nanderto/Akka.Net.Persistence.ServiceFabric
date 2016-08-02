namespace Akka.Persistence.ServiceFabric
{
    using System;
    using System.Fabric;

    using Akka.Actor;
    using Microsoft.ServiceFabric.Data;

    public class ServiceFabricExtension : IExtension
    {
        public ServiceFabricExtension(ExtendedActorSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException("system");
            }

            // Initialize fallback configuration defaults
            system.Settings.InjectTopLevelFallback(ServiceFabricPersistence.DefaultConfig());

            // Read config
            var journalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.servicefabric");
            this.JournalSettings = new ServiceFabricJournalSettings(journalConfig);

            var snapshotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.servicefabric");
            this.SnapshotStoreSettings = new ServiceFabricSnapshotSettings(snapshotConfig);

            this.StatefulServiceContext = AkkaStatefulService.ServiceContext;
            this.StateManager = AkkaStatefulService.StatefulService.StateManager;
        }

        /// <summary>
        /// Gets the settings for the ServiceFabric journal.
        /// </summary>
        public ServiceFabricJournalSettings JournalSettings { get; private set; }

        public StatefulServiceContext StatefulServiceContext { get; set; }

        /// <summary>
        /// Gets the settings for the Service Fabric Snapshot Store.
        /// </summary>
        public ServiceFabricSnapshotSettings SnapshotStoreSettings { get; private set; }

        public IReliableStateManager StateManager { get; internal set; }
    }
}
