using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MaidenSuccubus.Localization;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
internal static class MaidenEnergyFormatterPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        Safe.Run(
            MaidenLocalizationFormatters.Register,
            nameof(MaidenEnergyFormatterPatch));
    }
}
