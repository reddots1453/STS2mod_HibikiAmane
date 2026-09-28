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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Debugging.CardEffects;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignForgottenSoulTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_forgotten_soul";
    public override string Args => "confirm";
    public override string Description => "Destructive Forgotten Soul tests; disposable combat with two enemies only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.CombatState is not CombatState combat || combat.HittableEnemies.Count < 2
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_forgotten_soul confirm in a disposable single-player Maiden combat with two enemies.");
        return new CmdResult(Run(player, combat), true, "Destructive tests started; see [DS27ForgottenSoulTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks++;
        }
        string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            var run = (RunState)player.RunState;
            var canonical = ModelDb.Relic<ForgottenSoul>();
            string originalText = canonical.DynamicDescription.GetFormattedText();
            ForgottenSoul soul = await RelicCmd.Obtain<ForgottenSoul>(player);
            var id = soul.Id;
            int floor = soul.FloorAddedToDeck;
            var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = run;
            var remote = Player.CreateForNewRun<MaidenSuccubusCharacter>(player.UnlockState, player.NetId + 2000);
            remote.RunState = run;
            var remoteSoul = (ForgottenSoul)canonical.ToMutable();
            remoteSoul.Owner = remote;
            var foreignSoul = (ForgottenSoul)canonical.ToMutable();
            foreignSoul.Owner = foreign;
            var ownerless = canonical.ToMutable();
            var ashes = ModelDb.Relic<CharonsAshes>().ToMutable();
            ashes.Owner = player;

            // Full range, then both directions across the boundary on the SAME instance.
            foreach (int corruption in Enumerable.Range(-5, 11).Concat(new[] { 3, 4, 3, 5, -5 }))
            {
                await ctx.Reset();
                CorruptionCmd.Set(run, corruption);
                int expected = corruption >= 4 ? 2 : 1;
                string text = expected == 2
                    ? "每当你消耗1张牌，对随机敌人造成2点伤害。堕落值＜4：变奏。"
                    : "每当你消耗1张牌，对随机敌人造成1点伤害。堕落值≥4：变奏。";
                string rngBeforeReading = run.Rng.CombatTargets.ToSerializable().ToString();
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    Check(soul.DynamicVars.Damage.BaseValue == expected, "owned damage refreshes on each read");
                    Check(Plain(soul.DynamicDescription.GetFormattedText()) == text, "exact current variation text");
                }
                Check(run.Rng.CombatTargets.ToSerializable().ToString() == rngBeforeReading, "hover does not roll targets");
                Check(soul.Id == id && soul.FloorAddedToDeck == floor && player.Relics.Single() == soul,
                    "variation keeps original ID, inventory slot and acquisition floor");
                Check(soul.DynamicVars.Damage.Props == ValueProp.Unpowered, "native unpowered damage preserved");
                Check(canonical.DynamicVars.Damage.BaseValue == 1 && canonical.DynamicDescription.GetFormattedText() == originalText,
                    "canonical instance and global localization untouched");
                Check(!ForgottenSoulVariation.TryGetDamage(ownerless, out _), "ownerless preview remains vanilla");
                Check(foreignSoul.DynamicVars.Damage.BaseValue == 1 && foreignSoul.DynamicDescription.GetFormattedText() == originalText,
                    "other character preserves vanilla damage and description");
                Check(remoteSoul.DynamicVars.Damage.BaseValue == expected
                    && Plain(remoteSoul.DynamicDescription.GetFormattedText()) == text, "remote Maiden has same deterministic rule");
                Check(ashes.DynamicVars.Damage.BaseValue == 3 && !ForgottenSoulVariation.TryGetDamage(ashes, out _),
                    "Charons Ashes is not Forgotten Soul");
                var restored = (ForgottenSoul)RelicModel.FromSerializable(soul.ToSerializable());
                restored.Owner = player;
                Check(restored.Id == id && restored.FloorAddedToDeck == floor
                    && restored.DynamicVars.Damage.BaseValue == expected
                    && Plain(restored.DynamicDescription.GetFormattedText()) == text, "loaded model works without pickup replay");
                var copy = (ForgottenSoul)soul.ClonePreservingMutability();
                Check(copy.DynamicVars.Damage.BaseValue == expected, "mutable clone reads current route");

                // Execute real exhaust dispatch, not a mocked callback; only a single
                // vanilla target roll may occur, irrespective of the route threshold.
                await PowerCmd.Apply<StrengthPower>(choice, ctx.Self, 20, ctx.Self, null);
                foreach (var enemy in combat.HittableEnemies)
                    await PowerCmd.Apply<VulnerablePower>(choice, enemy, 2, ctx.Self, null);
                for (int hit = 0; hit < 2; hit++)
                {
                    var card = await ctx.Add<StrikeIronclad>(PileType.Hand);
                    var enemies = combat.HittableEnemies.ToArray();
                    var hp = enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
                    var expectedRng = new Rng(run.Rng.CombatTargets.ToSerializable());
                    var expectedTarget = expectedRng.NextItem(combat.HittableEnemies);
                    await CardCmd.Exhaust(choice, card, skipVisuals: true);
                    Check(card.Pile?.Type == PileType.Exhaust, "card actually exhausted");
                    foreach (var enemy in enemies)
                        Check(hp[enemy] - enemy.CurrentHp == (enemy == expectedTarget ? expected : 0),
                            "one native random enemy receives exact unpowered damage");
                    Check(run.Rng.CombatTargets.ToSerializable().ToString() == expectedRng.ToSerializable().ToString(),
                        "real target RNG advanced exactly as one native selection");
                }

                var foreignCard = run.CreateCard(ModelDb.Card<StrikeIronclad>(), foreign);
                int hpBeforeForeign = combat.HittableEnemies.Sum(enemy => enemy.CurrentHp);
                string rngBeforeForeign = run.Rng.CombatTargets.ToSerializable().ToString();
                await soul.AfterCardExhausted(choice, foreignCard, false);
                Check(combat.HittableEnemies.Sum(enemy => enemy.CurrentHp) == hpBeforeForeign
                    && run.Rng.CombatTargets.ToSerializable().ToString() == rngBeforeForeign,
                    "another player's exhaust callback deals no damage and rolls no RNG");
            }
            await RelicCmd.Remove(soul);
            int afterRemovalHp = combat.HittableEnemies.Sum(enemy => enemy.CurrentHp);
            var afterRemoval = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await CardCmd.Exhaust(choice, afterRemoval, skipVisuals: true);
            Check(combat.HittableEnemies.Sum(enemy => enemy.CurrentHp) == afterRemovalHp,
                "removed relic no longer participates in native exhaust dispatch");
            MaidenSuccubusMod.Logger.Info($"[DS27ForgottenSoulTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27ForgottenSoulTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
