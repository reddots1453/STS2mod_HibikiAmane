using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Cards;

internal static class CardHoverTipSupport
{
    public static IHoverTip Static(string key) => new HoverTip(
        new LocString("static_hover_tips", $"{key}.title"),
        new LocString("static_hover_tips", $"{key}.description"));

    public static IEnumerable<IHoverTip> FromDynamicPowerVars(
        IEnumerable<DynamicVar> vars)
    {
        foreach (DynamicVar dynamicVar in vars)
        {
            Type? powerType = FindPowerType(dynamicVar.GetType());
            if (powerType == null)
                continue;

            PowerModel? power = ModelDb.AllPowers.FirstOrDefault(
                candidate => candidate.GetType() == powerType);
            if (power != null)
                yield return HoverTipFactory.FromPower(power);
        }
    }

    private static Type? FindPowerType(Type type)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(PowerVar<>))
                return current.GetGenericArguments()[0];
        }

        return null;
    }
}
