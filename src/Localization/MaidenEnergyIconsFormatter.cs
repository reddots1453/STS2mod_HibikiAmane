using System.Linq;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MaidenSuccubus.UI;
using SmartFormat.Core.Extensions;

namespace MaidenSuccubus.Localization;

public static class MaidenEnergyIconAssets
{
    private const string FallbackPath =
        "res://images/packed/sprite_fonts/defect_energy_icon.png";

    public static string TextIconResourcePath { get; private set; } =
        FallbackPath;

    public static void Register()
    {
        TextIconResourcePath = RuntimeTextureAssets.PrepareResource(
            "ui/core/magic_energy_cost_icon_128.png",
            "user://maiden_succubus_magic_energy_text_icon.tres",
            FallbackPath);
    }
}

/// <summary>
/// Formats energy values with the character's magic-energy artwork without
/// changing the global formatter used by vanilla characters and other mods.
/// </summary>
public sealed class MaidenEnergyIconsFormatter : IFormatter
{
    public string Name
    {
        get => "maidenEnergyIcons";
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

        string icon = $"[img=16x16]{MaidenEnergyIconAssets.TextIconResourcePath}[/img]";
        string output = amount is > 0 and < 4
            ? string.Concat(Enumerable.Repeat(icon, amount))
            : $"{amount}{icon}";
        formattingInfo.Write(output);
        return true;
    }
}
