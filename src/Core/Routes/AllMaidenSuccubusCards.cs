using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Core.Routes;

public static class AllMaidenSuccubusCards
{
    public static IReadOnlyList<CardPoolModel> Pools =>
    [
        ModelDb.CardPool<MSNeutralCardPool>(),
        ModelDb.CardPool<MSCorruptCardPool>(),
        ModelDb.CardPool<MSHolyCardPool>(),
    ];

    // Only default random transformations include eligible derivative cards.
    // Other generation/reward consumers retain the three-route Pools contract.
    public static IReadOnlyList<CardPoolModel> TransformationPools =>
        [.. Pools, ModelDb.CardPool<MSGeneratedCardPool>()];

    public static IEnumerable<CardModel> GetCanonicalCards() =>
        Pools
            .SelectMany(pool => pool.AllCards)
            .Where(card => !RetiredCardCatalog.IsRetired(card))
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal);

    public static IEnumerable<CardModel> GetUnlockedCards(Player player) =>
        Pools
            .SelectMany(pool => pool.GetUnlockedCards(
                player.UnlockState,
                player.RunState.CardMultiplayerConstraint))
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal);
}
