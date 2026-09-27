#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

// Independent full sentences, not expectations reconstructed from DynamicVars.
internal static class DesignSyncTextBatchContract
{
    internal static readonly Type[] Types =
    [
        typeof(PlayingWithFire), typeof(BurningBladeRitual), typeof(MiasmaAffinity),
        typeof(PriceOfStrength), typeof(DesireWard), typeof(Worship), typeof(Judgment),
        typeof(OriginalSinBrand), typeof(ForgeNimble), typeof(ForgeCharge),
        typeof(JudgmentBlade), typeof(HealingArt),
    ];

    internal static string Text(CardModel card, PileType pile, Creature? target = null)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, target, card.DynamicVars);
        return Regex.Replace(card.GetDescriptionForPile(pile, target), @"\[[^\]]*\]", "");
    }

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        if (!Types.Contains(card.GetType())) return;
        string expected = card switch
        {
            PlayingWithFire => $"获得1层燃烧。\n抽{(upgraded ? 3 : 2)}张牌。",
            BurningBladeRitual => $"消耗1张牌。\n造成{(upgraded ? 13 : 10)}点伤害。",
            MiasmaAffinity => $"虚无。\n从抽牌堆选择{(upgraded ? 2 : 1)}张牌放入手牌，并使其获得虚无。",
            PriceOfStrength => (upgraded ? "" : "虚无。\n") + "获得4点力量。\n获得4层破碎。",
            DesireWard => $"造成{(upgraded ? 11 : 8)}点伤害。\n防止下一次增加欲望。",
            Worship => $"获得{(upgraded ? 2 : 1)}层敏捷。\n获得{(upgraded ? 2 : 1)}层净化。",
            Judgment => $"造成7点伤害。\n给予{(upgraded ? 3 : 2)}层断罪。\n魔力解放：额外给予1层断罪。",
            OriginalSinBrand => $"对所有敌人给予2层断罪。\n选择弃牌堆{(upgraded ? 2 : 1)}张牌加入手牌。",
            ForgeNimble => $"获得{(upgraded ? 8 : 5)}点格挡。\n为1张牌附魔：伶俐。",
            ForgeCharge => $"从弃牌堆中选择1张牌置于牌堆顶，并为它附魔：充能：{(upgraded ? 3 : 2)}。\n消耗。",
            JudgmentBlade => $"造成7点伤害。\n目标每有一层负面效果，额外造成{(upgraded ? 5 : 3)}点伤害。",
            HealingArt => $"恢复{(upgraded ? 8 : 4)}点生命值。\n每拥有1层增益效果，额外恢复2点生命值。\n消耗。",
            _ => throw new InvalidOperationException(),
        };
        // A genuine run/deck instance, not a combat card merely requested as Deck.
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertEqual("DS27 full outside text incl punctuation and native keywords",
            expected, Text(outside, PileType.Deck), effect: false);
        string combatExpected = card switch
        {
            JudgmentBlade => expected + "\n（造成7点伤害）",
            HealingArt => expected + $"\n（恢复{(upgraded ? 8 : 4)}点生命值）",
            _ => expected,
        };
        ctx.AssertEqual("DS27 full combat text without blank lines or repeated keywords",
            combatExpected, Text(card, PileType.Hand, ctx.PrimaryEnemy), effect: false);
    }
}
#endif
