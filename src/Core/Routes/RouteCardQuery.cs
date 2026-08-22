using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Core.Routes;

public static class RouteCardQuery
{
    public static bool TryGet(CardModel card, out RouteCardKind routeKind)
    {
        if (card is IMaidenSuccubusRouteCard routeCard)
        {
            routeKind = routeCard.RouteKind;
            return true;
        }

        routeKind = card.Pool switch
        {
            MSCorruptCardPool => RouteCardKind.Corrupt,
            MSHolyCardPool => RouteCardKind.Holy,
            MSNeutralCardPool => RouteCardKind.Neutral,
            _ => default,
        };
        if (card.Pool is MSCorruptCardPool or MSHolyCardPool or MSNeutralCardPool)
        {
            return true;
        }

        routeKind = default;
        return false;
    }

    public static RouteCardKind Get(CardModel card)
    {
        if (TryGet(card, out var routeKind))
        {
            return routeKind;
        }

        throw new ArgumentException(
            $"Card {card.Id} is not a MaidenSuccubus route card.",
            nameof(card));
    }

    public static bool IsNeutral(CardModel card) =>
        TryGet(card, out var kind) && kind == RouteCardKind.Neutral;

    public static bool IsCorrupt(CardModel card) =>
        TryGet(card, out var kind) && kind == RouteCardKind.Corrupt;

    public static bool IsHoly(CardModel card) =>
        TryGet(card, out var kind) && kind == RouteCardKind.Holy;
}
