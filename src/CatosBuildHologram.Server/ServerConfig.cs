using BepInEx.Configuration;

namespace CatosBuildHologram.Server
{
    internal sealed class ServerConfig
    {
        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<bool> EnableAuthoritativeMode;
        internal readonly ConfigEntry<bool> EnableAutobuild;
        internal readonly ConfigEntry<int> MaxBlueprintsPerOwner;
        internal readonly ConfigEntry<int> MaxBlueprintsPerWorld;
        internal readonly ConfigEntry<float> BuilderLeashMeters;
        internal readonly ConfigEntry<bool> DebugLogging;

        internal ServerConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the server plugin lifecycle. No gameplay behavior is active in Phase 1.");
            EnableAuthoritativeMode = config.Bind("Authority", "EnableAuthoritativeMode", true,
                "Allow protocol negotiation once the verified network adapter is implemented.");
            EnableAutobuild = config.Bind("Automation", "EnableAutobuild", false,
                "Safety gate: autobuild remains disabled until atomic material/build semantics are proven.");
            MaxBlueprintsPerOwner = config.Bind("Limits", "MaxBlueprintsPerOwner", 512,
                "Maximum server-owned blueprint records for one authenticated player.");
            MaxBlueprintsPerWorld = config.Bind("Limits", "MaxBlueprintsPerWorld", 4096,
                "Maximum server-owned blueprint records in the world.");
            BuilderLeashMeters = config.Bind("Limits", "BuilderLeashMeters", 32f,
                "Maximum distance from the authenticated builder for future autobuild steps.");
            DebugLogging = config.Bind("Diagnostics", "DebugLogging", false,
                "Enable bounded diagnostic logging for the server skeleton.");
        }
    }
}
