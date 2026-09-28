using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Update the owned instance at the same read boundary used by vanilla damage
// and tooltips. No async hook replacement, extra hit, target roll or subscription.
[HarmonyPatch(typeof(RelicModel), "get_DynamicVars")]
internal static class ForgottenSoulVariablesPatch
{
    private static void Postfix(RelicModel __instance, DynamicVarSet __result)
    {
        Safe.Run(() =>
        {
            if (ForgottenSoulVariation.TryGetDamage(__instance, out int damage))
                __result.Damage.BaseValue = damage;
        }, nameof(ForgottenSoulVariablesPatch));
    }
}

[HarmonyPatch(typeof(RelicModel), "get_Description")]
internal static class ForgottenSoulDescriptionPatch
{
    private static void Postfix(RelicModel __instance, ref LocString __result)
    {
        LocString result = __result;
        Safe.Run(() =>
        {
            if (ForgottenSoulVariation.TryGetDamage(__instance, out int damage))
                result = new LocString("relics", "MAIDEN_SUCCUBUS_FORGOTTEN_SOUL."
                    + (damage == 2 ? "descriptionCorrupt" : "description"));
        }, nameof(ForgottenSoulDescriptionPatch));
        __result = result;
    }
}
