using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

/// <summary>RELIC-START-004: route reward lens and neutral reroll.</summary>
[RegisterRelic(typeof(MSRelicPool))]
public sealed class BalancedLens : MSRelicTemplate, IMSRouteRewardModifierRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public RouteRewardProbabilityBonus GetRouteRewardProbabilityBonus(
        Player player)
    {
        int corruption = CorruptionQuery.Get((RunState)player.RunState);
        return new RouteRewardProbabilityBonus(
            Holy: 0.20m + (corruption <= -2 ? 0.20m : 0m),
            Corrupt: 0.20m + (corruption >= 2 ? 0.20m : 0m));
    }

    public override bool TryModifyRewardsLate(
        Player player,
        List<Reward> rewards,
        AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        int corruption = CorruptionQuery.Get((RunState)player.RunState);
        if (corruption is < -1 or > 1)
        {
            return false;
        }

        bool changed = false;
        foreach (CardReward reward in rewards.OfType<CardReward>())
        {
            reward.CanReroll = true;
            changed = true;
        }
        if (changed)
        {
            Flash();
        }
        return changed;
    }
}

/// <summary>RELIC-EVENT-001. Rendering is suppressed by BlindfoldIntentPatch.</summary>
[RegisterRelic(typeof(MSRelicPool))]
public sealed class Blindfold : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> AdditionalHoverTips =>
        BlindfoldPresentation.PreviewTips(this);
    public override Task AfterObtained() => BlindfoldPresentation.Refresh(Owner);
    public override Task AfterRemoved() => BlindfoldPresentation.Refresh(Owner);

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");
}

/// <summary>EVENT-VANILLA-002 Whispering Hollow reward; STS1 Dead Branch semantics.</summary>
[RegisterRelic(typeof(MSRelicPool))]
public sealed class WitheredTreeSoul : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public override async Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner != Owner || !CombatManager.Instance.IsInProgress) return;
        List<CardModel> created = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            1,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (created.FirstOrDefault() is not { } generated) return;
        Flash();
        await CardPileCmd.Add(generated, PileType.Hand);
    }
}

/// <summary>RELIC-EVENT-002: first unblocked damage each player turn.</summary>
[RegisterRelic(typeof(MSRelicPool))]
public sealed class Vibrator : MSRelicTemplate
{
    private bool _triggeredThisTurn;
    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_triggeredThisTurn
            || target != Owner.Creature
            || result.UnblockedDamage <= 0
            || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        _triggeredThisTurn = true;
        Flash();
        await Data.Desire.Modify(Owner, 1);
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner)
        {
            _triggeredThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _triggeredThisTurn = false;
        return Task.CompletedTask;
    }
}
