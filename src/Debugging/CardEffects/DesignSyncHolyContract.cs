#if DEBUG
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncHolyContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(PhotonVolt), "光子伏特", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 10, 12), ("MagicAmplificationPower", 1, 2)),
        new(typeof(InwardDiscipline), "武神的呼吸", CardType.Power, CardRarity.Rare, TargetType.Self, 1, 1,
            ("InwardDisciplinePower", 50, 75)),
        new(typeof(SunDance), "太阳之舞", CardType.Skill, CardRarity.Uncommon, TargetType.Self, 1, 1),
        new(typeof(CalmingMist), "镇静之雾", CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Cards", 1, 2), ("WeakPower", 2, 2)),
        new(typeof(EternalDamnation), "万劫不复", CardType.Power, CardRarity.Rare, TargetType.Self, 2, 1),
        new(typeof(BurningRack), "火刑架", CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, 0, 0,
            ("BurningPower", 2, 3), ("Block", 2, 3)),
        new(typeof(ExorcismPerfume), "退魔香水", CardType.Skill, CardRarity.Common, TargetType.Self, 0, 0,
            ("Temptation", 10, 15), ("Cards", 1, 1)),
        new(typeof(FinalJudgment), "光之审判", CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, 1, 0,
            ("Condemnation", 1, 1)),
        new(typeof(PurificationOrb), "纯净宝珠", CardType.Skill, CardRarity.Uncommon, TargetType.Self, 1, 0,
            ("Desire", 2, 2), ("Armor", 1, 1), ("Temptation", 10, 10)),
        new(typeof(OpeningPrayer), "开祷", CardType.Skill, CardRarity.Common, TargetType.Self, 0, 0,
            ("OpeningPrayerPower", 2, 3)),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        Expected? expected = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (expected == null) return;
        ctx.AssertEqual("DS27 holy name", expected.Title, card.TitleLocString.GetFormattedText(), effect: false);
        ctx.AssertEqual("DS27 holy type", expected.Type, card.Type, effect: false);
        ctx.AssertEqual("DS27 holy rarity", expected.Rarity, card.Rarity, effect: false);
        ctx.AssertEqual("DS27 holy target mode", expected.Target, card.TargetType, effect: false);
        ctx.AssertEqual("DS27 holy cost", upgraded ? expected.UpgradeCost : expected.BaseCost,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("DS27 holy pool", typeof(MSHolyCardPool), card.Pool.GetType(), effect: false);
        foreach (var variable in expected.Vars)
            ctx.AssertEqual("DS27 holy variable " + variable.Name, upgraded ? variable.Upgrade : variable.Base,
                card.DynamicVars[variable.Name].BaseValue, effect: false);
        ctx.AssertEqual("DS27 holy exhaust", card is SunDance or PurificationOrb,
            card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        ctx.AssertEqual("DS27 holy retain", card is SunDance && upgraded,
            card.Keywords.Contains(CardKeyword.Retain), effect: false);
        ctx.AssertEqual("DS27 holy sinking", card is EternalDamnation,
            card.Keywords.Contains(SinkingKeyword.Value), effect: false);
        ctx.AssertEqual("DS27 holy portable", card is PurificationOrb,
            card.Keywords.Contains(PortableKeyword.Value), effect: false);
    }
}
#endif
