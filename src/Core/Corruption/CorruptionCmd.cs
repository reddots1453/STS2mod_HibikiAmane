using MegaCrit.Sts2.Core.Runs;
using CorruptionData = MaidenSuccubus.Data.Corruption;

namespace MaidenSuccubus.Core.Corruption;

public static class CorruptionCmd
{
    public static int Modify(
        RunState runState,
        int delta,
        CorruptionChangeSource? source = null) =>
        Set(runState, CorruptionQuery.Get(runState) + delta, source);

    public static int Set(
        RunState runState,
        int value,
        CorruptionChangeSource? source = null)
    {
        int oldValue = CorruptionQuery.Get(runState);
        int newValue = Math.Clamp(value, CorruptionData.Min, CorruptionData.Max);
        if (newValue == oldValue)
        {
            return newValue;
        }

        CorruptionData.Handle.Modify(runState, data => data.Value = newValue);
        CorruptionEvents.Publish(new CorruptionChanged(
            runState,
            oldValue,
            newValue,
            source ?? CorruptionChangeSource.Unknown));
        return newValue;
    }

    public static bool TryTriggerOnce(RunState runState, string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            throw new ArgumentException(
                "A stable behavior ID is required.",
                nameof(behaviorId));
        }

        if (CorruptionQuery.HasTriggeredOnce(runState, behaviorId))
        {
            return false;
        }

        CorruptionData.Handle.Modify(
            runState,
            data => data.TriggeredOnceFlags.Add(behaviorId));
        return true;
    }
}
