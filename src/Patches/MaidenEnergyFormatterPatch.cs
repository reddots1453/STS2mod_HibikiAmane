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

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
internal static class MaidenLocalizationInitializePatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        Safe.Run(
            MaidenLocalizationFormatters.Register,
            nameof(MaidenLocalizationInitializePatch));
    }
}

[HarmonyPatch(typeof(LocManager), nameof(LocManager.SmartFormat))]
internal static class MaidenLocalizationSmartFormatPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        Safe.Run(
            MaidenLocalizationFormatters.Register,
            nameof(MaidenLocalizationSmartFormatPatch));
    }
}
