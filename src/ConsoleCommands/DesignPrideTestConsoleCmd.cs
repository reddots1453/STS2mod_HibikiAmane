#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignPrideTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_pride";
    public override string Args => "confirm";
    public override string Description => "Destructive Pride/SelfImportant tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || combat.HittableEnemies.Count == 0 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_pride confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive tests started; see [DS27PrideTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 pride: " + name);
            checks++;
        }
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        int Amount<T>() where T : PowerModel => ctx.Self.Powers.OfType<T>().Sum(power => power.Amount);
        async Task Hit(int damage) => await CreatureCmd.Damage(choice, ctx.Self, damage, ValueProp.Move, ctx.PrimaryEnemy);
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                foreach (var old in player.Relics.OfType<PrideRouteRelic>().ToArray()) await RelicCmd.Remove(old);
                var relic = (PrideRouteRelic)ModelDb.Relic<PrideRouteRelic>().ToMutable();
                relic.Stage = stage;
                await RelicCmd.Obtain(relic, player);
                int expected = Math.Min(stage, 3);
                await ctx.Reset();
                await relic.BeforeCombatStart();
                Check(Amount<StrengthPower>() == expected && Amount<SelfImportantPower>() == expected,
                    "each stage grants exact strength and self-important layers");
                if (expected > 0)
                {
                    var power = ctx.Self.Powers.OfType<SelfImportantPower>().Single();
                    Check(power.TypeForCurrentAmount == PowerType.Debuff && power.StackType == PowerStackType.Counter,
                        "self-important is a removable counter debuff");
                }
                await CreatureCmd.GainBlock(ctx.Self, 6, ValueProp.Unpowered, null);
                int hp = ctx.Self.CurrentHp;
                await Hit(3);
                await Hit(0);
                Check(ctx.Self.CurrentHp == hp && Amount<SelfImportantPower>() == expected && Amount<StrengthPower>() == expected,
                    "fully blocked and zero damage do not spend layers");
                await CreatureCmd.Damage(choice, ctx.PrimaryEnemy, 1, ValueProp.Unpowered, ctx.Self);
                Check(Amount<SelfImportantPower>() == expected && Amount<StrengthPower>() == expected,
                    "damage to someone else does not spend own layer");
                await Hit(4); // Three remaining block: exactly one unblocked damage.
                Check(ctx.Self.CurrentHp == hp - 1 && Amount<SelfImportantPower>() == Math.Max(0, expected - 1)
                    && Amount<StrengthPower>() == Math.Max(0, expected - 1), "partial block spends one layer not damage count");
                for (int hit = 2; hit <= expected + 2; hit++)
                {
                    await Hit(2);
                    Check(Amount<SelfImportantPower>() == Math.Max(0, expected - hit)
                        && Amount<StrengthPower>() == Math.Max(0, expected - hit), "each hit spends one layer until exhausted");
                }
                Check(!ctx.Self.Powers.OfType<SelfImportantPower>().Any(), "last layer removes power and cannot recur");

                await ctx.Reset();
                await relic.BeforeCombatStart();
                await ctx.ApplyPower<PurificationPower>(ctx.Self, 1);
                var purification = ctx.Self.Powers.OfType<PurificationPower>().Single();
                Check(purification.GetCandidates().OfType<SelfImportantPower>().Any() == (expected > 0),
                    "purification includes self-important as eligible debuff");
                await purification.AfterPlayerTurnStart(choice, player);
                Check(Amount<SelfImportantPower>() == Math.Max(0, expected - 1) && Amount<StrengthPower>() == expected,
                    "purification removes layer without losing strength");
                await Hit(1);
                Check(Amount<SelfImportantPower>() == Math.Max(0, expected - 2)
                    && Amount<StrengthPower>() == expected - (expected > 1 ? 1 : 0),
                    "later damage consumes only remaining uncleansed layers");

                var restored = (PrideRouteRelic)ModelDb.Relic<PrideRouteRelic>().ToMutable();
                SavedProperties.From(relic)!.Fill(restored);
                restored.Owner = player;
                await ctx.Reset();
                await restored.BeforeCombatStart();
                Check(restored.Stage == stage && Amount<SelfImportantPower>() == expected && Amount<StrengthPower>() == expected,
                    "saved stage restores correct new-combat grant");
                await ctx.Reset();
                await relic.BeforeCombatStart();
                Check(Amount<SelfImportantPower>() == expected && Amount<StrengthPower>() == expected,
                    "fresh combat recreates full grant without previous damage or cleanse carryover");
            }

            await ctx.Reset();
            await ctx.ApplyPower<SelfImportantPower>(ctx.Self, 1);
            int initialHp = ctx.Self.CurrentHp;
            await CreatureCmd.SetCurrentHp(ctx.Self, initialHp - 1);
            Check(Amount<SelfImportantPower>() == 1 && Amount<StrengthPower>() == 0,
                "direct HP assignment is not a damage notification");
            await CreatureCmd.Damage(choice, ctx.Self, 1, ValueProp.Unblockable | ValueProp.Unpowered, ctx.Self);
            Check(Amount<SelfImportantPower>() == 0 && Amount<StrengthPower>() == -1,
                "unblocked nonattack damage consumes final layer and can lower strength below zero");
            MaidenSuccubusMod.Logger.Info($"[DS27PrideTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27PrideTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
