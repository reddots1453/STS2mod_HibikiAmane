#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignReactiveRelicTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_reactive_relics";
    public override string Args => "confirm";
    public override string Description => "Destructive mirror/stardust tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.CombatState is not CombatState combat || combat.HittableEnemies.Count == 0
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_reactive_relics confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(player, combat), true, "Destructive tests started; see [DS27ReactiveRelicTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 reactive relic: " + name);
            checks++;
        }
        string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        int Burning() => ctx.PrimaryEnemy.GetPower<BurningPower>()?.Amount ?? 0;
        int Condemnation() => ctx.PrimaryEnemy.GetPower<CondemnationPower>()?.Amount ?? 0;
        int Amplification() => ctx.Self.GetPower<MagicAmplificationPower>()?.Amount ?? 0;
        CardPlay Receipt(CardModel card, Player? caster = null) => new()
        {
            Card = card, Player = caster ?? player, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = true, PlayIndex = 0, PlayCount = 1
        };
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            var run = (RunState)player.RunState;
            var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = run;
            CounterCurseMirror mirror = await RelicCmd.Obtain<CounterCurseMirror>(player);
            Check(mirror.Rarity == RelicRarity.Rare && mirror.HoverTipsExcludingRelic.Any(), "rare mirror with hover tips");
            foreach (int corruption in Enumerable.Range(-5, 11))
            {
                await ctx.Reset();
                CorruptionCmd.Set(run, corruption);
                bool holy = corruption is -5 or -4 or -3;
                string expected = holy ? "给予敌人负面状态时，额外给予1层断罪。堕落值＞-3：变奏。"
                    : "给予敌人负面状态时，额外给予1层燃烧。堕落值≤-3：变奏。";
                Check(Plain(mirror.DynamicDescription.GetFormattedText()) == expected, "exact current variation description");
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                await PowerCmd.Apply<VulnerablePower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
                Check(Burning() == (holy ? 0 : 3) && Condemnation() == (holy ? 3 : 0),
                    "each real harmful application, including stacking, triggers once without recursion");
                var restored = (CounterCurseMirror)RelicModel.FromSerializable(mirror.ToSerializable());
                restored.Owner = player;
                Check(Plain(restored.DynamicDescription.GetFormattedText()) == expected, "mirror load selects current run branch");
            }
            await ctx.Reset();
            await PowerCmd.Apply<ArtifactPower>(choice, ctx.PrimaryEnemy, 1, ctx.PrimaryEnemy, null);
            await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
            Check(Burning() == 0 && !ctx.PrimaryEnemy.HasPower<WeakPower>(), "blocked debuff does not reflect");
            await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 2, ctx.PrimaryEnemy, null);
            var weak = ctx.PrimaryEnemy.GetPower<WeakPower>()!;
            await mirror.AfterPowerAmountChanged(choice, weak, 1, foreign.Creature, null);
            await mirror.AfterPowerAmountChanged(choice, weak, 0, ctx.Self, null);
            await PowerCmd.ModifyAmount(choice, weak, -1, ctx.Self, null);
            await PowerCmd.Apply<StrengthPower>(choice, ctx.PrimaryEnemy, 2, ctx.Self, null);
            await PowerCmd.Apply<WeakPower>(choice, ctx.Self, 1, ctx.Self, null);
            Check(Burning() == 0 && !ctx.Self.HasPower<BurningPower>(), "foreign, zero, cleanse, buff and friendly target excluded");
            await PowerCmd.Apply<StrengthPower>(choice, ctx.PrimaryEnemy, -1, ctx.Self, null);
            Check(Burning() == 1 && ctx.PrimaryEnemy.GetPower<StrengthPower>()!.Amount == 1,
                "strength reduction is harmful even when final strength is positive");
            await ctx.Reset();
            await PowerCmd.Apply<BurningPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
            Check(Burning() == 2, "root burning gains one extra, not an infinite chain");
            var copyMirror = (CounterCurseMirror)mirror.ClonePreservingMutability();
            await RelicCmd.Obtain(copyMirror, player);
            await ctx.Reset();
            await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.Self, null);
            Check(Burning() == 2, "two mirror instances each reflect root once but not one another's reactions");
            await RelicCmd.Remove(copyMirror);
            await ctx.Reset();
            CorruptionCmd.Set(run, -3);
            await PowerCmd.Apply<CondemnationPower>(choice, ctx.PrimaryEnemy, 6, ctx.Self, null);
            Check(Condemnation() == 0 && ctx.PrimaryEnemy.CurrentHp < ctx.PrimaryEnemy.MaxHp,
                "extra condemnation reaches native judgment and completes without recursion");
            var foreignMirror = (CounterCurseMirror)ModelDb.Relic<CounterCurseMirror>().ToMutable();
            foreignMirror.Owner = foreign;
            await ctx.Reset();
            await PowerCmd.Apply<WeakPower>(choice, ctx.PrimaryEnemy, 1, ctx.PrimaryEnemy, null);
            weak = ctx.PrimaryEnemy.GetPower<WeakPower>()!;
            await foreignMirror.AfterPowerAmountChanged(choice, weak, 1, foreign.Creature, null);
            Check(Burning() == 0, "other character mirror cannot trigger");
            await RelicCmd.Remove(mirror);
            await mirror.AfterPowerAmountChanged(choice, weak, 1, ctx.Self, null);
            Check(Burning() == 0, "removed mirror cannot trigger");

            DivineStardust dust = await RelicCmd.Obtain<DivineStardust>(player);
            Check(dust.Rarity == RelicRarity.Uncommon && dust.ShowCounter && dust.HoverTipsExcludingRelic.Any(),
                "uncommon stardust with counter and hover tip");
            Check(Plain(dust.DynamicDescription.GetFormattedText()) == "你每生成或打出7张附魔牌，获得2层魔力增幅。", "stardust exact text");
            for (int initial = 0; initial < 7; initial++)
            {
                await ctx.Reset();
                await dust.BeforeCombatStart();
                dust.Progress = initial;
                for (int generated = 1; generated <= 7; generated++)
                {
                    var card = ctx.Create<MaidenDefend>();
                    CombatEnchantmentCmd.ApplyVanilla<Swift>(card, 1);
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, player);
                    Check(dust.Progress == (initial + generated) % 7 && dust.DisplayAmount == dust.Progress,
                        "real generation hook counts exactly one");
                    Check(Amplification() == (initial + generated >= 7 ? 2 : 0), "seventh generated card grants exactly two");
                }
                await dust.AfterCombatEnd(null!);
                await dust.BeforeCombatStart();
                Check(dust.Progress == initial, "combat boundary retains partial count");
                var restored = (DivineStardust)RelicModel.FromSerializable(dust.ToSerializable());
                restored.Owner = player;
                Check(restored.Progress == initial && restored.DisplayAmount == initial, "native relic save round trip");
                var pending = ctx.Create<MaidenDefend>();
                CombatEnchantmentCmd.ApplyVanilla<Swift>(pending, 1);
                int before = Amplification();
                await restored.AfterCardGeneratedForCombat(pending, player);
                Check(restored.Progress == (initial + 1) % 7 && Amplification() == before + (initial == 6 ? 2 : 0),
                    "loaded count continues without pickup");
            }
            await ctx.Reset();
            dust.Progress = 0;
            var ordinary = ctx.Create<MaidenDefend>();
            await CardPileCmd.AddGeneratedCardToCombat(ordinary, PileType.Discard, player);
            CombatEnchantmentCmd.ApplyVanilla<Swift>(ordinary, 1);
            await CardPileCmd.Add(ordinary, PileType.Hand);
            Check(dust.Progress == 0, "unenchanted generation, later enchant and pile movement do not count");
            await dust.AfterCardGeneratedForCombat(ordinary, null);
            await dust.AfterCardGeneratedForCombat(ordinary, foreign);
            Check(dust.Progress == 0, "enemy and foreign creators excluded");
            await ctx.Play(ordinary);
            Check(dust.Progress == 1, "actual enchanted play counts");
            var replay = await ctx.Add<MaidenDefend>(PileType.Hand);
            CombatEnchantmentCmd.ApplyVanilla<Glam>(replay, 1);
            await ctx.Play(replay);
            Check(dust.Progress == 3, "native Glam replay counts both actual plays");
            var layered = ctx.Create<LightWings>();
            CombatEnchantmentCmd.ApplyVanilla<Swift>(layered, 1);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(layered, 1);
            await CardPileCmd.AddGeneratedCardToCombat(layered, PileType.Discard, player);
            Check(dust.Progress == 4, "two enchantment layers count as one generated card");
            var expiring = ctx.Create<MaidenDefend>();
            CombatEnchantmentCmd.ApplyVanilla<Swift>(expiring, 1);
            CardPlay receipt = Receipt(expiring);
            await dust.BeforeCardPlayed(receipt);
            expiring.ClearEnchantmentInternal();
            await dust.AfterCardPlayed(choice, receipt);
            await dust.AfterCardPlayed(choice, receipt);
            Check(dust.Progress == 5, "pre-play enchantment snapshot survives removal and repeated callback");
            var foreignReceipt = Receipt(layered, foreign);
            await dust.BeforeCardPlayed(foreignReceipt);
            await dust.AfterCardPlayed(choice, foreignReceipt);
            Check(dust.Progress == 5, "actual player, not current card owner, controls eligibility");
            var receipt1 = Receipt(layered);
            var receipt2 = Receipt(layered);
            await dust.BeforeCardPlayed(receipt1);
            await dust.BeforeCardPlayed(receipt2);
            await dust.AfterCardPlayed(choice, receipt1);
            int amp = Amplification();
            await dust.AfterCardPlayed(choice, receipt2);
            Check(dust.Progress == 0 && Amplification() == amp + 2, "mixed generation/play stream reaches seven");
            var inFlight = Receipt(layered);
            await dust.BeforeCardPlayed(inFlight);
            var copyDust = (DivineStardust)dust.ClonePreservingMutability();
            copyDust.Owner = player;
            await copyDust.AfterCardPlayed(choice, inFlight);
            Check(copyDust.Progress == 0, "relic copy cannot inherit another instance's in-flight play");
            await copyDust.BeforeCardPlayed(inFlight);
            await dust.AfterCardPlayed(choice, inFlight);
            await copyDust.AfterCardPlayed(choice, inFlight);
            Check(dust.Progress == 1 && copyDust.Progress == 1, "each copy maintains its own play receipt");
            var foreignDust = (DivineStardust)ModelDb.Relic<DivineStardust>().ToMutable();
            foreignDust.Owner = foreign;
            await foreignDust.AfterCardGeneratedForCombat(layered, foreign);
            Check(foreignDust.Progress == 0, "other character cannot count mod relic");
            await RelicCmd.Remove(dust);
            await dust.AfterCardGeneratedForCombat(layered, player);
            Check(dust.Progress == 1, "removed stardust cannot count");
            MaidenSuccubusMod.Logger.Info($"[DS27ReactiveRelicTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27ReactiveRelicTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
