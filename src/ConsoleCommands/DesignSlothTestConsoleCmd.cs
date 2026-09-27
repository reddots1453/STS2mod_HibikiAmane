#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignSlothTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_sloth";
    public override string Args => "confirm";
    public override string Description => "Destructive Sloth tests; disposable single-player combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || combat.HittableEnemies.Count == 0 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_sloth confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive tests started; see [DS27SlothTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 sloth: " + name);
            checks++;
        }
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        async Task Pay(CardModel card)
        {
            (int energy, int stars) = await card.SpendResources();
            await card.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
                new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars },
                skipCardPileVisuals: true);
        }
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = player.RunState;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                foreach (var old in player.Relics.OfType<SlothRouteRelic>().ToArray()) await RelicCmd.Remove(old);
                var relic = (SlothRouteRelic)ModelDb.Relic<SlothRouteRelic>().ToMutable();
                relic.Stage = stage;
                await RelicCmd.Obtain(relic, player);
                foreach (int spent in new[] { 0, 1, 2, 3 })
                {
                    await ctx.Reset();
                    await relic.BeforeCombatStart();
                    await relic.BeforeSideTurnStart(choice, CombatSide.Player, [ctx.Self], combat);
                    int energyBefore = player.PlayerCombatState!.Energy;
                    await relic.AfterEnergyReset(player);
                    Check(player.PlayerCombatState.Energy == energyBefore, "first turn has no bonus");
                    for (int i = 0; i < spent; i++) await Pay(await ctx.Add<StrikeIronclad>(PileType.Hand));
                    Check(relic.EnergySpentThisTurn == (stage > 0 ? spent : 0), "native payment counts actual energy");
                    await ctx.Play(await ctx.Add<StrikeIronclad>(PileType.Draw), ctx.PrimaryEnemy);
                    Check(relic.EnergySpentThisTurn == (stage > 0 ? spent : 0), "free auto-play ignores nominal cost");
                    await relic.AfterEnergySpent(player.RunState.CreateCard<StrikeIronclad>(foreign), 10);
                    await relic.BeforeSideTurnStart(choice, CombatSide.Player, [foreign.Creature], combat);
                    Check(relic.EnergySpentThisTurn == (stage > 0 ? spent : 0), "foreign payment and turn start isolated");
                    await relic.AfterSideTurnEnd(choice, CombatSide.Player, [foreign.Creature]);
                    Check(!relic.TriggeredForNextTurn, "foreign end does not award");
                    int blockBefore = ctx.Self.Block;
                    await relic.AfterSideTurnEnd(choice, CombatSide.Player, [ctx.Self]);
                    bool eligible = stage > 0 && spent <= 2;
                    Check(ctx.Self.Block - blockBefore == (eligible && stage >= 3 ? 12 : 0), "end-of-turn block exact");
                    await relic.AfterSideTurnEnd(choice, CombatSide.Player, [ctx.Self]);
                    Check(ctx.Self.Block - blockBefore == (eligible && stage >= 3 ? 12 : 0), "duplicate end cannot double block");

                    var restored = (SlothRouteRelic)ModelDb.Relic<SlothRouteRelic>().ToMutable();
                    SavedProperties.From(relic)!.Fill(restored);
                    restored.Owner = player;
                    Check(restored.EnergySpentThisTurn == relic.EnergySpentThisTurn
                        && restored.TriggeredForNextTurn == eligible && restored.TurnEndResolved == (stage > 0),
                        "native saved properties preserve integer ledger and receipts");
                    await restored.AfterSideTurnEnd(choice, CombatSide.Player, [ctx.Self]);
                    Check(ctx.Self.Block - blockBefore == (eligible && stage >= 3 ? 12 : 0), "saved end receipt prevents duplicate block");
                    await relic.AfterEnergyReset(foreign);
                    Check(relic.TriggeredForNextTurn == eligible, "foreign energy reset preserves pending bonus");
                    await relic.BeforeSideTurnStart(choice, CombatSide.Enemy, ctx.Enemies, combat);
                    Check(relic.TriggeredForNextTurn == eligible, "enemy start preserves pending bonus");
                    int originalRound = combat.RoundNumber;
                    try
                    {
                        combat.RoundNumber = 1; // Synthetic extra-turn hook sequence, not a full native turn advance.
                        await relic.BeforeSideTurnStart(choice, CombatSide.Player, [ctx.Self], combat);
                        int before = player.PlayerCombatState.Energy;
                        await relic.AfterEnergyReset(player);
                        Check(player.PlayerCombatState.Energy - before == (eligible ? Math.Min(stage, 3) : 0),
                            "same-round next own turn receives bonus");
                        await relic.AfterEnergyReset(player);
                        Check(player.PlayerCombatState.Energy - before == (eligible ? Math.Min(stage, 3) : 0),
                            "duplicate reset cannot award twice");
                    }
                    finally { combat.RoundNumber = originalRound; }
                }

                await ctx.Reset();
                await relic.BeforeCombatStart();
                var replay = await ctx.Add<StrikeIronclad>(PileType.Hand);
                CardCmd.Enchant<Glam>(replay, 1);
                await Pay(replay);
                Check(relic.EnergySpentThisTurn == (stage > 0 ? 1 : 0), "paid replay counts one resource payment");
                var discounted = await ctx.Add<StrikeIronclad>(PileType.Hand);
                discounted.EnergyCost.SetThisCombat(0);
                await Pay(discounted);
                Check(relic.EnergySpentThisTurn == (stage > 0 ? 1 : 0), "zero-cost manual play adds no spending");
                await PlayerCmd.SetEnergy(2, player);
                await Pay(await ctx.Add<Whirlwind>(PileType.Hand));
                Check(relic.EnergySpentThisTurn == (stage > 0 ? 3 : 0), "X costs count actual paid two");

                relic.EnergySpentThisTurn = 2;
                relic.TriggeredForNextTurn = true;
                relic.TurnEndResolved = true;
                await relic.AfterCombatEnd((CombatRoom)player.RunState.CurrentRoom!);
                Check(relic.EnergySpentThisTurn == 0 && !relic.TriggeredForNextTurn && !relic.TurnEndResolved,
                    "battle end clears all state");
                relic.EnergySpentThisTurn = 99;
                relic.TriggeredForNextTurn = true;
                await relic.BeforeCombatStart();
                Check(relic.EnergySpentThisTurn == 0 && !relic.TriggeredForNextTurn, "battle start discards stale rewards");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27SlothTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27SlothTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
