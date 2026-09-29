using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Bootstrap;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

public static class FourthActRunAdapter
{
    public static bool Enabled => FourthActEntryRules.NormalEntryEnabled;

    private static readonly PropertyInfo? ActsProperty =
        ModCompatibility.FindWritableProperty(
            typeof(RunState),
            nameof(RunState.Acts),
            typeof(IReadOnlyList<ActModel>));

    public static bool NormalizePendingActs(RunState runState)
    {
        if (ActsProperty == null
            || !runState.Players.Any(p => p.Character is MaidenSuccubusCharacter)) return false;
        IReadOnlyList<ActModel> acts = FourthActEntryRules.WithoutPendingPlaceholder(
            runState.Acts, runState.CurrentActIndex, act => act is MaidenSuccubusFourthAct);
        if (ReferenceEquals(acts, runState.Acts)) return false;
        ActsProperty.SetValue(runState, acts);
        MaidenSuccubusMod.Logger.Info("Removed unentered placeholder Fourth Act; preserving vanilla ending.");
        return true;
    }

    // No process-global enable toggle: only an explicit single-player debug
    // command can append the test act, and it enters that act directly.
    internal static bool EnsureDebugPresent(RunState runState)
    {
#if DEBUG
        if (ActsProperty == null
            || runState.Players.Count != 1
            || runState.Players[0].Character is not MaidenSuccubusCharacter) return false;
        if (runState.Acts.Any(a => a is MaidenSuccubusFourthAct)) return true;

        List<ActModel> acts = runState.Acts.ToList();
        acts.Add(ModelDb.Act<MaidenSuccubusFourthAct>().ToMutable());
        ActsProperty.SetValue(runState, acts);
        MaidenSuccubusMod.Logger.Info(
            $"Fourth Act appended at index {acts.Count - 1}.");
        return true;
#else
        return false;
#endif
    }
}

[HarmonyPatch]
public static class FourthActCreationPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(
            typeof(RunState), nameof(RunState.CreateForNewRun));
        yield return AccessTools.Method(
            typeof(RunState), nameof(RunState.FromSerializable));
    }

    [HarmonyPostfix]
    public static void Postfix(RunState __result) =>
        Safe.Run(
            () => FourthActRunAdapter.NormalizePendingActs(__result),
            "FourthAct.NormalizePendingActs");
}

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class GoddessTrialNewRunPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState __result) => Safe.Run(
        () => GoddessTrialMode.CaptureNewRun(__result), "GoddessTrial.CaptureNewRun");
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
public static class FourthActEndingPatch
{
    // The game's synchronized end-of-act transition (after both bosses on A10)
    // owns timing. Never suppress it or replace its room/ending commands.
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance) => Safe.Run(() =>
    {
        RunState? state = __instance.DebugOnlyGetState();
        if (state is null) return;
        FourthActRunAdapter.NormalizePendingActs(state);
    }, "FourthAct.VanillaEnding");

    [HarmonyPostfix]
    public static void Postfix(RunManager __instance, ref Task __result)
    {
        Task original = __result;
        Task wrapped = original;
        Safe.Run(() =>
        {
            RunState? state = __instance.DebugOnlyGetState();
            if (state?.CurrentActIndex == 2)
                wrapped = ObserveEnding(original, __instance, state);
        }, "FourthAct.ObserveEnding");
        __result = wrapped;
    }

    private static async Task ObserveEnding(Task original, RunManager manager, RunState state)
    {
        // Failure/cancellation propagates unchanged, with no completion receipt.
        await original;
        Safe.Run(() =>
        {
            if (ReferenceEquals(manager.DebugOnlyGetState(), state)
                && state.CurrentRoom is EventRoom { LocalMutableEvent: TheArchitect })
                FourthRouteProgressService.RecordThirdActEnding(state);
        }, "FourthAct.RecordEnding");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetActInternal))]
public static class FourthActRoutePatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, int actIndex) =>
        Safe.Run(
            () =>
            {
                RunState? state = __instance.DebugOnlyGetState();
                if (state != null
                    && actIndex >= 0
                    && actIndex < state.Acts.Count
                    && state.Acts[actIndex] is MaidenSuccubusFourthAct)
                {
                    state.CurrentActIndex = actIndex;
                    FourthActRouteService.ConfigureBoss(state);
                }
            },
            "FourthAct.RouteBoss");
}
