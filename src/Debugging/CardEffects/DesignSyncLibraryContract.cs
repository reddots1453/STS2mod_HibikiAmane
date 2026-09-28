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
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncLibraryContract
{
    private static readonly PileType[] Piles = [PileType.Draw, PileType.Discard, PileType.Exhaust];
    private sealed class Selector(Func<CardModel[], Task<IEnumerable<CardModel>>> choose) : ICardSelector
    {
        internal int Calls { get; private set; }
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        { Calls++; return choose(options.ToArray()); }
        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => throw new InvalidOperationException("Unexpected reward");
    }
    private static string Generation(CardEffectTestContext ctx) => ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable().ToString();
    private static CardModel[] PlaysSince(int before) => CombatManager.Instance.History.Entries.Skip(before)
        .OfType<CardPlayStartedEntry>().Select(entry => entry.CardPlay.Card).ToArray();

    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        ctx.AssertEqual("library ancient rarity", CardRarity.Ancient, card.Rarity, effect: false);
        ctx.AssertEqual("library cost", 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("library upgraded retain", upgraded, card.Keywords.Contains(CardKeyword.Retain), effect: false);
        var outside = ctx.Player.RunState.CreateCard<TemperanceCirclet>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
        {
            string expected = (upgraded ? "保留。\n" : "") + "你不能抽牌。\n回合开始时，选择一个牌堆，随机打出其中的10张牌。";
            ctx.AssertEqual("library full rendered text " + pile, expected,
                DesignSyncNeutralTextContract.Normalize(instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)), effect: false);
            ctx.AssertEqual("library renamed title", upgraded ? "节制之环+" : "节制之环", instance.Title, effect: false);
        }
        foreach (PileType pile in Piles)
        foreach (int count in new[] { 0, 1, 9, 10, 12 })
            await CheckPile(ctx, upgraded, pile, count);
        await CheckNativeUnplayable(ctx, upgraded);
        await CheckLifecycle(ctx, upgraded);
    }

    private static async Task CheckPile(CardEffectTestContext ctx, bool upgraded, PileType pile, int count)
    {
        await ctx.Reset();
        await ctx.Play(ctx.Create<TemperanceCirclet>(upgraded));
        var power = ctx.Self.GetPower<YarusLibraryPower>()!;
        var cards = new List<CardModel>();
        for (int i = 0; i < count; i++) cards.Add(await ctx.Add<StrikeIronclad>(pile, i % 2 == 1));
        var untouched = new Dictionary<PileType, CardModel[]>();
        foreach (PileType other in Piles.Where(other => other != pile))
            untouched.Add(other, [await ctx.Add<DefendIronclad>(other), await ctx.Add<DefendIronclad>(other)]);
        var hand = await ctx.Add<DefendIronclad>(PileType.Hand);
        int deckCount = ctx.Player.Deck.Cards.Count;
        ctx.AssertTrue("library blocks both ordinary and hand draw", !power.ShouldDraw(ctx.Player, false) && !power.ShouldDraw(ctx.Player, true));
        var context = new BlockingPlayerChoiceContext();
        int history = CombatManager.Instance.History.Entries.Count();
        await CardPileCmd.Draw(context, 1, ctx.Player);
        ctx.AssertTrue("native draw is prevented", PileType.Hand.GetPile(ctx.Player).Cards.SequenceEqual([hand]));
        ctx.AssertEqual("prevented draw has no drawn history", 0,
            CombatManager.Instance.History.Entries.Skip(history).OfType<CardDrawnEntry>().Count());

        // Independent implementation of native stable Fisher-Yates; do not call the production shuffle helper.
        var predictedRng = new Rng(ctx.Player.RunState.Rng.CombatCardGeneration.ToSerializable());
        var sorted = cards.ToList();
        sorted.Sort(); // Match native equal-key ordering before independent Fisher-Yates.
        var expected = sorted.ToArray();
        for (int i = expected.Length - 1; i > 0; i--)
        { int j = predictedRng.NextInt(i + 1); (expected[i], expected[j]) = (expected[j], expected[i]); }
        CardModel[] chosen = expected.Take(10).ToArray();
        string shuffleBefore = ctx.Player.RunState.Rng.Shuffle.ToSerializable().ToString();
        int hpBefore = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
        await PlayerCmd.SetEnergy(0, ctx.Player);
        var selector = new Selector(options =>
        {
            ctx.AssertEqual("library offers exactly three pile choices", 3, options.Length);
            ctx.AssertTrue("library offered pile order", options.Cast<LibraryPileChoice>().Select(c => c.SelectedPile).SequenceEqual(Piles));
            ctx.AssertTrue("library choices remain unattached tokens", options.All(c => c.Pile == null && c.Owner == ctx.Player));
            return Task.FromResult<IEnumerable<CardModel>>([options.Cast<LibraryPileChoice>().Single(c => c.SelectedPile == pile)]);
        });
        history = CombatManager.Instance.History.Entries.Count();
        using (CardSelectCmd.UseSelector(selector)) await power.AfterPlayerTurnStart(context, ctx.Player);
        CardModel[] actual = PlaysSince(history);
        ctx.AssertEqual($"library {pile}/{count} prompts once", 1, selector.Calls);
        ctx.AssertTrue("library exact random play identities and order", actual.SequenceEqual(chosen));
        ctx.AssertEqual("library never replays returned discard cards", Math.Min(10, count), actual.Distinct().Count());
        ctx.AssertEqual("library actual upgraded and base damage", chosen.Sum(c => c.IsUpgraded ? 9 : 6), hpBefore - ctx.Enemies.Sum(e => e.CurrentHp));
        ctx.AssertEqual("library consumes only predicted generation RNG", predictedRng.ToSerializable().ToString(), Generation(ctx));
        ctx.AssertEqual("library does not consume deck shuffle RNG", shuffleBefore, ctx.Player.RunState.Rng.Shuffle.ToSerializable().ToString());
        ctx.AssertTrue("library unplayed cards stay in selected pile", cards.Except(chosen).All(c => c.Pile == pile.GetPile(ctx.Player)));
        foreach (var (other, originals) in untouched)
        {
            ctx.AssertTrue("library leaves other pile instances in place " + other, originals.All(c => c.Pile == other.GetPile(ctx.Player)));
            ctx.AssertTrue("library does not play from other piles " + other, !actual.Intersect(originals).Any());
            ctx.AssertTrue("library preserves original relative order " + other,
                other.GetPile(ctx.Player).Cards.Where(originals.Contains).SequenceEqual(originals));
        }
        ctx.AssertTrue("library leaves hand untouched", PileType.Hand.GetPile(ctx.Player).Cards.SequenceEqual([hand]));
        ctx.AssertEqual("library free plays cost no energy", 0, ctx.Player.PlayerCombatState!.Energy);
        ctx.AssertEqual("library does not change permanent deck", deckCount, ctx.Player.Deck.Cards.Count);
    }

    private static async Task CheckNativeUnplayable(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.Play(ctx.Create<TemperanceCirclet>(upgraded));
        var power = ctx.Self.GetPower<YarusLibraryPower>()!;
        var wound = await ctx.Add<Wound>(PileType.Exhaust);
        var defend = await ctx.Add<DefendIronclad>(PileType.Exhaust);
        var selector = new Selector(options => Task.FromResult<IEnumerable<CardModel>>(
            [options.Cast<LibraryPileChoice>().Single(c => c.SelectedPile == PileType.Exhaust)]));
        int history = CombatManager.Instance.History.Entries.Count();
        using (CardSelectCmd.UseSelector(selector))
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
        ctx.AssertTrue("library delegates unplayable skip to native AutoPlay", PlaysSince(history).SequenceEqual([defend]));
        ctx.AssertEqual("library plays skill from exhaust", 5m, ctx.Self.Block);
        ctx.AssertEqual("native unplayable result is discard", PileType.Discard, wound.Pile?.Type);
        ctx.AssertEqual("played exhaust-pile skill normal result", PileType.Discard, defend.Pile?.Type);
    }

    private static async Task CheckLifecycle(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.Play(ctx.Create<TemperanceCirclet>(upgraded));
        var power = ctx.Self.GetPower<YarusLibraryPower>()!;
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        var context = new BlockingPlayerChoiceContext();
        var forbidden = new Selector(_ => throw new InvalidOperationException("Inactive library must not prompt"));
        string before = Generation(ctx);
        using (CardSelectCmd.UseSelector(forbidden)) await power.AfterPlayerTurnStart(context, foreign);
        ctx.AssertTrue("library permits other player's draws", power.ShouldDraw(foreign, false) && power.ShouldDraw(foreign, true));
        ctx.AssertEqual("foreign turn consumes no generation RNG", before, Generation(ctx));
        ctx.AssertEqual("foreign turn never prompts", 0, forbidden.Calls);
        var selectedCard = await ctx.Add<StrikeIronclad>(PileType.Draw);
        int history = CombatManager.Instance.History.Entries.Count();
        foreach (bool cancel in new[] { false, true })
        {
            var failing = new Selector(async _ =>
            {
                await Task.Yield();
                if (cancel) throw new TaskCanceledException("library fixture cancellation");
                throw new InvalidOperationException("library fixture failure");
            });
            Exception? failure = null;
            try
            {
                using (CardSelectCmd.UseSelector(failing)) await power.AfterPlayerTurnStart(context, ctx.Player);
            }
            catch (Exception ex) { failure = ex; }
            ctx.AssertTrue("library selection failure propagates without settling", cancel ? failure is TaskCanceledException : failure is InvalidOperationException);
            ctx.AssertEqual("failed selection consumes no generation RNG", before, Generation(ctx));
            ctx.AssertEqual("failed selection plays nothing", 0, PlaysSince(history).Length);
            ctx.AssertTrue("failed selection retains source card and power", selectedCard.Pile?.Type == PileType.Draw && ctx.Self.Powers.Contains(power));
        }
        var removing = new Selector(async options =>
        {
            await PowerCmd.Remove(power);
            return [options.Cast<LibraryPileChoice>().Single(c => c.SelectedPile == PileType.Draw)];
        });
        using (CardSelectCmd.UseSelector(removing)) await power.AfterPlayerTurnStart(context, ctx.Player);
        ctx.AssertEqual("pending choice invoked once", 1, removing.Calls);
        ctx.AssertEqual("removed during choice plays nothing", 0, PlaysSince(history).Length);
        ctx.AssertTrue("removed during choice leaves pile intact", selectedCard.Pile == PileType.Draw.GetPile(ctx.Player));
        ctx.AssertEqual("removed during choice consumes no generation RNG", before, Generation(ctx));
        using (CardSelectCmd.UseSelector(forbidden)) await power.AfterPlayerTurnStart(context, ctx.Player);
        ctx.AssertEqual("removed reference never prompts", 0, forbidden.Calls);
        ctx.AssertTrue("removed reference permits both draws", power.ShouldDraw(ctx.Player, false) && power.ShouldDraw(ctx.Player, true));
        await CardPileCmd.Draw(context, 1, ctx.Player);
        ctx.AssertTrue("native draw resumes after removal", selectedCard.Pile?.Type == PileType.Hand);
        ctx.AssertTrue("canonical library does not dereference absent owner", ModelDb.Power<YarusLibraryPower>().ShouldDraw(ctx.Player, false));
    }
}
#endif
