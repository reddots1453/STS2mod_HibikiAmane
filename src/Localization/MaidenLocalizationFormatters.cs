using SmartFormat;

namespace MaidenSuccubus.Localization;

internal static class MaidenLocalizationFormatters
{
    public static void Register()
    {
        SmartFormatter? formatter = Smart.Default;
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
