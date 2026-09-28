using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Rewards;

internal static class GenerosityOffering
{
    private static readonly ConditionalWeakTable<RelicModel, object> PendingTreasure = new();

    internal static bool CanOffer(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || !FourthRouteProgressService.TryGetQuest(run, out var quest) || quest != FourthRouteQuest.Generosity)
            return false;
        return GenerosityOfferingRules.CanOffer(true, true, FourthRouteProgressService.Trial(run).Phase,
            player.Relics.OfType<GenerosityRouteRelic>().Select(relic => relic.Stage).DefaultIfEmpty(0).Max(),
            player.Deck.Cards.Count(card => card.IsRemovable));
    }

    internal static void WrapCombatRewards(RewardsSet set)
    {
        if (set.Room is not CombatRoom || !CanOffer(set.Player)) return;
        for (int i = 0; i < set.Rewards.Count; i++)
            if (set.Rewards[i].GetType() == typeof(RelicReward)
                && set.Rewards[i] is RelicReward { Relic: { } relic })
                set.Rewards[i] = new GenerosityOfferingGroup(relic, set.Player);
    }

    internal static bool IsPendingTreasure(RelicModel relic) => PendingTreasure.TryGetValue(relic, out _);

    // Called only at the native post-allocation treasure obtain site, never globally from RelicCmd.
    public static async Task<RelicModel> ObtainAllocated(RelicModel relic, Player player, int index = -1)
    {
        if (!CanOffer(player)) return await RelicCmd.Obtain(relic, player, index);
        PendingTreasure.Add(relic, new object());
        try
        {
            await RewardsCmd.OfferCustom(player, [new GenerosityOfferingGroup(relic, player)]);
            return relic;
        }
        finally { PendingTreasure.Remove(relic); }
    }

    internal static void AllowObtainAnimation(RelicModel relic) => PendingTreasure.Remove(relic);

    internal static async Task<bool> ApplyOffering(Player player)
    {
        if (!CanOffer(player)) return false;
        var run = (RunState)player.RunState;
        if (FourthRouteTrialRules.Active(FourthRouteProgressService.Trial(run).Phase))
        {
            await FourthRouteProgressService.AddProgress(player, FourthRouteQuest.Generosity);
            return true;
        }
        var selected = await CardSelectCmd.FromDeckForRemoval(player,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2) { Cancelable = false });
        var valid = selected.Where(card => card.Owner == player && card.Pile?.Type == PileType.Deck && card.IsRemovable)
            .Distinct().Take(2).ToArray();
        if (valid.Length == 0) return false;
        foreach (var card in valid)
            if (card.Pile?.Type == PileType.Deck && card.Owner == player && card.IsRemovable)
                await CardPileCmd.RemoveFromDeck(card);
        return true;
    }
}

internal sealed class GenerosityOfferingGroup : LinkedRewardSet
{
    private bool _busy;
    private bool _skipRecorded;
    internal bool Resolved { get; private set; }
    internal Reward[] Choices { get; }
    internal GenerosityRelicReward RelicChoice => (GenerosityRelicReward)Choices[0];

    internal GenerosityOfferingGroup(RelicModel relic, Player player)
        : this(new GenerosityRelicReward(relic, player), new GenerosityOfferReward(player), player) { }

    private GenerosityOfferingGroup(GenerosityRelicReward relic, GenerosityOfferReward offer, Player player)
        : base([relic, offer], player)
    {
        Choices = [relic, offer];
        relic.Group = this;
        offer.Group = this;
    }

    internal async Task<bool> Choose(Func<Task<bool>> action)
    {
        if (_busy || Resolved) return false;
        _busy = true;
        try
        {
            if (!await action()) return false;
            Resolved = true;
            // Native LinkedRewardSet doesn't mark its parent complete when a child is selected.
            // Mark this group only, without firing a second AfterRewardTaken for a fictitious reward.
            AccessTools.PropertySetter(typeof(Reward), nameof(SuccessfullySelected)).Invoke(this, [true]);
            return true;
        }
        finally { _busy = false; }
    }

    public override void OnSkipped()
    {
        if (_skipRecorded || RelicChoice.SuccessfullySelected) return;
        _skipRecorded = true;
        RelicChoice.OnSkipped();
    }

    // The native room save contains a deterministic relic, not an unregistered RewardType.None.
    // Eligibility is evaluated again when the restored combat rewards are generated.
    public override SerializableReward ToSerializable() => RelicChoice.ToSerializable();
}

internal sealed class GenerosityRelicReward(RelicModel relic, Player player) : RelicReward(relic, player)
{
    internal GenerosityOfferingGroup Group { get; set; } = null!;
    protected override Task<bool> OnSelect() => Group.Choose(async () =>
    {
        GenerosityOffering.AllowObtainAnimation(Relic!);
        return await base.OnSelect();
    });
}

internal sealed class GenerosityOfferReward(Player player) : Reward(player)
{
    internal GenerosityOfferingGroup Group { get; set; } = null!;
    protected override RewardType RewardType => RewardType.None;
    public override int RewardsSetIndex => 3;
    public override bool IsPopulated => true;
    public override LocString Description => new("relics", "MAIDEN_GENEROSITY_OFFER.description");
    public override void Populate() { }
    public override void MarkContentAsSeen() { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [new HoverTip(Description, new LocString("relics", Player.RunState is RunState run
            && FourthRouteTrialRules.Active(FourthRouteProgressService.Trial(run).Phase)
                ? "MAIDEN_GENEROSITY_OFFER.progress" : "MAIDEN_GENEROSITY_OFFER.removal"))];
    public override Control? CreateIcon()
    {
        if (TestMode.IsOn) return null;
        var icon = new Label
        {
            Text = "供奉", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 0.83f, 0.35f),
        };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        icon.AddThemeFontSizeOverride("font_size", 20);
        return icon;
    }
    protected override Task<bool> OnSelect() => Group.Choose(async () =>
    {
        if (!await GenerosityOffering.ApplyOffering(Player)) return false;
        Group.OnSkipped();
        return true;
    });
}
