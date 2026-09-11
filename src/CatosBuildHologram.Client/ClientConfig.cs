using BepInEx.Configuration;

namespace CatosBuildHologram.Client
{
    internal sealed class ClientConfig
    {
        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<bool> ClientOnlyMode;
        internal readonly ConfigEntry<bool> EnableBlueprintInterception;
        internal readonly ConfigEntry<bool> EnablePredictedSupport;
        internal readonly ConfigEntry<bool> EnableNativeInputAssist;
        internal readonly ConfigEntry<int> MaxLocalPlans;
        internal readonly ConfigEntry<bool> DebugLogging;

        internal ClientConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the client plugin lifecycle. No blueprint interception is active in Phase 1.");
            ClientOnlyMode = config.Bind("Mode", "ClientOnlyMode", true,
                "Keep the client in vanilla-server local mode unless a compatible server role is negotiated.");
            EnableBlueprintInterception = config.Bind("Blueprints", "EnableBlueprintInterception", false,
                "Reserved for the Phase 2 native preview interception. Safe default: disabled.");
            EnablePredictedSupport = config.Bind("Blueprints", "EnablePredictedSupport", false,
                "Reserved until pre-placement support semantics are verified. Safe default: disabled.");
            EnableNativeInputAssist = config.Bind("Automation", "EnableNativeInputAssist", false,
                "Reserved for a guarded ordinary-native-input assist. Safe default: disabled.");
            MaxLocalPlans = config.Bind("Limits", "MaxLocalPlans", 512,
                "Maximum detached local plan records held in memory.");
            DebugLogging = config.Bind("Diagnostics", "DebugLogging", false,
                "Enable bounded diagnostic logging for the client skeleton.");
        }
    }
}
