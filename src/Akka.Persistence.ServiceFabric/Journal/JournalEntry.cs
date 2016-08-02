namespace Akka.Persistence.ServiceFabric.Journal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class JournalEntry
    {
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public bool IsDeleted { get; set; }

        public object Payload { get; set; }

        public string Manifest { get; set; }
    }
}
