#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Curses;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

// Independent formal sentences. GagCurse has only a base scenario, never an upgrade.
internal static class DesignSyncRemainingTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(FlameSword), "造成9点伤害。\n完成5场战斗后，为这张牌附魔：特兹卡塔拉的余烬。\n（还剩5场战斗）", "造成12点伤害。\n完成5场战斗后，为这张牌附魔：特兹卡塔拉的余烬。\n（还剩5场战斗）"),
        new(typeof(WindGodCloak), "将你在每回合打出的第一张耗能为0的牌的复制加入你的手牌。", "将你在每回合打出的第一张耗能为0的牌的复制加入你的手牌。"),
        new(typeof(BeyondReasonForge), "从3个强大的附魔中选择一项，附加给1张手牌。\n消耗。", "从3个强大的附魔中选择一项，附加给1张手牌。\n消耗。"),
        new(typeof(SuperRegeneration), "从消耗堆选择打出一张牌。\n消耗。\n魔力解放：将此牌放回手牌。", "从消耗堆选择打出一张牌。\n消耗。\n魔力解放：将此牌放回手牌。"),
        new(typeof(GagCurse), "如果这张牌在你的手牌中，你的技能牌额外耗能〈能量〉。", "如果这张牌在你的手牌中，你的技能牌额外耗能〈能量〉。"),
        new(typeof(DarkStorm), "对所有敌人造成8点伤害并给予2层易伤。\n升级时，为这张牌附魔：华彩。", "对所有敌人造成8点伤害并给予2层易伤。\n升级时，为这张牌附魔：华彩。\n重放1。"),
        new(typeof(CurseInfection), "当这张牌被消耗时，抽2张牌，并将这一效果附加到随机手牌上。\n消耗。", "当这张牌被消耗时，抽2张牌，并将这一效果附加到随机手牌上。\n消耗。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (entry == null) return;
        if (card is GagCurse && upgraded)
            throw new InvalidOperationException("GagCurse must not have an upgraded scenario.");
        string expected = upgraded ? entry.Upgraded : entry.Base;
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "remaining exact run text");
        DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "remaining exact combat text");
    }

    internal static void FlameProgress(CardEffectTestContext ctx, FlameSword card, PileType pile,
        bool upgraded, int completed)
    {
        // Literal expected damage includes the native Ember +3 only after battle five.
        string damage = completed == 5 ? (upgraded ? "15" : "12") : (upgraded ? "12" : "9");
        string suffix = completed == 5 ? "\n永恒。" : $"\n（还剩{5 - completed}场战斗）";
        string expected = $"造成{damage}点伤害。\n完成5场战斗后，为这张牌附魔：特兹卡塔拉的余烬。" + suffix;
        DesignSyncCombatTextContract.AssertText(ctx, card, pile, expected, "flame progress full text " + pile);
    }
}
#endif
