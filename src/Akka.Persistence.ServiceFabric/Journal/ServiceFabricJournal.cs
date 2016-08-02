namespace Akka.Persistence.ServiceFabric.Journal
{
    using Akka.Actor;
    using Akka.Persistence.Journal;
    using Akka.Util.Internal;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    /// <summary>
    /// Service Fabric journal.
    /// </summary>
    public class ServiceFabricJournal : AsyncWriteJournal
    {
        private readonly IReliableStateManager StateManager;

        public ServiceFabricJournal(IReliableStateManager stateManager)
        {
            StateManager = stateManager;
        }

        public ServiceFabricJournal()
        {
            this.StateManager = ServiceFabricPersistence.Instance.Apply(Context.System).StateManager;
        }

        /// <summary>
        /// Write all messages contained in a single transaction to the underlying reliable dictionary
        /// </summary>
        /// <param name="messages">list of messages</param>
        /// <returns></returns>
        protected async override Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            IReliableDictionary<long, JournalEntry> messageList = null;
            IReliableDictionary<string, long> messageMetadata = null;
            long highestSequenceNumber = 0L;
            long newHighestSequenceNumber = 0L;

            using (var tx = this.StateManager.CreateTransaction())
            {
                foreach (var message in messages)
                {
                    foreach (var payload in (IEnumerable<IPersistentRepresentation>)message.Payload)
                    {
                        if (messageList == null)
                        {
                            messageList = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, JournalEntry>>($"Messages_{payload.PersistenceId}");
                        }

                        if (messageMetadata == null)
                        {
                            messageMetadata = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>($"MessageMetaData_{payload.PersistenceId}");
                            var result = await messageMetadata.TryGetValueAsync(tx, "HighestSequenceNumber");

                            if (result.HasValue)
                            {
                                newHighestSequenceNumber = result.Value;
                            }
                            else
                            {
                                var ret = await messageMetadata.TryAddAsync(tx, "HighestSequenceNumber", 0);
                            }
                        }

                        if (payload.SequenceNr > newHighestSequenceNumber)
                        {
                            newHighestSequenceNumber = payload.SequenceNr;
                        }

                        var journalEntry = ToJournalEntry(payload);


                        var list = await messageList.TryAddAsync(tx, journalEntry.SequenceNr, journalEntry);
                    }
                }

                await messageMetadata.TryUpdateAsync(tx, "HighestSequenceNumber", newHighestSequenceNumber, highestSequenceNumber);

                await tx.CommitAsync();
            }

            return (IImmutableList<Exception>) null; // all good
        }

        private JournalEntry ToJournalEntry(IPersistentRepresentation message)
        {
            return new JournalEntry
            {
                Id = message.PersistenceId + "_" + message.SequenceNr,
                IsDeleted = message.IsDeleted,
                Payload = message.Payload,
                PersistenceId = message.PersistenceId,
                SequenceNr = message.SequenceNr,
                Manifest = message.Manifest
            };
        }

        /// <summary>
        /// Read the highest Sequence number for the Persistance Id provided. from Sequence number is not required in this implementation
        /// </summary>
        /// <param name="persistenceId">PersistenceId For this Actor</param>
        /// <param name="fromSequenceNumber">Minimum Sequence number it could be</param>
        /// <returns>THe highest Sequence Number</returns>
        public async override Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNumber)
        {
            return await this.HighestSequenceNumberAsync(persistenceId);
        }

        public async override Task ReplayMessagesAsync(
            IActorContext context,
            string persistenceId,
            long fromSequenceNumber,
            long toSequenceNumber,
            long max,
            Action<IPersistentRepresentation> recoveryCallback)
        {
            var highest = await HighestSequenceNumberAsync(persistenceId);
            if (highest != 0L && max != 0L)
            {
                var result = await ReadAsync(persistenceId, fromSequenceNumber, Math.Min(toSequenceNumber, highest), max);
                result.ForEach(recoveryCallback);
            }

            return;
        }

        /// <summary>
        /// Deletes all messages from begining to the Sequence number
        /// </summary>
        /// <param name="persistenceId"></param>
        /// <param name="toSequenceNumber"></param>
        /// <returns></returns>
        protected async override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNumber)
        {
            var lowestSequenceNumber = 0L;
            long newLowestSequenceNumber = 0L;
            var highestSequenceNumber = await this.HighestSequenceNumberAsync(persistenceId);
            var toSeqNr = Math.Min(toSequenceNumber, highestSequenceNumber);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var messages = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, JournalEntry>>($"Messages_{persistenceId}");

                IReliableDictionary<string, long> messageMetadata = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>($"MessageMetaData_{persistenceId}");

                var result = await messageMetadata.TryGetValueAsync(tx, "LowestSequenceNumber");

                if (result.HasValue)
                {
                    newLowestSequenceNumber = result.Value;
                }
                else
                {
                    await messageMetadata.TryAddAsync(tx, "LowestSequenceNumber", 0);
                }

                for (long i = lowestSequenceNumber; i < toSequenceNumber; i++)
                {
                    var deleted = await messages.TryRemoveAsync(tx, i);
                    newLowestSequenceNumber = i + 1;
                }

                await messageMetadata.TryUpdateAsync(tx, "LowestSequenceNumber", newLowestSequenceNumber, lowestSequenceNumber);
                await tx.CommitAsync();
                return;
            }
        }

        public async Task<IEnumerable<IPersistentRepresentation>> ReadAsync(string pid, long fromSequenceNumber, long toSequenceNumber, long max)
        {
            var ret = new List<IPersistentRepresentation>();
            using (var tx = this.StateManager.CreateTransaction())
            {
                var messages = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, JournalEntry>>($"Messages_{pid}");

                for (long i = fromSequenceNumber; i < toSequenceNumber; i++)
                {
                    var result = await messages.TryGetValueAsync(tx, i);
                    if (result.HasValue)
                    {
                        ret.Add(this.ToPersistanceRepresentation(result.Value, this.Sender));
                    }
                }

                await tx.CommitAsync();
            }

            return ret;
        }

        private Persistent ToPersistanceRepresentation(JournalEntry entry, IActorRef sender)
        {
            return new Persistent(entry.Payload, entry.SequenceNr, entry.Manifest, entry.PersistenceId, entry.IsDeleted, sender);
        }

        public async Task<long> HighestSequenceNumberAsync(string pid)
        {
            long returnHighestSequenceNumberAsync = 0L;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var messageMetadata = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>($"MessageMetaData_{pid}");
                var result = await messageMetadata.TryGetValueAsync(tx, "HighestSequenceNumber");

                await tx.CommitAsync();

                if (result.HasValue)
                {
                    returnHighestSequenceNumberAsync = result.Value;
                }
            }

            return returnHighestSequenceNumberAsync;
        }

        public async Task<long> LowestSequenceNumberAsync(string pid)
        {
            long returnLowestSequenceNumberAsync = 0L;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var messageMetadata = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>($"MessageMetaData_{pid}");
                var result = await messageMetadata.TryGetValueAsync(tx, "LowestSequenceNumber");

                await tx.CommitAsync();

                if (result.HasValue)
                {
                    returnLowestSequenceNumberAsync = result.Value;
                }
            }

            return returnLowestSequenceNumberAsync;
        }
    }
}