#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Destructive, opt-in real selector/card-cost probes in a disposable combat.</summary>
public sealed class DesignCombatVirtueTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_combat_virtues";
    public override string Args => "confirm";
    public override string Description => "Destructive Temperance/Patience tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_combat_virtues confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive tests started; see [DS27CombatVirtueTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool priorTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("DS27 combat virtues: " + message);
            checks++;
        }
        async Task ClearCards()
        {
            foreach (CardPile pile in player.Piles.Where(pile => pile.IsCombatPile))
                await CardPileCmd.RemoveFromCombat(pile.Cards.ToArray());
        }
        async Task<CardModel> AddStrike(PileType pile)
        {
            CardModel card = combat.CreateCard<MaidenStrike>(player);
            await CardPileCmd.Add(card, pile, skipVisuals: true);
            return card;
        }
        var permanent = player.Deck.Cards.ToDictionary(card => card, card => card.Keywords.ToArray());
        try
        {
            TestMode.IsOn = true;
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                int max = stage == 0 ? 0 : stage == 1 ? 2 : 3;
                var prefs = TemperanceRouteRelic.SelectionPrefs(stage);
                Check(prefs.MinSelect == 0 && prefs.MaxSelect == max, "literal optional selection range");
                if (stage > 0) Check(prefs.RequireManualConfirmation, "optional selection not auto-forced");
                foreach (int selectedCount in new[] { 0, 1, max }.Distinct().Where(count => count <= max))
                {
                    await ClearCards();
                    List<CardModel> draw = [];
                    for (int i = 0; i < 4; i++) draw.Add(await AddStrike(PileType.Draw));
                    var hand = await AddStrike(PileType.Hand);
                    var discard = await AddStrike(PileType.Discard);
                    var relic = (TemperanceRouteRelic)ModelDb.Relic<TemperanceRouteRelic>().ToMutable();
                    relic.Stage = stage; relic.Owner = player;
                    var selector = new TestCardSelector();
                    selector.PrepareToSelect(draw.Take(selectedCount));
                    using (CardSelectCmd.UseSelector(selector)) await relic.BeforeCombatStart();
                    for (int i = 0; i < draw.Count; i++)
                    {
                        Check(draw[i].Keywords.Contains(CardKeyword.Exhaust) == (i < selectedCount), "only selected instance gains Exhaust");
                        Check(draw[i].Pile?.Type == PileType.Draw, "adding keyword does not move/exhaust card now");
                        Check(draw[i].Enchantment == null, "no removed Swift enchantment");
                    }
                    Check(!hand.Keywords.Contains(CardKeyword.Exhaust) && !discard.Keywords.Contains(CardKeyword.Exhaust),
                        "hand and discard not selected");
                }
            }
            await ClearCards();
            var temperance = (TemperanceRouteRelic)ModelDb.Relic<TemperanceRouteRelic>().ToMutable();
            temperance.Stage = 4; temperance.Owner = player;
            await temperance.BeforeCombatStart();
            Check(!PileType.Draw.GetPile(player).Cards.Any(), "empty draw safe without selector");
            var moved = await AddStrike(PileType.Draw);
            var delayed = new TestCardSelector();
            var selection = delayed.SetupForAsyncCardSelection();
            using (CardSelectCmd.UseSelector(delayed))
            {
                Task pending = temperance.BeforeCombatStart();
                await CardPileCmd.Add(moved, PileType.Hand, skipVisuals: true);
                selection.SetResult([moved]);
                await pending;
            }
            Check(!moved.Keywords.Contains(CardKeyword.Exhaust), "stale selection does not affect moved card");

            await ClearCards();
            var enchanted = await AddStrike(PileType.Draw);
            var originalEnchantment = CardCmd.Enchant(ModelDb.Enchantment<Sharp>().ToMutable(), enchanted, 2);
            var keepEnchant = new TestCardSelector();
            keepEnchant.PrepareToSelect([enchanted]);
            using (CardSelectCmd.UseSelector(keepEnchant)) await temperance.BeforeCombatStart();
            Check(enchanted.Keywords.Contains(CardKeyword.Exhaust) && ReferenceEquals(enchanted.Enchantment, originalEnchantment),
                "existing enchantment preserved alongside Exhaust");

            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                await ClearCards();
                var patience = (PatienceRouteRelic)ModelDb.Relic<PatienceRouteRelic>().ToMutable();
                patience.Stage = stage; patience.Owner = player;
                await patience.BeforeCombatStart();
                Check(PileType.Hand.GetPile(player).Cards.Count == (stage is 1 or 2 ? 1 : 0), "actual combat-start generation");
                await patience.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), player);
                Check(PileType.Hand.GetPile(player).Cards.Count == (stage == 0 ? 0 : 1), "actual first turn no double generation");
                await patience.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), player);
                Check(PileType.Hand.GetPile(player).Cards.Count == (stage == 0 ? 0 : stage >= 3 ? 2 : 1), "actual second-turn count");
                foreach (CardModel card in PileType.Hand.GetPile(player).Cards)
                {
                    Check(card.Pool is MSHolyCardPool && card.CanBeGeneratedInCombat, "legal holy generation");
                    Check(!card.IsUpgraded, "patience never upgrades generated card");
                    int original = card.EnergyCost.GetWithModifiers(CostModifiers.None);
                    int want = card.EnergyCost.CostsX || stage == 1 ? original : Math.Max(0, original - 1);
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == want, "relative discount applied");
                    card.EnergyCost.EndOfTurnCleanup();
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == want, "discount survives turn end");
                    card.EnergyCost.AfterCardPlayedCleanup();
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == original, "discount expires after played");
                }
            }
            foreach (int cost in new[] { 0, 1, 2, 4 })
            {
                var card = combat.CreateCard<MaidenStrike>(player);
                card.EnergyCost.SetCustomBaseCost(cost);
                PatienceRouteRelic.PrepareGeneratedCard(card, 2);
                Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == Math.Max(0, cost - 1), "zero/one/multi energy discount");
                var clone = combat.CloneCard(card);
                Check(clone.EnergyCost.GetWithModifiers(CostModifiers.Local) == Math.Max(0, cost - 1), "clone inherits unspent discount");
                clone.EnergyCost.AfterCardPlayedCleanup();
                Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == Math.Max(0, cost - 1), "clone cleanup independent");
                card.EnergyCost.EndOfTurnCleanup();
                Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == Math.Max(0, cost - 1), "known cost persists across turn");
                card.EnergyCost.AfterCardPlayedCleanup();
                Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == cost, "known cost restored");
            }
            var x = combat.CreateCard<Whirlwind>(player);
            int xSpend = x.EnergyCost.GetAmountToSpend();
            PatienceRouteRelic.PrepareGeneratedCard(x, 4);
            Check(x.EnergyCost.CostsX && x.EnergyCost.GetAmountToSpend() == xSpend, "native X energy semantics unchanged");
            var unplayable = combat.CreateCard<Injury>(player);
            int negative = unplayable.EnergyCost.GetWithModifiers(CostModifiers.Local);
            PatienceRouteRelic.PrepareGeneratedCard(unplayable, 4);
            Check(unplayable.EnergyCost.GetWithModifiers(CostModifiers.Local) == negative, "unplayable cost not made playable");

            var combined = combat.CreateCard<MaidenStrike>(player);
            combined.EnergyCost.SetCustomBaseCost(3);
            combined.EnergyCost.AddThisCombat(-1);
            PatienceRouteRelic.PrepareGeneratedCard(combined, 4);
            Check(combined.EnergyCost.GetWithModifiers(CostModifiers.Local) == 1, "relative discount stacks with other local reduction");
            combined.EnergyCost.AfterCardPlayedCleanup();
            Check(combined.EnergyCost.GetWithModifiers(CostModifiers.Local) == 2, "played cleanup preserves other combat modifier");

            await ClearCards();
            for (int i = 0; i < CardPile.MaxCardsInHand; i++) await AddStrike(PileType.Hand);
            var overflowRelic = (PatienceRouteRelic)ModelDb.Relic<PatienceRouteRelic>().ToMutable();
            overflowRelic.Owner = player; overflowRelic.Stage = 2;
            await overflowRelic.BeforeCombatStart();
            Check(PileType.Hand.GetPile(player).Cards.Count == CardPile.MaxCardsInHand
                && PileType.Discard.GetPile(player).Cards.Count == 1, "native full-hand fallback to discard");
            var overflow = PileType.Discard.GetPile(player).Cards.Single();
            Check(overflow.Pool is MSHolyCardPool && !overflow.IsUpgraded && overflow.EnergyCost.HasLocalModifiers,
                "overflow retains holy identity and unspent discount");
            Check(player.Deck.Cards.Count == permanent.Count && permanent.All(pair => pair.Key.Keywords.SequenceEqual(pair.Value)),
                "permanent deck untouched");
            MaidenSuccubusMod.Logger.Info($"[DS27CombatVirtueTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27CombatVirtueTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = priorTestMode; _running = false; }
    }
}
#endif
