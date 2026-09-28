#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

// Independent complete DesignDoc sentences; values are not read back from localization/vars.
internal static class DesignSyncCorruptTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(AbnormalAdaptation), "每回合两次，抽到诅咒牌或状态牌时，将其放入弃牌堆，抽2张牌。", "固有。\n每回合两次，抽到诅咒牌或状态牌时，将其放入弃牌堆，抽2张牌。"),
        new(typeof(AllCurseBite), "造成1点伤害。\n本场战斗中，每当有一张攻击牌被消耗时，将它的伤害数值增加到这张牌上。", "造成1点伤害。\n本场战斗中，每当有一张攻击牌被消耗时，将它的伤害数值增加到这张牌上。"),
        new(typeof(BerserkerMask), "攻击牌耗能变为0〈能量〉。\n每当你打出一张攻击牌后，失去1点力量。", "攻击牌耗能变为0〈能量〉。\n每当你打出一张攻击牌后，失去1点力量。"),
        new(typeof(BlackVortex), "打出牌堆顶2张牌。\n魔力解放：将其消耗并重复此效果。", "打出牌堆顶2张牌。\n魔力解放：将其消耗并重复此效果。"),
        new(typeof(ChainDestruction), "每消耗4张牌，你打出的下一张牌会被额外打出一次。", "每消耗4张牌，你打出的下一张牌会被额外打出一次。"),
        new(typeof(Coronation), "获得等量于当前堕落值的力量。", "固有。\n获得等量于当前堕落值的力量。"),
        new(typeof(CurseWedge), "本回合内，造成未受格挡的伤害后同时给予目标1层破碎。", "本回合内，造成未受格挡的伤害后同时给予目标2层破碎。"),
        new(typeof(DarkFlameBarrier), "获得6点格挡。\n持续1个回合，燃烧的敌人对你造成的伤害降低50%。\n魔力解放：额外持续1个回合。\n消耗。", "获得9点格挡。\n持续1个回合，燃烧的敌人对你造成的伤害降低50%。\n魔力解放：额外持续1个回合。\n消耗。"),
        new(typeof(DarkPunishment), "造成33点伤害。\n本场战斗每消耗1张牌，费用减少〈能量〉。", "造成44点伤害。\n本场战斗每消耗1张牌，费用减少〈能量〉。"),
        new(typeof(DarkThrust), "造成9点伤害。\n抽2张牌。", "造成12点伤害。\n抽2张牌。"),
        new(typeof(DemonStaff), "从3张其他角色的带有“消耗”的牌中选择1张加入手牌。\n这张牌在本回合免费打出。\n消耗。", "从3张其他角色的升级过的带有“消耗”的牌中选择1张加入手牌。\n这张牌在本回合免费打出。\n消耗。"),
        new(typeof(DestructionReaction), "抽3张牌，消耗其中的1张牌。", "抽4张牌，消耗其中的1张牌。"),
        new(typeof(FearAura), "所有敌人在本回合失去3点力量。\n魔力解放：本回合你获得这些力量。\n消耗。", "所有敌人在本回合失去4点力量。\n魔力解放：本回合你获得这些力量。\n消耗。"),
        new(typeof(FinalSlash), "造成9点伤害。\n选择抽牌堆的1张牌放入弃牌堆。", "造成11点伤害。\n选择抽牌堆的2张牌放入弃牌堆。"),
        new(typeof(FleetingYears), "抽2张牌。\n获得〈能量〉〈能量〉。\n消耗。", "抽3张牌。\n获得〈能量〉〈能量〉。\n消耗。"),
        new(typeof(Ignite), "消耗1张牌。\n下回合开始时将其打出。", "消耗1张牌。\n下回合开始时将其打出。"),
        new(typeof(LordOfBlaze), "你因燃烧状态受到的伤害将会转移给随机敌人。\n回合结束时，给予所有人2层燃烧。", "你因燃烧状态受到的伤害将会转移给随机敌人。\n回合结束时，给予所有人3层燃烧。"),
        new(typeof(MentalStabilizer), "获得11点格挡。\n将手牌中所有状态牌和诅咒牌洗入弃牌堆，然后抽相同数量的牌。", "获得14点格挡。\n将手牌中所有状态牌和诅咒牌洗入弃牌堆，然后抽相同数量的牌。"),
        new(typeof(MiasmaAbsorption), "获得〈能量〉〈能量〉。", "获得〈能量〉〈能量〉〈能量〉。"),
        new(typeof(MiasmaConversion), "消耗手牌中的攻击牌和诅咒牌。\n每消耗1张牌，获得1层魔力增幅。", "消耗手牌中的攻击牌和诅咒牌。\n每消耗1张牌，获得1层魔力增幅。"),
        new(typeof(MiasmaFlame), "对所有敌人造成7点伤害并给予3层燃烧。", "对所有敌人造成10点伤害并给予3层燃烧。"),
        new(typeof(MimicProliferation), "消耗1张牌。\n将它的2张复制加入手牌。", "消耗1张牌。\n将它的3张复制加入手牌。"),
        new(typeof(ReflectiveBarrier), "获得8点格挡。\n当这张牌被消耗时，获得8点格挡和1层魔力增幅。", "虚无。\n获得8点格挡。\n当这张牌被消耗时，获得8点格挡和1层魔力增幅。"),
        new(typeof(SacrificialFrenzy), "造成12点伤害。\n消耗牌组顶3张牌，每消耗1张攻击牌，额外造成6点伤害。", "造成16点伤害。\n消耗牌组顶4张牌，每消耗1张攻击牌，额外造成6点伤害。"),
        new(typeof(SharpForge), "造成8点伤害。\n为抽牌堆中随机2张攻击牌附魔：锋利：2。", "造成10点伤害。\n为抽牌堆中随机2张攻击牌附魔：锋利：3。"),
        new(typeof(ThousandCurseScythe), "造成8点伤害。\n被消耗时，这张牌在本局游戏中的伤害永久增加4。\n消耗。", "造成8点伤害。\n被消耗时，这张牌在本局游戏中的伤害永久增加6。\n消耗。"),
        new(typeof(WinterHolly), "虚无。\n获得10点格挡。\n将一张该牌具有消耗的复制加入你的手牌。", "虚无。\n获得13点格挡。\n将一张该牌具有消耗的复制加入你的手牌。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);
    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var entry = Entries.SingleOrDefault(entry => entry.Model == card.GetType());
        if (entry == null) return;
        string expected = upgraded ? entry.Upgraded : entry.Base;
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
        {
            instance.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, instance.DynamicVars);
            string actual = instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy);
            ctx.AssertEqual("DS27 corrupt full rendered text " + pile, expected,
                DesignSyncHolyTextContract.Normalize(actual), effect: false);
        }
    }
}
#endif
