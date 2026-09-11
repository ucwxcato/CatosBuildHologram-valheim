using System;
using System.Collections.Generic;
using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Server
{
    internal sealed class BlueprintRepository
    {
        private readonly int _maxPerOwner;
        private readonly int _maxPerWorld;
        private readonly List<BlueprintRecord> _records = new List<BlueprintRecord>();
        private uint _revision;

        internal BlueprintRepository(int maxPerOwner, int maxPerWorld)
        {
            _maxPerOwner = Math.Max(1, Math.Min(maxPerOwner, ProtocolLimits.MaxBlueprintsPerOwner));
            _maxPerWorld = Math.Max(1, Math.Min(maxPerWorld, ProtocolLimits.MaxBlueprintsPerWorld));
        }

        internal uint Revision => _revision;

        internal bool TryCreate(BlueprintRecord candidate, out BlueprintRecord stored, out RejectionCode rejection)
        {
            stored = null;
            rejection = RejectionCode.None;
            if (_records.Count >= _maxPerWorld)
            {
                rejection = RejectionCode.QuotaExceeded;
                return false;
            }

            var ownerCount = 0;
            foreach (var record in _records)
            {
                if (string.Equals(record.OwnerPlayerId, candidate.OwnerPlayerId, StringComparison.Ordinal))
                {
                    ownerCount++;
                }
            }
            if (ownerCount >= _maxPerOwner)
            {
                rejection = RejectionCode.QuotaExceeded;
                return false;
            }

            stored = Clone(candidate);
            stored.BlueprintId = "server-" + Guid.NewGuid().ToString("N");
            stored.Revision = ++_revision;
            _records.Add(stored);
            stored = Clone(stored);
            return true;
        }

        internal List<BlueprintRecord> Snapshot()
        {
            var snapshot = new List<BlueprintRecord>(_records.Count);
            foreach (var record in _records)
            {
                snapshot.Add(Clone(record));
            }
            return snapshot;
        }

        internal BlueprintDelta CreateFullDelta()
        {
            return new BlueprintDelta
            {
                WorldRevision = _revision,
                AddedOrChanged = Snapshot(),
                RemovedIds = new List<string>()
            };
        }

        internal void Clear()
        {
            _records.Clear();
            _revision = 0;
        }

        private static BlueprintRecord Clone(BlueprintRecord source)
        {
            return new BlueprintRecord
            {
                BlueprintId = source.BlueprintId,
                OwnerPlayerId = source.OwnerPlayerId,
                PieceTypeId = source.PieceTypeId,
                Transform = source.Transform,
                BuildState = source.BuildState,
                SupportState = source.SupportState,
                BlockReason = source.BlockReason,
                Revision = source.Revision
            };
        }
    }
}
