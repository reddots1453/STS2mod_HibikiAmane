using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
public static class StartRouteUnlockPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, bool __0, bool ____runHistoryWasUploaded,
        long ____startTime, out StartRouteVictoryRecord? __state)
    {
        StartRouteVictoryRecord? victory = null;
        Safe.Run(() =>
        {
            if (!__0 || ____runHistoryWasUploaded || !__instance.ShouldSave || __instance.IsAbandoned
                || __instance.DebugOnlyGetState() is not RunState run
                || LocalContext.GetMe(run) is not { Character: MaidenSuccubusCharacter } player) return;
            // Capture the real local player's final state before native cleanup.
            victory = new StartRouteVictoryRecord
            {
                StartTime = ____startTime,
                PlayerId = player.NetId,
                CharacterCategory = player.Character.Id.Category,
                FinalCorruption = Corruption.Get(run),
                IsVictory = true,
            };
        }, "StartRoutes.CaptureVictory");
        __state = victory;
    }

    [HarmonyPostfix]
    public static void Postfix(StartRouteVictoryRecord? __state)
    {
        if (__state == null) return;
        // A failed archive write must not skip the existing unlock write.
        Safe.Run(() => StartRouteUnlockHistory.RecordVictory(__state), "StartRoutes.ArchiveVictory");
        Safe.Run(() => StartUnlockProgress.RecordVictory(__state.FinalCorruption), "StartRoutes.UnlockAfterVictory");
    }
}
