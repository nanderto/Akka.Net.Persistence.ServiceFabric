namespace Akka.Persistence.ServiceFabric
{
    using System;
    using Akka.Configuration;

    public abstract class ServiceFabricSettings
    {
        protected ServiceFabricSettings(Config config)
        {
        }
    }

    public class ServiceFabricJournalSettings : ServiceFabricSettings
    {
        public ServiceFabricJournalSettings(Config config) : base(config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config",
                    "Service Fabric journal settings cannot be initialized, because required HOCON section couldn't been found");
            }
        }
    }

    public class ServiceFabricSnapshotSettings : ServiceFabricSettings
    {
        public ServiceFabricSnapshotSettings(Config config) : base(config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config",
                    "Service Fabric snapshot settings cannot be initialized, because required HOCON section couldn't been found");
            }
        }
    }
}
