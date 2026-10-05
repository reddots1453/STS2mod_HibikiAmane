#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Localization;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>Independent design sentences. Never derive expectations from runtime vars or localization.</summary>
internal static class DesignSyncNeutralTextContract
{
    internal sealed record Expected(Type Model, string Base, string Upgraded);
    internal static readonly Expected[] Entries =
    [
        new(typeof(RepairAlyssa), "获得4点格挡。\n获得1层魔装耐久。", "获得7点格挡。\n获得1层魔装耐久。"),
        new(typeof(MagiciansSecret), "获得1层魔力增幅。\n魔力解放：获得1层魔力增幅。", "获得1层魔力增幅。\n魔力解放：获得2层魔力增幅。"),
        new(typeof(StudyPlan), "获得7点格挡。\n下回合抽2张牌。", "获得10点格挡。\n下回合抽2张牌。"),
        new(typeof(Procrastinate), "选择一张牌，将其置于抽牌堆底部。\n抽2张牌。\n消耗。", "选择一张牌，将其置于抽牌堆底部。\n抽3张牌。\n消耗。"),
        new(typeof(DreamPigment), "从抽牌堆中抽取堕落牌、圣洁牌和中立牌各1张。", "从抽牌堆中抽取堕落牌、圣洁牌和中立牌各1张。"),
        new(typeof(ForgeStrike), "造成6点伤害。\n选择1张打击附魔：本能。", "造成9点伤害。\n选择1张打击附魔：本能。"),
        new(typeof(MagicStarBomb), "下个回合开始时，对所有敌人造成20点伤害。\n魔力解放：获得〈能量〉。", "下个回合开始时，对所有敌人造成28点伤害。\n魔力解放：获得〈能量〉。"),
        new(typeof(DreamMist), "给予所有人2层虚弱。\n消耗。", "给予所有人3层虚弱。\n消耗。"),
        new(typeof(SwordVerdict), "造成24点伤害。\n对生命值低于一半的目标造成2倍伤害，并将其击晕。\n消耗。", "保留。\n造成24点伤害。\n对生命值低于一半的目标造成2倍伤害，并将其击晕。\n消耗。"),
        new(typeof(LightningRecoil), "造成6点伤害。\n魔力解放：抽1张牌。", "造成9点伤害。\n魔力解放：抽1张牌。"),
        new(typeof(IceBreakingSlash), "造成7点伤害。\n将1张冰晶碎片加入手牌。", "造成7点伤害。\n将2张冰晶碎片加入手牌。"),
        new(typeof(IceShield), "将3张冰晶碎片加入手牌。\n消耗。", "将3张冰晶碎片+加入手牌。\n消耗。"),
        new(typeof(IceShard), "保留。\n获得3点格挡。\n消耗。", "保留。\n获得4点格挡。\n消耗。"),
        new(typeof(MentalUnity), "造成6点伤害。\n每当目标攻击时，获得2点格挡。\n消耗。", "造成8点伤害。\n每当目标攻击时，获得3点格挡。\n消耗。"),
        new(typeof(ObstructingShot), "造成3点伤害。\n如果目标不为攻击意图，将其击晕。\n消耗。", "造成3点伤害。\n如果目标不为攻击意图，将其击晕。\n消耗。"),
        new(typeof(MindsEye), "造成3点伤害。\n如果敌人的意图是攻击，给予1层虚弱。\n否则给予1层易伤。", "造成3点伤害。\n如果敌人的意图是攻击，给予2层虚弱。\n否则给予2层易伤。"),
        new(typeof(Surf), "抽一张牌并对所有敌人造成4点伤害。\n重复直到抽到的牌费用合计达到〈能量〉〈能量〉〈能量〉。", "抽一张牌并对所有敌人造成4点伤害。\n重复直到抽到的牌费用合计达到4〈能量〉。"),
        new(typeof(LightArrow), "造成5点伤害。\n获得5点格挡。\n魔力增幅对这张牌的效果翻倍。", "造成7点伤害。\n获得7点格挡。\n魔力增幅对这张牌的效果翻倍。"),
        new(typeof(BalanceBlade), "造成17点伤害。\n每有1点堕落值偏移，这张牌造成的伤害减少3点。", "造成21点伤害。\n每有1点堕落值偏移，这张牌造成的伤害减少3点。"),
        new(typeof(DoubleDefense), "获得4点格挡两次。", "获得6点格挡两次。"),
        new(typeof(BalanceShield), "获得14点格挡。\n每有1点堕落值偏移，这张牌获得的格挡减少3点。", "获得18点格挡。\n每有1点堕落值偏移，这张牌获得的格挡减少3点。"),
        new(typeof(BorrowedForceStrike), "造成9点伤害。\n如果目标敌人的意图为攻击，获得〈能量〉。", "造成10点伤害。\n如果目标敌人的意图为攻击，获得〈能量〉〈能量〉。"),
        new(typeof(BasicTraining), "打击额外造成3点伤害。\n防御额外获得3点格挡。", "打击额外造成5点伤害。\n防御额外获得5点格挡。"),
        new(typeof(Fusion), "从堕落、圣洁卡各一张中选择一张加入手牌，该牌在本回合可以免费打出。\n消耗。", "从升级过的堕落、圣洁卡各一张中选择一张加入手牌，该牌在本回合可以免费打出。\n消耗。"),
        new(typeof(CounterBarrier), "获得2荆棘。\n获得2覆甲。\n将1张功性魔防壁II放入弃牌堆。\n沉底。", "获得2荆棘。\n获得2覆甲。\n将1张功性魔防壁II放入弃牌堆。\n沉底。"),
        new(typeof(CounterBarrierII), "获得3荆棘。\n获得3覆甲。\n将1张功性魔防壁III放入弃牌堆。", "获得3荆棘。\n获得3覆甲。\n将1张功性魔防壁III放入弃牌堆。"),
        new(typeof(CounterBarrierIII), "获得5荆棘。\n获得5覆甲。\n将1张功性魔防壁IV放入弃牌堆。", "获得5荆棘。\n获得5覆甲。\n将1张功性魔防壁IV放入弃牌堆。"),
        new(typeof(CounterBarrierIV), "获得30荆棘。\n获得30覆甲。", "获得30荆棘。\n获得30覆甲。"),
        new(typeof(BurningBracelet), "造成14点伤害。\n每有1张其他手牌，耗能减少〈能量〉。", "造成20点伤害。\n每有1张其他手牌，耗能减少〈能量〉。"),
        new(typeof(FrozenBracelet), "获得12点格挡。\n每有1张其他手牌，耗能减少〈能量〉。", "获得16点格挡。\n每有1张其他手牌，耗能减少〈能量〉。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static string Normalize(string text)
    {
        // Keep wrong image paths, missing/repeated icons and compact numeric prefixes visible.
        string icon = "[img]" + MaidenEnergyIconAssets.TextIconResourcePath + "[/img]";
        return Regex.Replace(text.Replace(icon, "〈能量〉", StringComparison.Ordinal), @"\[[^\]]*\]", "");
    }

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        Expected? entry = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (entry == null) return;
        string expected = upgraded ? entry.Upgraded : entry.Base;
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
        {
            instance.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, instance.DynamicVars);
            string actual = instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy);
            ctx.AssertEqual("DS27 neutral full rendered text " + pile, expected, Normalize(actual), effect: false);
            if (instance is DreamPigment)
            {
                ctx.AssertTrue("holy route is gold " + pile, actual.Contains("[gold]圣洁牌[/gold]"), effect: false);
                ctx.AssertTrue("corrupt route remains purple " + pile, actual.Contains("[purple]堕落牌[/purple]"), effect: false);
            }
        }
    }
}
#endif
