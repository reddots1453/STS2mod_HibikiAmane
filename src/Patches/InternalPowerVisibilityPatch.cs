using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Patches;

internal static class MaidenInternalPowerUi
{
    // These are existing invisible implementation carriers, not gameplay buffs
    // or the visible enemy desire/control/invasion threshold powers.
    internal static bool IsInternal(PowerModel power) => power is
        EroticIntentRuntimePower or TemptationRuntimePower
        or TakemikazuchiTrackerPower or BattleTechniqueReplayPower;
}

// Preserve the hidden contract even if a framework/compatibility patch ignores
// a derived IsVisibleInternal getter. Do not remove the saved Power instances.
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.IsVisible), MethodType.Getter)]
[HarmonyPriority(Priority.Last)]
internal static class MaidenInternalPowerVisibilityPatch
{
    [HarmonyPostfix]
    private static void Postfix(PowerModel __instance, ref bool __result)
    {
        if (MaidenInternalPowerUi.IsInternal(__instance)) __result = false;
    }
}

// Guard the native UI entry as well: a small getter may be inlined or another
// UI extension may reassert visibility. Both fresh application and saved combat
// reconstruction enter NPowerContainer.Add in 0.107.1 and 0.111.0.
[HarmonyPatch(typeof(NPowerContainer), "Add")]
internal static class MaidenInternalPowerContainerPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PowerModel __0) => !MaidenInternalPowerUi.IsInternal(__0);
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.HoverTips), MethodType.Getter)]
[HarmonyPriority(Priority.Last)]
internal static class MaidenInternalPowerHoverTipsPatch
{
    [HarmonyPostfix]
    private static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (MaidenInternalPowerUi.IsInternal(__instance)) __result = [];
    }
}
