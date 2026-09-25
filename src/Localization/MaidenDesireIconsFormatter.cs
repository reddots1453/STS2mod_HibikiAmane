using System.Linq;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MaidenSuccubus.UI;
using SmartFormat.Core.Extensions;

namespace MaidenSuccubus.Localization;

public static class MaidenDesireIconAssets
{
    private const string FallbackPath =
        "res://images/packed/sprite_fonts/star_icon.png";

    public static string TextIconResourcePath { get; private set; } =
        FallbackPath;

    public static void Register()
    {
        TextIconResourcePath = RuntimeTextureAssets.PrepareResource(
            "ui/core/desire_resource_icon_32.png",
            "user://maiden_succubus_desire_text_icon_32.tres",
            FallbackPath);
    }

    public static string FormatAmount(int amount)
    {
        string icon = $"[img]{TextIconResourcePath}[/img]";
        return amount is > 0 and < 4
            ? string.Concat(Enumerable.Repeat(icon, amount))
            : $"{amount}{icon}";
    }
}

/// <summary>
/// Formats Desire amounts with the same heart artwork used by the secondary
/// resource UI while preserving vanilla rich-text auto-size measurement.
/// </summary>
public sealed class MaidenDesireIconsFormatter : IFormatter
{
    public string Name
    {
        get => "maidenDesireIcons";
        set => throw new NotImplementedException();
    }

    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        object? currentValue = formattingInfo.CurrentValue;
        int amount;
        if (currentValue is EnergyVar energyVar)
        {
            amount = Convert.ToInt32(energyVar.PreviewValue);
        }
        else if (currentValue is CalculatedVar calculatedVar)
        {
            amount = Convert.ToInt32(calculatedVar.Calculate(null));
        }
        else if (currentValue is decimal decimalValue)
        {
            amount = (int)decimalValue;
        }
        else if (currentValue is int intValue)
        {
            amount = intValue;
        }
        else if (currentValue is string
            && int.TryParse(formattingInfo.FormatterOptions, out int parsed))
        {
            amount = parsed;
        }
        else
        {
            return false;
        }

        formattingInfo.Write(MaidenDesireIconAssets.FormatAmount(amount));
        return true;
    }
}
