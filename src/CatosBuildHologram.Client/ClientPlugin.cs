using System;
using System.Diagnostics;
using BepInEx;
using CatosBuildHologram.Shared.Contracts;
using HarmonyLib;

namespace CatosBuildHologram.Client
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public sealed class ClientPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.catosaur.catosbuildhologram.client";
        internal const string PluginName = "Catos Build Hologram (Client)";
        internal const string PluginVersion = "0.1.0";

        internal static ClientPlugin Instance { get; private set; }

        private ClientConfig _config;
        private LocalPlanStore _localPlans;
        private NetworkClient _network;
        private NativeBuildPreviewController _previewController;
        private Harmony _harmony;
        private bool _processGuardPassed;

        internal NativeBuildPreviewController PreviewController => _previewController;

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
            Instance = this;

            if (!_config.Enabled.Value)
            {
                Logger.LogInfo("Client plugin disabled by configuration.");
                return;
            }

            _previewController = new NativeBuildPreviewController(_config, _localPlans);
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(ClientPlugin).Assembly);
            Logger.LogInfo("Phase 2 client skeleton loaded; blueprint interception is opt-in and local-only.");
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
            _harmony?.UnpatchAll(PluginGuid);
            _localPlans?.Clear();
            _network?.ResetToVanilla();
            _previewController = null;
            _harmony = null;
            _localPlans = null;
            _network = null;
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        internal AuthorityMode CurrentAuthorityMode => _network?.AuthorityMode ?? AuthorityMode.VanillaClientOnly;
        internal int LocalPlanCount => _localPlans?.Count ?? 0;

        internal void LogLocalBlueprint(CatosBuildHologram.Shared.Contracts.BlueprintRecord blueprint)
        {
            Logger.LogInfo("Stored local blueprint " + blueprint.BlueprintId + " for " + blueprint.PieceTypeId);
        }
    }
}
