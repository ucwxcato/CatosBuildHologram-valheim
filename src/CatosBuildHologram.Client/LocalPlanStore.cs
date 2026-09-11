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
        private readonly LocalPlanPersistence _persistence;

        internal LocalPlanStore(int capacity, string persistencePath)
        {
            _capacity = Math.Max(1, Math.Min(capacity, ProtocolLimits.MaxBlueprintsPerOwner));
            _persistence = new LocalPlanPersistence(persistencePath);
            _persistence.LoadInto(_records, _capacity);
        }

        internal int Count => _records.Count;
        internal uint HighestRevision => _records.Count == 0 ? 0 : HighestRevisionValue();

        internal bool TryAdd(BlueprintRecord record)
        {
            if (_records.Count >= _capacity || !ProtocolValidation.IsValidBlueprint(record))
            {
                return false;
            }

            _records.Add(Clone(record));
            if (!_persistence.TrySave(_records))
            {
                _records.RemoveAt(_records.Count - 1);
                return false;
            }
            return true;
        }

        internal bool Remove(string blueprintId)
        {
            for (var index = 0; index < _records.Count; index++)
            {
                if (string.Equals(_records[index].BlueprintId, blueprintId, StringComparison.Ordinal))
                {
                    var removed = _records[index];
                    _records.RemoveAt(index);
                    if (!_persistence.TrySave(_records))
                    {
                        _records.Insert(index, removed);
                        return false;
                    }
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

        internal void ClearRuntime()
        {
            _records.Clear();
        }

        private uint HighestRevisionValue()
        {
            var highest = 0u;
            foreach (var record in _records)
            {
                if (record.Revision > highest)
                {
                    highest = record.Revision;
                }
            }
            return highest;
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
