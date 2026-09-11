using System;
using System.Collections.Generic;
using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Server
{
    internal sealed class RequestRouter
    {
        private readonly Dictionary<string, HashSet<string>> _seenRequestIds
            = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        internal bool TryValidate(AuthenticatedPeer peer, ProtocolMessage message, out RejectionCode rejection)
        {
            rejection = RejectionCode.None;
            if (!peer.IsAuthenticated || message == null
                || !ProtocolValidation.IsCompatibleMajor(message.ProtocolMajor)
                || !ProtocolValidation.IsBoundedRequestId(message.RequestId)
                || !ProtocolValidation.IsBoundedPayload(message.Payload))
            {
                rejection = RejectionCode.InvalidRequest;
                return false;
            }

            if (!_seenRequestIds.TryGetValue(peer.ConnectionId, out var requestIds))
            {
                requestIds = new HashSet<string>(StringComparer.Ordinal);
                _seenRequestIds.Add(peer.ConnectionId, requestIds);
            }

            if (!requestIds.Add(message.RequestId))
            {
                rejection = RejectionCode.InvalidRequest;
                return false;
            }

            return true;
        }

        internal void ForgetPeer(string connectionId)
        {
            if (!string.IsNullOrWhiteSpace(connectionId))
            {
                _seenRequestIds.Remove(connectionId);
            }
        }

        internal void Clear()
        {
            _seenRequestIds.Clear();
        }
    }
}
