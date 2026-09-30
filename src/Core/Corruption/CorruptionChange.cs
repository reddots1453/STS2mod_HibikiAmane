using MegaCrit.Sts2.Core.Runs;

namespace MaidenSuccubus.Core.Corruption;

public readonly record struct CorruptionChangeSource(string Id)
{
    public static readonly CorruptionChangeSource Unknown = new("unknown");
    public static readonly CorruptionChangeSource Debug = new("debug");
    public static readonly CorruptionChangeSource FirstInvasion = new("invasion.first");
    public static readonly CorruptionChangeSource FirstMasturbation = new("rest_site.first_masturbation");
    public static readonly CorruptionChangeSource VirginAct = new("virgin.act");
    public static readonly CorruptionChangeSource DesireFirstMaximum =
        new("desire.first_maximum");
    public static readonly CorruptionChangeSource BossBlessingLight =
        new("boss_blessing.light");
    public static readonly CorruptionChangeSource BossBlessingDark =
        new("boss_blessing.dark");

    public override string ToString() => Id;
}

public readonly record struct CorruptionChanged(
    RunState RunState,
    int OldValue,
    int NewValue,
    CorruptionChangeSource Source)
{
    public int Delta => NewValue - OldValue;
}

public static class CorruptionEvents
{
    public static event Action<CorruptionChanged>? Changed;

    internal static void Publish(CorruptionChanged change)
    {
        var handlers = Changed;
        if (handlers == null)
        {
            return;
        }

        foreach (Action<CorruptionChanged> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(change);
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Corruption Changed listener failed: {ex.Message}");
            }
        }
    }
}
