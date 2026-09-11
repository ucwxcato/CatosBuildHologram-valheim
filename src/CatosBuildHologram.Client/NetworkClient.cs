using CatosBuildHologram.Shared.Contracts;
using CatosBuildHologram.Shared.Protocol;

namespace CatosBuildHologram.Client
{
    internal sealed class NetworkClient
    {
        private readonly string _clientBuildVersion;
        private AuthorityMode _authorityMode;
        private uint _lastServerRevision;

        internal NetworkClient(string clientBuildVersion)
        {
            _clientBuildVersion = clientBuildVersion;
            ResetToVanilla();
        }

        internal AuthorityMode AuthorityMode => _authorityMode;
        internal uint LastServerRevision => _lastServerRevision;

        internal ClientHello CreateHello()
        {
            return new ClientHello
            {
                ProtocolMajor = ProtocolLimits.CurrentMajor,
                ClientBuildVersion = _clientBuildVersion,
                Capabilities = 0
            };
        }

        internal bool TryApplyServerHello(ServerHello hello)
        {
            if (hello == null || !ProtocolValidation.IsCompatibleMajor(hello.ProtocolMajor))
            {
                ResetToVanilla();
                return false;
            }

            _authorityMode = hello.AuthorityMode == AuthorityMode.ServerAuthoritative
                ? AuthorityMode.ServerAuthoritative
                : AuthorityMode.VanillaClientOnly;
            _lastServerRevision = 0;
            return true;
        }

        internal bool TryAcceptRevision(uint revision)
        {
            if (revision == 0 || revision <= _lastServerRevision)
            {
                return false;
            }

            _lastServerRevision = revision;
            return true;
        }

        internal void ResetToVanilla()
        {
            _authorityMode = AuthorityMode.VanillaClientOnly;
            _lastServerRevision = 0;
        }

        internal void Tick()
        {
            // Transport registration is deferred until the native connection
            // hook is verified. No gameplay RPCs are emitted.
        }
    }
}
