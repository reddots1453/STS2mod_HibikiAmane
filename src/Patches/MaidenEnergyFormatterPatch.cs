using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MaidenSuccubus.Localization;
using MaidenSuccubus.Util;
using SmartFormat;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
internal static class MaidenEnergyFormatterPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        Safe.Run(
            () => Smart.Default?.AddExtensions(
                new MaidenEnergyIconsFormatter(),
                new MaidenDesireIconsFormatter()),
            nameof(MaidenEnergyFormatterPatch));
    }
}
