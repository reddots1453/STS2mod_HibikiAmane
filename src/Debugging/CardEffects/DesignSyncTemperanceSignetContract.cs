#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncTemperanceSignetContract
{
    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        int limit = upgraded ? 4 : 3;
        ctx.AssertEqual("signet ancient", CardRarity.Ancient, card.Rarity, effect: false);
        ctx.AssertEqual("signet skill", CardType.Skill, card.Type, effect: false);
        ctx.AssertEqual("signet cost", 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        var outside = ctx.Player.RunState.CreateCard<TemperanceSignet>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
            ctx.AssertEqual("signet full rendered text " + pile,
                $"本回合你不能再抽牌。\n选择一个牌堆，随机打出其中的{limit}张牌。\n消耗。",
                DesignSyncNeutralTextContract.Normalize(instance.GetDescriptionForPile(pile)), effect: false);
        foreach (var (pile, option) in new[] { (PileType.Draw, 0), (PileType.Discard, 1), (PileType.Exhaust, 2) })
        foreach (int count in new[] { 0, 1, 3, 4, 6 })
        {
            await ctx.Reset();
            var originals = new List<CardModel>();
            for (int i = 0; i < count; i++) originals.Add(await ctx.Add<DefendIronclad>(pile, i % 2 == 1));
            var rng = new Rng(ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable());
            originals.Sort();
            var expected = originals.ToArray();
            for (int i = expected.Length - 1; i > 0; i--)
            { int j = rng.NextInt(i + 1); (expected[i], expected[j]) = (expected[j], expected[i]); }
            var played = expected.Take(limit).ToArray();
            var signet = ctx.Create<TemperanceSignet>(upgraded);
            int history = CombatManager.Instance.History.Entries.Count();
            string shuffle = ctx.Player.RunState.Rng.Shuffle.ToSerializable().ToString();
            await ctx.Play(signet, selectedIndices: [option]);
            var actual = CombatManager.Instance.History.Entries.Skip(history).OfType<CardPlayStartedEntry>()
                .Select(e => e.CardPlay.Card).Where(c => c != signet).ToArray();
            ctx.AssertTrue("signet exact independent random identities " + pile + count, actual.SequenceEqual(played));
            ctx.AssertEqual("signet exact block", played.Sum(c => c.IsUpgraded ? 8m : 5m), ctx.Self.Block);
            ctx.AssertEqual("signet no repeated returned cards", Math.Min(count, limit), actual.Distinct().Count());
            ctx.AssertEqual("signet exact generation rng", rng.ToSerializable().ToString(), ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable().ToString());
            ctx.AssertEqual("signet preserves shuffle rng", shuffle, ctx.Player.RunState.Rng.Shuffle.ToSerializable().ToString());
            ctx.AssertEqual("signet actually exhausts", PileType.Exhaust, signet.Pile!.Type);
            ctx.AssertTrue("signet has native temporary draw ban", ctx.Self.HasPower<NoDrawPower>());
            var draw = await ctx.Add<StrikeIronclad>(PileType.Draw);
            var context = new BlockingPlayerChoiceContext();
            await CardPileCmd.Draw(context, 1, ctx.Player);
            ctx.AssertEqual("signet actually prevents draw", PileType.Draw, draw.Pile!.Type);
            await ctx.Self.GetPower<NoDrawPower>()!.AfterSideTurnEnd(context, CombatSide.Player, [ctx.Self]);
            ctx.AssertTrue("signet draw ban expires at turn end", !ctx.Self.HasPower<NoDrawPower>());
            int hand = PileType.Hand.GetPile(ctx.Player).Cards.Count;
            await CardPileCmd.Draw(context, 1, ctx.Player);
            ctx.AssertEqual("signet native draw resumes", hand + 1, PileType.Hand.GetPile(ctx.Player).Cards.Count);
        }
    }
}
#endif
