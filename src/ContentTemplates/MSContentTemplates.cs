using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Core.Routes;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.ContentTemplates;

public abstract class MSRelicTemplate : ModRelicTemplate
{
    protected MSRelicTriggerCounter TriggerCounter { get; private set; } = new();

    protected void CountTurnTrigger() =>
        TriggerCounter = TriggerCounter.NextTurn();

    protected void CountCombatTrigger() =>
        TriggerCounter = TriggerCounter.NextCombat();

    protected void ResetTriggerCounter() => TriggerCounter = new();
}

public sealed record MSRelicTriggerCounter(
    int Turn = 0,
    int Combat = 0)
{
    public MSRelicTriggerCounter NextTurn() => this with { Turn = Turn + 1 };
    public MSRelicTriggerCounter NextCombat() =>
        this with { Turn = 0, Combat = Combat + 1 };
}

public interface IMSRouteThresholdRelic
{
    bool IsActive(CorruptionBand band);
}

public interface IMSRouteRewardModifierRelic : IRouteRewardProbabilityModifier;

public static class MSRelicDescription
{
    public static string Threshold(int holyMax, int corruptMin) =>
        $"holy<={holyMax}; neutral={holyMax + 1}..{corruptMin - 1}; "
        + $"corrupt>={corruptMin}";
}

public abstract class MSEventTemplate : ModEventTemplate
{
    protected int Corruption => Owner?.RunState is RunState run
        ? CorruptionQuery.Get(run)
        : 0;

    protected int Desire => Owner == null ? 0 : Data.Desire.Get(Owner);

    protected bool MeetsCorruption(int min, int max) =>
        Corruption >= min && Corruption <= max;

    protected bool MeetsDesire(int min, int max = int.MaxValue) =>
        Desire >= min && Desire <= max;
}

public interface IMSRouteWeightedEvent
{
    double GetWeight(RouteCardKind route, RunState runState);
}

public enum MSMonsterThreatClass
{
    Weak,
    Strong,
}

public abstract class MSMonsterTemplate :
    ModMonsterTemplate,
    IControlIntentProvider,
    IInvasionIntentProvider,
    IDesireIntentProvider,
    ILowThreatIntentProvider
{
    public abstract MSMonsterThreatClass ThreatClass { get; }

    protected virtual ControlIntentSpec? ControlIntent => null;
    protected virtual InvasionIntentSpec? InvasionIntent => null;
    protected virtual DesireIntentSpec? DesireIntent => null;
    protected virtual MoveState? LowThreatIntent => null;

    ControlIntentSpec? IControlIntentProvider.GetControlIntent(MonsterModel monster) =>
        ControlIntent;

    InvasionIntentSpec? IInvasionIntentProvider.GetInvasionIntent(MonsterModel monster) =>
        InvasionIntent;

    DesireIntentSpec? IDesireIntentProvider.GetDesireIntent(MonsterModel monster) =>
        DesireIntent;

    MoveState? ILowThreatIntentProvider.GetLowThreatMove(MonsterModel monster) =>
        LowThreatIntent;
}
