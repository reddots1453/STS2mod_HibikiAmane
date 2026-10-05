using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

/// <summary>Use the native loot screen, reward row, tooltip and acquisition animation.</summary>
internal static class FourthRouteRewardScreen
{
    private sealed class PresentationGate
    {
        internal bool Busy;
        internal FourthRouteRewardOffer? LastOffer;
        internal AbstractRoom? LastRoom;
    }
    private static readonly ConditionalWeakTable<Player, PresentationGate> Presentations = new();

    internal static async Task<bool> Show(Player player, FourthRouteRewardOffer offer, Func<bool> isCurrent)
    {
        if (!offer.IsValid || !isCurrent()) return false;
        PresentationGate gate = Presentations.GetValue(player, _ => new PresentationGate());
        var room = player.RunState.CurrentRoom;
        if (gate.Busy || (gate.LastOffer == offer && ReferenceEquals(gate.LastRoom, room))) return false;
        gate.Busy = true;
        gate.LastOffer = offer;
        gate.LastRoom = room;
        var run = player.RunState;
        var scene = NRun.Instance;
        var map = NMapScreen.Instance;
        var stack = NOverlayStack.Instance;
        var previousOverlay = stack?.Peek();
        bool returnToMap = map?.IsOpen == true;
        bool previousTravel = map?.IsTravelEnabled == true;
        bool SameContext() => GodotObject.IsInstanceValid(scene) && scene!.IsInsideTree()
            && ReferenceEquals(NRun.Instance, scene) && ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run)
            && ReferenceEquals(run.CurrentRoom, room) && !player.Creature.IsDead
            && GodotObject.IsInstanceValid(map) && map!.IsInsideTree()
            && ReferenceEquals(NMapScreen.Instance, map)
            && !CombatManager.Instance.IsInProgress && NGame.Instance?.Transition.InTransition != true;
        try
        {
            if (returnToMap)
            {
                // Push() hides every overlay while the map covers the stack. Preserve
                // the original loot page/receipt, but uncover it before offering ours.
                map!.SetTravelEnabled(false);
                map.Close(animateOut: false);
            }
            MaidenSuccubusMod.Logger.Info($"[FourthRouteReward] Present {offer.Quest} {offer.Phase}; returnToMap={returnToMap}.");
            var reward = new FourthRouteTrialRelicReward(player, offer, isCurrent);
            // Closing this screen defers the saved trial receipt; it never forfeits it.
            await new RewardsSet(player).WithCustomRewards([reward]).Offer();
            return reward.SuccessfullySelected;
        }
        finally
        {
            try
            {
                if (returnToMap && SameContext())
                {
                    // Offer completes in the reward callback, before the native button
                    // finishes removing its page. Let that callback finish first.
                    if (NGame.Instance is { } game && GodotObject.IsInstanceValid(game) && game.IsInsideTree())
                        await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
                    Safe.Run(() =>
                    {
                        if (!SameContext()) return;
                        map!.SetTravelEnabled(previousTravel);
                        bool overlayRestored = ReferenceEquals(NOverlayStack.Instance, stack)
                            && ReferenceEquals(stack?.Peek(), previousOverlay);
                        bool restoreMap = overlayRestored && NModalContainer.Instance?.OpenModal == null
                            && NCapstoneContainer.Instance?.InUse != true
                            && NGame.Instance?.InspectCardScreen?.Visible != true
                            && NGame.Instance?.InspectRelicScreen?.Visible != true
                            && NGame.Instance?.FeedbackScreen?.Visible != true;
                        if (restoreMap && !map.IsOpen) map.Open(isOpenedFromTopBar: true);
                        MaidenSuccubusMod.Logger.Info($"[FourthRouteReward] Return {offer.Quest} {offer.Phase}; mapOpen={map.IsOpen}, travel={map.IsTravelEnabled}, overlayRestored={overlayRestored}.");
                    }, "FourthRouteReward.ReturnToMap");
                }
            }
            finally { gate.Busy = false; }
        }
    }
}

// A fixed progression reward owns its claim callback. Do not expose it as a
// RelicReward that random-relic-choice mods may replace and grant independently.
internal sealed class FourthRouteTrialRelicReward : Reward
{
    private readonly RelicReward _preview;
    private readonly FourthRouteRewardOffer _offer;
    private readonly Func<bool> _isCurrent;
    private Control? _icon;
    private bool _claiming;

    internal FourthRouteRelic Relic => (FourthRouteRelic)_preview.Relic!;
    protected override RewardType RewardType => RewardType.Relic;
    public override int RewardsSetIndex => _preview.RewardsSetIndex;
    public override LocString Description => _preview.Description;
    public override bool IsPopulated => _preview.IsPopulated;
    public override IEnumerable<IHoverTip> HoverTips => _preview.HoverTips;
    public override Vector2 IconPosition => _preview.IconPosition;

    internal FourthRouteTrialRelicReward(Player player, FourthRouteRewardOffer offer, Func<bool> isCurrent)
        : base(player)
    {
        _preview = new RelicReward(FourthRouteProgressService.CreateRelicPreview(offer.Quest, offer.Stage), player);
        _offer = offer;
        _isCurrent = isCurrent;
    }

    public override void Populate() => _preview.Populate();
    public override void MarkContentAsSeen() => _preview.MarkContentAsSeen();
    public override TextureRect CreateIcon()
    {
        TextureRect icon = _preview.CreateIcon();
        _icon = icon;
        return icon;
    }

    protected override async Task<bool> OnSelect()
    {
        if (_claiming || SuccessfullySelected || !_isCurrent()) return false;
        _claiming = true;
        try
        {
            if (!await FourthRouteRewardFlow.Claim(Player, _offer)) return false;
            FourthRouteRelic? obtained = Player.Relics.OfType<FourthRouteRelic>()
                .FirstOrDefault(relic => relic.Quest == _offer.Quest && relic.Stage == _offer.Stage);
            if (obtained == null) return false;
            if (TestMode.IsOff && _icon != null && GodotObject.IsInstanceValid(_icon) && _icon.IsInsideTree())
                NRun.Instance?.GlobalUi.RelicInventory.AnimateRelic(obtained, _icon.GlobalPosition);
            MaidenSuccubusMod.Logger.Info($"[FourthRouteReward] Claimed {_offer.Quest} {_offer.Phase}; stage={_offer.Stage}.");
            return true;
        }
        finally { _claiming = false; }
    }

    public override void OnSkipped() { } // Leave the persistent trial receipt pending for a later room.
}
