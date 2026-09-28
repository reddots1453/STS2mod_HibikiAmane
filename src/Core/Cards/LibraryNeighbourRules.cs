namespace MaidenSuccubus.Core.Cards;

[Flags]
internal enum LibraryAuraEffect { None = 0, Exhaust = 1, Replay = 2 }

internal static class LibraryNeighbourRules
{
    internal static LibraryAuraEffect Evaluate<T>(IReadOnlyList<T> hand, int index, Func<T, bool> isLibrary)
    {
        if (index < 0 || index >= hand.Count) return LibraryAuraEffect.None;
        LibraryAuraEffect effect = LibraryAuraEffect.None;
        if (index + 1 < hand.Count && isLibrary(hand[index + 1])) effect |= LibraryAuraEffect.Exhaust;
        if (index > 0 && isLibrary(hand[index - 1])) effect |= LibraryAuraEffect.Replay;
        return effect;
    }
}
