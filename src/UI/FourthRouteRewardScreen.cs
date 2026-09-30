using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.UI;

/// <summary>Use the native loot screen, reward row, tooltip and acquisition animation.</summary>
internal static class FourthRouteRewardScreen
{
    internal static async Task<bool> Show(Player player, FourthRouteRewardOffer offer, Func<bool> isCurrent)
    {
        if (!offer.IsValid || !isCurrent()) return false;
        var reward = new FourthRouteTrialRelicReward(player, offer, isCurrent);
        await new RewardsSet(player).WithCustomRewards([reward]).WithSkippingDisallowed().Offer();
        return reward.SuccessfullySelected;
    }
}

internal sealed class FourthRouteTrialRelicReward : RelicReward
{
    private static readonly MethodInfo ClaimedRelicSetter = AccessTools.PropertySetter(typeof(RelicReward), nameof(ClaimedRelic));
    private readonly FourthRouteRewardOffer _offer;
    private readonly Func<bool> _isCurrent;
    private bool _claiming;

    internal FourthRouteTrialRelicReward(Player player, FourthRouteRewardOffer offer, Func<bool> isCurrent)
        : base(FourthRouteProgressService.CreateRelicPreview(offer.Quest, offer.Stage), player)
    {
        _offer = offer;
        _isCurrent = isCurrent;
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
            // RelicReward's setter is private; the native reward button uses it
            // to animate the actual obtained relic rather than granting a second copy.
            ClaimedRelicSetter.Invoke(this, [obtained]);
            return true;
        }
        finally { _claiming = false; }
    }

    public override void OnSkipped() { } // Saved pending trial is the recovery receipt; no fictitious skip.
}
