#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncStarterTextContract
{
    // Basic Strike/Defend use native defaults; Drowsy uses native keyword wording/order.
    internal static readonly Expected[] Entries =
    [
        new(typeof(MaidenStrike), "造成6点伤害。", "造成9点伤害。"),
        new(typeof(MaidenDefend), "获得5点格挡。", "获得8点格挡。"),
        new(typeof(Transform), "进入变身：无垢天衣形态。\n堕落值≥3：变奏。\n消耗。", "固有。\n进入变身：无垢天衣形态。\n堕落值≥3：变奏。\n消耗。"),
        new(typeof(DrowsyStatus), "不能被打出。\n保留。", "不能被打出。\n保留。"),
        new(typeof(BindingInsight), "每当你打出挣脱牌时，获得〈能量〉。\n随身。", "每当你打出挣脱牌时，获得〈能量〉。\n随身。"),
    ];

    private const string CorruptText = "进入变身：邪瘴天衣形态。\n堕落值＜3：变奏。\n消耗。";
    private static readonly (int Value, bool Corrupt)[] RouteCases =
        [(-5, false), (-3, false), (-2, false), (0, false), (2, false),
         (3, true), (5, true), (2, false), (3, true), (0, false)];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (entry == null) return;
        if (card is DrowsyStatus && upgraded)
            throw new InvalidOperationException("DrowsyStatus only has a base scenario.");
        Metadata(ctx, card, upgraded);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        string expected = upgraded ? entry.Upgraded : entry.Base;
        if (card is not Transform)
        {
            DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "starter full run text");
            DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "starter full combat text");
            return;
        }

        var run = (RunState)ctx.Player.RunState;
        int saved = CorruptionQuery.Get(run);
        try
        {
            foreach (var test in RouteCases)
            {
                CorruptionCmd.Set(run, test.Value);
                string text = test.Corrupt ? (upgraded ? "固有。\n" : "") + CorruptText : expected;
                foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
                {
                    DesignSyncCombatTextContract.AssertText(ctx, instance, pile, text, "transform boundary full text " + test.Value);
                    ctx.AssertEqual("transform boundary route " + pile,
                        test.Corrupt ? RouteCardKind.Corrupt : RouteCardKind.Holy, RouteCardQuery.Get(instance), effect: false);
                    ctx.AssertTrue("transform variation stays unsealed " + pile,
                        !CombatSealQuery.IsSealed(run, instance), effect: false);
                }
            }
        }
        finally { CorruptionCmd.Set(run, saved); }
    }

    private static void Metadata(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        (int Cost, CardType Type, CardRarity Rarity, TargetType Target, Type Pool) expected = card switch
        {
            MaidenStrike => (1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, typeof(MSNeutralCardPool)),
            MaidenDefend => (1, CardType.Skill, CardRarity.Basic, TargetType.Self, typeof(MSNeutralCardPool)),
            Transform => (1, CardType.Skill, CardRarity.Basic, TargetType.Self, typeof(MSHolyCardPool)),
            DrowsyStatus => (0, CardType.Status, CardRarity.Common, TargetType.None, typeof(MSGeneratedCardPool)),
            BindingInsight => (upgraded ? 0 : 1, CardType.Power, CardRarity.Uncommon, TargetType.Self, typeof(MSNeutralCardPool)),
            _ => throw new InvalidOperationException("Unexpected starter text model."),
        };
        ctx.AssertEqual("starter cost", expected.Cost, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("starter type", expected.Type, card.Type, effect: false);
        ctx.AssertEqual("starter rarity", expected.Rarity, card.Rarity, effect: false);
        ctx.AssertEqual("starter target", expected.Target, card.TargetType, effect: false);
        ctx.AssertEqual("starter pool", expected.Pool, card.Pool.GetType(), effect: false);
        CardKeyword[] keywords = card switch
        {
            Transform => upgraded ? [CardKeyword.Exhaust, CardKeyword.Innate] : [CardKeyword.Exhaust],
            DrowsyStatus => [CardKeyword.Unplayable, CardKeyword.Retain],
            BindingInsight => [PortableKeyword.Value],
            _ => [],
        };
        ctx.AssertTrue("starter exact keywords", card.Keywords.SetEquals(keywords), effect: false);
        if (card is MaidenStrike or MaidenDefend)
            ctx.AssertTrue("starter vanilla tag", card.Tags.ToHashSet().SetEquals([card is MaidenStrike ? CardTag.Strike : CardTag.Defend]), effect: false);
    }
}
#endif
