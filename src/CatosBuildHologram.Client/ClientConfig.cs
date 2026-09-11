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
        internal readonly ConfigEntry<KeyboardShortcut> GuidedNextKey;
        internal readonly ConfigEntry<bool> ShowStatusHud;

        internal ClientConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the client plugin lifecycle.");
            ClientOnlyMode = config.Bind("Mode", "ClientOnlyMode", true,
                "Keep the client in vanilla-server local mode unless a compatible server role is negotiated.");
            EnableBlueprintInterception = config.Bind("Blueprints", "EnableBlueprintInterception", false,
                "Intercept valid native confirmations into local blueprints. Safe default: disabled.");
            EnablePredictedSupport = config.Bind("Blueprints", "EnablePredictedSupport", false,
                "Reserved until pre-placement support semantics are verified. Safe default: disabled.");
            EnableNativeInputAssist = config.Bind("Automation", "EnableNativeInputAssist", false,
                "Reserved for a guarded ordinary-native-input assist. Safe default: disabled.");
            MaxLocalPlans = config.Bind("Limits", "MaxLocalPlans", 512,
                "Maximum detached local plan records held in memory.");
            DebugLogging = config.Bind("Diagnostics", "DebugLogging", false,
                "Enable bounded diagnostic logging for the client skeleton.");
            GuidedNextKey = config.Bind("Guided", "NextPlanKey", new KeyboardShortcut(UnityEngine.KeyCode.F8),
                "Select the next saved local plan for guided native placement.");
            ShowStatusHud = config.Bind("Interface", "ShowStatusHud", true,
                "Show the bounded local mode and plan status panel.");
        }
    }
}
