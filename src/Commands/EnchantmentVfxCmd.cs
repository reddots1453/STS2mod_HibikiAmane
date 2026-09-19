using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MaidenSuccubus.Core.Control;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Presents the vanilla enchantment reveal for an already-enchanted card.
/// The vanilla VFX returns the preview to the card's current pile, so the same
/// entry point is valid for deck pickup enchantments and combat-card clones.
/// </summary>
public static class EnchantmentVfxCmd
{
    private static readonly HashSet<CardModel> ActiveCards =
        new(ReferenceEqualityComparer.Instance);

    public static void Preview(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        CardModel previewCard = card;
        bool projected = ControlQuery.GetProjection(card) != null;
        using (ControlQuery.SuppressPresentation())
        {
            if (card.Enchantment == null)
                return;

            if (projected)
            {
                // The VFX owns a temporary NCard for two seconds. Give it an
                // unprojected snapshot so the enchantment icon can be read
                // without mutating or exposing the live Escape card.
                previewCard = (CardModel)card.MutableClone();
            }
        }
        if (projected)
            previewCard.RemoveCapability<EscapeProjectionCapability>();

        // Several effects update both a persistent deck card and its combat
        // clone in the same frame. Only one reveal for a given on-screen card
        // should be alive at once; otherwise the two icon planes overlap.
        if (!ActiveCards.Add(card))
            return;

        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(previewCard);
        var container = NRun.Instance?.GlobalUi.CardPreviewContainer;
        if (vfx == null || container == null)
        {
            ActiveCards.Remove(card);
            vfx?.QueueFree();
            return;
        }

        vfx.TreeExited += () => ActiveCards.Remove(card);
        container.AddChildSafely(vfx);
    }
}
