using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Patches;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TwinSoulChalice : ModRelicTemplate
{
    private bool _showingFourthRouteFlow;
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(5), new MaxHpVar(5), new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/neows_lament.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres");

    public override async Task BeforeCombatStart()
    {
        if (Owner.RunState is not RunState) return;
        await FourthRouteProgressService.CheckThresholdQuest(Owner);
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await FourthRouteProgressService.CheckThresholdQuest(Owner);
    }

    internal async Task EnsureFourthRouteQuestSelected()
    {
        if (_showingFourthRouteFlow || Owner.RunState is not RunState runState
            || FourthRouteProgressService.TryGetQuest(runState, out _)) return;
        _showingFourthRouteFlow = true;
        try
        {
            var rng = Owner.RunState.Rng.Niche;
            FourthRouteQuest dark = FourthRouteProgressService.DarkQuests[rng.NextInt(7)];
            FourthRouteQuest light = FourthRouteProgressService.LightQuests[rng.NextInt(7)];
            FourthRouteQuest? selected = await FourthRouteSelectionScreen.ChooseQuest(dark, light);
            if (selected is FourthRouteQuest quest)
                FourthRouteProgressService.SelectQuest(runState, quest);
        }
        finally
        {
            _showingFourthRouteFlow = false;
        }
    }

    internal async Task EnsureFourthRouteRewardClaimed()
    {
        if (_showingFourthRouteFlow || Owner.RunState is not RunState runState
            || !FourthRouteProgressService.HasPendingInitialReward(runState)
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest))
            return;
        _showingFourthRouteFlow = true;
        try
        {
            if (await FourthRouteSelectionScreen.ShowReward(quest))
                await FourthRouteProgressService.ClaimInitialReward(Owner);
        }
        finally
        {
            _showingFourthRouteFlow = false;
        }
    }

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead) return;
        Flash();
        if (CorruptionQuery.Get((RunState)Owner.RunState) >= 4)
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
        else
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        if (Owner.RunState is RunState runState
            && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest))
        {
            if (quest == FourthRouteQuest.Pride && _.RoomType == RoomType.Elite)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Lust && Data.Desire.Get(Owner) >= 5)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Chastity && Data.Desire.Get(Owner) <= 2)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Wrath && _.CombatState.RoundNumber <= 3)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Patience && _.RoomType == RoomType.Monster)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            await FourthRouteProgressService.CheckThresholdQuest(Owner);

            M5ProgressState progress = M5Progress.Handle.Get(runState);
            if (_.RoomType == RoomType.Boss && runState.CurrentActIndex == 2
                && progress.FourthRouteRelicStage == 3)
            {
                await FourthRouteProgressService.AdvanceStage(Owner, 3);
                FourthActRunAdapter.EnsurePresent(runState);
            }
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) =>
        player == Owner && entry is MerchantRelicEntry { Model: FourthRouteFragmentRelic } ? 199m : cost;

    public override Task AfterPotionUsed(PotionModel potion, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target) =>
        TrackSimple(FourthRouteQuest.Gluttony);

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        player == Owner ? TrackSimple(FourthRouteQuest.Sloth) : Task.CompletedTask;

    public override async Task AfterRestSiteSmith(Player player)
    {
        if (player != Owner) return;
        if (Owner.RunState is RunState runState && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && quest == FourthRouteQuest.Diligence)
            await FourthRouteProgressService.AddProgress(Owner, quest);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != Owner || Owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        if (quest == FourthRouteQuest.Envy && oldPileType == PileType.Deck && card.Pile?.Type != PileType.Deck)
            await FourthRouteProgressService.AddProgress(Owner, quest);
        else if (quest == FourthRouteQuest.Benevolence && oldPileType == PileType.None && card.Pile?.Type == PileType.Deck)
            await FourthRouteProgressService.AddProgress(Owner, quest);
    }

    private Task TrackSimple(FourthRouteQuest expected)
    {
        if (Owner.RunState is RunState runState && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && quest == expected)
            return FourthRouteProgressService.AddProgress(Owner, quest);
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner
            || room is not CombatRoom
            || CorruptionQuery.Get((RunState)player.RunState) > -4)
            return false;

        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, Owner));
        Flash();
        return true;
    }
}
