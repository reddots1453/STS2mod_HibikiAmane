#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Rewards;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Opt-in destructive tests: run once outside combat and once during combat.</summary>
public sealed class DesignRandomRelicTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_random_relics";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 Tony/Dead Branch tests; disposable run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_random_relics confirm in a disposable single-player Maiden run.");
        if (CombatManager.Instance.IsInProgress
            && (issuingPlayer.Creature.CombatState is not CombatState state
                || state.HittableEnemies.Count == 0 || CombatManager.Instance.IsOverOrEnding))
            return new CmdResult(false, "Combat tests require a living enemy and an active combat.");
        return new CmdResult(Run(issuingPlayer), true, "Destructive tests started; see [DS27RandomRelicTest].");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 random relic: " + name);
            checks++;
        }
        try
        {
            TestMode.IsOn = true;
            CardPoolModel[] routes = [ModelDb.CardPool<MSNeutralCardPool>(),
                ModelDb.CardPool<MSHolyCardPool>(), ModelDb.CardPool<MSCorruptCardPool>()];
            foreach (bool rareOnly in new[] { false, true })
            {
                var actual = UnifiedRouteCardPool.Get(player, rareOnly);
                var expected = routes.SelectMany(route => route.GetUnlockedCards(player.UnlockState,
                        player.RunState.CardMultiplayerConstraint))
                    .Where(card => rareOnly ? card.Rarity == CardRarity.Rare && card.CanBeGeneratedByModifiers
                        : card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare
                            && card.CanBeGeneratedInCombat && card is not HealingArt)
                    .Select(card => card.Id).ToHashSet();
                Check(expected.SetEquals(actual.Select(card => card.Id)), "all three routes enter the same candidate pool");
                Check(actual.Length == expected.Count, "each eligible card occupies exactly one lottery slot");
                Check(actual.Select(card => card.Id.Entry).SequenceEqual(
                    actual.Select(card => card.Id.Entry).OrderBy(id => id, StringComparer.Ordinal)), "stable candidate order");
                Check(routes.All(route => actual.Any(card => route.GetUnlockedCards(player.UnlockState,
                    player.RunState.CardMultiplayerConstraint).Any(candidate => candidate.Id == card.Id))), "all routes represented");
            }
            Check(UnifiedRouteCardPool.Get(player).All(card => card is not HealingArt), "STS1 excludes healing from combat generation");
            Check(UnifiedRouteCardPool.Get(player, true).Any(card => card is HealingArt), "Tony's permanent rewards do not inherit the combat healing exclusion");

            if (CombatManager.Instance.IsInProgress)
                await TestBranch(player, Check);
            else
                await TestTony(player, Check);
            MaidenSuccubusMod.Logger.Info($"[DS27RandomRelicTest] PASS {checks} assertions ({(CombatManager.Instance.IsInProgress ? "combat" : "run")}); disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27RandomRelicTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }

    private static async Task TestTony(Player player, Action<bool, string> check)
    {
        foreach (int count in new[] { 0, 1, 4 })
        {
            foreach (var old in player.Relics.ToArray()) await RelicCmd.Remove(old);
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray(), showPreview: false);
            for (int i = 0; i < count; i++)
                await CardPileCmd.Add(player.RunState.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
            var originals = player.Deck.Cards.ToArray();
            var eternal = player.RunState.CreateCard<AscendersBane>(player);
            await CardPileCmd.Add(eternal, PileType.Deck, skipVisuals: true);
            var selected = originals.Take(2).ToArray();
            var relic = (TonysCharm)ModelDb.Relic<TonysCharm>().ToMutable();
            var candidates = UnifiedRouteCardPool.Get(player, true);
            var expectedRng = new Rng(player.PlayerRng.Rewards.ToSerializable());
            var expectedRewards = selected.Select(_ => expectedRng.NextItem(candidates)!.Id).ToArray();
            var selector = new TestCardSelector();
            selector.PrepareToSelect(selected);
            using (CardSelectCmd.UseSelector(selector)) await RelicCmd.Obtain(relic, player);
            var rewards = player.Deck.Cards.Except(originals).Where(card => card != eternal).ToArray();
            check(selected.All(card => !player.Deck.Cards.Contains(card)), "pickup removes selected originals");
            check(originals.Skip(2).All(player.Deck.Cards.Contains), "pickup preserves unselected originals");
            check(player.Deck.Cards.Contains(eternal), "unremovable curse retained");
            check(rewards.Select(card => card.Id).SequenceEqual(expectedRewards), "each removal draws once from the flat pool using native reward RNG");
            check(rewards.All(card => card.IsUpgraded && card.Rarity == CardRarity.Rare), "each replacement is upgraded and rare");
            check(player.Deck.Cards.Count == count + 1, "each permanent removal adds exactly one reward");
            await relic.AfterObtained();
            check(player.Deck.Cards.Count == count + 1, "pickup replay is idempotent");
            var restored = (TonysCharm)ModelDb.Relic<TonysCharm>().ToMutable();
            SavedProperties.From(relic)!.Fill(restored);
            restored.Owner = player;
            await restored.AfterObtained();
            check(restored.PickupEffectGranted && player.Deck.Cards.Count == count + 1, "saved receipt prevents a second picker");
            if (count == 4)
            {
                var before = player.Deck.Cards.ToHashSet();
                var expected = new Rng(player.PlayerRng.Rewards.ToSerializable()).NextItem(candidates)!.Id;
                await CardPileCmd.RemoveFromDeck(originals[2], showPreview: false);
                var added = player.Deck.Cards.Except(before).Single();
                check(added.Id == expected && added.IsUpgraded, "later permanent removal grants one new upgraded rare");
                check(player.Deck.Cards.Count == count + 1, "later removal keeps deck size");
            }
        }
        var branch = (WitheredTreeSoul)ModelDb.Relic<WitheredTreeSoul>().ToMutable();
        branch.Owner = player;
        var runCard = player.Deck.Cards.First();
        var rngBefore = player.RunState.Rng.CombatCardGeneration.ToSerializable();
        await branch.AfterCardExhausted(new BlockingPlayerChoiceContext(), runCard, false);
        check(player.RunState.Rng.CombatCardGeneration.ToSerializable().counter == rngBefore.counter,
            "branch does not roll outside combat");
    }

    private static async Task TestBranch(Player player, Action<bool, string> check)
    {
        var ctx = new CardEffectTestContext((CombatState)player.Creature.CombatState!, player);
        await ctx.PrepareSuite();
        await RelicCmd.Obtain(ModelDb.Relic<WitheredTreeSoul>().ToMutable(), player);
        var choice = new BlockingPlayerChoiceContext();
        var candidates = UnifiedRouteCardPool.Get(player).Select(card => card.Id).ToHashSet();
        foreach (int handCount in new[] { 0, 10 })
        {
            await ctx.Reset();
            await ctx.AddFillerCards(PileType.Hand, handCount);
            var source = await ctx.Add<MaidenDefend>(PileType.Draw);
            var deckBefore = player.Deck.Cards.ToArray();
            int start = CombatManager.Instance.History.Entries.Count();
            await CardCmd.Exhaust(choice, source);
            var generated = CombatManager.Instance.History.Entries.Skip(start).OfType<CardGeneratedEntry>().ToArray();
            check(generated.Length == 1, "one exhaust generates exactly one card and one generation history entry");
            var entry = generated.Single();
            check(entry.Creator == player && entry.Card.Owner == player, "native generation attributed to owner");
            check(candidates.Contains(entry.Card.Id) && !entry.Card.IsUpgraded, "generated card belongs to flat combat pool and is a fresh copy");
            check(entry.Card.Pile!.Type == (handCount == 10 ? PileType.Discard : PileType.Hand), "full hand uses native discard overflow");
            check(source.Pile!.Type == PileType.Exhaust, "source stays exhausted");
            check(player.Deck.Cards.SequenceEqual(deckBefore), "combat generation does not change permanent deck");
            var discard = await ctx.Add<MaidenDefend>(PileType.Draw);
            start = CombatManager.Instance.History.Entries.Count();
            await CardPileCmd.Add(discard, PileType.Discard, skipVisuals: true);
            check(!CombatManager.Instance.History.Entries.Skip(start).OfType<CardGeneratedEntry>().Any(), "ordinary pile movement does not trigger branch");
        }
    }
}
#endif
