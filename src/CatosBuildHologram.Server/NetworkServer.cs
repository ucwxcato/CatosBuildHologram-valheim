using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Server
{
    internal sealed class NetworkServer
    {
        private readonly RequestRouter _router = new RequestRouter();
        private readonly BlueprintValidator _validator;
        private readonly BlueprintRepository _repository;
        private readonly string _serverBuildVersion;
        private uint _worldRevision;

        internal NetworkServer(string serverBuildVersion, BlueprintValidator validator, BlueprintRepository repository)
        {
            _serverBuildVersion = serverBuildVersion;
            _validator = validator;
            _repository = repository;
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

        internal bool TryCreateBlueprint(AuthenticatedPeer peer, ProtocolMessage message,
            BlueprintRecord candidate, out BlueprintRecord stored, out RejectionCode rejection)
        {
            stored = null;
            if (!_router.TryValidate(peer, message, out rejection)
                || !_validator.TryValidateCreate(peer, candidate, out rejection))
            {
                return false;
            }

            return _repository.TryCreate(candidate, out stored, out rejection);
        }

        internal BlueprintDelta CreateFullDelta()
        {
            return _repository.CreateFullDelta();
        }

        internal void ForgetPeer(string connectionId)
        {
            _router.ForgetPeer(connectionId);
        }

        internal void Clear()
        {
            _router.Clear();
            _repository.Clear();
            _worldRevision = 0;
        }
    }
}
