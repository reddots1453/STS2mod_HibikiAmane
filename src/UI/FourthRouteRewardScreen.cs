using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;

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
        try
        {
            var reward = new FourthRouteTrialRelicReward(player, offer, isCurrent);
            // Closing this screen defers the saved trial receipt; it never forfeits it.
            await new RewardsSet(player).WithCustomRewards([reward]).Offer();
            return reward.SuccessfullySelected;
        }
        finally { gate.Busy = false; }
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
