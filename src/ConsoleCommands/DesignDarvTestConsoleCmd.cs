#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Events;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignDarvTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_darv";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 Darv/Compass tests; disposable run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || CombatManager.Instance.IsInProgress || issuingPlayer.RunState.Players.Count != 1
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_darv confirm outside combat in a disposable single-player Maiden run.");
        if (_running) return new CmdResult(false, "Darv test is already running.");
        return new CmdResult(Run(issuingPlayer), true, "Started destructive Darv tests; see [DS27DarvTest] log.");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 Darv assertion failed: " + name);
            checks++;
        }
        async Task<DarvAssistance> Start()
        {
            var instance = (DarvAssistance)ModelDb.Event<DarvAssistance>().ToMutable();
            await instance.BeginEvent(player, null, isPreFinished: false);
            return instance;
        }
        try
        {
            // The vanilla reward selector is a headless hook only in TestMode.
            // Scope it to this confirmed single-player test and always restore it.
            TestMode.IsOn = true;
            var run = (RunState)player.RunState;
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            var compass = (SoulCompass)ModelDb.Relic<SoulCompass>().ToMutable();
            compass.PickupRewardsGranted = true;
            await RelicCmd.Obtain(compass, player);
            for (int corruption = -5; corruption <= 5; corruption++)
            {
                CorruptionCmd.Set(run, corruption);
                RouteRewardProbabilityBonus bonus = RouteRewardProbabilityModifiers.GetTotal(player);
                decimal expected = corruption is > -3 and < 3 ? .20m : 0m;
                Check(bonus.Holy == expected && bonus.Corrupt == expected, "owned compass band " + corruption);
                DarvAssistance model = await Start();
                Check(model.CurrentOptions.Count == 3, "three initial options");
                Check(model.CurrentOptions[0].IsLocked == (corruption < 3), "dark lock " + corruption);
                Check(model.CurrentOptions[1].IsLocked == (corruption > -3), "holy lock " + corruption);
                Check(model.CurrentOptions[2].IsLocked == (Math.Abs(corruption) >= 3), "balance lock " + corruption);
            }
            await RelicCmd.Remove(compass);
            Check(RouteRewardProbabilityModifiers.GetTotal(player) == default, "removal removes probability bonus");

            foreach (RouteCardKind route in new[] { RouteCardKind.Holy, RouteCardKind.Corrupt })
            {
                var firstRng = new Rng(90227u);
                CardReward first = DarvAssistance.CreateInsightReward(player, route, firstRng);
                int ordinaryCounter = player.PlayerRng.Rewards.ToSerializable().counter;
                first.Populate();
                CardModel[] cards = first.Cards.ToArray();
                Check(cards.Length == 3 && cards.Select(card => card.Id).Distinct().Count() == 3, "three distinct insight options");
                Check(cards.All(card => card.Rarity == CardRarity.Rare && RouteCardQuery.Get(card) == route), "only promised rare route");
                Check(cards.All(card => !card.IsUpgraded), "no implicit upgrades");
                Check(player.PlayerRng.Rewards.ToSerializable().counter == ordinaryCounter, "event rng does not consume encounter reward rng");
                int counter = firstRng.ToSerializable().counter;
                first.Populate();
                Check(firstRng.ToSerializable().counter == counter, "populating twice does not reroll");
                CardReward second = DarvAssistance.CreateInsightReward(player, route, new Rng(90227u));
                second.Populate();
                Check(cards.Select(card => card.Id).SequenceEqual(second.Cards.Select(card => card.Id)), "same event seed same offers");
                first.OnSkipped();
                second.OnSkipped();

                CorruptionCmd.Set(run, route == RouteCardKind.Holy ? -3 : 3);
                DarvAssistance model = await Start();
                var option = model.CurrentOptions[route == RouteCardKind.Holy ? 1 : 0];
                var before = player.Deck.Cards.ToHashSet();
                var selector = new TestCardSelector();
                selector.PrepareToSelectCardRewardAtIndex(0);
                using (CardSelectCmd.UseSelector(selector)) await option.Chosen();
                CardModel[] gained = player.Deck.Cards.Except(before).ToArray();
                Check(gained.Length == 1 && gained[0].Rarity == CardRarity.Rare && RouteCardQuery.Get(gained[0]) == route, "insight actually adds one chosen card");
                Check(model.IsFinished, "insight reaches result page");
                int count = player.Deck.Cards.Count;
                await option.Chosen();
                Check(player.Deck.Cards.Count == count, "duplicate choice grants no second card");
            }

            CorruptionCmd.Set(run, 0);
            DarvAssistance balance = await Start();
            var balanceOption = balance.CurrentOptions[2];
            int deckBefore = player.Deck.Cards.Count;
            int offered = 0;
            var pickupSelector = new TestCardSelector();
            pickupSelector.PrepareToSelectCardReward((cards, _) =>
            {
                offered++;
                Check(cards.Count == 3, "each ordinary reward has three choices");
                Check(cards.All(card => card.Card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare), "ordinary rarity pool");
                return new CardRewardSelection { card = cards[0].Card };
            });
            using (CardSelectCmd.UseSelector(pickupSelector)) await balanceOption.Chosen();
            Check(offered == 3, "exactly three rewards, not event plus relic twice");
            Check(player.Deck.Cards.Count == deckBefore + 3, "one chosen card from each reward");
            SoulCompass obtained = player.Relics.OfType<SoulCompass>().Single();
            Check(obtained.PickupRewardsGranted && balance.IsFinished, "pickup guard and result page");
            await obtained.AfterObtained();
            await balanceOption.Chosen();
            Check(player.Deck.Cards.Count == deckBefore + 3, "repeat acquisition callback and option do not duplicate rewards");

            // Test the separate rare reward skip path, not a fake empty card list.
            CorruptionCmd.Set(run, -3);
            DarvAssistance skipped = await Start();
            var skipSelector = new TestCardSelector();
            skipSelector.PrepareToSelectCardReward((_, _) => new CardRewardSelection());
            int beforeSkip = player.Deck.Cards.Count;
            using (CardSelectCmd.UseSelector(skipSelector)) await skipped.CurrentOptions[1].Chosen();
            Check(skipped.IsFinished && player.Deck.Cards.Count == beforeSkip, "skip finishes event without adding a card");

            await RelicCmd.Remove(obtained);
            CorruptionCmd.Set(run, 0);
            DarvAssistance skipAll = await Start();
            int skippedOffers = 0;
            var skipAllSelector = new TestCardSelector();
            skipAllSelector.PrepareToSelectCardReward((_, _) =>
            {
                skippedOffers++;
                return new CardRewardSelection();
            });
            using (CardSelectCmd.UseSelector(skipAllSelector)) await skipAll.CurrentOptions[2].Chosen();
            Check(skippedOffers == 3 && player.Deck.Cards.Count == beforeSkip, "all three ordinary rewards can be skipped");
            Check(skipAll.IsFinished && player.Relics.OfType<SoulCompass>().Single().PickupRewardsGranted,
                "skipping rewards still completes one relic acquisition");
            MaidenSuccubusMod.Logger.Info($"[DS27DarvTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27DarvTest] FAIL " + ex);
            throw;
        }
        finally
        {
            TestMode.IsOn = previousTestMode;
            _running = false;
        }
    }
}
#endif
