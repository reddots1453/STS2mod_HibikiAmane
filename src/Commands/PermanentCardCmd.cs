using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Commands;

/// <summary>
/// Explicit gate for the rare card effects that intentionally persist a
/// mutation from a combat clone back to its run-deck card.
/// </summary>
public static class PermanentCardCmd
{
    public static void ModifyDeckVersion(
        CardModel combatCard,
        Action<CardModel> mutation)
    {
        ArgumentNullException.ThrowIfNull(combatCard);
        ArgumentNullException.ThrowIfNull(mutation);

        if (combatCard is not IPermanentGrowthCard)
        {
            throw new InvalidOperationException(
                $"Card {combatCard.Id} has not opted into permanent growth.");
        }

        if (!CombatEnchantmentCmd.IsCombatClone(combatCard))
        {
            throw new InvalidOperationException(
                $"Card {combatCard.Id} is not an active combat clone.");
        }

        CardModel deckCard = combatCard.DeckVersion
            ?? throw new InvalidOperationException(
                $"Combat card {combatCard.Id} has no run-deck version.");
        if (deckCard is not IPermanentGrowthCard
            || deckCard.GetType() != combatCard.GetType())
        {
            throw new InvalidOperationException(
                $"DeckVersion for {combatCard.Id} is not a matching permanent-growth card.");
        }

        mutation(deckCard);
    }
}
