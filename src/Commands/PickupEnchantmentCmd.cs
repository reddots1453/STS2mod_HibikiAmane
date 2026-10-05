using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Applies an out-of-combat pickup enchantment and presents the same shimmer,
/// icon reveal, sound, and deck-flight preview used by vanilla enchant relics.
/// </summary>
public static class PickupEnchantmentCmd
{
    /// <summary>
    /// Native permanent acquisition reports None for a pickup, but Deck for
    /// a transformed replacement. Both invoke the hook after deck insertion.
    /// Combat generation, draw/discard movement and load are not pickups.
    /// Each caller retains its saved one-shot flag and existing enchantment.
    /// </summary>
    public static bool IsPickupOrDeckTransformation(
        CardModel self, CardModel changedCard, PileType oldPileType) =>
        ReferenceEquals(self, changedCard)
        && oldPileType is PileType.None or PileType.Deck
        && changedCard.Pile?.Type == PileType.Deck;

    public static T? EnchantAndPreview<T>(CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        T? applied = CardCmd.Enchant<T>(card, amount);
        if (applied != null)
            EnchantmentVfxCmd.PreviewAfterCardPickup(card);
        return applied;
    }

    public static EnchantmentModel? EnchantAndPreview(
        EnchantmentModel enchantment,
        CardModel card,
        decimal amount)
    {
        EnchantmentModel? applied = CardCmd.Enchant(
            enchantment, card, amount);
        if (applied != null)
            EnchantmentVfxCmd.PreviewAfterCardPickup(card);
        return applied;
    }
}
