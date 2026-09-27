using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))]
public static class StarterRelicSelectionPatch
{
    // A synchronous prefix: an async postfix would be too late (first await).
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance) => Safe.Run(() =>
    {
        if (__instance.DebugOnlyGetState() is not RunState run) return;
        foreach (var player in run.Players) StarterRelicSelection.Apply(player);
    }, "StarterRelic.ApplyNewRunChoice");
}
