using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// RitsuLib 0.4.64 resolves X payments in a separate branch that has no
/// shortfall-replacement hook. This postfix only adapts the desire X line when
/// the one-shot HP-payment power is active.
/// </summary>
[HarmonyPatch]
public static class DesireXLifePaymentPatch
{
    public static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(SecondaryResourcePaymentResolver),
            "ResolveLine")
        ?? throw new MissingMethodException(
            typeof(SecondaryResourcePaymentResolver).FullName,
            "ResolveLine");

    public static void Postfix(
        Player __1,
        SecondaryResourceDefinition __3,
        object __6,
        ref SecondaryResourcePaymentLine __result)
    {
        SecondaryResourcePaymentLine updated = __result;
        Safe.Run(
            () =>
            {
                if (!updated.CostsX
                    || updated.IsFree
                    || updated.AmountToSpend <= 0
                    || __3.Id != DesireResource.Id
                    || IsFixedCostFree(__6))
                {
                    return;
                }

                DesirePaidWithHpPower? power =
                    __1.Creature.Powers
                        .OfType<DesirePaidWithHpPower>()
                        .FirstOrDefault();
                if (power != null)
                {
                    updated = power.ReplaceXPayment(updated);
                }
            },
            nameof(DesireXLifePaymentPatch));
        __result = updated;
    }

    private static bool IsFixedCostFree(object freeMode)
    {
        PropertyInfo? property = AccessTools.Property(
            freeMode.GetType(),
            "FixedCostsFree");
        return property?.GetValue(freeMode) is true;
    }
}
