using HarmonyLib;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class BattleReplayReloadVisualPatch
{
    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    public static void Prefix(NCard __instance) =>
        Safe.Run(() => BattleReplayCardVisuals.Restore(__instance), "ReplayVisual.Restore");
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance) =>
        Safe.Run(() => BattleReplayCardVisuals.Apply(__instance), "ReplayVisual.Reload");
}

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class BattleReplayRefreshVisualPatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance) =>
        Safe.Run(() => BattleReplayCardVisuals.Apply(__instance), "ReplayVisual.Refresh");
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class BattleReplayPortraitVisualPatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance) =>
        Safe.Run(() => BattleReplayCardVisuals.Apply(__instance), "ReplayVisual.Portrait");
}
