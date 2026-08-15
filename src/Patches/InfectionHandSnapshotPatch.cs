using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class InfectionHandSnapshotPatch
{
    public static void Prefix(CardModel __instance)
    {
        Safe.Run(
            () => InfectionHandSnapshot.Capture(__instance),
            nameof(InfectionHandSnapshotPatch));
    }
}
