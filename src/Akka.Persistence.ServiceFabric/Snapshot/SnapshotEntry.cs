namespace Akka.Persistence.ServiceFabric.Snapshot
{
    public class SnapshotEntry
    {
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public long Timestamp { get; set; }

        public object Snapshot { get; set; }
    }
}
