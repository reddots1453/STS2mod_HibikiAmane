using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
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

    /// <summary>
    /// Pickup hooks run before the game's ordinary obtained-card preview is
    /// created. Defer the enchantment reveal until that preview has left the
    /// shared container, otherwise two identical card nodes overlap and both
    /// enchantment tabs remain visible.
    /// </summary>
    public static void PreviewAfterCardPickup(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        TaskHelper.RunSafely(PreviewAfterCardPickupAsync(card));
    }

    private static async Task PreviewAfterCardPickupAsync(CardModel card)
    {
        // Let CardPileCmd.Add finish its pile-change hooks so its caller can
        // create the standard obtained-card preview first.
        await Cmd.Wait(0.2f);

        for (int check = 0; check < 30; check++)
        {
            var container = NRun.Instance?.GlobalUi.CardPreviewContainer;
            bool ordinaryPreviewIsActive = container?.GetChildren()
                .OfType<NCard>()
                .Any(node => ReferenceEquals(node.Model, card)) == true;
            if (!ordinaryPreviewIsActive)
                break;

            await Cmd.Wait(0.1f);
        }

        Preview(card);
    }

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
