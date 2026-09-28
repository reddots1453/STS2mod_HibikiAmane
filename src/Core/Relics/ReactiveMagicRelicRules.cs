namespace MaidenSuccubus.Core.Relics;

internal static class ReactiveMagicRelicRules
{
    internal static bool MirrorVariation(int corruption) => corruption <= -3;

    internal static bool ShouldReflect(bool ownApplication, bool enemyInSameCombat,
        decimal actualChange, bool harmfulChange, bool reacting) =>
        ownApplication && enemyInSameCombat && actualChange != 0 && harmfulChange && !reacting;

    internal static int NextStardust(int progress) => (Math.Clamp(progress, 0, 6) + 1) % 7;
}
