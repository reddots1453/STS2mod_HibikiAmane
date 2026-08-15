using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using System.Runtime.CompilerServices;
using MaidenSuccubus.Data;

namespace MaidenSuccubus.Acts;

public enum FourthActRoute
{
    Holy,
    Neutral,
    Corrupt,
}

public interface INeutralFourthActRoutePolicy
{
    FourthActRoute Resolve(RunState runState);
}

public sealed class NeutralPlaceholderRoutePolicy : INeutralFourthActRoutePolicy
{
    public FourthActRoute Resolve(RunState runState) => FourthActRoute.Neutral;
}

public static class FourthActRouteService
{
    private sealed class DebugRoute
    {
        public FourthActRoute Value { get; init; }
    }

    private static readonly ConditionalWeakTable<RunState, DebugRoute> DebugRoutes = new();

    public static INeutralFourthActRoutePolicy NeutralPolicy { get; set; } =
        new NeutralPlaceholderRoutePolicy();

    public static FourthActRoute DefaultForBand(CorruptionBand band) =>
        band switch
        {
            CorruptionBand.Holy => FourthActRoute.Holy,
            CorruptionBand.Corrupt => FourthActRoute.Corrupt,
            _ => FourthActRoute.Neutral,
        };

    public static FourthActRoute Resolve(RunState runState)
    {
        if (FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && M5Progress.Handle.Get(runState).FourthRouteRelicStage >= 4)
        {
            return FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark
                ? FourthActRoute.Holy
                : FourthActRoute.Corrupt;
        }
        return Resolve(CorruptionQuery.GetBand(runState), runState);
    }

    public static FourthActRoute Resolve(CorruptionBand band, RunState runState) =>
        band switch
        {
            CorruptionBand.Holy => FourthActRoute.Holy,
            CorruptionBand.Corrupt => FourthActRoute.Corrupt,
            _ => NeutralPolicy.Resolve(runState),
        };

    public static void ForceNextDebugRoute(RunState runState, FourthActRoute route)
    {
        DebugRoutes.Remove(runState);
        DebugRoutes.Add(runState, new DebugRoute { Value = route });
    }

    public static FourthActRoute ConfigureBoss(RunState runState)
    {
        FourthActRoute route;
        if (DebugRoutes.TryGetValue(runState, out DebugRoute? forced))
        {
            route = forced.Value;
            DebugRoutes.Remove(runState);
        }
        else
        {
            route = Resolve(runState);
        }

        EncounterModel boss = route switch
        {
            FourthActRoute.Holy =>
                ModelDb.Encounter<HolyAct4PlaceholderBoss>(),
            FourthActRoute.Corrupt =>
                ModelDb.Encounter<CorruptAct4PlaceholderBoss>(),
            _ => ModelDb.Encounter<NeutralAct4PlaceholderBoss>(),
        };
        runState.Act.SetBossEncounter(boss);
        MaidenSuccubusMod.Logger.Info(
            $"Fourth Act route={route}; boss={boss.Id.Entry}; "
            + $"corruption={CorruptionQuery.Get(runState)}.");
        return route;
    }
}
