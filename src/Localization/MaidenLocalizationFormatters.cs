using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using SmartFormat;

namespace MaidenSuccubus.Localization;

internal static class MaidenLocalizationFormatters
{
    private static readonly System.Reflection.FieldInfo? ActiveFormatterField =
        AccessTools.Field(typeof(LocManager), "_smartFormatter");

    public static void Register()
    {
        SmartFormatter? formatter =
            ActiveFormatterField?.GetValue(null) as SmartFormatter
            ?? Smart.Default;
        if (formatter == null)
        {
            return;
        }

        if (!formatter.GetFormatterExtensions().Any(
                extension => extension is MaidenEnergyIconsFormatter))
        {
            formatter.AddExtensions(new MaidenEnergyIconsFormatter());
        }
        if (!formatter.GetFormatterExtensions().Any(
                extension => extension is MaidenDesireIconsFormatter))
        {
            formatter.AddExtensions(new MaidenDesireIconsFormatter());
        }
    }
}
