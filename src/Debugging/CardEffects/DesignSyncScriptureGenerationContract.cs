#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncScriptureGenerationContract
{
    internal static readonly Type[] Types = [typeof(Chant), typeof(Consecration), typeof(HolyPunishment), typeof(Gospel)];
    private static readonly Type[] RandomTypes = [typeof(GuardianScripture), typeof(NimbleScripture),
        typeof(PunishmentScripture), typeof(WisdomScripture), typeof(VitalityScripture), typeof(BlissScripture)];

    private sealed class Selector(Func<CardModel[], Task<IEnumerable<CardModel>>> choose) : ICardSelector
    {
        internal int Calls { get; private set; }
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        { Calls++; return choose(options.ToArray()); }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => throw new InvalidOperationException("Unexpected reward");
    }
    private static async Task Play(CardEffectTestContext ctx, CardModel card, Selector selector, bool attack = false)
    {
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        using (CardSelectCmd.UseSelector(selector))
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, attack ? ctx.PrimaryEnemy : null, skipCardPileVisuals: true);
    }
    private static string RandomState(CardEffectTestContext ctx) => ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable().ToString();

    internal static async Task Chant(CardEffectTestContext ctx, CardModel _, bool upgraded)
    {
        for (int selectedIndex = 0; selectedIndex < 3; selectedIndex++)
        {
            await ctx.Reset();
            var source = ctx.Create<Chant>(upgraded);
            var predicted = new Rng(ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable());
            var available = new List<Type> { typeof(GuardianScripture), typeof(PunishmentScripture), typeof(NimbleScripture),
                typeof(WisdomScripture), typeof(VitalityScripture), typeof(BlissScripture) };
            var expected = new List<Type>();
            for (int i = 0; i < 3; i++) { int index = predicted.NextInt(available.Count); expected.Add(available[index]); available.RemoveAt(index); }
            CardModel? selected = null;
            CardModel[] offered = [];
            var selector = new Selector(options =>
            {
                offered = options;
                ctx.AssertEqual("chant has three distinct candidates", 3, options.Select(c => c.Id).Distinct().Count());
                ctx.AssertTrue("chant exact without-replacement RNG offers", options.Select(c => c.GetType()).SequenceEqual(expected));
                ctx.AssertTrue("all offers are unupgraded zero-cost scriptures", options.All(c => c is ScriptureCardTemplate
                    && !c.IsUpgraded && c.EnergyCost.Canonical == 0 && c.Owner == ctx.Player));
                selected = options[selectedIndex];
                return Task.FromResult<IEnumerable<CardModel>>([selected]);
            });
            int deckCount = ctx.Player.Deck.Cards.Count;
            await Play(ctx, source, selector);
            ctx.AssertEqual("one choose-a-card prompt", 1, selector.Calls);
            ctx.AssertTrue("chosen instance reaches hand", PileType.Hand.GetPile(ctx.Player).Cards.SequenceEqual([selected!]));
            ctx.AssertTrue("unselected offers not in any pile", offered.Where(c => c != selected).All(c => c.Pile == null));
            ctx.AssertEqual("chant consumes only three generation rolls", predicted.ToSerializable().ToString(), RandomState(ctx));
            ctx.AssertEqual("chant source normal discard", PileType.Discard, source.Pile?.Type);
            ctx.AssertEqual("chant does not add to permanent deck", deckCount, ctx.Player.Deck.Cards.Count);
            ctx.AssertEqual("chant upgrade only changes source energy", upgraded ? 0 : 1, source.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        }
    }

    internal static async Task Gospel(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        var deck = ctx.Player.RunState.CreateCard<MaidenStrike>(ctx.Player);
        CardCmd.Upgrade(deck);
        CardCmd.Enchant<Sharp>(deck, 2);
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        try
        {
            var clone = ctx.Combat.CloneCard(deck);
            clone.DeckVersion = deck;
            await CardPileCmd.Add(clone, PileType.Hand, skipVisuals: false);
            var attack = await ctx.Add<StrikeIronclad>(PileType.Hand, skipVisuals: false);
            var skill = await ctx.Add<DefendIronclad>(PileType.Hand, skipVisuals: false);
            var upgradedSkill = await ctx.Add<DefendIronclad>(PileType.Hand, true, skipVisuals: false);
            var power = await ctx.Add<Consecration>(PileType.Hand);
            var status = await ctx.Add<Wound>(PileType.Hand);
            var curse = await ctx.Add<Regret>(PileType.Hand);
            string rngBefore = RandomState(ctx);
            await ctx.Play(card);
            var hand = PileType.Hand.GetPile(ctx.Player).Cards;
            ctx.AssertEqual("gospel keeps total hand count", 7, hand.Count);
            ctx.AssertEqual("both skills become guardians", 2, hand.OfType<GuardianScripture>().Count());
            ctx.AssertEqual("both attacks become punishment", 2, hand.OfType<PunishmentScripture>().Count());
            ctx.AssertTrue("only gospel upgrade determines replacement upgrade", hand.OfType<ScriptureCardTemplate>().All(c => c.IsUpgraded == upgraded));
            ctx.AssertTrue("native explicit transform does not inherit old enchantments", hand.OfType<ScriptureCardTemplate>().All(c => c.Enchantment == null));
            ctx.AssertTrue("all four original combat cards removed", new[] { clone, attack, skill, upgradedSkill }.All(c => c.HasBeenRemovedFromState));
            ctx.AssertTrue("power status curse are same untouched instances", new CardModel[] { power, status, curse }.All(hand.Contains));
            ctx.AssertTrue("permanent original untouched", deck.IsUpgraded && deck.Enchantment is Sharp { Amount: 2 }
                && deck.Pile?.Type == PileType.Deck && !deck.HasBeenRemovedFromState);
            ctx.AssertEqual("gospel exhausts only itself", PileType.Exhaust, card.Pile?.Type);
            ctx.AssertEqual("deterministic transformation consumes no generation RNG", rngBefore, RandomState(ctx));
        }
        finally { await CardPileCmd.RemoveFromDeck(deck); }
    }

    internal static async Task Consecration(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        await ctx.Play(card);
        var power = ctx.Self.GetPower<ConsecrationPower>()!;
        var choice = new BlockingPlayerChoiceContext();
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        string before = RandomState(ctx);
        await power.AfterPlayerTurnStart(choice, foreign);
        await power.AfterPlayerTurnStart(choice, ctx.Player); // Empty hand.
        ctx.AssertEqual("empty hand and foreign turn consume no RNG", before, RandomState(ctx));
        ctx.AssertEqual("consecration upgrade is innate", upgraded, card.Keywords.Contains(CardKeyword.Innate), effect: false);
        var selected = await ctx.Add<StrikeIronclad>(PileType.Hand, true, skipVisuals: false);
        var untouched = await ctx.Add<DefendIronclad>(PileType.Hand);
        var predicted = new Rng(ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable());
        Type expected = RandomTypes[predicted.NextInt(6)];
        var selector = new Selector(options =>
        {
            ctx.AssertTrue("consecration offers current hand only", options.ToHashSet().SetEquals(new CardModel[] { selected, untouched }));
            return Task.FromResult<IEnumerable<CardModel>>([selected]);
        });
        using (CardSelectCmd.UseSelector(selector)) await power.AfterPlayerTurnStart(choice, ctx.Player);
        var replacement = PileType.Hand.GetPile(ctx.Player).Cards.OfType<ScriptureCardTemplate>().Single();
        ctx.AssertEqual("consecration predicted scripture", expected, replacement.GetType());
        ctx.AssertEqual("consecration exact RNG advance", predicted.ToSerializable().ToString(), RandomState(ctx));
        ctx.AssertTrue("only selected instance transformed", selected.HasBeenRemovedFromState && untouched.Pile?.Type == PileType.Hand);
        ctx.AssertTrue("native transform resets previous upgrade", !replacement.IsUpgraded);

        foreach (bool removePower in new[] { false, true })
        {
            var moving = await ctx.Add<StrikeIronclad>(PileType.Hand);
            before = RandomState(ctx);
            var changing = new Selector(async options =>
            {
                ctx.AssertTrue("stale test starts with legal offered card", options.Contains(moving));
                await Task.Yield();
                if (removePower) await PowerCmd.Remove(power);
                else await CardPileCmd.Add(moving, PileType.Discard, skipVisuals: true);
                return [moving];
            });
            using (CardSelectCmd.UseSelector(changing)) await power.AfterPlayerTurnStart(choice, ctx.Player);
            ctx.AssertTrue("stale selection never transforms card", !moving.HasBeenRemovedFromState);
            ctx.AssertEqual("stale selection does not consume RNG", before, RandomState(ctx));
        }
        before = RandomState(ctx);
        await power.AfterPlayerTurnStart(choice, ctx.Player);
        ctx.AssertEqual("removed power callback is inert", before, RandomState(ctx));
    }

    internal static async Task Punishment(CardEffectTestContext ctx, CardModel _, bool upgraded)
    {
        foreach (string scenario in new[] { "plain", "accept", "decline", "amplified", "empty", "moved", "blocked" })
        {
            await ctx.Reset();
            bool armor = scenario != "plain";
            if (armor) await ctx.SetUpArmour(1);
            if (scenario == "amplified") await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 2);
            if (scenario == "blocked") await ctx.ApplyPower<NoDrawPower>(ctx.Self, 1);
            var source = ctx.Create<HolyPunishment>(upgraded);
            CardModel? target = null, untouched = null;
            if (scenario != "empty")
            {
                target = await ctx.Add<StrikeIronclad>(PileType.Draw);
                untouched = await ctx.Add<DefendIronclad>(PileType.Draw);
            }
            var predicted = new Rng(ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable());
            bool transforms = scenario is not "empty" and not "moved";
            Type? expected = transforms ? RandomTypes[predicted.NextInt(6)] : null;
            int prompts = 0, hp = ctx.PrimaryEnemy.CurrentHp;
            int historyBefore = CombatManager.Instance.History.Entries.Count();
            var selector = new Selector(async options =>
            {
                if (options.Contains(target!))
                {
                    ctx.AssertTrue("punishment choice only draw-pile cards", options.ToHashSet().SetEquals(new[] { target!, untouched! }));
                    if (scenario == "moved") await CardPileCmd.Add(target!, PileType.Discard, skipVisuals: true);
                    return [target!];
                }
                prompts++;
                ctx.AssertTrue("only armor payment requests overdraft choice", scenario is "accept" or "decline" or "blocked");
                return [options[scenario == "decline" ? 1 : 0]];
            });
            await Play(ctx, source, selector, attack: true);
            ctx.AssertDamage("punishment damage " + scenario, ctx.PrimaryEnemy, hp,
                scenario == "amplified" ? upgraded ? 18 : 13 : upgraded ? 12 : 9);
            ctx.AssertEqual("exact resource choice count " + scenario, scenario is "accept" or "decline" or "blocked" ? 1 : 0, prompts);
            ctx.AssertPower("exact armor payment " + scenario, ctx.Self, "MagicArmorPower", armor && scenario is not "accept" and not "blocked" ? 1 : 0);
            ctx.AssertPower("exact amplification consumption " + scenario, ctx.Self, "MagicAmplificationPower", scenario == "amplified" ? 1 : 0);
            ctx.AssertEqual("generation stream " + scenario, predicted.ToSerializable().ToString(), RandomState(ctx));
            var draws = CombatManager.Instance.History.Entries.Skip(historyBefore).OfType<CardDrawnEntry>().ToArray();
            ctx.AssertEqual("native draw history count " + scenario, scenario is "accept" or "amplified" ? 1 : 0, draws.Length);
            if (transforms)
            {
                var result = ctx.Player.Piles.SelectMany(p => p.Cards).OfType<ScriptureCardTemplate>().Single();
                ctx.AssertEqual("exact random scripture " + scenario, expected, result.GetType());
                ctx.AssertEqual("release retrieves only transformed card " + scenario,
                    scenario is "accept" or "amplified" ? PileType.Hand : PileType.Draw, result.Pile?.Type);
                ctx.AssertTrue("transformation unupgraded and other draw card unchanged", !result.IsUpgraded
                    && target!.HasBeenRemovedFromState && untouched!.Pile?.Type == PileType.Draw);
                if (draws.Length == 1)
                    ctx.AssertTrue("native draw records transformed instance not an unrelated top card", draws[0].Card == result && !draws[0].FromHandDraw);
                if (scenario == "blocked")
                    ctx.AssertTrue("NoDraw prevents retrieval and leaves selected scripture on top", PileType.Draw.GetPile(ctx.Player).Cards[0] == result);
            }
            else ctx.AssertEqual("no generated scriptures without valid draw target", 0,
                ctx.Player.Piles.SelectMany(p => p.Cards).OfType<ScriptureCardTemplate>().Count());
        }
    }
}
#endif
