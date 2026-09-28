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
                int countBefore = player.Deck.Cards.Count;
                var relic = (TemperanceRouteRelic)ModelDb.Relic<TemperanceRouteRelic>().ToMutable();
                relic.Stage = stage; relic.Owner = player;
                await relic.AfterObtained();
                var added = player.Deck.Cards.Where(card => !permanent.ContainsKey(card)).ToArray();
                try
                {
                    Check(added.Length == (stage == 0 ? 0 : 1), "one permanent pickup reward");
                    if (stage > 0)
                    {
                        Check(added[0].GetType() == (stage <= 2 ? typeof(TemperanceSignet) : typeof(TemperanceCirclet)),
                            "literal stage reward identity");
                        Check(!added[0].IsUpgraded && added[0].Rarity == CardRarity.Ancient, "unupgraded ancient reward");
                    }
                    await relic.AfterObtained();
                    await relic.BeforeCombatStart();
                    Check(player.Deck.Cards.Count == countBefore + added.Length, "repeat pickup and combat do not duplicate");
                    var restored = (TemperanceRouteRelic)RelicModel.FromSerializable(relic.ToSerializable());
                    restored.Owner = player;
                    Check(restored.Stage == stage && restored.PickupEffectGranted == (stage > 0), "pickup state round trip");
                    await restored.AfterObtained();
                    await restored.BeforeCombatStart();
                    Check(player.Deck.Cards.Count == countBefore + added.Length, "saved pickup does not reissue");
                }
                finally { if (added.Length > 0) await CardPileCmd.RemoveFromDeck(added, showPreview: false); }
            }
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
