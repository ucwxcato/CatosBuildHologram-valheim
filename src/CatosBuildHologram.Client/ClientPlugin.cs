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
        private HologramRenderer _renderer;
        private GuidedBuildService _guided;
        private HologramHud _hud;
        private NativeBuildPreviewController _previewController;
        private Harmony _harmony;
        private bool _processGuardPassed;

        internal NativeBuildPreviewController PreviewController => _previewController;
        internal GuidedBuildService Guided => _guided;

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
            _localPlans = new LocalPlanStore(_config.MaxLocalPlans.Value,
                System.IO.Path.Combine(Paths.ConfigPath, "CatosBuildHologram.localplans.json"));
            _network = new NetworkClient(PluginVersion);
            _renderer = new HologramRenderer();
            Instance = this;

            if (!_config.Enabled.Value)
            {
                Logger.LogInfo("Client plugin disabled by configuration.");
                return;
            }

            _previewController = new NativeBuildPreviewController(_config, _localPlans, _renderer);
            _guided = new GuidedBuildService(_config, _localPlans, _renderer);
            _hud = new HologramHud(_config, _renderer, _guided);
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(ClientPlugin).Assembly);
            Logger.LogInfo("Phase 3 client visuals loaded; blueprint interception remains opt-in and client-only until transport is verified.");
        }

        private void Update()
        {
            if (!_processGuardPassed || _config == null || !_config.Enabled.Value)
            {
                return;
            }

            _network.Tick();
            _guided?.Tick();
            _renderer?.Sync(_localPlans.Snapshot());
        }

        private void OnGUI()
        {
            if (_config != null && _config.ShowStatusHud.Value)
            {
                _hud?.Draw(LocalPlanCount, CurrentAuthorityMode == AuthorityMode.ServerAuthoritative);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll(PluginGuid);
            _guided?.Clear();
            _renderer?.Clear();
            _localPlans?.ClearRuntime();
            _network?.ResetToVanilla();
            _previewController = null;
            _guided = null;
            _hud = null;
            _renderer = null;
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
