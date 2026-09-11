using UnityEngine;

namespace CatosBuildHologram.Client
{
    internal sealed class HologramHud
    {
        private readonly ClientConfig _config;
        private readonly HologramRenderer _renderer;
        private readonly GuidedBuildService _guided;

        internal HologramHud(ClientConfig config, HologramRenderer renderer, GuidedBuildService guided)
        {
            _config = config;
            _renderer = renderer;
            _guided = guided;
        }

        internal void Draw(int localPlanCount, bool serverAuthoritative)
        {
            if (!_config.Enabled.Value)
            {
                return;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUILayout.BeginArea(new Rect(12f, 12f, 340f, 110f), GUI.skin.box);
            GUILayout.Label("Catos Build Hologram");
            GUILayout.Label("Mode: " + (serverAuthoritative ? "Server-authoritative" : "Client-only (Predicted/local)"));
            GUILayout.Label("Plans: " + localPlanCount + "  Holograms: " + _renderer.Count);
            GUILayout.Label("Guided target: " + (_guided.ActiveBlueprintId ?? "none") + "  [F8]");
            GUILayout.EndArea();
        }
    }
}
