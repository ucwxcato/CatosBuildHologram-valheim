using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CatosBuildHologram.Shared.Contracts;

namespace CatosBuildHologram.Client
{
    internal sealed class GuidedBuildService
    {
        private readonly ClientConfig _config;
        private readonly LocalPlanStore _localPlans;
        private readonly HologramRenderer _renderer;
        private string _activeBlueprintId;
        private BlueprintRecord _activeRecord;

        internal GuidedBuildService(ClientConfig config, LocalPlanStore localPlans, HologramRenderer renderer)
        {
            _config = config;
            _localPlans = localPlans;
            _renderer = renderer;
        }

        internal string ActiveBlueprintId => _activeBlueprintId;

        internal void Tick()
        {
            var player = GetLocalPlayer();
            if (player == null || !_config.EnableBlueprintInterception.Value)
            {
                return;
            }

            if (_config.GuidedNextKey.Value.IsDown())
            {
                SelectNext(player);
            }
        }

        internal void OnNativePiecePlaced(Piece piece, Vector3 position, Quaternion rotation)
        {
            if (_activeRecord == null || piece == null
                || !string.Equals(piece.name, _activeRecord.PieceTypeId, StringComparison.Ordinal))
            {
                return;
            }

            var target = new Vector3(_activeRecord.Transform.PositionX, _activeRecord.Transform.PositionY,
                _activeRecord.Transform.PositionZ);
            if (Vector3.Distance(target, position) > 1.5f)
            {
                return;
            }

            if (_localPlans.Remove(_activeRecord.BlueprintId))
            {
                _activeBlueprintId = null;
                _activeRecord = null;
            }
        }

        internal void Clear()
        {
            _activeBlueprintId = null;
            _activeRecord = null;
        }

        private void SelectNext(Player player)
        {
            var records = _localPlans.Snapshot();
            foreach (var record in records)
            {
                if (record.BuildState != BuildState.Planned && record.BuildState != BuildState.Failed)
                {
                    continue;
                }

                var pieces = player.GetBuildPieces();
                if (pieces == null)
                {
                    return;
                }
                foreach (var piece in pieces)
                {
                    if (piece == null || !string.Equals(piece.name, record.PieceTypeId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    try
                    {
                        var table = player.GetBuildTool();
                        if (table != null)
                        {
                            AccessTools.Method(typeof(Player), "SetPlaceMode", new[] { typeof(PieceTable) })
                                ?.Invoke(player, new object[] { table });
                        }
                        if (!player.SetSelectedPiece(piece))
                        {
                            return;
                        }
                        AccessTools.Method(typeof(Player), "SetupPlacementGhost")?.Invoke(player, null);
                        _activeBlueprintId = record.BlueprintId;
                        _activeRecord = record;
                        return;
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        private static Player GetLocalPlayer()
        {
            var field = AccessTools.Field(typeof(Player), "m_localPlayer");
            return field?.GetValue(null) as Player;
        }
    }
}
