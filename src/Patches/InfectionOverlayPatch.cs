using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HasBuiltInOverlay), MethodType.Getter)]
public static class InfectionHasOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        bool result = __result;
        Safe.Run(() =>
        {
            if (__instance.Enchantment is InfectionEnchantment)
            {
                result = true;
            }
        }, nameof(InfectionHasOverlayPatch));
        __result = result;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CreateOverlay))]
public static class InfectionCreateOverlayPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref Control __result)
    {
        bool runOriginal = true;
        Control? result = null;
        Safe.Run(() =>
        {
            if (__instance is not Infection
                && __instance.Enchantment is InfectionEnchantment)
            {
                result = ModelDb.Card<Infection>().CreateOverlay();
                runOriginal = false;
            }
        }, nameof(InfectionCreateOverlayPatch));
        if (!runOriginal && result != null)
        {
            __result = result;
        }
        return runOriginal;
    }
}
