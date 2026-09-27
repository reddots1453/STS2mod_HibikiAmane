#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignDesireRelicTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_resource_relics";
    public override string Args => "confirm";
    public override string Description => "Destructive Lust/Chastity tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.CombatState is not CombatState combat || combat.HittableEnemies.Count == 0
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_resource_relics confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(player, combat), true, "Destructive tests started; see [DS27ResourceRelicTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 resource relic: " + name);
            checks++;
        }
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        int Hand() => PileType.Hand.GetPile(player).Cards.Count;
        int Energy() => player.PlayerCombatState!.Energy;
        async Task ClearRelics()
        {
            foreach (var relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
        }
        SecondaryResourceChangeContext Change(Player owner, SecondaryResourceChangeReason reason, int delta = 1) =>
            new(combat, owner, DesireResource.Definition, 0, delta, delta, reason, null);
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = player.RunState;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                await ClearRelics();
                await ctx.Reset();
                var lust = (LustRouteRelic)ModelDb.Relic<LustRouteRelic>().ToMutable();
                lust.Stage = stage;
                await RelicCmd.Obtain(lust, player);
                await lust.BeforeCombatStart();
                var cards = new List<CardModel>();
                for (int i = 0; i < 5; i++)
                {
                    var card = await ctx.Add<MaidenStrike>(PileType.Draw, upgraded: i % 2 == 0);
                    card.SecondaryCosts().Set(DesireResource.Id, 2);
                    card.SetStarCostUntilPlayed(2);
                    cards.Add(card);
                }
                await Data.Desire.Set(player, 2);
                await lust.AfterSecondaryResourceChanged(Change(player, SecondaryResourceChangeReason.Reset));
                await lust.AfterSecondaryResourceChanged(Change(foreign, SecondaryResourceChangeReason.Gain));
                await lust.AfterSecondaryResourceChanged(Change(player, SecondaryResourceChangeReason.Gain, 0));
                await Data.Desire.Modify(player, -1);
                Check(Hand() == 0 && !lust.UsedThisCombat, "set reset loss equal refresh and foreign owner cannot trigger");
                await Data.Desire.Modify(player, 1);
                int draw = stage == 0 ? 0 : stage == 1 ? 2 : 3;
                Check(Hand() == draw && lust.UsedThisCombat == (stage > 0), "actual gain draws two three three by stage");
                CardModel[] drawn = PileType.Hand.GetPile(player).Cards.ToArray();
                foreach (CardModel card in drawn)
                {
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == (stage >= 3 ? 0 : 1), "fixed energy cost by stage");
                    Check(card.SecondaryCosts().Get(DesireResource.Id).Amount == (stage >= 3 ? 0 : 2), "fixed secondary cost by stage");
                    Check(card.GetStarCostWithModifiers() == (stage >= 3 ? 0 : 2), "fixed star cost by stage");
                    bool upgraded = card.IsUpgraded;
                    card.EndOfTurnCleanup();
                    await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
                    await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == (stage >= 3 ? 0 : 1)
                        && card.SecondaryCosts().Get(DesireResource.Id).Amount == (stage >= 3 ? 0 : 2)
                        && card.GetStarCostWithModifiers() == (stage >= 3 ? 0 : 2),
                        "cost duration survives turn cleanup and discard redraw");
                    Check(card.IsUpgraded == upgraded && cards.Contains(card), "actual drawn instance and upgrade retained");
                }
                Check(PileType.Draw.GetPile(player).Cards.All(card => card.EnergyCost.GetWithModifiers(CostModifiers.All) == 1
                    && card.SecondaryCosts().Get(DesireResource.Id).Amount == 2), "undrawn cards not discounted");
                await Data.Desire.Modify(player, 1);
                Check(Hand() == draw, "second gain cannot repeat draw");
                var saved = (LustRouteRelic)ModelDb.Relic<LustRouteRelic>().ToMutable();
                SavedProperties.From(lust)!.Fill(saved);
                saved.Owner = player;
                await saved.AfterSecondaryResourceChanged(Change(player, SecondaryResourceChangeReason.Gain));
                Check(Hand() == draw && saved.UsedThisCombat == (stage > 0), "saved receipt prevents repeated draw");
                if (stage >= 3)
                {
                    CardModel card = drawn[0];
                    int energy = Energy(), resource = Data.Desire.Get(player);
                    await ctx.Play(card, ctx.PrimaryEnemy);
                    Check(Energy() == energy && Data.Desire.Get(player) == resource, "actual play spends neither fixed resource");
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 1
                        && card.SecondaryCosts().Get(DesireResource.Id).Amount == 2
                        && card.GetStarCostWithModifiers() == 0, "playing consumes free layers including temporary stars");
                    await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
                    await ctx.Play(card, ctx.PrimaryEnemy);
                    Check(Energy() == energy - 1 && Data.Desire.Get(player) == resource - 2, "second play pays original costs");
                }

                await ctx.Reset();
                await lust.BeforeCombatStart();
                await ctx.AddFillerCards(PileType.Draw, 5);
                await ctx.AddFillerCards(PileType.Hand, CardPile.MaxCardsInHand);
                await Data.Desire.Modify(player, 1);
                Check(Hand() == CardPile.MaxCardsInHand && lust.UsedThisCombat == (stage > 0), "full hand consumes first gain without extra cards");
                await ctx.Reset();
                await lust.BeforeCombatStart();
                await ctx.AddFillerCards(PileType.Draw, 5);
                await PowerCmd.Apply<NoDrawPower>(choice, ctx.Self, 1, ctx.Self, null);
                await Data.Desire.Modify(player, 1);
                Check(Hand() == 0 && lust.UsedThisCombat == (stage > 0), "no draw respected without deferring first trigger");
                await ctx.Reset();
                await lust.BeforeCombatStart();
                await Data.Desire.Modify(player, 1);
                Check(Hand() == 0 && lust.UsedThisCombat == (stage > 0), "empty piles safe and consume first trigger");
            }

            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                await ClearRelics();
                await ctx.Reset();
                var chastity = (ChastityRouteRelic)ModelDb.Relic<ChastityRouteRelic>().ToMutable();
                chastity.Stage = stage;
                await RelicCmd.Obtain(chastity, player);
                var lust = (LustRouteRelic)ModelDb.Relic<LustRouteRelic>().ToMutable();
                lust.Stage = 1;
                await RelicCmd.Obtain(lust, player);
                await lust.BeforeCombatStart();
                await ctx.AddFillerCards(PileType.Draw, 5);
                await chastity.BeforeCombatStart();
                int layers = stage == 0 ? 0 : Math.Min(stage, 2);
                Check((ctx.Self.GetPower<PreventNextDesireGainPower>()?.Amount ?? 0) == layers, "chastity grants zero one two two layers");
                await Data.Desire.Modify(player, 0);
                Check((ctx.Self.GetPower<PreventNextDesireGainPower>()?.Amount ?? 0) == layers, "zero gain does not spend protection");
                for (int i = 0; i < layers; i++)
                {
                    await Data.Desire.Modify(player, 4);
                    Check(Data.Desire.Get(player) == 0, "one layer blocks entire positive gain");
                    Check((ctx.Self.GetPower<PreventNextDesireGainPower>()?.Amount ?? 0) == layers - i - 1, "exactly one protection layer consumed");
                    Check(!lust.UsedThisCombat && Hand() == 0, "prevented increase does not trigger lust");
                }
                await Data.Desire.Modify(player, 1);
                Check(Data.Desire.Get(player) == 1 && Hand() == 2, "unprotected gain finally triggers actual draw");
                foreach (int amount in new[] { 0, 1, 2, 3, 7 })
                {
                    await Data.Desire.Set(player, amount);
                    int before = Energy();
                    await chastity.AfterPlayerTurnStart(choice, foreign);
                    Check(Energy() == before, "foreign turn never grants energy");
                    await chastity.AfterPlayerTurnStart(choice, player);
                    Check(Energy() == before + (stage >= 3 && amount <= 2 ? 1 : 0), "own turn energy includes threshold two only when awakened");
                }
            }
            MaidenSuccubusMod.Logger.Info($"[DS27ResourceRelicTest] PASS {checks} assertions; X/Y free semantics still awaiting Q17.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27ResourceRelicTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
