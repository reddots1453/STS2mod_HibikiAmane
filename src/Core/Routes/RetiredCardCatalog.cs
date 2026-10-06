using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Core.Routes;

/// <summary>
/// Acquisition policy, not a model registry. Retired identities must remain in
/// AllCards for old saves, history, and multiplayer model-ID serialization.
/// Never use this to delete or replace a card already owned by a player.
/// </summary>
internal static class RetiredCardCatalog
{
    internal static bool IsRetired(CardModel card) =>
        card is MagicResonance or SemenAppetite or Procrastinate;

    internal static IEnumerable<CardModel> Obtainable(IEnumerable<CardModel> cards) =>
        cards.Where(card => !IsRetired(card));
}
