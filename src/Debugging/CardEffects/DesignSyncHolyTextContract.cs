#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Localization;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

// Literal design oracles, never reconstructed from localization or DynamicVars.
internal static class DesignSyncHolyTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(AutoReactionArmor), "保留。\n获得5点格挡。\n回合结束时，在弃牌堆中自动打出此牌。", "保留。\n获得7点格挡。\n回合结束时，在弃牌堆中自动打出此牌。"),
        new(typeof(Blizzard), "对所有敌人造成6点伤害，将1张冰雾置入手牌。\n接下来2个回合开始时，重复此效果。", "对所有敌人造成8点伤害，将1张冰雾置入手牌。\n接下来2个回合开始时，重复此效果。"),
        new(typeof(BurningRack), "给予2层燃烧。\n获得2点格挡。", "给予3层燃烧。\n获得3点格挡。"),
        new(typeof(CalmingMist), "抽1张牌。\n对一名角色给予2层虚弱。", "抽2张牌。\n对一名角色给予2层虚弱。"),
        new(typeof(Chant), "从3张圣言牌中选择1张加入手牌。", "从3张圣言牌中选择1张加入手牌。"),
        new(typeof(Consecration), "回合开始时，选择1张手牌变化为随机圣言牌。", "固有。\n回合开始时，选择1张手牌变化为随机圣言牌。"),
        new(typeof(DevoutBulwark), "获得12格挡。\n获得2层虚弱。", "获得15格挡。\n获得2层虚弱。"),
        new(typeof(DivineEcho), "使你的可叠加增益效果额外获得1层。\n消耗。", "使你的可叠加增益效果额外获得2层。\n消耗。"),
        new(typeof(DragonflyTouch), "获得7点格挡。\n每有一个敌人，重复一次。", "获得10点格挡。\n每有一个敌人，重复一次。"),
        new(typeof(EternalDamnation), "断罪审判不再清除断罪层数。\n沉底。", "断罪审判不再清除断罪层数。\n沉底。"),
        new(typeof(ExorcismPerfume), "失去10点诱惑度。\n抽1张牌。", "失去15点诱惑度。\n抽1张牌。"),
        new(typeof(ExternalPowerSkeleton), "保留。\n对所有敌人造成6点伤害。\n回合结束时，在弃牌堆中自动打出此牌。", "保留。\n对所有敌人造成8点伤害。\n回合结束时，在弃牌堆中自动打出此牌。"),
        new(typeof(FamiliarContract), "将X张随机圣洁牌加入手牌。\n为这些牌附魔：使魔。\n消耗。", "将X张升级过的随机圣洁牌加入手牌。\n为这些牌附魔：使魔。\n消耗。"),
        new(typeof(FinalJudgment), "给予1层断罪，并触发断罪审判。\n魔力解放：对其他敌人造成断罪审判的等量伤害。", "给予1层断罪，并触发断罪审判。\n魔力解放：对其他敌人造成断罪审判的等量伤害。"),
        new(typeof(FocusedSlash), "造成30点伤害。\n每有〈欲望〉，耗能增加〈能量〉。", "造成40点伤害。\n每有〈欲望〉，耗能增加〈能量〉。"),
        new(typeof(Gospel), "将手牌中所有技能牌变化为守护圣言，所有攻击牌变化为惩戒圣言。\n消耗。", "将手牌中所有技能牌变化为守护圣言+，所有攻击牌变化为惩戒圣言+。\n消耗。"),
        new(typeof(HolyCurse), "造成7点伤害。\n目标每有1层负面状态，抽1张牌。\n消耗。", "造成7点伤害。\n目标每有1层负面状态，抽1张牌。"),
        new(typeof(HolyFlame), "给予4层燃烧。\n本场战斗中，给予燃烧时额外给予2层。", "给予5层燃烧。\n本场战斗中，给予燃烧时额外给予3层。"),
        new(typeof(HolyPunishment), "造成9点伤害。\n选择抽牌堆的1张牌变化为随机圣言牌。\n魔力解放：并抽取它。", "造成12点伤害。\n选择抽牌堆的1张牌变化为随机圣言牌。\n魔力解放：并抽取它。"),
        new(typeof(HolyRadiance), "每当你获得增益效果时，对随机敌人造成4点伤害。", "每当你获得增益效果时，对随机敌人造成6点伤害。"),
        new(typeof(HolyResonance), "每当你触发圣言效果时，获得2点格挡。", "每当你触发圣言效果时，获得3点格挡。"),
        new(typeof(InwardDiscipline), "当你虚弱时，额外获得50%格挡。\n当你脆弱时，额外造成50%伤害。", "当你虚弱时，额外获得75%格挡。\n当你脆弱时，额外造成75%伤害。"),
        new(typeof(LightPowerRelease), "进入变身：永恒天衣形态。\n沉底。", "进入变身：永恒天衣形态。\n沉底。"),
        new(typeof(MemoryImprint), "回合开始时，将弃牌堆的一张牌置于抽牌堆顶。", "固有。\n回合开始时，将弃牌堆的一张牌置于抽牌堆顶。"),
        new(typeof(MomentaryGrace), "获得6点格挡。\n将1张冰雾置入手牌。", "获得8点格挡。\n将1张冰雾+置入手牌。"),
        new(typeof(MultipleReproduction), "在下个回合结束后，获得1个额外的回合。\n魔力解放：改为本回合。\n消耗。", "在下个回合结束后，获得1个额外的回合。\n魔力解放：改为本回合。\n消耗。"),
        new(typeof(NoLewdness), "获得〈能量〉〈能量〉〈能量〉。\n抽3张牌。\n每有〈欲望〉，耗能增加〈能量〉。\n消耗。", "获得4〈能量〉。\n抽4张牌。\n每有〈欲望〉，耗能增加〈能量〉。\n消耗。"),
        new(typeof(OpeningPrayer), "接下来2个回合开始时，获得1层魔力增幅。", "接下来3个回合开始时，获得1层魔力增幅。"),
        new(typeof(PenanceSlash), "造成14点伤害。\n获得2层脆弱。", "造成17点伤害。\n获得2层脆弱。"),
        new(typeof(PhotonVolt), "造成10点伤害。\n如果小于等于〈欲望〉〈欲望〉，获得1层魔力增幅。", "造成12点伤害。\n如果小于等于〈欲望〉〈欲望〉，获得2层魔力增幅。"),
        new(typeof(PurificationOrb), "失去〈欲望〉〈欲望〉。\n获得1层魔装耐久。\n失去10诱惑度。\n消耗。\n随身。", "失去〈欲望〉〈欲望〉。\n获得1层魔装耐久。\n失去10诱惑度。\n消耗。\n随身。"),
        new(typeof(RetainedGuard), "保留。\n获得8点格挡。", "保留。\n获得11点格挡。"),
        new(typeof(SneakSnack), "获得〈能量〉〈能量〉。\n获得2层断罪。", "获得〈能量〉〈能量〉〈能量〉。\n获得2层断罪。"),
        new(typeof(SoulFuenika), "从3张圣洁牌中选择一张加入手牌。\n战斗结束后，将那张牌的复制加入牌组。\n消耗。", "从3张圣洁牌中选择一张加入手牌。\n战斗结束后，将那张牌的复制加入牌组。\n消耗。"),
        new(typeof(SoulImpact), "造成12点伤害。\n如果目标敌人具有负面效果，获得〈能量〉〈能量〉。", "造成14点伤害。\n如果目标敌人具有负面效果，获得〈能量〉〈能量〉〈能量〉。"),
        new(typeof(SoulPurification), "每当你打出一张圣言牌后，将其消耗并抽1张牌。", "每当你打出一张圣言牌后，将其消耗并抽2张牌。"),
        new(typeof(Stigma), "对一名角色选择一项：给予2层断罪或虚弱。\n消耗。", "对一名角色选择一项：给予3层断罪或虚弱。\n消耗。"),
        new(typeof(SunDance), "每拥有1层增益效果，在本回合中获得1层敏捷。\n消耗。", "保留。\n每拥有1层增益效果，在本回合中获得1层敏捷。\n消耗。"),
        new(typeof(TacticalAnalyzer), "抽1张牌。\n升级一张手牌并附魔：稳定。", "抽2张牌。\n升级一张手牌并附魔：稳定。"),
        new(typeof(TacticalCore), "获得7点格挡。\n3回合内，魔力增幅对于附魔牌的效果翻倍。", "获得9点格挡。\n4回合内，魔力增幅对于附魔牌的效果翻倍。"),
        new(typeof(TerminalSanctuary), "虚无。\n获得2层净化。\n获得2层残影。\n获得2层圣域。", "获得2层净化。\n获得2层残影。\n获得2层圣域。"),
        new(typeof(Tranquilizer), "失去〈欲望〉〈欲望〉。\n抽1张牌。\n消耗。", "失去〈欲望〉〈欲望〉〈欲望〉。\n抽1张牌。\n消耗。"),
        new(typeof(WindRumor), "造成9点伤害。\n选择弃牌堆的1张牌放入抽牌堆。", "造成11点伤害。\n选择弃牌堆的2张牌放入抽牌堆。"),
        new(typeof(YarusMemory), "固有。\n拾起时，另选择牌组中2张牌，为这三张牌附魔：灵魂联结。", "固有。\n拾起时，另选择牌组中2张牌，为这三张牌附魔：灵魂联结。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static string Normalize(string text)
    {
        string icon = "[img]" + MaidenDesireIconAssets.TextIconResourcePath + "[/img]";
        // Normalize only exact resource tags, then reuse the energy-tag normalization.
        return DesignSyncNeutralTextContract.Normalize(text.Replace(icon, "〈欲望〉", StringComparison.Ordinal));
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
            ctx.AssertEqual("DS27 holy full rendered text " + pile, expected, Normalize(actual), effect: false);
        }
    }
}
#endif
