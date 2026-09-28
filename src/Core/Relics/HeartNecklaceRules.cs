namespace MaidenSuccubus.Core.Relics;

internal static class HeartNecklaceRules
{
    internal static bool IsMurky(int corruption) => corruption >= 3;

    internal static int TurnDelta(int corruption, int desire) => IsMurky(corruption)
        ? desire <= 5 ? 1 : 0
        : desire >= 5 ? -1 : 0;
}
