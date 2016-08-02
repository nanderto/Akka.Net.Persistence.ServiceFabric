namespace Akka.Persistence.ServiceFabric
{
    using System.Fabric;
    using System.Threading.Tasks;
    using Akka.Persistence.ServiceFabric.Snapshot;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Journal;

    public class AkkaStatefulService : StatefulService
    {
        public static StatefulServiceContext ServiceContext;

        public static StatefulService StatefulService;

        public AkkaStatefulService(StatefulServiceContext context)
            : this(context, new InitializationCallbackAdapter())
        {
        }

        public AkkaStatefulService(StatefulServiceContext context, InitializationCallbackAdapter adapter)
            : base(context, new ReliableStateManager(context, new ReliableStateManagerConfiguration(onInitializeStateSerializersEvent: adapter.OnInitialize)))
        {
            adapter.StateManager = this.StateManager;
            ServiceContext = context;
            StatefulService = this;
        }
    }

    public class InitializationCallbackAdapter
    {
        public IReliableStateManager StateManager { get; set; }

        public Task OnInitialize()
        {
            this.StateManager.TryAddStateSerializer(new SnapshotEntrySerializer());
            this.StateManager.TryAddStateSerializer(new JournalEntrySerializer());
            return Task.FromResult(true);
        }

    }
}
