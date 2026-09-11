using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Client
{
    internal sealed class LocalPlanPersistence
    {
        private const int SchemaVersion = 2;
        private readonly string _path;

        internal LocalPlanPersistence(string path)
        {
            _path = path;
        }

        internal void LoadInto(List<BlueprintRecord> destination, int capacity)
        {
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(PersistedDocument));
                using (var stream = File.OpenRead(_path))
                {
                    var document = serializer.ReadObject(stream) as PersistedDocument;
                    if (document == null || document.SchemaVersion != SchemaVersion || document.Records == null)
                    {
                        return;
                    }

                    foreach (var persisted in document.Records)
                    {
                        if (destination.Count >= capacity)
                        {
                            break;
                        }

                        var record = persisted.ToRecord();
                        if (ProtocolValidation.IsValidBlueprint(record))
                        {
                            destination.Add(record);
                        }
                    }
                }
            }
            catch
            {
                // A corrupt local plan file must fail closed. It must never
                // prevent the client or native building from loading.
            }
        }

        internal bool TrySave(IEnumerable<BlueprintRecord> records)
        {
            var temporaryPath = _path + ".tmp";
            try
            {
                var document = new PersistedDocument
                {
                    SchemaVersion = SchemaVersion,
                    Records = new List<PersistedRecord>()
                };
                foreach (var record in records)
                {
                    document.Records.Add(PersistedRecord.FromRecord(record));
                }

                var parent = Path.GetDirectoryName(_path);
                if (string.IsNullOrWhiteSpace(parent))
                {
                    return false;
                }
                Directory.CreateDirectory(parent);

                var serializer = new DataContractJsonSerializer(typeof(PersistedDocument));
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.WriteObject(stream, document);
                    stream.Flush(true);
                }

                if (File.Exists(_path))
                {
                    File.Replace(temporaryPath, _path, null);
                }
                else
                {
                    File.Move(temporaryPath, _path);
                }
                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch
                {
                    // Best-effort cleanup only; the original file is retained.
                }
                return false;
            }
        }

        [DataContract]
        private sealed class PersistedDocument
        {
            [DataMember(Order = 0)]
            public int SchemaVersion { get; set; }

            [DataMember(Order = 1)]
            public List<PersistedRecord> Records { get; set; }
        }

        [DataContract]
        private sealed class PersistedRecord
        {
            [DataMember(Order = 0)] public string BlueprintId { get; set; }
            [DataMember(Order = 1)] public string OwnerPlayerId { get; set; }
            [DataMember(Order = 2)] public string PieceTypeId { get; set; }
            [DataMember(Order = 3)] public float PositionX { get; set; }
            [DataMember(Order = 4)] public float PositionY { get; set; }
            [DataMember(Order = 5)] public float PositionZ { get; set; }
            [DataMember(Order = 6)] public float RotationX { get; set; }
            [DataMember(Order = 7)] public float RotationY { get; set; }
            [DataMember(Order = 8)] public float RotationZ { get; set; }
            [DataMember(Order = 9)] public float RotationW { get; set; }
            [DataMember(Order = 10)] public BuildState BuildState { get; set; }
            [DataMember(Order = 11)] public SupportState SupportState { get; set; }
            [DataMember(Order = 12)] public RejectionCode BlockReason { get; set; }
            [DataMember(Order = 13)] public uint Revision { get; set; }

            internal BlueprintRecord ToRecord()
            {
                return new BlueprintRecord
                {
                    BlueprintId = BlueprintId,
                    OwnerPlayerId = OwnerPlayerId,
                    PieceTypeId = PieceTypeId,
                    Transform = new TransformData
                    {
                        PositionX = PositionX,
                        PositionY = PositionY,
                        PositionZ = PositionZ,
                        RotationX = RotationX,
                        RotationY = RotationY,
                        RotationZ = RotationZ,
                        RotationW = RotationW
                    },
                    BuildState = BuildState,
                    SupportState = SupportState,
                    BlockReason = BlockReason,
                    Revision = Revision
                };
            }

            internal static PersistedRecord FromRecord(BlueprintRecord record)
            {
                return new PersistedRecord
                {
                    BlueprintId = record.BlueprintId,
                    OwnerPlayerId = record.OwnerPlayerId,
                    PieceTypeId = record.PieceTypeId,
                    PositionX = record.Transform.PositionX,
                    PositionY = record.Transform.PositionY,
                    PositionZ = record.Transform.PositionZ,
                    RotationX = record.Transform.RotationX,
                    RotationY = record.Transform.RotationY,
                    RotationZ = record.Transform.RotationZ,
                    RotationW = record.Transform.RotationW,
                    BuildState = record.BuildState,
                    SupportState = record.SupportState,
                    BlockReason = record.BlockReason,
                    Revision = record.Revision
                };
            }
        }
    }
}
