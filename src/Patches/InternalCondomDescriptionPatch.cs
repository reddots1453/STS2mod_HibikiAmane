using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.DynamicDescription), MethodType.Getter)]
internal static class InternalCondomDescriptionPatch
{
    [HarmonyPostfix]
    private static void AfterDescription(RelicModel __instance, ref LocString __result)
    {
        if (__instance is InternalCondom condom && condom.CorruptVariation)
            __result = new LocString("relics", "MAIDEN_SUCCUBUS_RELIC_INTERNAL_CONDOM.descriptionCorrupt");
    }
}
