using MegaCrit.Sts2.Core.Runs;
using CorruptionData = MaidenSuccubus.Data.Corruption;

namespace MaidenSuccubus.Core.Corruption;

public static class CorruptionQuery
{
    public static int Get(RunState runState) =>
        CorruptionData.Handle.Get(runState).Value;

    public static CorruptionBand GetBand(RunState runState) =>
        GetBand(Get(runState));

    public static CorruptionBand GetBand(int value)
    {
        int clamped = Math.Clamp(value, CorruptionData.Min, CorruptionData.Max);
        if (clamped <= CorruptionData.HolyThreshold)
        {
            return CorruptionBand.Holy;
        }

        if (clamped >= CorruptionData.CorruptThreshold)
        {
            return CorruptionBand.Corrupt;
        }

        return CorruptionBand.Neutral;
    }

    public static bool IsHoly(RunState runState) =>
        GetBand(runState) == CorruptionBand.Holy;

    public static bool IsNeutral(RunState runState) =>
        GetBand(runState) == CorruptionBand.Neutral;

    public static bool IsCorrupt(RunState runState) =>
        GetBand(runState) == CorruptionBand.Corrupt;

    public static bool IsMaxHoly(RunState runState) =>
        Get(runState) <= CorruptionData.MaxHoly;

    public static bool IsMaxCorrupt(RunState runState) =>
        Get(runState) >= CorruptionData.MaxCorrupt;

    public static bool HasTriggeredOnce(RunState runState, string behaviorId) =>
        CorruptionData.Handle.Get(runState).TriggeredOnceFlags.Contains(behaviorId);
}
