using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
internal static class RunUiCombatEnteredPatch
{
    [HarmonyPostfix]
    private static void Postfix() => Safe.Run(
        () => RunUiRefreshEvents.PublishCombatVisibility(inCombat: true),
        "RunUi.CombatEntered");
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._ExitTree))]
internal static class RunUiCombatExitedPatch
{
    [HarmonyPrefix]
    private static void Prefix() => Safe.Run(
        () => RunUiRefreshEvents.PublishCombatVisibility(inCombat: false),
        "RunUi.CombatExited");
}
