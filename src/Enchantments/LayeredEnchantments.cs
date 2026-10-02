using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Enchantments;

internal static class LayeredEnchantments
{
    [ThreadStatic] private static CardModel? _eligibilityCard;

    private sealed class EligibilityScope(CardModel? previous) : IDisposable
    {
        public void Dispose() => _eligibilityCard = previous;
    }

    internal static bool Supports(CardModel card) => card is LightWings;
    internal static bool HasOpenSlot(CardModel card) => Supports(card) || card.Enchantment == null
        || MultiEnchantmentCompatibility.Active; // CanEnchant still enforces type/cap/duplicate rules.
    internal static bool HidesSlot(CardModel card) => ReferenceEquals(card, _eligibilityCard);
    internal static IDisposable HideSlotForEligibility(CardModel card)
    {
        var scope = new EligibilityScope(_eligibilityCard);
        _eligibilityCard = card;
        return scope;
    }

    internal static bool Has<T>(CardModel card) where T : EnchantmentModel =>
        card.Enchantment is T || card.Enchantment is LayeredEnchantment layers && layers.Layers.Any(layer => layer is T)
        || MultiEnchantmentCompatibility.Has<T>(card);

    internal static EnchantmentModel Apply(EnchantmentModel incoming, CardModel card, decimal amount)
    {
        if (!Supports(card) || incoming is LayeredEnchantment)
            throw new InvalidOperationException("Only LightWings accepts layered enchantments.");
        incoming.AssertMutable();
        card.AssertMutable();
        using var originalState = ControlQuery.SuppressPresentation();
        if (!incoming.CanEnchant(card))
            throw new InvalidOperationException($"Enchantment {incoming.Id} is not legal for {card.Id}.");
        if (card.Enchantment is not LayeredEnchantment container)
        {
            EnchantmentModel? original = card.Enchantment;
            container = (LayeredEnchantment)ModelDb.Enchantment<LayeredEnchantment>().ToMutable();
            if (original != null) card.ClearEnchantmentInternal();
            container.Seed(original == null ? [] : [original]);
            card.EnchantInternal(container, 1);
        }
        var applied = container.Add(incoming, amount);
        card.FinalizeUpgradeInternal();
        if (card.Pile is { } pile)
            NCard.FindOnTable(card)?.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
        return applied;
    }

    // Both native streams can be expanded: the run stream sees already-expanded
    // combat children and never expands those a second time. No new subscriptions.
    internal static IEnumerable<AbstractModel> Expand(IEnumerable<AbstractModel> original)
    {
        foreach (var model in original)
        {
            EnchantmentModel[]? layers = null;
            Safe.Run(() =>
            {
                if (model is LayeredEnchantment layered) layers = layered.Layers.ToArray();
            }, "LayeredEnchantments.Expand");
            if (layers == null) { yield return model; continue; }
            foreach (var layer in layers)
                if (layer.HasCard && !layer.Card.HasBeenRemovedFromState && layer.Card.Owner.IsActiveForHooks)
                    yield return layer;
        }
    }
}
