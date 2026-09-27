using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.UI;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Acts;

/// <summary>
/// Trial listeners belong to the character, not to a removable starter relic.
/// M5Progress remains the saved source of truth. This extraction does not replace
/// the pending DS27 trial rewrite.
/// </summary>
[RegisterSingleton]
public sealed class FourthRouteLifecycle : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;
    private static readonly ConditionalWeakTable<Player, FourthRouteLifecycle> Instances = new();
    private Player _owner = null!;
    private bool _showingFourthRouteFlow;

    // ModelDb needs a public parameterless canonical model. Runtime per-player
    // listeners must be mutable clones, never new canonical model instances.
    public FourthRouteLifecycle() { }
    internal static FourthRouteLifecycle For(Player player) => Instances.GetValue(player, owner =>
    {
        var listener = (FourthRouteLifecycle)ModelDb.Singleton<FourthRouteLifecycle>().MutableClone();
        listener._owner = owner;
        return listener;
    });
    internal static bool IsEligible(Player player) => player.Character is MaidenSuccubusCharacter;

    // Only the run stream: it is also included by run+combat dispatch, so never
    // subscribe this tracker to both streams (that would double every trial).
    internal static IEnumerable<AbstractModel> Listeners(RunState run) =>
        run.Players.Where(player => player.IsActiveForHooks && IsEligible(player)).Select(For);

    public override Task BeforeCombatStart() => CheckThresholdQuest();
    public override Task AfterRoomEntered(AbstractRoom room) => CheckThresholdQuest();

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) =>
        player == _owner && IsEligible(player) && entry is MerchantRelicEntry { Model: FourthRouteFragmentRelic }
            ? 199m : cost;

    public override Task AfterPotionUsed(PotionModel potion, Creature? target) =>
        potion.Owner == _owner ? TrackSimple(FourthRouteQuest.Gluttony) : Task.CompletedTask;

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        player == _owner ? TrackSimple(FourthRouteQuest.Sloth) : Task.CompletedTask;

    public override Task AfterRestSiteSmith(Player player) =>
        player == _owner ? TrackSimple(FourthRouteQuest.Diligence) : Task.CompletedTask;

    internal Task CheckThresholdQuest() => IsEligible(_owner)
        ? FourthRouteProgressService.CheckThresholdQuest(_owner) : Task.CompletedTask;

    internal async Task EnsureFourthRouteQuestSelected()
    {
        if (!IsEligible(_owner) || _showingFourthRouteFlow || _owner.RunState is not RunState runState
            || FourthRouteProgressService.TryGetQuest(runState, out _)) return;
        _showingFourthRouteFlow = true;
        try
        {
            var rng = runState.Rng.Niche;
            FourthRouteQuest dark = FourthRouteProgressService.DarkQuests[rng.NextInt(7)];
            FourthRouteQuest light = FourthRouteProgressService.LightQuests[rng.NextInt(7)];
            FourthRouteQuest? selected = await FourthRouteSelectionScreen.ChooseQuest(dark, light);
            if (selected is FourthRouteQuest quest)
                FourthRouteProgressService.SelectQuest(runState, quest);
        }
        finally { _showingFourthRouteFlow = false; }
    }

    internal async Task EnsureFourthRouteRewardClaimed()
    {
        if (!IsEligible(_owner) || _showingFourthRouteFlow || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.HasPendingInitialReward(runState)
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        _showingFourthRouteFlow = true;
        try
        {
            if (await FourthRouteSelectionScreen.ShowReward(quest))
                await FourthRouteProgressService.ClaimInitialReward(_owner);
        }
        finally { _showingFourthRouteFlow = false; }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!IsEligible(_owner) || _owner.Creature.IsDead || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        if (quest == FourthRouteQuest.Pride && room.RoomType == RoomType.Elite)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        else if (quest == FourthRouteQuest.Lust && Desire.Get(_owner) >= 5)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        else if (quest == FourthRouteQuest.Chastity && Desire.Get(_owner) <= 2)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        else if (quest == FourthRouteQuest.Wrath && room.CombatState.RoundNumber <= 3)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        else if (quest == FourthRouteQuest.Patience && room.RoomType == RoomType.Monster)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        await CheckThresholdQuest();

        M5ProgressState progress = M5Progress.Handle.Get(runState);
        if (room.RoomType == RoomType.Boss && runState.CurrentActIndex == 2
            && progress.FourthRouteRelicStage == 3)
        {
            await FourthRouteProgressService.AdvanceStage(_owner, 3);
            FourthActRunAdapter.EnsurePresent(runState);
        }
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!IsEligible(_owner) || card.Owner != _owner || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        if (quest == FourthRouteQuest.Envy && oldPileType == PileType.Deck && card.Pile?.Type != PileType.Deck)
            await FourthRouteProgressService.AddProgress(_owner, quest);
        else if (quest == FourthRouteQuest.Benevolence && oldPileType == PileType.None && card.Pile?.Type == PileType.Deck)
            await FourthRouteProgressService.AddProgress(_owner, quest);
    }

    internal Task TrackSimple(FourthRouteQuest expected)
    {
        if (IsEligible(_owner) && _owner.RunState is RunState runState
            && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && quest == expected)
            return FourthRouteProgressService.AddProgress(_owner, quest);
        return Task.CompletedTask;
    }
}
