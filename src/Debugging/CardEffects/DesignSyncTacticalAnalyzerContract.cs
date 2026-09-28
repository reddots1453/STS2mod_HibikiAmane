#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncTacticalAnalyzerContract
{
    private sealed class InspectSelector(Func<CardModel[], int, int, Task<IEnumerable<CardModel>>> choose) : ICardSelector
    {
        internal int Calls { get; private set; }
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            Calls++;
            return choose(options.ToArray(), minSelect, maxSelect);
        }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => throw new InvalidOperationException("Unexpected reward selection");
    }

    private static async Task Play(CardEffectTestContext ctx, CardModel card, InspectSelector selector)
    {
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        using (CardSelectCmd.UseSelector(selector))
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, null, skipCardPileVisuals: true);
    }

    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        int count = upgraded ? 2 : 1;
        var fillers = await ctx.AddFillerCards(PileType.Draw, count);
        CardModel chosen = fillers[0];
        var alternative = await ctx.Add<DefendIronclad>(PileType.Hand);
        var alreadyUpgraded = await ctx.Add<DefendIronclad>(PileType.Hand, true);
        var enchanted = await ctx.Add<StrikeIronclad>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(enchanted, 3);
        var steady = await ctx.Add<DefendIronclad>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Steady>(steady, 1);
        var selector = new InspectSelector((options, min, max) =>
        {
            ctx.AssertTrue("only upgradeable enchantable hand instances offered",
                options.ToHashSet().SetEquals(fillers.Append(alternative)));
            ctx.AssertEqual("choose exactly one minimum", 1, min, effect: false);
            ctx.AssertEqual("choose exactly one maximum", 1, max, effect: false);
            return Task.FromResult<IEnumerable<CardModel>>([chosen]);
        });
        await Play(ctx, card, selector);
        ctx.AssertEqual("one actual selector call", 1, selector.Calls);
        ctx.AssertTrue("all promised cards drawn before selection", fillers.All(c => c.Pile?.Type == PileType.Hand));
        ctx.AssertTrue("selected drawn card upgraded and retained", chosen.IsUpgraded
            && chosen.Enchantment is Steady { Amount: 1 } && chosen.Keywords.Contains(CardKeyword.Retain));
        ctx.AssertTrue("ordinary enchanted card unchanged", !enchanted.IsUpgraded && enchanted.Enchantment is Sharp { Amount: 3 });
        ctx.AssertTrue("existing steady not overwritten or duplicated", !steady.IsUpgraded && steady.Enchantment is Steady { Amount: 1 });
        ctx.AssertTrue("other legal and upgraded cards untouched", !alternative.IsUpgraded && alternative.Enchantment == null
            && alreadyUpgraded.IsUpgraded && alreadyUpgraded.Enchantment == null);
        ctx.AssertEqual("source resolves to discard", PileType.Discard, card.Pile?.Type);

        // A combat clone receives both modifications; its permanent original receives neither.
        await ctx.Reset();
        var deck = ctx.Player.RunState.CreateCard<MaidenStrike>(ctx.Player);
        try
        {
            await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
            var clone = ctx.Combat.CloneCard(deck);
            clone.DeckVersion = deck;
            await CardPileCmd.Add(clone, PileType.Hand, skipVisuals: true);
            await ctx.AddFillerCards(PileType.Draw, count);
            await ctx.Play(ctx.Create<TacticalAnalyzer>(upgraded), selectedCards: [clone]);
            ctx.AssertTrue("combat clone upgraded and enchanted", clone.IsUpgraded && clone.Enchantment is Steady);
            ctx.AssertTrue("permanent deck card unmodified", !deck.IsUpgraded && deck.Enchantment == null
                && !deck.Keywords.Contains(CardKeyword.Retain));
        }
        finally { await CardPileCmd.RemoveFromDeck(deck); }

        // No legal targets: the draw still resolves and no empty chooser is requested.
        await ctx.Reset();
        var ineligible = new List<CardModel>();
        for (int i = 0; i < count; i++)
            ineligible.Add(await ctx.Add<DefendIronclad>(PileType.Draw, true));
        var occupied = await ctx.Add<StrikeIronclad>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(occupied, 2);
        var noChoice = new InspectSelector((_, _, _) => throw new InvalidOperationException("Empty eligibility must not open a selector"));
        var noTargetCard = ctx.Create<TacticalAnalyzer>(upgraded);
        await Play(ctx, noTargetCard, noChoice);
        ctx.AssertEqual("no selector for empty candidate set", 0, noChoice.Calls);
        ctx.AssertTrue("empty eligibility does not cancel draw", ineligible.All(c => c.Pile?.Type == PileType.Hand));
        ctx.AssertTrue("no illegal upgrade applied", !occupied.IsUpgraded && occupied.Enchantment is Sharp { Amount: 2 });
        ctx.AssertEqual("empty eligibility resolves source normally", PileType.Discard, noTargetCard.Pile?.Type);

        // The explicit LightWings exception must not be lost by a blanket empty-slot filter.
        await ctx.Reset();
        var wings = await ctx.Add<LightWings>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(wings, 2);
        await ctx.AddFillerCards(PileType.Draw, count);
        await ctx.Play(ctx.Create<TacticalAnalyzer>(upgraded), selectedCards: [wings]);
        ctx.AssertTrue("already enchanted LightWings remains eligible", wings.IsUpgraded
            && wings.Enchantment is LayeredEnchantment);
        var layers = (LayeredEnchantment)wings.Enchantment!;
        ctx.AssertEqual("exactly two layers after analyzer", 2, layers.Layers.Count);
        ctx.AssertTrue("original sharp plus new steady", layers.Layers.OfType<Sharp>().Single().Amount == 2
            && layers.Layers.OfType<Steady>().Single().Amount == 1 && wings.Keywords.Contains(CardKeyword.Retain));

        // Simulate changes while the native selector awaits a response, not invalid initial offers.
        foreach (string invalidation in new[] { "enchanted", "upgraded", "moved" })
        {
            await ctx.Reset();
            var target = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.AddFillerCards(PileType.Draw, count);
            var changingSelector = new InspectSelector(async (options, _, _) =>
            {
                ctx.AssertTrue("target was legal when chooser opened", options.Contains(target));
                await Task.Yield();
                if (invalidation == "enchanted") CombatEnchantmentCmd.ApplyVanilla<Sharp>(target, 4);
                else if (invalidation == "upgraded") CardCmd.Upgrade(target);
                else await CardPileCmd.Add(target, PileType.Discard, skipVisuals: true);
                return [target];
            });
            var source = ctx.Create<TacticalAnalyzer>(upgraded);
            await Play(ctx, source, changingSelector);
            ctx.AssertEqual("stale choice invoked once", 1, changingSelector.Calls);
            ctx.AssertEqual("stale target not additionally upgraded", invalidation == "upgraded", target.IsUpgraded);
            ctx.AssertTrue("stale target never gets stable enchantment", target.Enchantment is not Steady);
            if (invalidation == "enchanted")
                ctx.AssertTrue("intervening enchantment preserved", target.Enchantment is Sharp { Amount: 4 });
            if (invalidation == "moved")
                ctx.AssertEqual("departed target not moved back", PileType.Discard, target.Pile?.Type);
            ctx.AssertEqual("stale choice does not strand source", PileType.Discard, source.Pile?.Type);
        }
    }
}
#endif
