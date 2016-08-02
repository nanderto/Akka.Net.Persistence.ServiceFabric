namespace Akka.Persistence.ServiceFabric
{
    using Akka.Actor;
    using Akka.Configuration;

    public class ServiceFabricPersistence : ExtensionIdProvider<ServiceFabricExtension>
    {
        public static readonly ServiceFabricPersistence Instance = new ServiceFabricPersistence();

        private ServiceFabricPersistence() { }

        public static Config DefaultConfig()
        {
            return ConfigurationFactory.FromResource<ServiceFabricPersistence>("Akka.Persistence.ServiceFabric.reference.conf");
        }

        public override ServiceFabricExtension CreateExtension(ExtendedActorSystem system)
        {
            return new ServiceFabricExtension(system);
        }
    }
}
