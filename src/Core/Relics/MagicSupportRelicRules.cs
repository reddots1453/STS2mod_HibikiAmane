namespace MaidenSuccubus.Core.Relics;

internal static class MagicSupportRelicRules
{
    internal static bool PrayerVariation(int corruption) => corruption <= -2;

    internal static bool GrantsOnForm(int corruption, bool ownForm, decimal currentAmount, decimal addedAmount) =>
        PrayerVariation(corruption) && ownForm && addedAmount > 0 && currentAmount == addedAmount;

    internal static int NextBarrierTurn(int completed) => (Math.Clamp(completed, 0, 4) + 1) % 5;
}
