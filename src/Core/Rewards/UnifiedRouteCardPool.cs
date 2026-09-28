using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Core.Rewards;

/// <summary>One entry per eligible card, not one equal-weight bucket per route.</summary>
internal static class UnifiedRouteCardPool
{
    internal static CardModel[] Get(Player player, bool rareOnly = false)
    {
        if (player.Character is not MaidenSuccubusCharacter) return [];
        CardPoolModel[] pools = [ModelDb.CardPool<MSNeutralCardPool>(),
            ModelDb.CardPool<MSHolyCardPool>(), ModelDb.CardPool<MSCorruptCardPool>()];
        return pools.SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(card => rareOnly
                ? card.Rarity == CardRarity.Rare && card.CanBeGeneratedByModifiers
                : card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare
                    && card.CanBeGeneratedInCombat && card is not HealingArt)
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToArray();
    }
}
