using System;
using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Server
{
    internal sealed class BlueprintValidator
    {
        internal bool TryValidateCreate(AuthenticatedPeer peer, BlueprintRecord candidate,
            out RejectionCode rejection)
        {
            rejection = RejectionCode.None;
            if (!peer.IsAuthenticated || !ProtocolValidation.IsValidBlueprint(candidate))
            {
                rejection = RejectionCode.InvalidRequest;
                return false;
            }

            if (!string.Equals(peer.PlayerId, candidate.OwnerPlayerId, StringComparison.Ordinal))
            {
                rejection = RejectionCode.NoPermission;
                return false;
            }

            if (Math.Abs(candidate.Transform.PositionX) > 100000f
                || Math.Abs(candidate.Transform.PositionY) > 100000f
                || Math.Abs(candidate.Transform.PositionZ) > 100000f)
            {
                rejection = RejectionCode.OutOfRange;
                return false;
            }

            if (candidate.BuildState != BuildState.Planned
                || candidate.SupportState != SupportState.Unknown)
            {
                rejection = RejectionCode.InvalidRequest;
                return false;
            }

            return true;
        }
    }
}
