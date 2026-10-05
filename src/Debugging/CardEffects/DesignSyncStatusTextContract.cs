#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Curses;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncStatusTextContract
{
    private sealed record Expected(Type Model, string Base, string? Upgraded = null);
    // Literal DesignDoc oracles. No localization/vars are used to manufacture expectations.
    private static readonly Expected[] Entries =
    [
        new(typeof(AphrodisiacPoisoningCurse), "虚无。\n如果这张牌在你的手牌中，获得欲望时，额外获得〈欲望〉。"),
        new(typeof(AphrodisiacCurse), "打出后移除出牌组。\n获得〈欲望〉〈欲望〉。"),
        new(typeof(ArousalStatus), "不能被打出。\n虚无。\n每当你抽到该牌时，获得〈欲望〉。"),
        new(typeof(BarbedHookStatus), "失去1层魔装耐久。\n消耗。"),
        new(typeof(BiteInvader), "选择一名意图为侵犯的敌人，给于7层虚弱并将其击晕。\n随身。", "保留。\n选择一名意图为侵犯的敌人，给于7层虚弱并将其击晕。\n随身。"),
        new(typeof(BitingPaperStatus), "回合结束时如果这张牌在你的手牌中，失去1层魔装耐久。\n消耗。"),
        new(typeof(ChangePanties), "欲望大于等于5时才能打出。\n获得2层滑溜。\n获得2层易伤。", "欲望大于等于4时才能打出。\n获得2层滑溜。\n获得2层易伤。"),
        new(typeof(Milk), "恢复3点生命值。\n消耗。"),
        new(typeof(ClimaxBanCurse), "虚无。\n如果这张牌在你的手牌中，你不会因欲望值满进入高潮平复。"),
        new(typeof(ClothingBurnStatus), "虚无。\n回合结束时如果这张牌在你的手牌中，失去1层魔装耐久。\n消耗。"),
        new(typeof(CorrosiveSlimeCurse), "打出后移除出牌组。\n失去1层魔装耐久。"),
        new(typeof(DesireWhip), "造成5点伤害。\n如果目标的意图是拘束或侵犯，将其击晕。\n消耗。\n随身。"),
        new(typeof(DissolvingFluidStatus), "保留。\n回合结束时如果这张牌在你的手牌中，失去1层魔装耐久。\n消耗。"),
        new(typeof(EctoplasmResidueCurse), "打出后移除出牌组。\n下回合失去〈能量〉。"),
        new(typeof(Exhibitionist), "获得等同于当前诱惑度的格挡。\n将1张赤裸欲放入手牌。"),
        new(typeof(ExperimentalLiquidCurse), "打出后移除出牌组。\n随机获得1层虚弱、脆弱、易伤、力量、敏捷。"),
        new(typeof(ExposePlay), "获得20点诱惑度。\n如果魔装耐久不高于1层，随机敌人本回合意图变为色情攻击。\n消耗。", "获得20点诱惑度。\n如果魔装耐久不高于2层，随机敌人本回合意图变为色情攻击。\n消耗。"),
        new(typeof(FoulSlimeCurse), "打出后移除出牌组。\n获得1层易伤。"),
        new(typeof(FullOfOpenings), "给予1层虚弱和1层易伤。\n如果处于被拘束状态，重复1次。\n随身。", "给予1层虚弱和1层易伤。\n如果处于被拘束状态，重复2次。\n随身。"),
        new(typeof(InfatuationCurse), "如果这张牌在你的手牌中，不能打出攻击牌。\n消耗。"),
        new(typeof(InkFluidCurse), "打出后移除出牌组。\n随机使1张其他手牌获得虚无。"),
        new(typeof(InsectEggCurse), "打出后移除出牌组。\n将1张发情洗入抽牌堆。"),
        new(typeof(LewdMarkCompleteCurse), "不能被打出。\n保留。\n回合结束时如果这张牌在你的手牌中，将2张发情洗入抽牌堆。"),
        new(typeof(LewdMarkMinorCurse), "不能被打出。\n回合结束时如果这张牌在你的手牌中，将1张发情洗入抽牌堆。"),
        new(typeof(LewdMarkSpreadCurse), "不能被打出。\n回合结束时如果这张牌在你的手牌中，将2张发情洗入抽牌堆。"),
        new(typeof(LoversDagger), "造成18点伤害。\n处于被拘束状态时伤害翻倍。\n对击晕的敌人伤害翻倍。\n随身。", "造成24点伤害。\n处于被拘束状态时伤害翻倍。\n对击晕的敌人伤害翻倍。\n随身。"),
        new(typeof(MagicResidueCurse), "打出后移除出牌组。\n将1张粘液洗入抽牌堆。"),
        new(typeof(MasochisticGirl), "每当你获得负面状态时，获得3倍于层数的格挡。", "每当你获得负面状态时，获得4倍于层数的格挡。"),
        new(typeof(NakedDesireStatus), "不能被打出。\n回合结束时如果这张牌在你的手牌中，失去1层魔装耐久。"),
        new(typeof(ParalyticSlimeCurse), "打出后移除出牌组。\n获得1层脆弱。"),
        new(typeof(ParasiticEggCurse), "打出后移除出牌组。\n失去3点生命值。"),
        new(typeof(PleasureGarden), "获得30点诱惑度。\n所有敌人本回合意图变为色情攻击。\n消耗。"),
        new(typeof(RoyalEssenceCurse), "打出后移除出牌组。\n恢复5点生命值。"),
        new(typeof(ScorchingFluidCurse), "打出后移除出牌组。\n获得2层燃烧。"),
        new(typeof(SemenConversion), "抽2张牌。\n你打出的下一张消耗〈欲望〉的牌将改为消耗等量生命值。\n消耗。", "抽2张牌。\n你打出的下一张消耗〈欲望〉的牌将改为消耗等量生命值。"),
        new(typeof(SemenCurse), "打出后移除出牌组。"),
        new(typeof(SludgeSemenCurse), "打出后移除出牌组。"),
        new(typeof(SporeMucusCurse), "打出后移除出牌组。\n获得1层虚弱。"),
        new(typeof(VineSeedCurse), "打出后移除出牌组。\n将1张孢子心灵洗入抽牌堆。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(item => item.Model == card.GetType());
        if (entry == null) return;
        string expected = upgraded ? entry.Upgraded ?? entry.Base : entry.Base;
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "status full run text");
        DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "status full combat text");
        if (card is MSInvasionCurseTemplate)
        {
            ctx.AssertTrue("permanent removal is not exhaust", !card.Keywords.Contains(CardKeyword.Exhaust));
            ctx.AssertTrue("permanent removal is not ethereal", !card.Keywords.Contains(CardKeyword.Ethereal));
        }
    }
}
#endif
