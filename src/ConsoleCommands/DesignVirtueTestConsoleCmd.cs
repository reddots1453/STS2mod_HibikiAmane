#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Opt-in destructive model/command tests, never runs during normal play.</summary>
public sealed class DesignVirtueTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_virtues";
    public override string Args => "confirm";
    public override string Description => "Destructive Benevolence/Diligence tests; disposable run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || CombatManager.Instance.IsInProgress
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_virtues confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(issuingPlayer), true, "Destructive test started; see [DS27VirtueTest].");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        var run = (RunState)player.RunState;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 virtue: " + name);
            checks++;
        }
        async Task Reset(int count)
        {
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray());
            for (int i = 0; i < count; i++)
                await CardPileCmd.Add(run.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
        }
        try
        {
            TestMode.IsOn = true;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                await Reset(4);
                var relic = (BenevolenceRouteRelic)ModelDb.Relic<BenevolenceRouteRelic>().ToMutable();
                relic.Stage = stage;
                await RelicCmd.Obtain(relic, player);
                int want = stage is 1 or 2 ? 2 : 0;
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == want, "benevolence pickup stage " + stage);
                Check(player.Deck.Cards.Count == 4, "benevolence never adds reward cards");
                await relic.AfterObtained();
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == want, "duplicate pickup has no second upgrade");
                var restored = (BenevolenceRouteRelic)ModelDb.Relic<BenevolenceRouteRelic>().ToMutable();
                SavedProperties.From(relic)!.Fill(restored);
                restored.Owner = player;
                await restored.AfterObtained();
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == want, "saved pickup receipt prevents replay");
                var added = run.CreateCard<MaidenDefend>(player);
                await CardPileCmd.Add(added, PileType.Deck, skipVisuals: true);
                int afterAdd = want + (stage is 3 or 4 ? 2 : 0);
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == afterAdd, "real permanent addition triggers awakened only");
                await relic.AfterCardChangedPiles(added, PileType.Deck, null);
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == afterAdd, "deck to deck is not addition");
            }
            foreach (int count in new[] { 0, 1 })
            {
                await Reset(count);
                var relic = (BenevolenceRouteRelic)ModelDb.Relic<BenevolenceRouteRelic>().ToMutable();
                relic.Stage = 1;
                await RelicCmd.Obtain(relic, player);
                Check(player.Deck.Cards.Count(card => card.IsUpgraded) == count, "insufficient upgrade targets handled");
            }

            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                await Reset(3);
                var relic = (DiligenceRouteRelic)ModelDb.Relic<DiligenceRouteRelic>().ToMutable();
                relic.Stage = stage;
                int selections = 0;
                var selector = new TestCardSelector();
                selector.PrepareToSelectCardReward((options, _) =>
                {
                    selections++;
                    Check(options.Count > 0, "real generated reward is populated");
                    foreach (var option in options)
                    {
                        CardModel card = option.Card;
                        if (stage >= 2) Check(card.IsUpgraded || !card.IsUpgradable, "all options upgraded before selection");
                        if (stage >= 3 && card.Enchantment is { } enchantment)
                        {
                            Check(enchantment.GetType().Assembly == typeof(EnchantmentModel).Assembly,
                                "generated enchantment is vanilla");
                            var range = VirtuePickupRules.EnchantmentRange(enchantment.GetType().Name);
                            Check(enchantment.Amount >= range.Min && enchantment.Amount <= range.Max, "generated amount in correct range");
                        }
                    }
                    return default; // No card or alternative: skip unrelated pickup-card selectors.
                });
                using (CardSelectCmd.UseSelector(selector)) await RelicCmd.Obtain(relic, player);
                Check(selections == (stage == 0 ? 0 : stage <= 2 ? 2 : 3), "actual OfferCustom reward count");
                using (CardSelectCmd.UseSelector(selector)) await relic.AfterObtained();
                Check(selections == (stage == 0 ? 0 : stage <= 2 ? 2 : 3), "duplicate diligence pickup no second reward set");
                Check(player.Deck.Cards.Count == 3 && player.Deck.Cards.All(card => !card.IsUpgraded),
                    "diligence does not upgrade old deck and skipped rewards add nothing");
            }

            await Reset(0);
            var probe = (DiligenceRouteRelic)ModelDb.Relic<DiligenceRouteRelic>().ToMutable();
            probe.Owner = player; probe.Stage = 4;
            var attack = run.CreateCard<MaidenStrike>(player);
            var skill = run.CreateCard<MaidenDefend>(player);
            var candidates = DiligenceRouteRelic.EnchantmentOptions(attack);
            foreach (Type expected in new[] { typeof(Sharp), typeof(Adroit), typeof(Momentum), typeof(Vigorous), typeof(Swift), typeof(Sown) })
                Check(candidates.Any(enchantment => enchantment.GetType() == expected), "full vanilla pool includes " + expected.Name);
            Check(candidates.All(enchantment => enchantment is not DeprecatedEnchantment), "deprecated placeholder excluded");
            Check(!candidates.Any(enchantment => enchantment is Nimble), "Nimble requires actual block");
            Check(DiligenceRouteRelic.EnchantmentOptions(skill).Any(enchantment => enchantment is Nimble), "Nimble legal on block card");
            probe.PrepareRewardOptions([attack, skill], VirtuePickupRules.Diligence(4));
            Check(attack.IsUpgraded && skill.IsUpgraded && attack.Enchantment != null && skill.Enchantment != null,
                "known reward cards are upgraded and enchanted");
            var original = attack.Enchantment;
            var counter = run.Rng.Niche.ToSerializable().counter;
            probe.PrepareRewardOptions([attack, skill], VirtuePickupRules.Diligence(4));
            Check(ReferenceEquals(attack.Enchantment, original) && run.Rng.Niche.ToSerializable().counter == counter,
                "repeat preparation does not stack or consume RNG");
            var curse = run.CreateCard<Injury>(player);
            Check(DiligenceRouteRelic.EnchantmentOptions(curse).Count == 0, "no legal enchantment is safe");
            probe.PrepareRewardOptions([curse], VirtuePickupRules.Diligence(4));
            Check(curse.Enchantment == null, "invalid enchantment not forced onto curse");
            MaidenSuccubusMod.Logger.Info($"[DS27VirtueTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27VirtueTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
