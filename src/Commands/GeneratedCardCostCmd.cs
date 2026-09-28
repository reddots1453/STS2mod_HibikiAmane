using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Cards;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Commands;

internal static class GeneratedCardCostCmd
{
    public static void SetFreeUntilPlayed(CardModel card)
    {
        if (!ModelCapabilities.TryGet(card, out ModelCapabilitySet? set)
            || set.Get<FreeUntilPlayedCapability>() == null)
            card.AddCapability(ModelCapabilityRegistry.Create<FreeUntilPlayedCapability>(), allowMerge: false);
        // Like native free play, only fixed costs become zero; X retains native payment/value.
        if (!card.EnergyCost.CostsX) card.EnergyCost.SetUntilPlayed(0);
        if (!card.HasStarCostX) card.SetStarCostUntilPlayed(0);
        if (!card.TryGetSecondaryCosts(out SecondaryResourceCostSet costs)) return;
        foreach (string resourceId in costs.ResourceIds.ToArray())
            if (!costs.Get(resourceId).CostsX)
                costs.Set(resourceId, SecondaryResourceCost.Free, SecondaryResourceCostDuration.UntilPlayed);
    }

    public static void SetFreeThisTurn(CardModel card)
    {
        // RitsuLib binds this native call to fixed secondary costs as well.
        // Replacing attached costs here would erase X and bypass its native semantics.
        card.SetToFreeThisTurn();
    }
}
