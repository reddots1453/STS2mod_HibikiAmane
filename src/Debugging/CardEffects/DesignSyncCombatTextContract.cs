#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using Expected = MaidenSuccubus.Debugging.CardEffects.DesignSyncNeutralTextContract.Expected;

namespace MaidenSuccubus.Debugging.CardEffects;

// Independent sentences, not reconstructed from current localization or card variables.
internal static class DesignSyncCombatTextContract
{
    internal static readonly Expected[] Entries =
    [
        new(typeof(FlameBloom), "造成8点伤害。\n给予1层燃烧。\n魔力解放：给予1层燃烧。", "造成11点伤害。\n给予1层燃烧。\n魔力解放：给予1层燃烧。"),
        new(typeof(Lullaby), "回合结束时，将困了加入手牌。\n回合结束时，你每有1张手牌，获得2点格挡。", "回合结束时，将困了加入手牌。\n回合结束时，你每有1张手牌，获得2点格挡。"),
        new(typeof(IceMist), "保留。\n获得2点临时敏捷。\n消耗。", "保留。\n获得3点临时敏捷。\n消耗。"),
        new(typeof(MagicBurst), "造成7点伤害。\n魔力解放：每拥有一层增益效果，额外造成2点伤害。", "造成7点伤害。\n魔力解放：每拥有一层增益效果，额外造成3点伤害。"),
        new(typeof(Takemikazuchi), "造成6点伤害2次。\n本场战斗中每打出过一张附魔牌，这张牌额外造成一次伤害。", "造成8点伤害2次。\n本场战斗中每打出过一张附魔牌，这张牌额外造成一次伤害。"),
        new(typeof(Rest), "只能在变身时使用。\n退出变身，结束你的回合。\n下个回合抽2张牌并获得〈能量〉〈能量〉。", "保留。\n只能在变身时使用。\n退出变身，结束你的回合。\n下个回合抽2张牌并获得〈能量〉〈能量〉。"),
        new(typeof(BattleTechniqueReplay), "这张牌始终是你打出的上一张牌的复制。", "保留。\n这张牌始终是你打出的上一张牌的复制。"),
        new(typeof(LightWings), "造成9点伤害。\n抽1张附魔牌。\n可以被多重附魔。", "造成12点伤害。\n抽1张附魔牌。\n可以被多重附魔。"),
    ];

    internal static bool Contains(Type type) => Entries.Any(entry => entry.Model == type);

    internal static string ExpectedOutside(CardModel card, bool upgraded)
    {
        var entry = Entries.Single(entry => entry.Model == card.GetType());
        return upgraded ? entry.Upgraded : entry.Base;
    }

    internal static void AssertText(CardEffectTestContext ctx, CardModel card, PileType pile,
        string expected, string label)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, card.DynamicVars);
        string actual = card.GetDescriptionForPile(pile, ctx.PrimaryEnemy);
        ctx.AssertEqual(label, expected, DesignSyncHolyTextContract.Normalize(actual), effect: false);
    }

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        if (!Contains(card.GetType())) return;
        string expected = ExpectedOutside(card, upgraded);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        AssertText(ctx, outside, PileType.Deck, expected, "combat text contract true run instance");
        string combatSuffix = card switch
        {
            MagicBurst => "\n（造成7点伤害）",
            Takemikazuchi => "\n（造成2次伤害）",
            _ => "",
        };
        AssertText(ctx, card, PileType.Hand, expected + combatSuffix, "combat text contract fresh combat instance");
    }

    internal static void TrackedHits(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        string expected = ExpectedOutside(card, upgraded);
        AssertText(ctx, card, PileType.Hand, expected + "\n（造成5次伤害）", "tracked hits update full combat description");
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        AssertText(ctx, outside, PileType.Deck, expected, "run preview does not inherit active tracker total");
    }
}
#endif
