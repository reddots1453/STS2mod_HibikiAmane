using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Commands;

internal static class GeneratedCardCostCmd
{
    public static void SetFreeUntilPlayed(CardModel card)
    {
        // Fixed-cost contract only; free X/Y payment and effect values await Q17.
        if (!card.EnergyCost.CostsX) card.EnergyCost.SetUntilPlayed(0);
        if (!card.HasStarCostX) card.SetStarCostUntilPlayed(0);
        if (!card.TryGetSecondaryCosts(out SecondaryResourceCostSet costs)) return;
        foreach (string resourceId in costs.ResourceIds.ToArray())
            if (!costs.Get(resourceId).CostsX)
                costs.Set(resourceId, SecondaryResourceCost.Free, SecondaryResourceCostDuration.UntilPlayed);
    }

    public static void SetFreeThisTurn(CardModel card)
    {
        card.SetToFreeThisTurn();
        if (!card.TryGetSecondaryCosts(out SecondaryResourceCostSet costs))
        {
            return;
        }

        foreach (string resourceId in costs.ResourceIds.ToArray())
        {
            costs.Set(
                resourceId,
                SecondaryResourceCost.Free,
                SecondaryResourceCostDuration.UntilPlayed
                    | SecondaryResourceCostDuration.ThisTurn);
        }
    }
}
