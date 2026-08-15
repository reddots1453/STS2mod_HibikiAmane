using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Compatibility guard for the required Desire payment. RitsuLib normally
/// performs this check itself; keeping the mod-owned guard makes the rule
/// resilient to patch-order and game-signature changes while still honoring
/// free play and registered shortfall replacements such as HP payment.
/// </summary>
[HarmonyPatch]
public static class DesireAffordabilityPatch
{
    public static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [
                typeof(UnplayableReason).MakeByRefType(),
                typeof(AbstractModel).MakeByRefType(),
            ])
        ?? throw new MissingMethodException(
            typeof(CardModel).FullName,
            nameof(CardModel.CanPlay));

    public static void Postfix(
        CardModel __instance,
        ref bool __result,
        ref UnplayableReason reason)
    {
        if (!__result)
        {
            return;
        }

        bool canPlay = true;
        Safe.Run(
            () =>
            {
                if (!__instance.TryGetSecondaryCosts(out var costs)
                    || !costs.Get(DesireResource.Id).IsMaterial)
                {
                    return;
                }

                SecondaryResourcePaymentPlan plan =
                    SecondaryResourcePaymentResolver.Plan(__instance);
                canPlay = plan.Lines.All(line =>
                    line.ResourceId != DesireResource.Id || line.CanPlay);
            },
            nameof(DesireAffordabilityPatch));

        if (!canPlay)
        {
            reason |= UnplayableReason.BlockedByCardLogic;
            __result = false;
        }
    }
}
