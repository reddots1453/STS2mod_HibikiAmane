using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Commands;

internal static class GeneratedCardCostCmd
{
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
