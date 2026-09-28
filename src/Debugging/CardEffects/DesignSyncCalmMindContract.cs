#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncCalmMindContract
{
    private sealed record Case(int OtherHand, int DrawPile, bool NoDraw,
        int DrawBase, int DrawUpgraded, int EnergyBase, int EnergyUpgraded);

    // Independent boundary oracles, not derived from the implementation or its vars.
    private static readonly Case[] Cases =
    [
        new(0, 0, false, 0, 0, 0, 0),
        new(0, 1, false, 0, 0, 0, 0),
        new(0, 10, false, 0, 0, 0, 0),
        new(5, 0, false, 0, 0, 0, 0),
        new(5, 1, false, 0, 0, 0, 0),
        new(5, 10, false, 0, 0, 0, 0),
        new(6, 0, false, 0, 0, 2, 3),
        new(6, 1, false, 1, 1, 2, 3),
        new(6, 10, false, 2, 3, 2, 3),
        new(7, 0, false, 0, 0, 2, 3),
        new(7, 1, false, 1, 1, 2, 3),
        new(7, 10, false, 2, 3, 2, 3),
        new(6, 10, true, 0, 0, 2, 3),
    ];

    internal static async Task Run(CardEffectTestContext ctx, CalmMind card, bool upgraded)
    {
        string expected = upgraded
            ? "保留。\n如果你的手牌有6张或更多，则抽3张牌并获得〈能量〉〈能量〉〈能量〉。"
            : "保留。\n如果你的手牌有6张或更多，则抽2张牌并获得〈能量〉〈能量〉。";
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "calm mind exact run text");
        DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "calm mind exact combat text");
        ctx.AssertEqual("calm mind zero cost", 0, card.EnergyCost.Canonical);
        ctx.AssertTrue("calm mind retain", card.Keywords.Contains(CardKeyword.Retain));
        ctx.AssertEqual("calm mind skill", CardType.Skill, card.Type);
        ctx.AssertEqual("calm mind uncommon", CardRarity.Uncommon, card.Rarity);
        ctx.AssertEqual("calm mind self target", TargetType.Self, card.TargetType);
        foreach (Case entry in Cases)
        {
            await ctx.Reset();
            var held = await ctx.AddFillerCards(PileType.Hand, entry.OtherHand);
            var draw = await ctx.AddFillerCards(PileType.Draw, entry.DrawPile);
            var source = await ctx.Add<CalmMind>(PileType.Hand, upgraded);
            if (entry.NoDraw) await ctx.ApplyPower<NoDrawPower>(ctx.Self, 1);
            string label = $"hand={entry.OtherHand}, draw={entry.DrawPile}, blocked={entry.NoDraw}";
            int energyBefore = ctx.Player.PlayerCombatState!.Energy;
            int drawn = upgraded ? entry.DrawUpgraded : entry.DrawBase;
            int energy = upgraded ? entry.EnergyUpgraded : entry.EnergyBase;
            ctx.AssertEqual(label + " hand includes source before play", entry.OtherHand + 1,
                PileType.Hand.GetPile(ctx.Player).Cards.Count);
            await ctx.Play(source);
            ctx.AssertEqual(label + " resulting hand excludes played source", entry.OtherHand + drawn,
                PileType.Hand.GetPile(ctx.Player).Cards.Count);
            ctx.AssertEqual(label + " energy independent of draw availability", energy,
                ctx.Player.PlayerCombatState.Energy - energyBefore);
            ctx.AssertEqual(label + " remaining draw pile", entry.DrawPile - drawn,
                PileType.Draw.GetPile(ctx.Player).Cards.Count);
            ctx.AssertTrue(label + " original hand preserved", held.All(c => c.Pile?.Type == PileType.Hand));
            ctx.AssertEqual(label + " actual drawn identities", drawn, draw.Count(c => c.Pile?.Type == PileType.Hand));
            ctx.AssertEqual(label + " played card discarded", PileType.Discard, source.Pile?.Type);
        }
    }
}
#endif
