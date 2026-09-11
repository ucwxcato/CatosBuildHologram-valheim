using HarmonyLib;

namespace CatosBuildHologram.Client
{
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    internal static class PlayerTryPlacePiecePatch
    {
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            var plugin = ClientPlugin.Instance;
            if (plugin == null || plugin.PreviewController == null)
            {
                return true;
            }

            if (!plugin.PreviewController.TryIntercept(__instance, piece))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
