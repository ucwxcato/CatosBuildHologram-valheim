using System;
using System.Diagnostics;
using BepInEx;
using CatosBuildHologram.Shared.Contracts;

namespace CatosBuildHologram.Client
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public sealed class ClientPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.catosaur.catosbuildhologram.client";
        internal const string PluginName = "Catos Build Hologram (Client)";
        internal const string PluginVersion = "0.1.0";

        private ClientConfig _config;
        private LocalPlanStore _localPlans;
        private NetworkClient _network;
        private bool _processGuardPassed;

        private void Awake()
        {
            _processGuardPassed = string.Equals(Process.GetCurrentProcess().ProcessName, "valheim",
                StringComparison.OrdinalIgnoreCase);
            if (!_processGuardPassed)
            {
                Logger.LogError("Refusing to initialize outside valheim.exe.");
                enabled = false;
                return;
            }

            _config = new ClientConfig(Config);
            _localPlans = new LocalPlanStore(_config.MaxLocalPlans.Value);
            _network = new NetworkClient(PluginVersion);

            if (!_config.Enabled.Value)
            {
                Logger.LogInfo("Client plugin disabled by configuration.");
                return;
            }

            Logger.LogInfo("Phase 1 client skeleton loaded; native blueprint interception is disabled.");
        }

        private void Update()
        {
            if (!_processGuardPassed || _config == null || !_config.Enabled.Value)
            {
                return;
            }

            _network.Tick();
        }

        private void OnDestroy()
        {
            _localPlans?.Clear();
            _network?.ResetToVanilla();
            _localPlans = null;
            _network = null;
        }

        internal AuthorityMode CurrentAuthorityMode => _network?.AuthorityMode ?? AuthorityMode.VanillaClientOnly;
        internal int LocalPlanCount => _localPlans?.Count ?? 0;
    }
}
