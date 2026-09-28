#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncConfirmedStackContract
{
    internal static async Task Chain(CardEffectTestContext ctx, ChainDestruction card, bool upgraded)
    {
        var choice = new BlockingPlayerChoiceContext();
        async Task ExhaustTwo()
        {
            for (int i = 0; i < 2; i++)
                await CardCmd.Exhaust(choice, await ctx.Add<MaidenDefend>(PileType.Hand));
        }
        await ctx.Play(card);
        var first = ctx.Self.Powers.OfType<ChainDestructionPower>().Single();
        await ExhaustTwo();
        await ctx.Play(ctx.Create<ChainDestruction>(upgraded));
        var instances = ctx.Self.Powers.OfType<ChainDestructionPower>().ToArray();
        ctx.AssertEqual("chain applications remain separate", 2, instances.Length);
        var second = instances.Single(power => power != first);
        ctx.AssertEqual("existing chain retains progress", 2, first.DisplayAmount);
        ctx.AssertEqual("new chain starts fresh", 4, second.DisplayAmount);
        await ExhaustTwo();
        ctx.AssertEqual("first chain wraps alone", 4, first.DisplayAmount);
        ctx.AssertEqual("second chain counts independently", 2, second.DisplayAmount);
        ctx.AssertPower("first queued card", ctx.Self, "ChainDestructionReplayPower", 1);
        await ExhaustTwo();
        ctx.AssertEqual("second chain wraps", 4, second.DisplayAmount);
        ctx.AssertPower("two queued cards", ctx.Self, "ChainDestructionReplayPower", 2);
        await ExhaustTwo();
        ctx.AssertPower("three queued cards", ctx.Self, "ChainDestructionReplayPower", 3);
        for (int i = 0; i < 4; i++)
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("queue buffs successive cards " + i, ctx.PrimaryEnemy, hp, i < 3 ? 12 : 6);
            ctx.AssertPower("queue consumes one per card " + i, ctx.Self, "ChainDestructionReplayPower", Math.Max(0, 2 - i));
        }
    }

    internal static async Task Lullaby(CardEffectTestContext ctx, Lullaby card, bool upgraded)
    {
        foreach (int layers in new[] { 1, 2, 3 })
        {
            await ctx.Reset();
            for (int i = 0; i < layers; i++) await ctx.Play(ctx.Create<Lullaby>(upgraded));
            await ctx.AddFillerCards(PileType.Hand, 2);
            ctx.AssertPower("lullaby displays application count", ctx.Self, "LullabyPower", layers);
            int block = ctx.Self.Block;
            await Hook.BeforeFlush(ctx.Combat, ctx.Player);
            ctx.AssertEqual("one drowsy per layer", layers, ctx.CountCards<DrowsyStatus>(PileType.Hand));
            ctx.AssertEqual("all generated before block calculation", layers + 2, PileType.Hand.GetPile(ctx.Player).Cards.Count);
            ctx.AssertBlock("all layers use final hand size", block, (layers + 2) * layers * 2);
        }
    }

    internal static async Task Purification(CardEffectTestContext ctx, SoulPurification card, bool upgraded)
    {
        int perPlay = upgraded ? 2 : 1;
        await ctx.Play(card);
        await ctx.AddFillerCards(PileType.Draw, perPlay * 2);
        var scripture = await ctx.Add<GuardianScripture>(PileType.Hand);
        CardCmd.Enchant<Glam>(scripture, 1);
        int start = CombatManager.Instance.History.Entries.Count();
        await ctx.Play(scripture);
        var events = CombatManager.Instance.History.Entries.Skip(start).ToArray();
        int exhaustedAt = Array.FindIndex(events, entry => entry is CardExhaustedEntry e && e.Card == scripture);
        ctx.AssertTrue("scripture exhausted after native replay group", exhaustedAt >= 0);
        ctx.AssertEqual("scripture exhaust occurs only once", 1, events.OfType<CardExhaustedEntry>().Count(e => e.Card == scripture));
        ctx.AssertEqual("draw on every replay", perPlay * 2, events.OfType<CardDrawnEntry>().Count(e => e.Card.Owner == ctx.Player));
        ctx.AssertEqual("all draws precede exhaustion", perPlay * 2,
            events.Take(exhaustedAt).OfType<CardDrawnEntry>().Count(e => e.Card.Owner == ctx.Player));
        ctx.AssertEqual("scripture final pile", PileType.Exhaust, scripture.Pile!.Type);
    }
}
#endif
