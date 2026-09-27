#if DEBUG
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>Independent DS27-02C design expectations, checked before executing effects.</summary>
internal static class DesignSyncNeutralContract
{
    internal sealed record Expected(Type Model, string Title, CardType Type, CardRarity Rarity,
        TargetType Target, int BaseCost, int UpgradeCost,
        params (string Name, decimal Base, decimal Upgrade)[] Vars);

    internal static readonly Expected[] Entries =
    [
        new(typeof(BorrowedForceStrike), "借力打击", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 9, 10), ("Energy", 1, 2)),
        new(typeof(ForgeStrike), "锻成·打击", CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, 1, 1,
            ("Damage", 6, 9)),
        new(typeof(SummonThunder), "唤雷", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 7, 9)),
        new(typeof(ObstructingShot), "妨碍射击", CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, 2, 1,
            ("Damage", 3, 3)),
        new(typeof(DreamMist), "梦幻之雾", CardType.Skill, CardRarity.Common, TargetType.AllEnemies, 0, 0,
            ("WeakPower", 2, 3)),
        new(typeof(FlameBloom), "火焰绽放", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 8, 11), ("BurningPower", 1, 1)),
        new(typeof(IceBreakingSlash), "碎冰斩", CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, 1, 1,
            ("Damage", 7, 7), ("Cards", 1, 2)),
        new(typeof(CounterBarrier), "反伤屏障", CardType.Power, CardRarity.Rare, TargetType.Self, 1, 0,
            ("ThornsPower", 2, 2), ("PlatingPower", 2, 2)),
        new(typeof(CounterBarrierII), "功性魔防壁II", CardType.Power, CardRarity.Rare, TargetType.Self, 1, 0,
            ("ThornsPower", 3, 3), ("PlatingPower", 3, 3)),
        new(typeof(CounterBarrierIII), "功性魔防壁III", CardType.Power, CardRarity.Rare, TargetType.Self, 1, 0,
            ("ThornsPower", 5, 5), ("PlatingPower", 5, 5)),
        new(typeof(CounterBarrierIV), "功性魔防壁IV", CardType.Power, CardRarity.Rare, TargetType.Self, 2, 1,
            ("ThornsPower", 30, 30), ("PlatingPower", 30, 30)),
        new(typeof(Procrastinate), "拖延", CardType.Skill, CardRarity.Common, TargetType.Self, 0, 0,
            ("Cards", 2, 3)),
        new(typeof(Bath), "泡澡", CardType.Skill, CardRarity.Uncommon, TargetType.Self, 1, 1,
            ("Energy", 2, 2), ("NextEnergy", 2, 3)),
        new(typeof(Lullaby), "子守歌", CardType.Power, CardRarity.Rare, TargetType.Self, 2, 1,
            ("LullabyPower", 2, 2)),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        Expected? expected = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (expected == null) return;
        ctx.AssertEqual("DS27 name", expected.Title, card.TitleLocString.GetFormattedText(), effect: false);
        ctx.AssertEqual("DS27 card type", expected.Type, card.Type, effect: false);
        ctx.AssertEqual("DS27 rarity", expected.Rarity, card.Rarity, effect: false);
        ctx.AssertEqual("DS27 target", expected.Target, card.TargetType, effect: false);
        ctx.AssertEqual("DS27 cost", upgraded ? expected.UpgradeCost : expected.BaseCost,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        Type pool = card is MSGeneratedCard ? typeof(MSGeneratedCardPool) : typeof(MSNeutralCardPool);
        ctx.AssertEqual("DS27 registered pool", pool, card.Pool.GetType(), effect: false);
        foreach (var variable in expected.Vars)
            ctx.AssertEqual("DS27 variable " + variable.Name, upgraded ? variable.Upgrade : variable.Base,
                card.DynamicVars[variable.Name].BaseValue, effect: false);
        bool exhaust = card is DreamMist or ObstructingShot or Procrastinate;
        ctx.AssertEqual("DS27 exhaust keyword", exhaust, card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        if (card is BorrowedForceStrike or ForgeStrike)
            ctx.AssertTrue("DS27 Strike tag", card.Tags.Contains(CardTag.Strike), effect: false);
    }
}
#endif
