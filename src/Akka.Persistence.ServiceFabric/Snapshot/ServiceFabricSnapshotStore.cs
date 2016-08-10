namespace Akka.Persistence.ServiceFabric.Snapshot
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Akka.Persistence.Snapshot;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    public class ServiceFabricSnapshotStore : SnapshotStore
    {
        public IReliableStateManager StateManager { get; set; }

        public ServiceFabricSnapshotStore()
        {
            this.StateManager = ServiceFabricPersistence.Instance.Apply(Context.System).StateManager;
        }

        protected override async Task DeleteAsync(SnapshotMetadata metadata)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(this.DeleteAsync)} PersistenceId: {metadata.PersistenceId} SequencNumer: {metadata.SequenceNr}");
            long sequenceNumber = 0;
            using (var tx = this.StateManager.CreateTransaction())
            {
                var snapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(metadata.PersistenceId);

                if (!(metadata.SequenceNr > 0 && metadata.SequenceNr < long.MaxValue))
                {
                    tx.Abort();
                    return;
                }             

                var removed = await snapshots.TryRemoveAsync(tx, $"{metadata.PersistenceId}_{metadata.SequenceNr}");
                if (removed.HasValue)
                {
                    var result = removed.Value;
                    if (metadata.Timestamp != DateTime.MinValue && metadata.Timestamp != DateTime.MaxValue)
                    {
                        if (metadata.Timestamp.Ticks == result.Timestamp)
                        {
                            await tx.CommitAsync();
                        }
                        else
                        {
                            tx.Abort();
                        }
                    }
                    else
                    {
                        await tx.CommitAsync();
                    }
                }
                else
                {
                    await tx.CommitAsync();
                }
            }
        }

        protected async override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(this.DeleteAsync)} PersistenceId: {persistenceId} ");
            ServiceEventSource.Current.Message($"SnapshotSelectionCriteria criteria.MaxSequenceNr: {criteria.MaxSequenceNr} criteria.MaxTimeStamp: {criteria.MaxTimeStamp}");

            if (criteria.MaxSequenceNr > 0)
            //if ((criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr <= long.MaxValue) &&
            //   (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue))
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var snapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(persistenceId);
                    long firstSequenceNumber = 0;
                    for (long i = 0; i <= criteria.MaxSequenceNr; i++)
                    {
                        var result = await snapshots.TryGetValueAsync(tx, $"{persistenceId}_{i}");
                        if (result.HasValue)
                        {
                            var snapShot = result.Value;
                            if (snapShot.Timestamp <= criteria.MaxTimeStamp.Ticks)
                            {
                                ServiceEventSource.Current.Message($"Deleting persistenceID: {persistenceId}_{i}");
                                firstSequenceNumber = i;
                                await snapshots.TryRemoveAsync(tx, $"{persistenceId}_{i}");

                                ServiceEventSource.Current.Message($"Deleted persistenceID: {persistenceId}_{i}");
                            }
                            else
                            {
                                ServiceEventSource.Current.Message($"Failed to delets persistenceId: {persistenceId}_{i}");
                            }
                        }
                        else
                        {
                            ServiceEventSource.Current.Message($"Failed to get a value for persistenceId: {persistenceId}_{i}");
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

            using (var tx = this.StateManager.CreateTransaction())
            {
                ServiceEventSource.Current.Message($"{persistenceId} ");

                var snapshotStorageCurrentHighSequenceNumber = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("SnapshotStorageCurrentHighSequenceNumber");
                var maxSequenceNumberConditional = await snapshotStorageCurrentHighSequenceNumber.TryGetValueAsync(tx, persistenceId);

                if (maxSequenceNumberConditional.HasValue)
                {
                    var maxSequenceNumber = maxSequenceNumberConditional.Value;
                    if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
                    {
                        if (maxSequenceNumber > criteria.MaxSequenceNr)
                        {
                            maxSequenceNumber = criteria.MaxSequenceNr;
                        }
                    }

                    var snapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(persistenceId);
                    var ret = await snapshots.TryGetValueAsync(tx, $"{persistenceId}_{maxSequenceNumber}");
                    if (ret.HasValue)
                    {
                        snapshot = ret.Value;

                        if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
                        {
                            if (snapshot.Timestamp > criteria.MaxTimeStamp.Ticks) return null;
                        }

                        await tx.CommitAsync();
                        var selectedSnapshot = new SelectedSnapshot(new SnapshotMetadata(persistenceId, snapshot.SequenceNr, new DateTime(snapshot.Timestamp)), snapshot.Snapshot);
                        return selectedSnapshot;
                    }
                    else
                    {
                        tx.Abort();
                    }
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
            ServiceEventSource.Current.Message($"Entering ServiceFabricSnapshotStore.{nameof(SaveAsync)} PersistenceId: {metadata.PersistenceId} SequenceNr: {metadata.SequenceNr}");

            var snapshotEntry = new SnapshotEntry
            {
                Id = metadata.PersistenceId + "_" + metadata.SequenceNr,
                PersistenceId = metadata.PersistenceId,
                SequenceNr = metadata.SequenceNr,
                Snapshot = snapshot,
                Timestamp = metadata.Timestamp.Ticks
            };

            var snapshotEntryId = metadata.PersistenceId + "_" + metadata.SequenceNr;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var snapshotStorageCurrentHighSequenceNumber = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("SnapshotStorageCurrentHighSequenceNumber");

                var resultCurrentHighSquenceNumber = await snapshotStorageCurrentHighSequenceNumber.AddOrUpdateAsync(tx, metadata.PersistenceId, metadata.SequenceNr, (ssschsn, lng) => metadata.SequenceNr);

                ServiceEventSource.Current.Message($"resultCurrentHighSquenceNumber: {resultCurrentHighSquenceNumber}");

                var snapshots = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SnapshotEntry>>(metadata.PersistenceId);
                var resultSnapshotAdd = await snapshots.GetOrAddAsync(tx, snapshotEntry.Id, ssid => snapshotEntry);

                ServiceEventSource.Current.Message($"resultSnapshotAdd: {resultSnapshotAdd}");

                await tx.CommitAsync();
                ServiceEventSource.Current.Message($"Leaving {nameof(this.SaveAsync)} PersistenceId: {metadata.PersistenceId} SequencNumer: {metadata.SequenceNr}");
            }

            return;
        }
    }
}
