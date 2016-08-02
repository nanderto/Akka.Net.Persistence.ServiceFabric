namespace Akka.Persistence.ServiceFabric.Snapshot
{
    using System.IO;

    using Akka.Serialization;
    using Microsoft.ServiceFabric.Data;

    public class SnapshotEntrySerializer : Wire.Serializer, IStateSerializer<SnapshotEntry>
    {
        public SnapshotEntrySerializer()
        {
            this.Serializer = new Wire.Serializer();
        }

        public Wire.Serializer Serializer { get; set; }

        void IStateSerializer<SnapshotEntry>.Write(SnapshotEntry value, BinaryWriter writer)
        {
            this.Serializer.Serialize(value, writer.BaseStream);
        }

        SnapshotEntry IStateSerializer<SnapshotEntry>.Read(BinaryReader reader)
        {
            SnapshotEntry value = new SnapshotEntry();
            value = this.Serializer.Deserialize<SnapshotEntry>(reader.BaseStream);
            return value;
        }

        void IStateSerializer<SnapshotEntry>.Write(SnapshotEntry currentValue, SnapshotEntry newValue, BinaryWriter writer)
        {
            ((IStateSerializer<SnapshotEntry>)this).Write(newValue, writer);
        }

        SnapshotEntry IStateSerializer<SnapshotEntry>.Read(SnapshotEntry baseValue, BinaryReader reader)
        {
            return ((IStateSerializer<SnapshotEntry>)this).Read(reader);
        }
    }
}
