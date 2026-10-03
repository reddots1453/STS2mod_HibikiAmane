using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// The lobby payload is imported before starter finalization. CreateForNewRun
// is too early to read it. Loaded runs never call FinalizeStartingRelics.
[HarmonyPatch(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))]
public static class StartProfilePatch
{
    [HarmonyPrefix, HarmonyPriority(Priority.High)]
    public static void Prefix(RunManager __instance) => Safe.Run(() =>
    {
        if (__instance.DebugOnlyGetState() is not RunState run) return;
        // Corruption is already a shared run resource; preserve its existing
        // first-Maiden ownership rule instead of last-player-wins mutation.
        var player = run.Players.FirstOrDefault(p => p.Character is MaidenSuccubusCharacter);
        if (player == null) return;
        var state = StarterRelicChoice.Handle.Get(player);
        if (state.RouteApplied) return;
        var route = StartUnlockProgress.Normalize(state.Route);
        if (route != MaidenSuccubusStartProfileId.Normal && !state.RouteUnlockedAtSelection)
            route = MaidenSuccubusStartProfileId.Normal;
        int initial = StartUnlockProgress.InitialValue(route);
        CorruptionCmd.Set(run, initial, new CorruptionChangeSource($"start.{route.ToString().ToLowerInvariant()}"));
        StarterRelicChoice.Handle.Modify(player, data => { data.Route = route; data.RouteApplied = true; });
        M5Progress.Handle.Modify(run, data =>
        {
            data.StartProfileApplied = true;
            data.StartProfileId = route.ToString();
        });
        MaidenSuccubusMod.Logger.Info($"[StartRoutes] Applied {route}; initial corruption={initial}.");
    }, "StartProfile.FinalizeNewRun");
}
