using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.UI;
using MaidenSuccubus.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Acts;

/// <summary>
/// Trial listeners belong to the character, not to a removable starter relic.
/// M5Progress owns the saved three-trial phase and counters.
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
        GoddessTrialMode.Enabled(run)
            ? run.Players.Where(player => player.IsActiveForHooks && IsEligible(player)).Select(For)
            : [];

    public override Task BeforeCombatStart() => CheckThresholdQuest();
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsEligible(_owner)) await FourthRouteProgressService.EnsureDormantRelic(_owner);
        await CheckThresholdQuest();
    }
    public override Task AfterGoldGained(Player player) => player == _owner ? CheckThresholdQuest() : Task.CompletedTask;

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) =>
        player == _owner && IsEligible(player) && entry is MerchantRelicEntry { Model: FourthRouteFragmentRelic }
            ? 100m : cost;

    public override Task AfterPotionUsed(PotionModel potion, Creature? target) =>
        potion.Owner == _owner ? TrackSimple(FourthRouteQuest.Gluttony) : Task.CompletedTask;

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        player == _owner && !isMimicked ? TrackSimple(FourthRouteQuest.Sloth) : Task.CompletedTask;

    public override Task AfterRestSiteSmith(Player player) =>
        player == _owner ? TrackSimple(FourthRouteQuest.Diligence) : Task.CompletedTask;

    internal Task CheckThresholdQuest() => IsEligible(_owner)
        ? FourthRouteProgressService.CheckThresholdQuest(_owner) : Task.CompletedTask;

    internal async Task EnsureFourthRouteQuestSelected()
    {
        if (!IsEligible(_owner) || _showingFourthRouteFlow || _owner.RunState is not RunState runState
            || (runState.Players.Count == 1
                ? !FourthRouteOpeningService.NeedsOpening(runState)
                : FourthRouteProgressService.TryGetQuest(runState, out _))) return;
        _showingFourthRouteFlow = true;
        try
        {
            if (runState.Players.Count == 1)
            {
                // First-ever runs can skip Neow. Reuse the narrative flow rather
                // than the old route-only chooser; also resume a locked story.
                NMapScreen? map = NMapScreen.Instance;
                bool Current() => map is not null && GodotObject.IsInstanceValid(map)
                    && map.IsInsideTree() && map.IsOpen && ReferenceEquals(NMapScreen.Instance, map)
                    && ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState);
                MaidenSuccubusMod.Logger.Info("[FourthRouteOpening] entry=map-fallback; needsOpening="
                    + FourthRouteOpeningService.NeedsOpening(runState));
                bool completed = await FourthRouteOpeningScreen.Show(_owner, runState, Current);
                MaidenSuccubusMod.Logger.Info("[FourthRouteOpening] exit=map-fallback; completed=" + completed);
                return;
            }
            var rng = runState.Rng.Niche;
            FourthRouteQuest dark = FourthRouteProgressService.DarkQuests[rng.NextInt(7)];
            FourthRouteQuest light = FourthRouteProgressService.LightQuests[rng.NextInt(7)];
            FourthRouteQuest? selected = await FourthRouteSelectionScreen.ChooseQuest(dark, light);
            if (selected is FourthRouteQuest quest)
            {
                FourthRouteProgressService.SelectQuest(runState, quest);
                await FourthRouteProgressService.EnsureDormantRelic(_owner);
            }
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
            if (runState.Players.Count == 1)
            {
                await FourthRouteRewardFlow.Show(_owner);
                return;
            }
            var offer = new FourthRouteRewardOffer(quest, FourthRouteProgressService.Trial(runState).Phase);
            await FourthRouteRewardScreen.Show(_owner, offer, () => _owner.RunState == runState
                && offer.Matches(quest, FourthRouteProgressService.Trial(runState).Phase));
        }
        finally { _showingFourthRouteFlow = false; }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!IsEligible(_owner) || _owner.Creature.IsDead || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        int trial = FourthRouteTrialRules.Number(FourthRouteProgressService.Trial(runState).Phase);
        if (FourthRouteTrialRules.CountsCombat(quest, trial, room.RoomType == RoomType.Elite,
            room.RoomType == RoomType.Monster, room.CombatState.RoundNumber, Desire.Get(_owner)))
            await FourthRouteProgressService.AddProgress(_owner, quest);
        await CheckThresholdQuest();

        // A boss victory is not a universal final trial. Only the route's own
        // trial/reward flow may awaken its relic; ending is handled by vanilla.
    }

    public override Task BeforeCardRemoved(CardModel card)
    {
        // Permanent deck removal uses this hook, not AfterCardChangedPiles.
        if (!IsEligible(_owner) || card.Owner != _owner || card.Pile?.Type != PileType.Deck
            || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            || quest != FourthRouteQuest.Envy) return Task.CompletedTask;
        return FourthRouteProgressService.AddProgress(_owner, quest);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!IsEligible(_owner) || card.Owner != _owner || _owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            || quest != FourthRouteQuest.Benevolence || oldPileType != PileType.None
            || card.Pile?.Type != PileType.Deck) return Task.CompletedTask;
        return FourthRouteProgressService.AddProgress(_owner, quest);
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
