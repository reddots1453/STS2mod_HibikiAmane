namespace MaidenSuccubus.Core.Relics;

/// <summary>Pure branch rules shared with the offline executable contract tests.</summary>
internal static class OrbRules
{
    internal static bool GrantsMaxHp(int corruption, bool sky) => sky || corruption >= 4;

    // Vanilla evaluates this BEFORE removing the selected card. Returning true
    // for the final card would reopen a selector with no cards left.
    internal static bool AllowsMoreCards(int corruption, bool sky, int offeredCount) =>
        offeredCount > 1 && (sky || corruption <= -4);
}
