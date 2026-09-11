using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

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

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    internal static class PlayerPlacePiecePatch
    {
        private static void Postfix(Piece piece, UnityEngine.Vector3 pos, UnityEngine.Quaternion rot)
        {
            ClientPlugin.Instance?.Guided?.OnNativePiecePlaced(piece, pos, rot);
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.GetSnapPoints),
        new[] { typeof(Vector3), typeof(float), typeof(List<Transform>), typeof(List<Piece>) })]
    internal static class PieceNearbySnapPointsPatch
    {
        private static void Postfix(Vector3 point, float radius, List<Transform> points)
        {
            ClientPlugin.Instance?.Renderer?.AppendNearbySnapPoints(point, radius, points);
        }
    }
}
