using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

public static class FourthActRunAdapter
{
    public static bool Enabled { get; set; } = false;

    private static readonly PropertyInfo ActsProperty =
        AccessTools.Property(typeof(RunState), nameof(RunState.Acts));

    public static bool EnsurePresent(RunState runState)
    {
        if ((!Enabled && !FourthRouteProgressService.CanEnterFourthAct(runState))
            || !runState.Players.Any(p => p.Character is MaidenSuccubusCharacter)
            || runState.Acts.Any(a => a is MaidenSuccubusFourthAct))
        {
            return false;
        }

        List<ActModel> acts = runState.Acts.ToList();
        acts.Add(ModelDb.Act<MaidenSuccubusFourthAct>().ToMutable());
        ActsProperty.SetValue(runState, acts);
        MaidenSuccubusMod.Logger.Info(
            $"Fourth Act appended at index {acts.Count - 1}.");
        return true;
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
            () => FourthActRunAdapter.EnsurePresent(__result),
            "FourthAct.EnsurePresent");
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
