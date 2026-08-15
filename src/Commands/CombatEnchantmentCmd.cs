using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Enchantments;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Applies an enchantment to a combat-card clone without touching its
/// DeckVersion (the permanent run-deck card) or run-history enchantment log.
/// </summary>
public static class CombatEnchantmentCmd
{
    public static T Apply<T>(CardModel card, decimal amount)
        where T : CombatOnlyEnchantmentTemplate
    {
        T enchantment = (T)ModelDb.Enchantment<T>().ToMutable();
        return Apply(enchantment, card, amount);
    }

    public static T Apply<T>(T enchantment, CardModel card, decimal amount)
        where T : CombatOnlyEnchantmentTemplate
    {
        return ApplyCore(enchantment, card, amount);
    }

    /// <summary>
    /// Applies one of the explicitly audited vanilla enchantments as a
    /// combat-only effect. Sharp and Nimble only read their combat card and do
    /// not synchronize changes through DeckVersion.
    /// </summary>
    public static T ApplyVanilla<T>(CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        if (typeof(T) != typeof(Sharp) && typeof(T) != typeof(Nimble))
        {
            throw new InvalidOperationException(
                $"Vanilla enchantment {typeof(T).Name} has not been audited for combat-only use.");
        }

        T enchantment = (T)ModelDb.Enchantment<T>().ToMutable();
        return ApplyCore(enchantment, card, amount);
    }

    /// <summary>
    /// Applies a registered enchantment whose implementation has been audited
    /// for temporary combat use. Infection is also a valid permanent event
    /// enchantment, but its propagation uses this guarded combat-only path.
    /// </summary>
    public static T ApplyAudited<T>(CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        if (typeof(T) != typeof(InfectionEnchantment)
            && typeof(T) != typeof(ChargeEnchantment)
            && typeof(T) != typeof(NecromancyEnchantment))
        {
            throw new InvalidOperationException(
                $"Registered enchantment {typeof(T).Name} has not been audited for combat-only use.");
        }

        T enchantment = (T)ModelDb.Enchantment<T>().ToMutable();
        return ApplyCore(enchantment, card, amount);
    }

    private static T ApplyCore<T>(T enchantment, CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        ArgumentNullException.ThrowIfNull(enchantment);
        ArgumentNullException.ThrowIfNull(card);

        EnsureCombatClone(card);
        enchantment.AssertMutable();

        // A permanent enchantment is deep-cloned together with its deck card.
        // CardModel only has one Enchantment slot, so it cannot coexist with a
        // combat-only enchantment without a separate overlay system.
        if (card.Enchantment != null)
        {
            if (card.Enchantment is T existing
                && card.DeckVersion?.Enchantment?.GetType() != typeof(T)
                && existing.IsStackable)
            {
                existing.Amount += (int)amount;
                card.FinalizeUpgradeInternal();
                return existing;
            }

            throw new InvalidOperationException(
                $"Cannot apply combat-only enchantment {enchantment.Id} to {card.Id}: " +
                $"the combat card already has enchantment {card.Enchantment.Id}.");
        }

        if (!enchantment.CanEnchant(card))
        {
            throw new InvalidOperationException(
                $"Combat-only enchantment {enchantment.Id} cannot enchant card {card.Id}.");
        }

        // This intentionally mirrors the state-changing part of
        // CardCmd.Enchant, but omits CardsEnchanted run-history recording.
        // The target is the combat clone; DeckVersion is never written.
        card.EnchantInternal(enchantment, amount);
        enchantment.ModifyCard();
        card.FinalizeUpgradeInternal();
        return enchantment;
    }

    public static bool TryApply<T>(CardModel card, decimal amount, out T? enchantment)
        where T : CombatOnlyEnchantmentTemplate
    {
        try
        {
            enchantment = Apply<T>(card, amount);
            return true;
        }
        catch (InvalidOperationException)
        {
            enchantment = null;
            return false;
        }
    }

    public static bool TryApplyAudited<T>(
        CardModel card,
        decimal amount,
        out T? enchantment)
        where T : EnchantmentModel
    {
        try
        {
            enchantment = ApplyAudited<T>(card, amount);
            return true;
        }
        catch (InvalidOperationException)
        {
            enchantment = null;
            return false;
        }
    }

    public static bool IsCombatClone(CardModel card)
    {
        CardPile? pile = card.Pile;
        return pile is { IsCombatPile: true }
            && card.CombatState != null
            && card.Owner.PlayerCombatState?.AllCards.Contains(card) == true
            && !card.Owner.Deck.Cards.Contains(card);
    }

    private static void EnsureCombatClone(CardModel card)
    {
        if (!IsCombatClone(card))
        {
            throw new InvalidOperationException(
                $"Card {card.Id} is not a card instance owned by the active combat state.");
        }
    }
}
