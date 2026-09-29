using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// The vanilla reveal hides the normal tab in _Ready, but later model updates
/// call UpdateVisuals and turn it back on while the reveal tab is still active.
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
internal static class EnchantmentRevealVisualPatch
{
    [HarmonyPostfix]
    private static void AfterUpdate(NCard __instance) => Safe.Run(() =>
    {
        if (__instance.Model?.Enchantment == null) return;
        for (var parent = __instance.GetParent(); parent != null; parent = parent.GetParent())
        {
            if (parent is not NCardEnchantVfx) continue;
            __instance.EnchantmentTab.Visible = false;
            return;
        }
    }, nameof(AfterUpdate));
}
