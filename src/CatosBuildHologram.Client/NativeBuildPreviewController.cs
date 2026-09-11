using System;
using CatosBuildHologram.Shared.Contracts;
using UnityEngine;

namespace CatosBuildHologram.Client
{
    internal sealed class NativeBuildPreviewController
    {
        private readonly ClientConfig _config;
        private readonly LocalPlanStore _localPlans;
        private readonly HologramRenderer _renderer;
        private uint _nextRevision;
        private int _lastInterceptFrame = -1;

        internal NativeBuildPreviewController(ClientConfig config, LocalPlanStore localPlans, HologramRenderer renderer)
        {
            _config = config;
            _localPlans = localPlans;
            _renderer = renderer;
            _nextRevision = localPlans.HighestRevision;
        }

        internal int LocalPlanCount => _localPlans.Count;

        internal bool TryIntercept(Player player, Piece piece)
        {
            if (!_config.Enabled.Value || !_config.EnableBlueprintInterception.Value
                || player == null || piece == null)
            {
                return false;
            }

            // Enhanced mode will use a server request once the native transport
            // adapter is verified. Never create a local record while it claims
            // server authority without that request path.
            if (ClientPlugin.Instance != null
                && ClientPlugin.Instance.CurrentAuthorityMode == AuthorityMode.ServerAuthoritative)
            {
                return false;
            }

            if (Time.frameCount == _lastInterceptFrame)
            {
                return true;
            }

            if (player.GetPlacementStatus() != Player.PlacementStatus.Valid)
            {
                return false;
            }

            var transform = piece.transform;
            if (transform == null)
            {
                return false;
            }

            var detachedTransform = new TransformData
            {
                PositionX = transform.position.x,
                PositionY = transform.position.y,
                PositionZ = transform.position.z,
                RotationX = transform.rotation.x,
                RotationY = transform.rotation.y,
                RotationZ = transform.rotation.z,
                RotationW = transform.rotation.w
            };
            if (!detachedTransform.IsFinite())
            {
                return false;
            }

            var blueprint = new BlueprintRecord
            {
                BlueprintId = "local-" + Guid.NewGuid().ToString("N"),
                OwnerPlayerId = player.GetPlayerID().ToString(),
                PieceTypeId = piece.name,
                Transform = detachedTransform,
                BuildState = BuildState.Planned,
                SupportState = SupportState.Unknown,
                BlockReason = RejectionCode.None,
                Revision = ++_nextRevision
            };

            if (!_localPlans.TryAdd(blueprint))
            {
                return false;
            }

            _renderer.ShowLocal(blueprint, piece);

            _lastInterceptFrame = Time.frameCount;
            if (_config.DebugLogging.Value)
            {
                ClientPlugin.Instance?.LogLocalBlueprint(blueprint);
            }

            // Returning false prevents TryPlacePiece from instantiating the
            // real piece. UpdatePlacement therefore also skips its native
            // ConsumeResources and UseStamina branch for this confirmation.
            return true;
        }
    }
}
