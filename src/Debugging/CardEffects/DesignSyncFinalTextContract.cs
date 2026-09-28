#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Curses;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncFinalTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(BlasphemousDesire), "本回合失去3点力量。\n获得5〈欲望〉。\n消耗。", "本回合失去3点力量。\n获得7〈欲望〉。\n消耗。"),
        new(typeof(DeepSeaSlimeCurse), "打出后移除出牌组。\n失去1层魔装耐久；", "打出后移除出牌组。\n失去1层魔装耐久；"),
        new(typeof(TransparentOutfitCurse), "不能被打出。\n保留。\n当这张牌在你的手牌中，获得20点诱惑度。", "不能被打出。\n保留。\n当这张牌在你的手牌中，获得20点诱惑度。"),
        new(typeof(Bath), "获得〈能量〉〈能量〉。\n移除牌组中的所有精液诅咒牌。\n魔力解放：下回合获得〈能量〉〈能量〉。", "获得〈能量〉〈能量〉。\n移除牌组中的所有精液诅咒牌。\n魔力解放：下回合获得〈能量〉〈能量〉〈能量〉。"),
        new(typeof(EcstasyDew), "获得〈欲望〉〈欲望〉。\n抽1张牌。\n消耗。", "获得〈欲望〉〈欲望〉〈欲望〉。\n抽1张牌。\n消耗。"),
        new(typeof(HumilityLesson), "保留。\n选择1张攻击牌或技能牌，翻倍它的伤害和格挡数值，移除它的其他卡牌描述。\n消耗。", "保留。\n选择1张攻击牌或技能牌，翻倍它的伤害和格挡数值，移除它的其他卡牌描述。\n消耗。"),
        new(typeof(LureDeep), "获得10点诱惑度。\n抽1张牌。", "获得15点诱惑度。\n抽1张牌。"),
        new(typeof(MasochisticTrance), "获得6点格挡。\n每有1层负面状态，抽1张牌。\n消耗。", "获得6点格挡。\n每有1层负面状态，抽1张牌。"),
        new(typeof(SmallFry), "给予3层易伤。\n给予1点力量。\n消耗。", "给予4层易伤。\n给予1点力量。\n消耗。"),
        new(typeof(TentacleArmor), "获得4层覆甲。\n将一张这张牌的复制洗入抽牌堆。", "获得6层覆甲。\n将一张这张牌的复制洗入抽牌堆。"),
        new(typeof(PleasureDrowning), "获得7点格挡。\n抽2张牌。\n将2张发情洗入抽牌堆。", "获得10点格挡。\n抽2张牌。\n将2张发情洗入抽牌堆。"),
        new(typeof(DesireRecycle), "每当有一张花费〈欲望〉的牌被消耗时，获得〈欲望〉并抽1张牌。", "每当有一张花费〈欲望〉的牌被消耗时，获得〈欲望〉并抽1张牌。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(item => item.Model == card.GetType());
        if (entry == null) return;
        string expected = upgraded ? entry.Upgraded : entry.Base;
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "final exact run text");
        DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "final exact combat text");
    }
}
#endif
