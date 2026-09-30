using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Core.Rewards;

public static class RouteCardRewardService
{
    public static bool TryReplaceOne(
        Player player,
        List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (!IsEligibleEncounterReward(player, options, creationOptions))
        {
            return false;
        }

        Rng rng = creationOptions.RngOverride ?? player.PlayerRng.Rewards;
        var bonus = RouteRewardProbabilityModifiers.GetTotal(player);
        var probabilities = RouteRewardProbabilities.Calculate(
            CorruptionQuery.Get((RunState)player.RunState),
            bonus.Holy,
            bonus.Corrupt);
        RouteCardKind selectedRoute = RollRoute(probabilities, rng.NextFloat());
        if (selectedRoute == RouteCardKind.Neutral)
        {
            return false;
        }

        int replacementIndex = rng.NextInt(options.Count);
        CardModel originalCard = options[replacementIndex].Card;
        CardModel[] candidates = GetCandidates(
            player,
            selectedRoute,
            originalCard.Rarity,
            options);
        if (candidates.Length == 0)
        {
            MaidenSuccubusMod.Logger.Info(
                $"Route reward skipped: route={selectedRoute}, " +
                $"rarity={originalCard.Rarity}, no eligible card.");
            return false;
        }

        CardModel canonicalReplacement = rng.NextItem(candidates)
            ?? throw new InvalidOperationException(
                "Route reward candidate selection returned null.");
        CardModel replacement = player.RunState.CreateCard(
            canonicalReplacement,
            player);
        CopyUpgradeLevel(originalCard, replacement);
        options[replacementIndex] = new CardCreationResult(replacement);

        MaidenSuccubusMod.Logger.Info(
            $"Route reward replaced slot {replacementIndex}: " +
            $"{originalCard.Id} -> {replacement.Id}, " +
            $"route={selectedRoute}, rarity={replacement.Rarity}.");
        return true;
    }

    public static RouteCardKind RollRoute(RouteRewardProbabilities probabilities, float roll) => probabilities.RollRoute(roll);

    private static bool IsEligibleEncounterReward(
        Player player,
        List<CardCreationResult> options,
        CardCreationOptions creationOptions) =>
        player.Character is MaidenSuccubusCharacter
        && player.RunState is RunState
        && options.Count > 0
        && creationOptions.Source == CardCreationSource.Encounter
        && creationOptions.RarityOdds is
            CardRarityOddsType.RegularEncounter
            or CardRarityOddsType.EliteEncounter
            or CardRarityOddsType.BossEncounter;

    private static CardModel[] GetCandidates(
        Player player,
        RouteCardKind route,
        CardRarity rarity,
        IEnumerable<CardCreationResult> currentOptions)
    {
        CardPoolModel pool = route switch
        {
            RouteCardKind.Corrupt => ModelDb.CardPool<MSCorruptCardPool>(),
            RouteCardKind.Holy => ModelDb.CardPool<MSHolyCardPool>(),
            _ => throw new ArgumentOutOfRangeException(nameof(route), route, null),
        };

        HashSet<ModelId> offeredIds = currentOptions
            .Select(option => option.Card.Id)
            .ToHashSet();

        return pool
            .GetUnlockedCards(
                player.UnlockState,
                player.RunState.CardMultiplayerConstraint)
            .Where(card =>
                card.Rarity == rarity
                && !offeredIds.Contains(card.Id))
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToArray();
    }

    private static void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        int desiredLevel = Math.Min(
            source.CurrentUpgradeLevel,
            target.MaxUpgradeLevel);
        while (target.CurrentUpgradeLevel < desiredLevel)
        {
            CardCmd.Upgrade(target, CardPreviewStyle.None);
        }
    }
}
