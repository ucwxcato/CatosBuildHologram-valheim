using System;

namespace CatosBuildHologram.Server
{
    internal readonly struct AuthenticatedPeer
    {
        internal AuthenticatedPeer(string connectionId, string playerId)
        {
            ConnectionId = connectionId;
            PlayerId = playerId;
        }

        internal string ConnectionId { get; }
        internal string PlayerId { get; }
        internal bool IsAuthenticated => !string.IsNullOrWhiteSpace(ConnectionId)
            && !string.IsNullOrWhiteSpace(PlayerId);
    }
}
