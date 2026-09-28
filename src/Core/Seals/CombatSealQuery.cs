using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;

namespace MaidenSuccubus.Core.Seals;

public static class CombatSealQuery
{
    public static bool IsSealed(RunState runState, CardModel card)
    {
        if (!RouteCardQuery.TryGet(card, out var route))
        {
            return false;
        }

        return IsSealed(CorruptionQuery.GetBand(runState), route);
    }

    public static bool IsSealed(
        CorruptionBand band,
        RouteCardKind route) =>
        SealRules.IsSealed(band, route);

    public static IReadOnlyList<CardModel> GetSealedDeckCards(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter
            || player.RunState is not RunState runState)
        {
            return [];
        }

        return player.Deck.Cards
            .Where(card => IsSealed(runState, card))
            .ToList();
    }
}
