using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Util;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers))]
public static class FreeUntilPlayedEnergyPatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(CardEnergyCost __instance, CardModel ____card, CostModifiers modifiers, ref int __result)
    {
        bool free = false;
        Safe.Run(() => free = modifiers.HasFlag(CostModifiers.Local) && !__instance.CostsX
            && FreeUntilPlayedCapability.IsActive(____card), "FreeUntilPlayed.Energy");
        if (free && __result >= 0) __result = 0;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetStarCostWithModifiers))]
public static class FreeUntilPlayedStarPatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel __instance, ref int __result)
    {
        bool free = false;
        Safe.Run(() => free = !__instance.HasStarCostX && FreeUntilPlayedCapability.IsActive(__instance),
            "FreeUntilPlayed.Stars");
        if (free && __result >= 0) __result = 0;
    }
}

// Resolve each required line before the resolver reserves its AmountToSpend.
// Patching ModifyCost would also alter optional/extra spending for the same resource.
[HarmonyPatch(typeof(SecondaryResourcePaymentResolver), "ResolveLine")]
public static class FreeUntilPlayedSecondaryPatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void Postfix(CardModel card, ref SecondaryResourcePaymentLine __result)
    {
        SecondaryResourcePaymentLine result = __result;
        Safe.Run(() =>
        {
            if (!result.CostsX && result.Kind == SecondaryResourceUseKind.RequiredCost
                && FreeUntilPlayedCapability.IsActive(card))
                result = result with
                {
                    Cost = 0, AmountToSpend = 0, Value = 0, IsFree = true,
                    Activated = true, SpendAllowed = true, HasRuntimeCostModifier = true,
                    OriginalShortfall = 0, CoveredShortfall = 0, Shortfall = 0,
                    ShortfallResolution = SecondaryResourceShortfallResolution.None,
                    InsufficientPayment = SecondaryResourceInsufficientPayment.BlockPlay,
                };
        }, "FreeUntilPlayed.Secondary");
        __result = result;
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AfterCardPlayedCleanup))]
public static class FreeUntilPlayedCleanupPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel ____card, ref bool __result)
    {
        bool removed = false;
        Safe.Run(() => removed = ____card.RemoveCapability<FreeUntilPlayedCapability>() != null,
            "FreeUntilPlayed.Cleanup");
        __result |= removed;
    }
}
