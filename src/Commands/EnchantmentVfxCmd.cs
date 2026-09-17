using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Presents the vanilla enchantment reveal for an already-enchanted card.
/// The vanilla VFX returns the preview to the card's current pile, so the same
/// entry point is valid for deck pickup enchantments and combat-card clones.
/// </summary>
public static class EnchantmentVfxCmd
{
    public static void Preview(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card.Enchantment == null)
            return;

        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
        if (vfx != null)
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
    }
}
