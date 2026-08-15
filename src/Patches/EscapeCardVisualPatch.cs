using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
internal static class EscapeCardVisualPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance) =>
        Safe.Run(() =>
        {
            var existing = __instance.OverlayContainer.GetNodeOrNull<Godot.Control>(
                EscapeCardVisuals.OverlayName);
            bool projected = __instance.Model != null
                && ControlQuery.GetProjection(__instance.Model) != null;

            if (!projected)
            {
                if (existing != null)
                {
                    __instance.OverlayContainer.RemoveChild(existing);
                    existing.QueueFree();
                }
                return;
            }

            if (existing == null)
            {
                __instance.OverlayContainer.AddChild(EscapeCardVisuals.CreateOverlay());
            }
        }, "Escape.CardOverlay");
}
