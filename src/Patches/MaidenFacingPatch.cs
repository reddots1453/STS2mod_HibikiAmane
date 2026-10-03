using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(SurroundedPower), "FaceDirection")]
internal static class MaidenFacingPatch
{
    private static void Postfix(SurroundedPower __instance, ref Task __result) =>
        __result = AfterFacing(__instance, __result);

    private static async Task AfterFacing(SurroundedPower power, Task native)
    {
        await native;
        Safe.Run(() =>
        {
            if (NCombatRoom.Instance?.GetCreatureNode(power.Owner)?.Visuals is MaidenSuccubusCreatureVisuals visuals)
                visuals.SyncFacing();
        }, nameof(MaidenFacingPatch));
    }
}
