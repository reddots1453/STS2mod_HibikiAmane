#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncForgeContract
{
    private sealed record Expected(string Id, Type Model, int Amount, string Display);
    // Design oracle, independent of the private production Option/Apply switch.
    private static readonly Expected[] All =
    [
        new("swift", typeof(Swift), 4, "迅捷：4"),
        new("charge", typeof(ChargeEnchantment), 3, "充能：3"),
        new("glam", typeof(Glam), 1, "华彩"),
        new("ember", typeof(TezcatarasEmber), 1, "特兹卡塔拉的余烬"),
        new("instinct", typeof(Instinct), 1, "本能"),
        new("proliferation", typeof(ProliferationEnchantment), 1, "增殖"),
        new("iron_wall", typeof(IronWallEnchantment), 1, "铁壁"),
        new("nimble", typeof(Nimble), 8, "灵巧：8"),
        new("sharp", typeof(Sharp), 8, "锋利：8"),
    ];
    private sealed class Selector(Func<CardModel[], Task<IEnumerable<CardModel>>> choose) : ICardSelector
    {
        internal int Calls { get; private set; }
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int min, int max)
        { Calls++; return choose(options.ToArray()); }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => throw new InvalidOperationException("Unexpected reward");
    }
    private static string[] Predict(string[] ids, Rng rng)
    {
        string[] shuffled = ids.ToArray();
        for (int i = shuffled.Length - 1; i > 0; i--)
        { int j = rng.NextInt(i + 1); (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); }
        return shuffled.Take(3).ToArray();
    }
    private static void SeedFor(CardEffectTestContext ctx, string[] ids, string desired)
    {
        for (ulong seed = 0; seed < 1000; seed++)
            if (Predict(ids, new Rng(seed)).Contains(desired))
            { ctx.Player.RunState.Rng.CombatCardSelection.LoadFromSerializable(new Rng(seed).ToSerializable()); return; }
        throw new InvalidOperationException("No deterministic fixture seed for " + desired);
    }
    private static async Task Play(CardEffectTestContext ctx, CardModel source, Selector selector)
    {
        await CardPileCmd.Add(source, PileType.Hand, skipVisuals: true);
        using (CardSelectCmd.UseSelector(selector))
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), source, null, skipCardPileVisuals: true);
    }
    private static bool Allowed(string id, CardModel card) => id switch
    {
        "instinct" or "iron_wall" or "sharp" => card.Type == CardType.Attack,
        "nimble" => card.GainsBlock,
        _ => true,
    };

    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        ctx.AssertEqual("forge rarity", CardRarity.Rare, card.Rarity, effect: false);
        ctx.AssertEqual("forge cost", upgraded ? 0 : 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("forge full description", "从3个强大的附魔中选择一项，附加给1张手牌。\n消耗。",
            DesignSyncNeutralTextContract.Normalize(card.GetDescriptionForPile(PileType.Hand, ctx.PrimaryEnemy)), effect: false);
        var selectionState = ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable();
        try
        {
            foreach (Expected expected in All) await CheckOption(ctx, upgraded, expected);
            await CheckSkillOnly(ctx, upgraded);
            await CheckEmpty(ctx, upgraded);
            foreach (string change in new[] { "moved", "enchanted", "all_moved", "foreign_option" })
                await CheckInvalidation(ctx, upgraded, change);
            await CheckPermanentAndLayered(ctx, upgraded);
        }
        finally { ctx.Player.RunState.Rng.CombatCardSelection.LoadFromSerializable(selectionState); }
    }

    private static async Task CheckOption(CardEffectTestContext ctx, bool upgraded, Expected expected)
    {
        await ctx.Reset();
        CardModel[] hand = [await ctx.Add<StrikeIronclad>(PileType.Hand), await ctx.Add<StrikeIronclad>(PileType.Hand),
            await ctx.Add<DefendIronclad>(PileType.Hand), await ctx.Add<DefendIronclad>(PileType.Hand),
            await ctx.Add<BasicTraining>(PileType.Hand)];
        var occupied = await ctx.Add<StrikeIronclad>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(occupied, 2);
        var outside = await ctx.Add<StrikeIronclad>(PileType.Draw);
        CardModel target = hand.First(c => Allowed(expected.Id, c));
        SeedFor(ctx, All.Select(x => x.Id).ToArray(), expected.Id);
        var predicted = new Rng(ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable());
        string[] choices = Predict(All.Select(x => x.Id).ToArray(), predicted);
        string generation = ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable().ToString();
        var selector = new Selector(options =>
        {
            if (options[0] is EnchantmentChoiceCard)
            {
                ctx.AssertEqual("three distinct options", 3, options.Select(c => ((EnchantmentChoiceCard)c).ChoiceId).Distinct().Count());
                ctx.AssertTrue("predicted option order", options.Cast<EnchantmentChoiceCard>().Select(c => c.ChoiceId).SequenceEqual(choices));
                foreach (var choice in options.Cast<EnchantmentChoiceCard>())
                    ctx.AssertEqual("exact option text " + choice.ChoiceId, "选择附魔：" + All.Single(x => x.Id == choice.ChoiceId).Display + "。",
                        DesignSyncNeutralTextContract.Normalize(choice.GetDescriptionForPile(PileType.None, null)));
                return Task.FromResult<IEnumerable<CardModel>>([options.Cast<EnchantmentChoiceCard>().Single(c => c.ChoiceId == expected.Id)]);
            }
            ctx.AssertTrue("exact legal hand target set " + expected.Id,
                options.ToHashSet().SetEquals(hand.Where(c => Allowed(expected.Id, c))));
            return Task.FromResult<IEnumerable<CardModel>>([target]);
        });
        var source = ctx.Create<BeyondReasonForge>(upgraded);
        await Play(ctx, source, selector);
        ctx.AssertEqual("both choices executed", 2, selector.Calls);
        ctx.AssertEqual("precise enchantment type", expected.Model, target.Enchantment?.GetType());
        ctx.AssertEqual("precise enchantment amount", expected.Amount, target.Enchantment?.Amount);
        ctx.AssertTrue("unselected legal cards unchanged", hand.Where(c => c != target).All(c => c.Enchantment == null));
        ctx.AssertTrue("occupied slot preserved", occupied.Enchantment is Sharp { Amount: 2 });
        ctx.AssertTrue("draw pile card unchanged", outside.Enchantment == null && outside.Pile?.Type == PileType.Draw);
        ctx.AssertEqual("only expected selection RNG consumed", predicted.ToSerializable().ToString(), ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable().ToString());
        ctx.AssertEqual("generation RNG untouched", generation, ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable().ToString());
        ctx.AssertEqual("forge exhausts", PileType.Exhaust, source.Pile?.Type);
    }

    private static async Task CheckSkillOnly(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        var skill1 = await ctx.Add<DefendIronclad>(PileType.Hand);
        var skill2 = await ctx.Add<DefendIronclad>(PileType.Hand);
        string[] legal = ["swift", "charge", "glam", "ember", "proliferation", "nimble"];
        SeedFor(ctx, legal, "nimble");
        var predicted = new Rng(ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable());
        var expected = Predict(legal, predicted);
        var selector = new Selector(options =>
        {
            if (options[0] is EnchantmentChoiceCard)
            {
                ctx.AssertTrue("skills exclude attack-only enchantments before RNG",
                    options.Cast<EnchantmentChoiceCard>().Select(c => c.ChoiceId).SequenceEqual(expected));
                return Task.FromResult<IEnumerable<CardModel>>([options.Cast<EnchantmentChoiceCard>().Single(c => c.ChoiceId == "nimble")]);
            }
            ctx.AssertTrue("only the two skill targets", options.ToHashSet().SetEquals([skill1, skill2]));
            return Task.FromResult<IEnumerable<CardModel>>([skill1]);
        });
        await Play(ctx, ctx.Create<BeyondReasonForge>(upgraded), selector);
        ctx.AssertTrue("skill receives nimble eight", skill1.Enchantment is Nimble { Amount: 8 });
        ctx.AssertTrue("unselected skill unchanged", skill2.Enchantment == null);
    }

    private static async Task CheckEmpty(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        var occupied = await ctx.Add<StrikeIronclad>(PileType.Hand);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(occupied, 2);
        await ctx.Add<Wound>(PileType.Hand);
        var selector = new Selector(_ => throw new InvalidOperationException("No legal target must not prompt"));
        string before = ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable().ToString();
        var source = ctx.Create<BeyondReasonForge>(upgraded);
        await Play(ctx, source, selector);
        ctx.AssertEqual("empty target set never prompts", 0, selector.Calls);
        ctx.AssertEqual("empty target set consumes no random", before, ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable().ToString());
        ctx.AssertEqual("empty target set still exhausts source", PileType.Exhaust, source.Pile?.Type);
    }

    private static async Task CheckInvalidation(CardEffectTestContext ctx, bool upgraded, string change)
    {
        await ctx.Reset();
        var target = await ctx.Add<StrikeIronclad>(PileType.Hand);
        var other = await ctx.Add<StrikeIronclad>(PileType.Hand);
        string[] legal = All.Where(x => x.Id != "nimble").Select(x => x.Id).ToArray();
        SeedFor(ctx, legal, "charge");
        var selector = new Selector(async options =>
        {
            await Task.Yield();
            if (options[0] is EnchantmentChoiceCard)
            {
                var choice = options.Cast<EnchantmentChoiceCard>().Single(c => c.ChoiceId == "charge");
                if (change == "all_moved")
                { await CardPileCmd.Add(target, PileType.Discard, skipVisuals: true); await CardPileCmd.Add(other, PileType.Discard, skipVisuals: true); }
                if (change == "foreign_option")
                { var fake = ctx.Player.RunState.CreateCard<EnchantmentChoiceCard>(ctx.Player); fake.Configure("charge", "充能：3"); return [fake]; }
                return [choice];
            }
            ctx.AssertTrue("target was legal at chooser open", options.Contains(target));
            if (change == "moved") await CardPileCmd.Add(target, PileType.Discard, skipVisuals: true);
            if (change == "enchanted") CombatEnchantmentCmd.ApplyVanilla<Sharp>(target, 4);
            return [target];
        });
        var source = ctx.Create<BeyondReasonForge>(upgraded);
        await Play(ctx, source, selector);
        ctx.AssertTrue("stale choice does not receive charge " + change, target.Enchantment is not ChargeEnchantment);
        ctx.AssertTrue("other target never receives fallback enchantment", other.Enchantment == null);
        ctx.AssertEqual("stale choice resolves source normally", PileType.Exhaust, source.Pile?.Type);
        ctx.AssertEqual("expected prompt count " + change, change is "all_moved" or "foreign_option" ? 1 : 2, selector.Calls);
        if (change == "enchanted") ctx.AssertTrue("intervening enchantment preserved", target.Enchantment is Sharp { Amount: 4 });
        if (change is "moved" or "all_moved") ctx.AssertEqual("departed card stays in discard", PileType.Discard, target.Pile?.Type);
    }

    private static async Task CheckPermanentAndLayered(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        var deck = ctx.Player.RunState.CreateCard<LightWings>(ctx.Player);
        try
        {
            await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
            var target = ctx.Combat.CloneCard(deck);
            target.DeckVersion = deck;
            await CardPileCmd.Add(target, PileType.Hand, skipVisuals: true);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(target, 2);
            var allIds = All.Where(x => x.Id != "nimble").Select(x => x.Id).ToArray();
            SeedFor(ctx, allIds, "charge");
            var selector = new Selector(options => Task.FromResult<IEnumerable<CardModel>>(
                options[0] is EnchantmentChoiceCard
                    ? [options.Cast<EnchantmentChoiceCard>().Single(c => c.ChoiceId == "charge")] : [target]));
            await Play(ctx, ctx.Create<BeyondReasonForge>(upgraded), selector);
            ctx.AssertTrue("light wings keeps layered exception", target.Enchantment is LayeredEnchantment);
            var layers = ((LayeredEnchantment)target.Enchantment!).Layers;
            ctx.AssertEqual("exactly two layers", 2, layers.Count);
            ctx.AssertTrue("original sharp retained", layers.OfType<Sharp>().Single().Amount == 2);
            ctx.AssertTrue("new charge applied", layers.OfType<ChargeEnchantment>().Single().Amount == 3);
            ctx.AssertTrue("permanent original not enchanted", deck.Enchantment == null);
        }
        finally { await CardPileCmd.RemoveFromDeck(deck); }
    }
}
#endif
