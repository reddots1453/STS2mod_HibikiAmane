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

        foreach ((string term, string color, string key, bool matchWithin) in StaticTerms)
        {
            if (HasColoredReference(description, term, color, matchWithin))
                yield return Static(key);
        }

        foreach ((string term, string color, Func<IHoverTip> factory, bool matchWithin) in PowerTerms)
        {
            if (HasColoredReference(description, term, color, matchWithin))
                yield return factory();
        }

        foreach ((string token, Func<IHoverTip> factory) in KeywordTerms)
        {
            // Match the complete rich-text token.  A substring check would
            // incorrectly treat "[gold]消耗牌堆[/gold]" as the Exhaust keyword.
            if (description.Contains(token, StringComparison.Ordinal))
                yield return factory();
        }
    }

    private static readonly
        (string Term, string Color, string Key, bool MatchWithin)[] StaticTerms =
    [
        ("魔力解放", "gold", "MAIDENSUCCUBUS_OVERDRAFT", false),
        ("变奏", "gold", "MAIDENSUCCUBUS_VARIATION", false),
        ("欲望", "pink", "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE", false),
        ("堕落值", "purple", "MAIDENSUCCUBUS_CORRUPTION_REFERENCE", false),
        ("圣言", "gold", "MAIDENSUCCUBUS_SCRIPTURE", true),
        ("挣脱", "gold", "MAIDENSUCCUBUS_ESCAPE_KEYWORD", false),
        ("拘束", "gold", "MAIDENSUCCUBUS_CONTROL", false),
        ("诱惑度", "gold", "MAIDENSUCCUBUS_TEMPTATION_REFERENCE", false),
        ("变身", "gold", "MAIDENSUCCUBUS_TRANSFORMATION", false),
        ("侵犯", "gold", "MAIDENSUCCUBUS_INVASION_REFERENCE", false),
        ("色情攻击", "pink", "MAIDENSUCCUBUS_EROTIC_ATTACK_REFERENCE", false),
    ];

    private static readonly
        (string Term, string Color, Func<IHoverTip> Factory, bool MatchWithin)[] PowerTerms =
    [
        ("断罪", "gold", () => HoverTipFactory.FromPower<CondemnationPower>(), true),
        ("净化", "gold", () => HoverTipFactory.FromPower<PurificationPower>(), false),
        ("魔力增幅", "gold", () => HoverTipFactory.FromPower<MagicAmplificationPower>(), false),
        ("魔装耐久", "gold", () => HoverTipFactory.FromPower<MagicArmorPower>(), false),
        ("燃烧", "gold", () => HoverTipFactory.FromPower<BurningPower>(), false),
        ("破碎", "gold", () => HoverTipFactory.FromPower<ShatterPower>(), false),
        ("圣域", "gold", () => HoverTipFactory.FromPower<SanctuaryPower>(), false),
        ("虚弱", "gold", () => HoverTipFactory.FromPower<WeakPower>(), false),
        ("易伤", "gold", () => HoverTipFactory.FromPower<VulnerablePower>(), false),
        ("脆弱", "gold", () => HoverTipFactory.FromPower<FrailPower>(), false),
        ("力量", "gold", () => HoverTipFactory.FromPower<StrengthPower>(), false),
        ("敏捷", "gold", () => HoverTipFactory.FromPower<DexterityPower>(), false),
        ("荆棘", "gold", () => HoverTipFactory.FromPower<ThornsPower>(), false),
        ("覆甲", "gold", () => HoverTipFactory.FromPower<PlatingPower>(), false),
        ("滑溜", "gold", () => HoverTipFactory.FromPower<SlipperyPower>(), false),
        ("残影", "gold", () => HoverTipFactory.FromPower<BlurPower>(), false),
        ("格挡", "gold", () => HoverTipFactory.Static(StaticHoverTip.Block), false),
        ("击晕", "gold", () => HoverTipFactory.Static(StaticHoverTip.Stun), false),
    ];

    private static readonly (string Token, Func<IHoverTip> Factory)[] KeywordTerms =
    [
        ("[gold]消耗[/gold]", () => HoverTipFactory.FromKeyword(CardKeyword.Exhaust)),
        ("[gold]保留[/gold]", () => HoverTipFactory.FromKeyword(CardKeyword.Retain)),
        ("[gold]虚无[/gold]", () => HoverTipFactory.FromKeyword(CardKeyword.Ethereal)),
        ("[gold]固有[/gold]", () => HoverTipFactory.FromKeyword(CardKeyword.Innate)),
    ];

    private static bool HasColoredReference(
        string description,
        string term,
        string color,
        bool matchWithin)
    {
        string open = $"[{color}]";
        string close = $"[/{color}]";
        int searchFrom = 0;
        while (true)
        {
            int start = description.IndexOf(open, searchFrom, StringComparison.Ordinal);
            if (start < 0)
                return false;
            start += open.Length;
            int end = description.IndexOf(close, start, StringComparison.Ordinal);
            if (end < 0)
                return false;

            ReadOnlySpan<char> content = description.AsSpan(start, end - start);
            if (matchWithin
                ? content.Contains(term, StringComparison.Ordinal)
                : content.Equals(term, StringComparison.Ordinal))
                return true;

            searchFrom = end + close.Length;
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
