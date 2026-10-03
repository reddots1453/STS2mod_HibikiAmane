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
    public static void Prefix(RunManager __instance, bool __0, bool ____runHistoryWasUploaded, out int? __state)
    {
        int? corruption = null;
        Safe.Run(() =>
        {
            if (!__0 || ____runHistoryWasUploaded || !__instance.ShouldSave || __instance.IsAbandoned
                || __instance.DebugOnlyGetState() is not RunState run
                || LocalContext.GetMe(run)?.Character is not MaidenSuccubusCharacter) return;
            // Capture before the native finalizer deletes the current-run save.
            corruption = Corruption.Get(run);
        }, "StartRoutes.CaptureVictory");
        __state = corruption;
    }

    [HarmonyPostfix]
    public static void Postfix(int? __state) => Safe.Run(() =>
    {
        if (__state.HasValue) StartUnlockProgress.RecordVictory(__state.Value);
    }, "StartRoutes.UnlockAfterVictory");
}
