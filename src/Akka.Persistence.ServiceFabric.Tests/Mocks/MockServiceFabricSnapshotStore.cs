namespace Akka.Persistence.ServiceFabric.Tests.Mocks
{
    using System.Runtime.CompilerServices;

    using Akka.Persistence.ServiceFabric.Snapshot;
    using Akka.Persistence.Snapshot;
    public class MockServiceFabricSnapshotStore : ServiceFabricSnapshotStore
    {
        public MockServiceFabricSnapshotStore()
        {
            this.StateManager = new MockReliableStateManager();
        }
    }
}
