namespace Akka.Persistence.ServiceFabric.Journal
{
    using System.IO;

    using Akka.Serialization;
    using Microsoft.ServiceFabric.Data;

    public class JournalEntrySerializer : Wire.Serializer, IStateSerializer<JournalEntry>
    {
        public JournalEntrySerializer()
        {
            this.Serializer = new Wire.Serializer();
        }

        public Wire.Serializer Serializer { get; set; }

        void IStateSerializer<JournalEntry>.Write(JournalEntry value, BinaryWriter writer)
        {
            this.Serializer.Serialize(value, writer.BaseStream);
        }

        JournalEntry IStateSerializer<JournalEntry>.Read(BinaryReader reader)
        {
            JournalEntry value = new JournalEntry();
            value = this.Serializer.Deserialize<JournalEntry>(reader.BaseStream);
            return value;
        }

        void IStateSerializer<JournalEntry>.Write(JournalEntry currentValue, JournalEntry newValue, BinaryWriter writer)
        {
            ((IStateSerializer<JournalEntry>)this).Write(newValue, writer);
        }

        JournalEntry IStateSerializer<JournalEntry>.Read(JournalEntry baseValue, BinaryReader reader)
        {
            return ((IStateSerializer<JournalEntry>)this).Read(reader);
        }
    }
}
