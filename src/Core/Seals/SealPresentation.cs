using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;

namespace MaidenSuccubus.Core.Seals;

internal static class SealPresentation
{
    internal static string? DescriptionKey(CardModel? card)
    {
        if (card?.IsMutable != true || card.Owner?.Character is not MaidenSuccubusCharacter
            || card.Owner.RunState is not RunState run || !RouteCardQuery.TryGet(card, out var route))
            return null;
        return SealRules.DescriptionKey(true, card.Owner.Deck.Cards.Contains(card), CorruptionQuery.GetBand(run), route);
    }
}
