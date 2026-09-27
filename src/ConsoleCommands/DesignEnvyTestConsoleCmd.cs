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
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignEnvyTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_envy";
    public override string Args => "confirm";
    public override string Description => "Destructive Envy tests; disposable single-player combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || combat.HittableEnemies.Count == 0 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_envy confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive tests started; see [DS27EnvyTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 envy: " + name);
            checks++;
        }
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        int Energy() => player.PlayerCombatState!.Energy;
        int Hand() => PileType.Hand.GetPile(player).Cards.Count;
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = player.RunState;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                foreach (var old in player.Relics.OfType<EnvyRouteRelic>().ToArray()) await RelicCmd.Remove(old);
                var relic = (EnvyRouteRelic)ModelDb.Relic<EnvyRouteRelic>().ToMutable();
                relic.Stage = stage;
                await RelicCmd.Obtain(relic, player);
                bool active = stage > 0;
                int draws = stage >= 2 ? 1 : 0;
                async Task Reset()
                {
                    await ctx.Reset();
                    await relic.BeforeCombatStart();
                    await ctx.AddFillerCards(PileType.Draw, 6, type: CardType.Skill);
                }

                await Reset();
                int before = Energy();
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0), "first actual debuff grants energy by stage");
                Check(Hand() == draws, "first actual debuff draws by stage");
                Check(relic.UsedThisWindow == active, "first application reserves receipt");
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                await PowerCmd.Apply<VulnerablePower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0) && Hand() == draws,
                    "stacking and different debuffs cannot repeat reward");
                var restored = (EnvyRouteRelic)ModelDb.Relic<EnvyRouteRelic>().ToMutable();
                SavedProperties.From(relic)!.Fill(restored);
                restored.Owner = player;
                var weak = ctx.PrimaryEnemy.Powers.OfType<WeakPower>().Single();
                await restored.AfterPowerAmountChanged(choice, weak, 1, ctx.Self, null);
                Check(restored.UsedThisWindow == active && Energy() == before + (active ? 1 : 0),
                    "native saved receipt prevents repeated reward");

                await relic.BeforeSideTurnStart(choice, CombatSide.Enemy, ctx.Enemies, combat);
                await relic.BeforeSideTurnStart(choice, CombatSide.Player, [foreign.Creature], combat);
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0), "foreign and enemy turn starts do not refresh");
                await relic.AfterSideTurnEnd(choice, CombatSide.Player, [ctx.Self]);
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0), "end of own turn does not open another window");
                await relic.BeforeSideTurnStart(choice, CombatSide.Player, [ctx.Self], combat);
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0) + (stage >= 3 ? 1 : 0)
                    && Hand() == draws + (stage >= 3 ? 1 : 0), "only awakened refreshes each own turn");

                await Reset();
                before = Energy();
                await PowerCmd.Apply<ArtifactPower>(choice, ctx.PrimaryEnemy, 1, ctx.PrimaryEnemy, null);
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(!relic.UsedThisWindow && Energy() == before && Hand() == 0, "artifact-blocked application gives no reward");
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0), "blocked attempt did not consume first use");

                await Reset();
                before = Energy();
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 2, ctx.PrimaryEnemy, null);
                weak = ctx.PrimaryEnemy.Powers.OfType<WeakPower>().Single();
                await relic.AfterPowerAmountChanged(choice, weak, 1, foreign.Creature, null);
                await relic.AfterPowerAmountChanged(choice, weak, 0, ctx.Self, null);
                await PowerCmd.ModifyAmount(choice, weak, -1, ctx.Self, null);
                await PowerCmd.Apply<StrengthPower>(choice, ctx.PrimaryEnemy, 2, ctx.Self, null);
                Check(!relic.UsedThisWindow && Energy() == before, "foreign caster, zero, cleanse and buff are excluded");
                await PowerCmd.Apply<StrengthPower>(choice, ctx.PrimaryEnemy, -1, ctx.Self, null);
                Check(ctx.PrimaryEnemy.Powers.OfType<StrengthPower>().Single().Amount == 1,
                    "strength reduction fixture remains positive overall");
                Check(Energy() == before + (active ? 1 : 0) && Hand() == draws,
                    "negative strength delta counts regardless of remaining buff total");

                await Reset();
                before = Energy();
                await PowerCmd.Apply<WeakPower>(choice, ctx.Self, 1, ctx.Self, null);
                Check(Energy() == before + (active ? 1 : 0) && Hand() == draws,
                    "own self-target harmful application is not excluded by side");
                await relic.AfterCombatEnd((CombatRoom)player.RunState.CurrentRoom!);
                Check(!relic.UsedThisWindow, "battle end clears receipt");
                relic.UsedThisWindow = true;
                await relic.BeforeCombatStart();
                Check(!relic.UsedThisWindow, "battle start clears stale receipt");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27EnvyTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27EnvyTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
