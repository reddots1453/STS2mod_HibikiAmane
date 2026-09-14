using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Powers;

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

    /// <summary>
    /// STS2 only creates automatic hover tips for canonical CardKeywords and
    /// PowerVars.  A number of Maiden/Succubus cards intentionally use a
    /// literal amount (for example, "给予2层虚弱") or name a shared mechanic
    /// without owning a DynamicVar.  Resolve those visible references here so
    /// every card follows the same hover contract as vanilla cards.
    ///
    /// This deliberately returns a text-only Scripture tip.  Source cards must
    /// not expand the six Scripture derivative cards beside the hovered card.
    /// </summary>
    public static IEnumerable<IHoverTip> FromDescriptionReferences(CardModel card)
    {
        string description = card.Description.GetRawText();

        foreach ((string term, string key) in StaticTerms)
        {
            if (description.Contains(term, StringComparison.Ordinal))
                yield return Static(key);
        }

        foreach ((string term, Func<IHoverTip> factory) in PowerTerms)
        {
            if (description.Contains(term, StringComparison.Ordinal))
                yield return factory();
        }
    }

    private static readonly (string Term, string Key)[] StaticTerms =
    [
        ("魔力解放", "MAIDENSUCCUBUS_OVERDRAFT"),
        ("变奏", "MAIDENSUCCUBUS_VARIATION"),
        ("欲望", "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE"),
        ("堕落值", "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION"),
        ("圣言", "MAIDENSUCCUBUS_SCRIPTURE"),
        ("挣脱", "MAIDENSUCCUBUS_ESCAPE_KEYWORD"),
        ("拘束", "MAIDENSUCCUBUS_CONTROL"),
        ("诱惑度", "MAIDENSUCCUBUS_TEMPTATION"),
    ];

    private static readonly (string Term, Func<IHoverTip> Factory)[] PowerTerms =
    [
        ("断罪", () => HoverTipFactory.FromPower<CondemnationPower>()),
        ("净化", () => HoverTipFactory.FromPower<PurificationPower>()),
        ("魔力增幅", () => HoverTipFactory.FromPower<MagicAmplificationPower>()),
        ("魔装耐久", () => HoverTipFactory.FromPower<MagicArmorPower>()),
        ("燃烧", () => HoverTipFactory.FromPower<BurningPower>()),
        ("破碎", () => HoverTipFactory.FromPower<ShatterPower>()),
        ("圣域", () => HoverTipFactory.FromPower<SanctuaryPower>()),
        ("虚弱", () => HoverTipFactory.FromPower<WeakPower>()),
        ("易伤", () => HoverTipFactory.FromPower<VulnerablePower>()),
        ("脆弱", () => HoverTipFactory.FromPower<FrailPower>()),
        ("力量", () => HoverTipFactory.FromPower<StrengthPower>()),
        ("敏捷", () => HoverTipFactory.FromPower<DexterityPower>()),
        ("荆棘", () => HoverTipFactory.FromPower<ThornsPower>()),
        ("覆甲", () => HoverTipFactory.FromPower<PlatingPower>()),
        ("滑溜", () => HoverTipFactory.FromPower<SlipperyPower>()),
        ("残影", () => HoverTipFactory.FromPower<BlurPower>()),
        ("变身", () => HoverTipFactory.Static(StaticHoverTip.Transform)),
        ("格挡", () => HoverTipFactory.Static(StaticHoverTip.Block)),
        ("击晕", () => HoverTipFactory.Static(StaticHoverTip.Stun)),
        ("消耗", () => HoverTipFactory.FromKeyword(CardKeyword.Exhaust)),
        ("保留", () => HoverTipFactory.FromKeyword(CardKeyword.Retain)),
        ("虚无", () => HoverTipFactory.FromKeyword(CardKeyword.Ethereal)),
        ("固有", () => HoverTipFactory.FromKeyword(CardKeyword.Innate)),
    ];

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
