using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Server
{
    internal sealed class NetworkServer
    {
        private readonly RequestRouter _router = new RequestRouter();
        private readonly string _serverBuildVersion;
        private uint _worldRevision;

        internal NetworkServer(string serverBuildVersion)
        {
            _serverBuildVersion = serverBuildVersion;
        }

        internal uint WorldRevision => _worldRevision;

        internal ServerHello CreateHello()
        {
            return new ServerHello
            {
                ProtocolMajor = ProtocolLimits.CurrentMajor,
                AuthorityMode = AuthorityMode.ServerAuthoritative,
                ServerBuildVersion = _serverBuildVersion,
                WorldSessionId = string.Empty,
                FeatureFlags = 0
            };
        }

        internal bool TryRoute(AuthenticatedPeer peer, ProtocolMessage message, out RejectionCode rejection)
        {
            // Transport registration and native player authentication are
            // deferred until their Valheim hooks are verified.
            return _router.TryValidate(peer, message, out rejection);
        }

        internal uint NextWorldRevision()
        {
            return ++_worldRevision;
        }

        internal void ForgetPeer(string connectionId)
        {
            _router.ForgetPeer(connectionId);
        }

        internal void Clear()
        {
            _router.Clear();
            _worldRevision = 0;
        }
    }
}
