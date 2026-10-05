namespace MaidenSuccubus.Core.Relics;

internal readonly record struct RetentionOrbRule(bool PermanentRetain, bool Upgrade, bool Enchant)
{
    internal static RetentionOrbRule At(int corruption, bool eternal) => eternal
        ? new(true, true, true)
        : new(corruption <= -4, corruption >= 4, corruption <= -4);
}
