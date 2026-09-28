#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Pools;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncVariationTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(DarkElement), "造成4点伤害。\n魔力解放：造成4点伤害。\n堕落值≤-3：变奏。", "造成6点伤害。\n魔力解放：造成6点伤害。\n堕落值≤-3：变奏。"),
        new(typeof(DarkOrigin), "造成8点伤害。\n魔力解放：造成8点伤害。\n堕落值≤-3：变奏。", "造成12点伤害。\n魔力解放：造成12点伤害。\n堕落值≤-3：变奏。"),
        new(typeof(LastStand), "造成3点伤害。\n每有1层负面状态，额外造成3点伤害。", "造成4点伤害。\n每有1层负面状态，额外造成4点伤害。"),
        new(typeof(LegendaryMiner), "每当〈欲望〉被获得或使用，获得3点格挡。", "每当〈欲望〉被获得或使用，获得4点格挡。"),
        new(typeof(RecollectionRoom), "回合开始时额外抽1张牌，并优先从消耗牌堆抽牌。", "固有。\n回合开始时额外抽1张牌，并优先从消耗牌堆抽牌。"),
    ];
    private static readonly Expected[] HolyEntries =
    [
        new(typeof(DarkElement), "造成4点伤害。\n魔力解放：获得4点格挡。\n堕落值＞-3：变奏。", "造成6点伤害。\n魔力解放：获得6点格挡。\n堕落值＞-3：变奏。"),
        new(typeof(DarkOrigin), "造成8点伤害。\n魔力解放：获得8点格挡。\n堕落值＞-3：变奏。", "造成12点伤害。\n魔力解放：获得12点格挡。\n堕落值＞-3：变奏。"),
    ];
    private static readonly (int Value, bool Holy)[] RouteCases =
        [(-5, true), (-3, true), (-2, false), (0, false), (2, false),
         (3, false), (5, false), (-3, true), (-2, false), (-5, true)];
    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (entry == null) return;
        Metadata(ctx, card, upgraded);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        string expected = upgraded ? entry.Upgraded : entry.Base;
        if (card is not DarkElementBase)
        {
            Assert(ctx, outside, PileType.Deck, expected);
            Assert(ctx, card, PileType.Hand, expected + (card is LastStand ? (upgraded ? "\n（造成4点伤害）" : "\n（造成3点伤害）") : ""));
            return;
        }
        var holy = HolyEntries.Single(row => row.Model == card.GetType());
        var run = (RunState)ctx.Player.RunState;
        int saved = CorruptionQuery.Get(run);
        try
        {
            foreach (var test in RouteCases)
            {
                CorruptionCmd.Set(run, test.Value);
                string text = test.Holy ? (upgraded ? holy.Upgraded : holy.Base) : expected;
                foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
                {
                    Assert(ctx, instance, pile, text);
                    ctx.AssertEqual("element route at " + test.Value, test.Holy ? RouteCardKind.Holy : RouteCardKind.Corrupt,
                        RouteCardQuery.Get(instance), effect: false);
                    ctx.AssertEqual("element block presentation matches variation", test.Holy, instance.GainsBlock, effect: false);
                    ctx.AssertTrue("starter variation remains playable route", !CombatSealQuery.IsSealed(run, instance), effect: false);
                }
            }
        }
        finally { CorruptionCmd.Set(run, saved); }
    }

    internal static void LastStandTotal(CardEffectTestContext ctx, LastStand card, bool upgraded, int total)
    {
        var entry = Entries.Single(row => row.Model == typeof(LastStand));
        string expected = upgraded ? entry.Upgraded : entry.Base;
        Assert(ctx, card, PileType.Hand, expected + "\n（造成" + total + "点伤害）");
        var outside = ctx.Player.RunState.CreateCard<LastStand>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        Assert(ctx, outside, PileType.Deck, expected);
    }

    private static void Assert(CardEffectTestContext ctx, CardModel card, PileType pile, string expected)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, card.DynamicVars);
        ctx.AssertEqual("variation full rendered text " + pile, expected,
            DesignSyncHolyTextContract.Normalize(card.GetDescriptionForPile(pile, ctx.PrimaryEnemy)), effect: false);
    }

    private static void Metadata(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        (int Cost, CardType Type, CardRarity Rarity, TargetType Target) expected = card switch
        {
            DarkElement => (0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy),
            DarkOrigin => (0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy),
            LastStand => (0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy),
            LegendaryMiner => (1, CardType.Power, CardRarity.Uncommon, TargetType.Self),
            RecollectionRoom => (1, CardType.Power, CardRarity.Rare, TargetType.Self),
            _ => throw new InvalidOperationException("Unexpected variation text model."),
        };
        ctx.AssertEqual("variation text energy", expected.Cost, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("variation text type", expected.Type, card.Type, effect: false);
        ctx.AssertEqual("variation text rarity", expected.Rarity, card.Rarity, effect: false);
        ctx.AssertEqual("variation text target", expected.Target, card.TargetType, effect: false);
        ctx.AssertTrue("variation text source pool", card.Pool is MSCorruptCardPool, effect: false);
        ctx.AssertTrue("variation text exact keywords",
            card.Keywords.SetEquals(card is RecollectionRoom && upgraded ? [CardKeyword.Innate] : []), effect: false);
    }
}
#endif
