using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Explicit gate for the rare card effects that intentionally persist a
/// mutation from a combat clone back to its run-deck card.
/// </summary>
public static class PermanentCardCmd
{
    /// <summary>
    /// Persists a combat mutation when the card originated in the run deck.
    /// Generated combat-only copies are valid permanent-growth cards too, but
    /// have no deck version to update and therefore return <see langword="false"/>.
    /// </summary>
    public static bool TryModifyDeckVersion(
        CardModel combatCard,
        Action<CardModel> mutation)
    {
        ArgumentNullException.ThrowIfNull(combatCard);
        ArgumentNullException.ThrowIfNull(mutation);

        ValidatePermanentCombatCard(combatCard);

        CardModel? deckCard = combatCard.DeckVersion;
        if (deckCard == null)
        {
            return false;
        }
        if (deckCard is not IPermanentGrowthCard
            || deckCard.GetType() != combatCard.GetType())
        {
            throw new InvalidOperationException(
                $"DeckVersion for {combatCard.Id} is not a matching permanent-growth card.");
        }

        mutation(deckCard);
        return true;
    }

    public static void ModifyDeckVersion(
        CardModel combatCard,
        Action<CardModel> mutation)
    {
        ArgumentNullException.ThrowIfNull(combatCard);
        ArgumentNullException.ThrowIfNull(mutation);

        if (!TryModifyDeckVersion(combatCard, mutation))
            throw new InvalidOperationException(
                $"Combat card {combatCard.Id} has no run-deck version.");
    }

    private static void ValidatePermanentCombatCard(CardModel combatCard)
    {
        if (combatCard is not IPermanentGrowthCard)
            throw new InvalidOperationException(
                $"Card {combatCard.Id} has not opted into permanent growth.");
        if (!CombatEnchantmentCmd.IsCombatClone(combatCard))
            throw new InvalidOperationException(
                $"Card {combatCard.Id} is not an active combat clone.");
    }
}
