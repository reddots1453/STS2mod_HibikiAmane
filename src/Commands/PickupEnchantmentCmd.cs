using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Applies an out-of-combat pickup enchantment and presents the same shimmer,
/// icon reveal, sound, and deck-flight preview used by vanilla enchant relics.
/// </summary>
public static class PickupEnchantmentCmd
{
    public static T? EnchantAndPreview<T>(CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        T? applied = CardCmd.Enchant<T>(card, amount);
        if (applied != null)
            Preview(card);
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
            Preview(card);
        return applied;
    }

    private static void Preview(CardModel card)
    {
        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }
}
