#if DEBUG
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Pools;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncCardBatchSixContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(DreamPigment), "梦色的颜料", CardType.Skill, CardRarity.Uncommon, TargetType.Self, 1, 0),
        new(typeof(DarkThrust), "黑暗突刺", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 9, 12), ("Cards", 2, 2)),
        new(typeof(MiasmaAbsorption), "瘴气吸收", CardType.Skill, CardRarity.Common, TargetType.Self, 0, 0,
            ("Energy", 2, 3)),
        new(typeof(LastStand), "背水一战", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 0, 0,
            ("CalculationBase", 3, 4), ("ExtraDamage", 3, 4)),
        new(typeof(PleasureDrowning), "沉溺快感", CardType.Skill, CardRarity.Common, TargetType.Self, 1, 1,
            ("Block", 7, 10), ("Cards", 2, 2)),
        new(typeof(ReflectiveBarrier), "反射屏障", CardType.Skill, CardRarity.Common, TargetType.Self, 1, 1,
            ("Block", 8, 8)),
        new(typeof(ThousandCurseScythe), "千咒之大镰", CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, 2, 2,
            ("Damage", 8, 8), ("Growth", 4, 6)),
        new(typeof(MiasmaFlame), "瘴炎", CardType.Attack, CardRarity.Common, TargetType.AllEnemies, 0, 0,
            ("Damage", 7, 10), ("BurningPower", 3, 3)),
        new(typeof(FinalSlash), "终焉的斩击", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 9, 11), ("Cards", 1, 2)),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        Expected? expected = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (expected == null) return;
        ctx.AssertEqual("DS27 batch6 title", expected.Title, card.TitleLocString.GetFormattedText(), effect: false);
        ctx.AssertEqual("DS27 batch6 type", expected.Type, card.Type, effect: false);
        ctx.AssertEqual("DS27 batch6 rarity", expected.Rarity, card.Rarity, effect: false);
        ctx.AssertEqual("DS27 batch6 target", expected.Target, card.TargetType, effect: false);
        ctx.AssertEqual("DS27 batch6 cost", upgraded ? expected.UpgradeCost : expected.BaseCost,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("DS27 batch6 pool", card is DreamPigment ? typeof(MSNeutralCardPool) : typeof(MSCorruptCardPool),
            card.Pool.GetType(), effect: false);
        foreach (var variable in expected.Vars)
            ctx.AssertEqual("DS27 batch6 variable " + variable.Name, upgraded ? variable.Upgrade : variable.Base,
                card.DynamicVars[variable.Name].BaseValue, effect: false);
        ctx.AssertEqual("DS27 batch6 exhaust", card is ThousandCurseScythe,
            card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        ctx.AssertEqual("DS27 batch6 ethereal", card is ReflectiveBarrier && upgraded,
            card.Keywords.Contains(CardKeyword.Ethereal), effect: false);
    }
}
#endif
