using System;
using System.Collections.Generic;
using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Client
{
    internal sealed class LocalPlanStore
    {
        private readonly int _capacity;
        private readonly List<BlueprintRecord> _records = new List<BlueprintRecord>();

        internal LocalPlanStore(int capacity)
        {
            _capacity = Math.Max(1, Math.Min(capacity, ProtocolLimits.MaxBlueprintsPerOwner));
        }

        internal int Count => _records.Count;

        internal bool TryAdd(BlueprintRecord record)
        {
            if (_records.Count >= _capacity || !ProtocolValidation.IsValidBlueprint(record))
            {
                return false;
            }

            _records.Add(Clone(record));
            return true;
        }

        internal bool Remove(string blueprintId)
        {
            for (var index = 0; index < _records.Count; index++)
            {
                if (string.Equals(_records[index].BlueprintId, blueprintId, StringComparison.Ordinal))
                {
                    _records.RemoveAt(index);
                    return true;
                }
            }

            return false;
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

        internal void Clear()
        {
            _records.Clear();
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
