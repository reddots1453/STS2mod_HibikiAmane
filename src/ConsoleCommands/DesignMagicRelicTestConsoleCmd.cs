#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignMagicRelicTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_magic_relics";
    public override string Args => "confirm";
    public override string Description => "Destructive prayer/barrier relic tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.CombatState is not CombatState combat || combat.HittableEnemies.Count == 0
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_magic_relics confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(player, combat), true, "Destructive tests started; see [DS27MagicRelicTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 magic relic: " + name);
            checks++;
        }
        string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        int Amplification() => ctx.Self.GetPower<MagicAmplificationPower>()?.Amount ?? 0;
        int Sanctuary() => ctx.Self.GetPower<SanctuaryPower>()?.Amount ?? 0;
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            var run = (RunState)player.RunState;
            PrayerEarrings earrings = await RelicCmd.Obtain<PrayerEarrings>(player);
            Check(earrings.Rarity == RelicRarity.Common, "earrings are common");
            Check(earrings.HoverTipsExcludingRelic.Any(), "amplification hover exists");
            foreach (int corruption in Enumerable.Range(-5, 11))
            {
                await ctx.Reset();
                CorruptionCmd.Set(run, corruption);
                bool holy = corruption is -5 or -4 or -3 or -2;
                string expectedText = holy ? "战斗开始时和进入变身时，获得1层魔力增幅。"
                    : "战斗开始时，获得1层魔力增幅。堕落值≤-2：变奏。";
                Check(Plain(earrings.DynamicDescription.GetFormattedText()) == expectedText,
                    "prayer formatted description exact at every corruption value");
                await earrings.BeforeCombatStart();
                Check(Amplification() == 1, "combat start grants one in both branches");
                Func<Task>[] enter = [
                    () => TransformationCmd.EnterImmaculateRobe(choice, ctx.Self, null),
                    () => TransformationCmd.EnterCorruptRobe(choice, ctx.Self, null),
                    () => TransformationCmd.EnterEternalRobe(choice, ctx.Self, null)];
                foreach (Func<Task> transform in enter)
                {
                    int before = Amplification();
                    await transform();
                    Check(Amplification() == before + (holy ? 1 : 0), "actual form command triggers one, including eternal");
                    int after = Amplification();
                    await transform();
                    Check(Amplification() == after, "same form entry does not trigger again");
                    await TransformationCmd.GainArmor(choice, ctx.Self, 1, null);
                    Check(Amplification() == after, "armour gain is not entering a form");
                }
                int current = Amplification();
                await PowerCmd.Apply<EternalRobePower>(choice, ctx.Self, 9, ctx.Self, null);
                Check(Amplification() == current, "stacking an existing form does not count as entering it");
                await TransformationCmd.Exit(choice, ctx.Self);
                Check(Amplification() == current, "form removal never grants amplification");

                var saved = (PrayerEarrings)RelicModel.FromSerializable(earrings.ToSerializable());
                saved.Owner = player;
                Check(Plain(saved.DynamicDescription.GetFormattedText()) == expectedText,
                    "loaded prayer description needs no pickup or event subscription");
            }
            await ctx.Reset();
            CorruptionCmd.Set(run, -2);
            int unchanged = Amplification();
            await PowerCmd.Apply<ImmaculateRobePower>(choice, ctx.Self, 0, ctx.Self, null);
            Check(!ctx.Self.HasPower<ImmaculateRobePower>() && Amplification() == unchanged,
                "zero application creates no form and grants nothing");
            PowerModel? enemyForm = await PowerCmd.Apply<ImmaculateRobePower>(choice, ctx.PrimaryEnemy, 1, ctx.PrimaryEnemy, null);
            Check(enemyForm != null && Amplification() == unchanged, "another creature's applied form does not trigger");
            var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = run;
            var foreignEarrings = (PrayerEarrings)ModelDb.Relic<PrayerEarrings>().ToMutable();
            foreignEarrings.Owner = foreign;
            await foreignEarrings.BeforeCombatStart();
            await foreignEarrings.AfterPowerAmountChanged(choice, enemyForm!, 1, ctx.PrimaryEnemy, null);
            Check(!foreign.Creature.HasPower<MagicAmplificationPower>(), "other character owner cannot gain mod power");
            await RelicCmd.Remove(earrings);
            await earrings.BeforeCombatStart();
            Check(Amplification() == unchanged, "removed prayer relic cannot grant");

            BarrierGenerator barrier = await RelicCmd.Obtain<BarrierGenerator>(player);
            Check(barrier.Rarity == RelicRarity.Rare && barrier.ShowCounter, "barrier is rare and counter visible");
            Check(barrier.HoverTipsExcludingRelic.Any(), "sanctuary hover exists");
            Check(Plain(barrier.DynamicDescription.GetFormattedText()) == "每5个回合，获得1层圣域。", "barrier exact description");
            for (int initial = 0; initial < 5; initial++)
            {
                await ctx.Reset();
                barrier.TurnsSeen = initial;
                await barrier.AfterSideTurnStartLate(CombatSide.Enemy, [ctx.PrimaryEnemy], combat);
                await barrier.AfterSideTurnStartLate(CombatSide.Player, [foreign.Creature], combat);
                Check(barrier.TurnsSeen == initial && Sanctuary() == 0, "enemy and other participant turns do not count");
                for (int turn = 1; turn <= 10; turn++)
                {
                    // Exercise both native hook phases: old Sanctuary ticks before
                    // the late relic grant, so the fifth turn retains its new layer.
                    await Hook.AfterSideTurnStart(combat, CombatSide.Player, [ctx.Self]);
                    int expected = (initial + turn) % 5;
                    Check(barrier.TurnsSeen == expected && barrier.DisplayAmount == expected, "actual fifth-turn counter and display");
                    Check(Sanctuary() == (expected == 0 ? 1 : 0), "new layer survives this turn and expires on next");
                }
                barrier.TurnsSeen = initial;
                await barrier.AfterCombatEnd(null!);
                await barrier.BeforeCombatStart();
                Check(barrier.TurnsSeen == initial, "combat boundaries retain partial progress");
                var restored = (BarrierGenerator)RelicModel.FromSerializable(barrier.ToSerializable());
                restored.Owner = player;
                Check(restored.TurnsSeen == initial && restored.DisplayAmount == initial, "counter native serialization round trip");
                if (ctx.Self.GetPower<SanctuaryPower>() is { } sanctuary) await PowerCmd.Remove(sanctuary);
                await restored.AfterSideTurnStartLate(CombatSide.Player, [ctx.Self], combat);
                Check(restored.TurnsSeen == (initial + 1) % 5 && Sanctuary() == (initial == 4 ? 1 : 0),
                    "restored counter continues actual effect without pickup callback");
            }
            var foreignBarrier = (BarrierGenerator)ModelDb.Relic<BarrierGenerator>().ToMutable();
            foreignBarrier.Owner = foreign;
            foreignBarrier.TurnsSeen = 4;
            await foreignBarrier.AfterSideTurnStartLate(CombatSide.Player, [foreign.Creature], combat);
            Check(foreignBarrier.TurnsSeen == 4 && !foreign.Creature.HasPower<SanctuaryPower>(), "other character barrier isolated");
            await RelicCmd.Remove(barrier);
            int removedCount = barrier.TurnsSeen;
            await barrier.AfterSideTurnStartLate(CombatSide.Player, [ctx.Self], combat);
            Check(barrier.TurnsSeen == removedCount, "removed barrier cannot count or grant");
            // These are simulated combat boundaries, not natural travel/save acceptance.
            var turnProperty = typeof(PlayerCombatState).GetProperty(nameof(PlayerCombatState.TurnNumber))!;
            int originalTurn = player.PlayerCombatState!.TurnNumber;
            var refreshed = (Refreshed)ModelDb.Relic<Refreshed>().ToMutable();
            await RelicCmd.Obtain(refreshed, player);
            try
            {
                Check(Plain(refreshed.DynamicDescription.GetFormattedText()) == "拾起时，在接下来的3场战斗开始时，额外抽2张牌。",
                    "refreshed description matches design");
                for (int battle = 1; battle <= 3; battle++)
                {
                    await ctx.Reset();
                    turnProperty.SetValue(player.PlayerCombatState, 1);
                    await refreshed.BeforeCombatStart();
                    await refreshed.BeforeCombatStart();
                    Check(refreshed.RemainingCombats == 3 - battle && refreshed.DisplayAmount == 3 - battle,
                        "opening receipt spends exactly one battle despite duplicate setup");
                    decimal draw = Hook.ModifyHandDraw(combat, player, 5, out _);
                    Check(draw == 7 && Hook.ModifyHandDraw(combat, player, 5, out _) == 7
                        && refreshed.RemainingCombats == 3 - battle, "native opening draw query adds two without spending again");
                    Check(refreshed.ModifyHandDraw(foreign, 5) == 5, "another player's hand is unchanged");
                    var restored = (Refreshed)RelicModel.FromSerializable(refreshed.ToSerializable());
                    restored.Owner = player;
                    await restored.BeforeCombatStart();
                    Check(restored.RemainingCombats == 3 - battle && restored.ModifyHandDraw(player, 5) == 7,
                        "restored pending opening retains charge receipt and bonus");
                    await ctx.AddFillerCards(MegaCrit.Sts2.Core.Entities.Cards.PileType.Draw, 10);
                    if (battle == 2) await ctx.ApplyPower<MegaCrit.Sts2.Core.Models.Powers.NoDrawPower>(ctx.Self, 1);
                    await CardPileCmd.Draw(choice, draw, player, fromHandDraw: true);
                    await Hook.AfterPlayerTurnStart(combat, choice, player);
                    Check(MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand.GetPile(player).Cards.Count == (battle == 2 ? 0 : 7),
                        "real opening draw respects native no-draw effect");
                    Check(!refreshed.OpeningPending && refreshed.ModifyHandDraw(player, 5) == 5,
                        "completed opening cannot grant bonus again");
                    player.PlayerCombatState.IncrementTurnNumber();
                    Check(refreshed.ModifyHandDraw(player, 5) == 5, "later turn receives no opening bonus");
                    await refreshed.AfterCombatEnd(null!);
                    Check(player.Relics.Contains(refreshed) == (battle < 3), "third use removes event relic only after opening draw");
                }
                var foreignRefreshed = (Refreshed)ModelDb.Relic<Refreshed>().ToMutable();
                foreignRefreshed.Owner = foreign;
                await foreignRefreshed.BeforeCombatStart();
                Check(foreignRefreshed.RemainingCombats == 3 && !foreignRefreshed.OpeningPending,
                    "another character cannot activate the event reward");
            }
            finally
            {
                turnProperty.SetValue(player.PlayerCombatState, originalTurn);
                if (player.Relics.Contains(refreshed)) await RelicCmd.Remove(refreshed);
            }
            MaidenSuccubusMod.Logger.Info($"[DS27MagicRelicTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27MagicRelicTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
