using System;
using System.Diagnostics;
using BepInEx;
using CatosBuildHologram.Shared.Contracts;

namespace CatosBuildHologram.Server
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim_server.exe")]
    public sealed class ServerPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.catosaur.catosbuildhologram.server";
        internal const string PluginName = "Catos Build Hologram (Server)";
        internal const string PluginVersion = "0.1.0";

        private ServerConfig _config;
        private NetworkServer _network;
        private BlueprintRepository _repository;
        private bool _processGuardPassed;

        private void Awake()
        {
            _processGuardPassed = string.Equals(Process.GetCurrentProcess().ProcessName, "valheim_server",
                StringComparison.OrdinalIgnoreCase);
            if (!_processGuardPassed)
            {
                Logger.LogError("Refusing to initialize outside valheim_server.exe.");
                enabled = false;
                return;
            }

            _config = new ServerConfig(Config);
            _repository = new BlueprintRepository(_config.MaxBlueprintsPerOwner.Value,
                _config.MaxBlueprintsPerWorld.Value);
            _network = new NetworkServer(PluginVersion, new BlueprintValidator(), _repository);

            if (!_config.Enabled.Value)
            {
                Logger.LogInfo("Server plugin disabled by configuration.");
                return;
            }

            Logger.LogInfo("Phase 3 server validation/repository scaffolding loaded; native network/build hooks remain disabled.");
        }

        private void OnDestroy()
        {
            _network?.Clear();
            _network = null;
        }

        internal AuthorityMode AuthorityMode => _config != null && _config.EnableAuthoritativeMode.Value
            ? AuthorityMode.ServerAuthoritative
            : AuthorityMode.VanillaClientOnly;
    }
}
