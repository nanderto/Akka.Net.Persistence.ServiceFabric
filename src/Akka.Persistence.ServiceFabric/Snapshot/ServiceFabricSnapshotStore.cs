namespace Akka.Persistence.ServiceFabric.Snapshot
{
    using System;
    using System.Threading.Tasks;
    using Akka.Persistence.Snapshot;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    public class ServiceFabricSnapshotStore : SnapshotStore
    {
        private readonly IReliableStateManager stateManager;

        public ServiceFabricSnapshotStore()
        {
            this.stateManager = ServiceFabricPersistence.Instance.Apply(Context.System).StateManager;
        }

        protected async override Task DeleteAsync(SnapshotMetadata metadata)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(this.DeleteAsync)} PersistenceId: {metadata.PersistenceId} SequencNumer: {metadata.SequenceNr}");

            using (var tx = this.stateManager.CreateTransaction())
            {
                var snapshots = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(metadata.PersistenceId);

                var removed = await snapshots.TryRemoveAsync(tx, $"{metadata.PersistenceId}_{metadata.SequenceNr}");
                if (removed.HasValue)
                {
                    var result = removed.Value;
                }

                await tx.CommitAsync();
            }
        }

        protected async override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(this.DeleteAsync)} PersistenceId: {persistenceId} ");

            if ((criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue) &&
               (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue))
            {
                using (var tx = this.stateManager.CreateTransaction())
                {
                    var snapshots = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(persistenceId);
                    long firstSequenceNumber = 0;
                    for (long i = 0; i < criteria.MaxSequenceNr; i++)
                    {
                        var result = await snapshots.TryGetValueAsync(tx, $"{persistenceId}_{i}");
                        var snapShot = result.HasValue ? result.Value : null;
                        if (snapShot.Timestamp > criteria.MaxTimeStamp.Ticks)
                        {
                            firstSequenceNumber = i;
                            await snapshots.TryRemoveAsync(tx, $"{persistenceId}_{i}");
                        }
                    }

                    await tx.CommitAsync();
                }
            }
        }

        /// <summary>
        /// Asynchronously loads snapshot with the highest sequence number for a persistent actor/view matching specified criteria.
        /// </summary>
        /// <param name="persistenceId">Persistence ID of the Actor</param>
        /// <param name="criteria">Selecton Criteria to select the snapshot</param>
        /// <returns>The selected snapshot</returns>
        protected async override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(this.LoadAsync)} PersistenceId: {persistenceId} ");

            SnapshotEntry snapshot = null;

            using (var tx = this.stateManager.CreateTransaction())
            {
                ServiceEventSource.Current.Message($"{persistenceId} ");

                var snapshotStorageCurrentHighSequenceNumber = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("SnapshotStorageCurrentHighSequenceNumber");

                var maxSequenceNumberConditional = await snapshotStorageCurrentHighSequenceNumber.TryGetValueAsync(tx, persistenceId);
                if(maxSequenceNumberConditional.HasValue)
                {
                    var maxSequenceNumber = maxSequenceNumberConditional.Value;
                    var snapshots = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(persistenceId);
                    var ret = await snapshots.TryGetValueAsync(tx, $"{persistenceId}_{maxSequenceNumber}");
                    snapshot = ret.HasValue ? ret.Value : null;
                    await tx.CommitAsync();
                    SelectedSnapshot selectedSnapshot = new SelectedSnapshot(new SnapshotMetadata(persistenceId, snapshot.SequenceNr), snapshot.Snapshot);
                    return selectedSnapshot;
                }
                else
                {
                    await snapshotStorageCurrentHighSequenceNumber.AddAsync(tx, persistenceId, 0);
                    await tx.CommitAsync();
                }
            }

            return null;
        }

        /// <summary>
        /// Asynchronously stores a snapshot with metadata as object in the reliable dictionary, saves the highest Sequence number 
        /// separatly to allow the last one to be found by sequence number.
        /// </summary>
        /// <param name="metadata">metadata about the snapshot</param>
        /// <param name="snapshot">The snapshot to save</param>
        /// <returns></returns>
        protected async override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(SaveAsync)} PersistenceId: {metadata.PersistenceId} SequencNumer: {metadata.SequenceNr}");

            var snapshotEntry = new SnapshotEntry
            {
                Id = metadata.PersistenceId + "_" + metadata.SequenceNr,
                PersistenceId = metadata.PersistenceId,
                SequenceNr = metadata.SequenceNr,
                Snapshot = snapshot,
                Timestamp = metadata.Timestamp.Ticks
            };

            var snapshotEntryId = metadata.PersistenceId + "_" + metadata.SequenceNr;

            using (var tx = this.stateManager.CreateTransaction())
            {
                var snapshotStorageCurrentHighSequenceNumber = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("SnapshotStorageCurrentHighSequenceNumber");

                var resultCurrentHighSquenceNumber = await snapshotStorageCurrentHighSequenceNumber.AddOrUpdateAsync(tx, metadata.PersistenceId, metadata.SequenceNr, (ssschsn, lng) => metadata.SequenceNr);

                ServiceEventSource.Current.Message($"resultCurrentHighSquenceNumber: {resultCurrentHighSquenceNumber}");

                var snapshots = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(metadata.PersistenceId);
                var resultSnapshotAdd = await snapshots.GetOrAddAsync(tx, snapshotEntry.Id, ssid => snapshotEntry);

                ServiceEventSource.Current.Message($"resultSnapshotAdd: {resultSnapshotAdd}");

                await tx.CommitAsync();
                ServiceEventSource.Current.Message($"Leaving {nameof(this.SaveAsync)} PersistenceId: {metadata.PersistenceId} SequencNumer: {metadata.SequenceNr}");
            }

            return;
        }
    }
}
