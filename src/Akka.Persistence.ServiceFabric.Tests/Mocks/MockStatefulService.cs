namespace Akka.Persistence.ServiceFabric.Tests.Mocks
{
    using System.Fabric;
    using System.Threading.Tasks;
    using Akka.Persistence.ServiceFabric.Snapshot;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Journal;

    public class MockStatefulService : StatefulService
    {
        public static StatefulServiceContext ServiceContext;

        public static StatefulService StatefulService;

        public MockStatefulService(StatefulServiceContext context)
            : this(context, new InitializationCallbackAdapter())
        {
        }

        public MockStatefulService(StatefulServiceContext context, InitializationCallbackAdapter adapter)
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
