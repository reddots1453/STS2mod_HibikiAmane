#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.ConsoleCommands;

// Generated-catalog cases plus legacy runtime regressions pending migration.
// Formal selector cases exercise the same card selection and play pipeline as gameplay.
public sealed class DesignHumilityRuntimeTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_humility_runtime";
    public override string Args => "confirm";
    public override string Description => "Destructive humility runtime tests; disposable single-player combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsOverOrEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || combat.HittableEnemies.Count == 0 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_humility_runtime confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive runtime tests started; see [DS27HumilityRuntimeTest].");
    }

    private static string Text(CardModel card) => Regex.Replace(card.GetDescriptionForPile(PileType.Hand), @"\[[^\]]*\]", "");

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DS27 humility runtime: " + message);
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
            await ctx.Reset();
            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var lesson = await ctx.Add<HumilityLesson>(PileType.Hand, upgraded);
                var selected = await ctx.Add<PommelStrike>(PileType.Hand, upgraded);
                var untouched = await ctx.Add<PommelStrike>(PileType.Hand, upgraded);
                decimal damage = selected.DynamicVars.Damage.BaseValue;
                string originalText = Text(untouched);
                await ctx.AddFillerCards(PileType.Draw, 4);
                await ctx.Play(lesson, selectedCards: [selected]);
                Check(HumilityRewriteCapability.Find(selected) != null
                    && HumilityRewriteCapability.Find(untouched) == null
                    && Text(selected) == $"造成{damage * 2}点伤害。" && Text(untouched) == originalText,
                    "formal selector changes only selected combat instance and refreshes damage-only description");
                Check(lesson.Pile?.Type == PileType.Exhaust && selected.DynamicVars.Damage.BaseValue == damage,
                    "lesson exhausts normally; rewrite is not a fake stat upgrade");
                var again = await ctx.Add<HumilityLesson>(PileType.Hand, upgraded);
                await ctx.Play(again, selectedCards: [selected]);
                CardCmd.Enchant<Swift>(selected, 1);
                int hp = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(selected, ctx.PrimaryEnemy);
                Check(hp - ctx.PrimaryEnemy.CurrentHp == damage * 4 && PileType.Draw.GetPile(player).Cards.Count == 3,
                    "repeated formal selection doubles again, intrinsic draw removed but Swift draw retained");

                await ctx.Reset();
                lesson = await ctx.Add<HumilityLesson>(PileType.Hand, upgraded);
                var skill = await ctx.Add<CloakAndDagger>(PileType.Hand, upgraded);
                decimal block = skill.DynamicVars.Block.BaseValue;
                await ctx.Play(lesson, selectedCards: [skill]);
                Check(Text(skill) == $"获得{block * 2}点格挡。", "formal skill selection replaces generation text with block only");
                await ctx.Play(skill);
                Check(ctx.Self.Block == block * 2 && PileType.Hand.GetPile(player).Cards.Count == 0,
                    "formal rewritten skill gains block but creates no token");

                await ctx.Reset();
                lesson = await ctx.Add<HumilityLesson>(PileType.Hand, upgraded);
                var scripture = await ctx.Add<MaidenSuccubus.Cards.Scriptures.GuardianScripture>(PileType.Hand);
                await ctx.Play(lesson, selectedCards: [scripture]);
                Check(HumilityRewriteCapability.Find(scripture)?.Program.Effects.Count == 0 && Text(scripture) == "",
                    "inherited scripture selected normally and rewritten to explicit empty effect");
                await ctx.Play(scripture);
                Check(!ctx.Self.Powers.Any(), "rewritten inherited card does not invoke original power command");
            }
            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var extractedSurf = await ctx.Add<Surf>(PileType.Hand, upgraded);
                await ctx.AddFillerCards(PileType.Draw, 6);
                var generatedEntry = HumilityExtractedCards.Get(extractedSurf);
                Check(generatedEntry.Program?.Effects.Count == 1, "Surf resolved from generated embedded catalog, not manual table");
                HumilityExtractedCards.Apply(extractedSurf);
                var enemyHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
                await ctx.Play(extractedSurf);
                Check(enemyHp.All(pair => pair.Value - pair.Key.CurrentHp == 8)
                    && PileType.Draw.GetPile(player).Cards.Count == 6 && PileType.Hand.GetPile(player).Cards.Count == 0,
                    "generated Surf executes one doubled all-enemy attack without drawing or simulating draw loop");

                await ctx.Reset();
                var helperAttack = await ctx.Add<BurningBracelet>(PileType.Hand, upgraded);
                HumilityExtractedCards.Apply(helperAttack);
                int hpBefore = ctx.PrimaryEnemy.CurrentHp;
                decimal amount = helperAttack.DynamicVars.Damage.BaseValue * 2;
                await ctx.Play(helperAttack, ctx.PrimaryEnemy);
                Check(hpBefore - ctx.PrimaryEnemy.CurrentHp == amount,
                    "generated helper expansion executes damage through original card/native modifiers");

                await ctx.Reset();
                var splitAttack = await ctx.Add<PerfectedStrike>(PileType.Hand, upgraded);
                HumilityExtractedCards.Apply(splitAttack);
                amount = splitAttack.DynamicVars.CalculatedDamage.Calculate(ctx.PrimaryEnemy) * 2;
                hpBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(splitAttack, ctx.PrimaryEnemy);
                Check(hpBefore - ctx.PrimaryEnemy.CurrentHp == amount,
                    "generated split builder retains calculated amount and executes once");

                await ctx.Reset();
                var generationAndBlock = await ctx.Add<CloakAndDagger>(PileType.Hand, upgraded);
                HumilityExtractedCards.Apply(generationAndBlock);
                amount = generationAndBlock.DynamicVars.Block.BaseValue * 2;
                Check(Text(generationAndBlock) == $"获得{amount}点格挡。",
                    "generated projection contains block only, not the removed token-generation description");
                int blockBefore = ctx.Self.Block;
                await ctx.Play(generationAndBlock);
                Check(ctx.Self.Block - blockBefore == amount && PileType.Hand.GetPile(player).Cards.Count == 0,
                    "source-overload extraction retains block without creating tokens");
            }
            await ctx.Reset();
            foreach (Type type in HumilityCardProfiles.SupportedTypes)
            foreach (bool upgraded in new[] { false, true })
            {
                CardModel card = ctx.Create(type, upgraded);
                Check(HumilityCardProfiles.TryGet(card, out var program), "exact real model binding: " + type.Name);
                Check(card.Type is CardType.Attack or CardType.Skill, "profile type is eligible: " + type.Name);
                HumilityNativeEffects.Validate(program);
                foreach (var effect in program.Effects)
                {
                    decimal Resolve(string name) => HumilityNativeEffects.ResolveValue(card, name, ctx.PrimaryEnemy);
                    _ = effect.Amount.Evaluate(new(3, 0, 4), Resolve);
                    _ = effect.Repeats.Evaluate(new(3, 0, 4), Resolve);
                    Check(true, "real upgraded/base dynamic variables resolve: " + type.Name);
                }
            }
            // Remaining legacy regression cases will migrate with the formal selector;
            // generated entries never silently fall back to that hand-maintained table.
            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var pommel = await ctx.Add<PommelStrike>(PileType.Hand, upgraded);
                await ctx.AddFillerCards(PileType.Draw, 8);
                decimal baseDamage = pommel.DynamicVars.Damage.BaseValue;
                var rewrite = HumilityCardProfiles.ApplyKnown(pommel);
                var probe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                pommel.AddCapability(probe, allowMerge: false);
                Check(!combat.IterateHookListeners().Contains(pommel), "original card removed from native hook stream");
                Check(combat.IterateHookListeners().Contains(rewrite), "rewrite remains after Ritsu capability expansion");
                Check(combat.IterateHookListeners().Contains(probe), "other capabilities remain available");
                var runListeners = player.RunState.IterateHookListeners(combat).ToArray();
                Check(!runListeners.Contains(pommel) && runListeners.Count(model => ReferenceEquals(model, rewrite)) == 1
                    && runListeners.Count(model => ReferenceEquals(model, probe)) == 1,
                    "nested run/combat expansion retains each capability once, not the original card");
                Check(!Text(pommel).Contains("抽") && Text(pommel).Contains($"造成{baseDamage * 2}点伤害"), "only projected damage description remains");
                int before = ctx.PrimaryEnemy.CurrentHp;
                await Pay(pommel);
                Check(before - ctx.PrimaryEnemy.CurrentHp == baseDamage * 2, "original card OnPlay replaced, not followed by second attack");
                Check(PileType.Hand.GetPile(player).Cards.Count == 0, "original draw removed");
                Check(probe.BeforeCount == 1 && probe.AfterCount == 1, "native attack hooks fire exactly once");
                Check(pommel.Pile?.Type == PileType.Discard, "native wrapper result pile preserved");
                Check(pommel.DynamicVars.Damage.BaseValue == baseDamage, "source variables not mutated");

                await ctx.Reset();
                var swift = await ctx.Add<PommelStrike>(PileType.Hand, upgraded);
                CardCmd.Enchant<Swift>(swift, 2);
                var enchantment = swift.Enchantment;
                HumilityCardProfiles.ApplyKnown(swift);
                await ctx.AddFillerCards(PileType.Draw, 8);
                Check(ReferenceEquals(enchantment, swift.Enchantment), "same enchantment instance retained");
                await ctx.Play(swift, ctx.PrimaryEnemy);
                Check(PileType.Hand.GetPile(player).Cards.Count == 2, "only native Swift draws; original Pommel draw absent");

                await ctx.Reset();
                var replay = await ctx.Add<PommelStrike>(PileType.Hand, upgraded);
                CardCmd.Enchant<Glam>(replay, 1);
                HumilityCardProfiles.ApplyKnown(replay);
                await ctx.AddFillerCards(PileType.Draw, 8);
                before = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(replay, ctx.PrimaryEnemy);
                Check(before - ctx.PrimaryEnemy.CurrentHp == replay.DynamicVars.Damage.BaseValue * 4, "native enchant replay executes rewritten body twice");
                Check(PileType.Hand.GetPile(player).Cards.Count == 0, "replay does not resurrect removed draw");

                await ctx.Reset();
                var defense = await ctx.Add<DoubleDefense>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(defense);
                await ctx.ApplyPower<DexterityPower>(ctx.Self, 3);
                int initialBlock = ctx.Self.Block;
                await ctx.Play(defense);
                Check(ctx.Self.Block - initialBlock == (defense.DynamicVars.Block.BaseValue * 2 + 3) * 2,
                    "block doubles base only; native Dexterity applies once per block; two blocks retained");
            }

            await ctx.Reset();
            var empty = await ctx.Add<AcceleratedMotion>(PileType.Hand);
            CardCmd.Enchant<Swift>(empty, 2);
            HumilityCardProfiles.ApplyKnown(empty);
            await ctx.AddFillerCards(PileType.Draw, 8);
            Check(!HumilityRewriteCapability.Find(empty)!.Program.HasDamageOrBlock, "empty program is not pure damage/block");
            await ctx.Play(empty);
            Check(PileType.Hand.GetPile(player).Cards.Count == 2 && empty.Pile?.Type == PileType.Discard,
                "empty program deletes original draw/exhaust but retains native Swift");

            await ctx.Reset();
            var flare = await ctx.Add<UltimateFlare>(PileType.Hand);
            var ordinaryFlare = await ctx.Add<UltimateFlare>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(flare);
            Check(!flare.HasTurnEndInHandEffect && ordinaryFlare.HasTurnEndInHandEffect,
                "deleted turn-end effect no longer advertises a UI trigger");
            int originalFlareCost = flare.EnergyCost.GetWithModifiers(CostModifiers.Local);
            await flare.OnTurnEndInHandWrapper(choice);
            await ordinaryFlare.OnTurnEndInHandWrapper(choice);
            Check(flare.EnergyCost.GetWithModifiers(CostModifiers.Local) == originalFlareCost
                && ordinaryFlare.EnergyCost.GetWithModifiers(CostModifiers.Local) == originalFlareCost - 1,
                "original turn-end discount removed on rewritten instance only");
            var flareHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
            await ctx.Play(flare);
            Check(flareHp.All(pair => pair.Value - pair.Key.CurrentHp == 80), "all-enemy profile doubles each enemy damage");

            await ctx.Reset();
            var explosive = await ctx.Add<ExplosiveImpact>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(explosive);
            var randomProbe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
            explosive.AddCapability(randomProbe, allowMerge: false);
            int totalBefore = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
            await ctx.Play(explosive);
            Check(totalBefore - ctx.Enemies.Sum(enemy => enemy.CurrentHp) == 12
                && randomProbe.BeforeCount == 1 && randomProbe.AfterCount == 1,
                "two random hits remain one native attack, not two command boundaries");
            Check(ctx.Enemies.All(enemy => !enemy.Powers.OfType<MaidenSuccubus.Powers.ShatterPower>().Any()),
                "random profile removes original shatter effect");

            await ctx.Reset();
            var rest = await ctx.Add<Rest>(PileType.Hand);
            var ordinaryRest = await ctx.Add<Rest>(PileType.Hand);
            ordinaryRest.CanPlay(out var beforeRestReason, out _);
            Check(beforeRestReason.HasFlag(UnplayableReason.BlockedByCardLogic), "ordinary Rest requires transformation");
            HumilityCardProfiles.ApplyKnown(rest);
            Check(rest.CanPlay(), "rewritten Rest removes its original transformation restriction");
            ordinaryRest.CanPlay(out var afterRestReason, out _);
            Check(afterRestReason.HasFlag(UnplayableReason.BlockedByCardLogic), "unmodified Rest remains restricted");
            var costed = await ctx.Add<MaidenStrike>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(costed);
            await PlayerCmd.SetEnergy(0, player);
            Check(!costed.CanPlay(), "intrinsic-rule removal does not bypass native insufficient energy");
            await PlayerCmd.SetEnergy(20, player);
            await ctx.ApplyPower<SlothPower>(ctx.Self, 1);
            var sloth = ctx.Self.GetPower<SlothPower>() ?? throw new InvalidOperationException("Missing native Sloth test power.");
            await ctx.Play(costed, ctx.PrimaryEnemy);
            Check(!rest.CanPlay(out _, out var preventer) && ReferenceEquals(preventer, sloth),
                "external native ShouldPlay restriction still prevents a rewritten card");
            await PowerCmd.Remove(sloth);
            await ctx.Play(rest);
            Check(!ctx.Self.HasPower<MaidenSuccubus.Powers.RestNextTurnPower>() && rest.Pile?.Type == PileType.Discard,
                "rewritten Rest executes empty program, no next-turn power or end-turn rule");

            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var dragonfly = await ctx.Add<DragonflyTouch>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(dragonfly);
                await ctx.ApplyPower<DexterityPower>(ctx.Self, 3);
                int targets = combat.HittableEnemies.Count;
                int blockBefore = ctx.Self.Block;
                await ctx.Play(dragonfly);
                Check(ctx.Self.Block - blockBefore == ((upgraded ? 20 : 14) + 3) * targets,
                    "enemy-count repetitions preserve native Dexterity per block operation");

                await ctx.Reset();
                var nimble = await ctx.Add<ForgeNimble>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(nimble);
                var unenchanted = await ctx.Add<MaidenDefend>(PileType.Hand);
                await ctx.Play(nimble);
                Check(ctx.Self.Block == (upgraded ? 16 : 10) && unenchanted.Enchantment == null,
                    "rewritten ForgeNimble gives doubled block without opening enchant selection");
            }

            await ctx.Reset();
            var skeleton = await ctx.Add<ExternalPowerSkeleton>(PileType.Discard);
            var ordinarySkeleton = await ctx.Add<ExternalPowerSkeleton>(PileType.Discard);
            var reaction = await ctx.Add<AutoReactionArmor>(PileType.Discard);
            var ordinaryReaction = await ctx.Add<AutoReactionArmor>(PileType.Discard);
            HumilityCardProfiles.ApplyKnown(skeleton);
            HumilityCardProfiles.ApplyKnown(reaction);
            var beforeFlushHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
            int beforeFlushBlock = ctx.Self.Block;
            await Hook.BeforeFlush(combat, player);
            Check(beforeFlushHp.All(pair => pair.Value - pair.Key.CurrentHp == 6)
                && ctx.Self.Block - beforeFlushBlock == 5,
                "native BeforeFlush autoplays only the untouched skeleton/armor copies");
            Check(!skeleton.Keywords.Contains(CardKeyword.Retain) && ordinarySkeleton.Keywords.Contains(CardKeyword.Retain)
                && !reaction.Keywords.Contains(CardKeyword.Retain) && ordinaryReaction.Keywords.Contains(CardKeyword.Retain),
                "intrinsic Retain removed only on rewritten discard-autoplay cards");

            foreach (bool upgraded in new[] { false, true })
            foreach (int mode in new[] { 0, 1, 2 })
            {
                await ctx.Reset();
                var arrow = await ctx.Add<LightArrow>(PileType.Hand, upgraded);
                if (mode > 0) HumilityCardProfiles.ApplyKnown(arrow);
                if (mode == 2)
                {
                    CardCmd.Enchant<Steady>(arrow, 1);
                    await ctx.ApplyPower<MaidenSuccubus.Powers.TacticalCorePower>(ctx.Self, 1);
                }
                var arrowProbe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                arrow.AddCapability(arrowProbe, allowMerge: false);
                await ctx.ApplyPower<MaidenSuccubus.Powers.MagicAmplificationPower>(ctx.Self, 1);
                Check(MaidenSuccubus.Core.Transformation.MagicAmplificationCardRules.HasIntrinsicDouble(arrow) == (mode == 0),
                    "only untouched LightArrow retains intrinsic double amplification marker");
                decimal expected = (upgraded ? 7 : 5) * (mode == 0 ? 2 : mode == 1 ? 3 : 4);
                int arrowBefore = ctx.PrimaryEnemy.CurrentHp;
                int arrowBlockBefore = ctx.Self.Block;
                if (mode > 0) Check(!Text(arrow).Contains("对这张牌的效果翻倍"), "removed marker rule absent from card text");
                await ctx.Play(arrow, ctx.PrimaryEnemy);
                Check(arrowBefore - ctx.PrimaryEnemy.CurrentHp == expected && ctx.Self.Block - arrowBlockBefore == expected,
                    "mixed damage/block keeps ordinary amplification and external TacticalCore, not erased intrinsic bonus");
                Check(arrowProbe.DelayedAmplificationSample == (mode == 1 ? 15m : 20m),
                    "delayed-value helper uses same intrinsic/external qualification during native reserved play");
                Check(!ctx.Self.HasPower<MaidenSuccubus.Powers.MagicAmplificationPower>(),
                    "native amplification consumption retained after mixed effect");
            }

            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var barrier = await ctx.Add<ReflectiveBarrier>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(barrier);
                await ctx.Play(barrier);
                Check(ctx.Self.Block == 16, "barrier keeps only on-play block, not deleted exhaust-trigger block");
                await CardCmd.Exhaust(choice, barrier);
                Check(ctx.Self.Block == 16 && !ctx.Self.HasPower<MaidenSuccubus.Powers.MagicAmplificationPower>(),
                    "exhausting rewritten barrier does not restore deleted block/amplification trigger");

                await ctx.Reset();
                var scythe = await ctx.Add<ThousandCurseScythe>(PileType.Hand, upgraded);
                scythe.CurrentDamage = 18;
                HumilityCardProfiles.ApplyKnown(scythe);
                int scytheBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(scythe, ctx.PrimaryEnemy);
                Check(scytheBefore - ctx.PrimaryEnemy.CurrentHp == 36 && scythe.CurrentDamage == 18,
                    "existing permanent growth is doubled only in effect program, stored source value untouched");
                await CardCmd.Exhaust(choice, scythe);
                Check(scythe.CurrentDamage == 18, "deleted exhaust growth does not change the stored value");

                await ctx.Reset();
                var shiningSword = await ctx.Add<ShiningSword>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(shiningSword);
                var swordProbe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                shiningSword.AddCapability(swordProbe, allowMerge: false);
                int swordBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(shiningSword, ctx.PrimaryEnemy);
                Check(swordBefore - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 24 : 16)
                    && swordProbe.BeforeCount == 1 && swordProbe.AfterCount == 1,
                    "upgraded ShiningSword keeps Repeat variable within one native attack");
            }

            await ctx.Reset();
            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var anger = await ctx.Add<Anger>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(anger);
                int angerBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(anger, ctx.PrimaryEnemy);
                Check(angerBefore - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 16 : 12)
                    && PileType.Discard.GetPile(player).Cards.Count == 1,
                    "rewritten native Anger deals damage without creating a copy");

                await ctx.Reset();
                var poisoned = await ctx.Add<PoisonedStab>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(poisoned);
                int poisonedBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(poisoned, ctx.PrimaryEnemy);
                Check(poisonedBefore - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 16 : 12)
                    && !ctx.PrimaryEnemy.HasPower<PoisonPower>(), "native attack damage remains but poison application is removed");

                await ctx.Reset();
                var backflip = await ctx.Add<Backflip>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(backflip);
                await ctx.AddFillerCards(PileType.Draw, 5);
                await ctx.Play(backflip);
                Check(ctx.Self.Block == (upgraded ? 16 : 10) && PileType.Hand.GetPile(player).Cards.Count == 0
                    && PileType.Draw.GetPile(player).Cards.Count == 5, "native block remains, draw removed");

                await ctx.Reset();
                var ironWave = await ctx.Add<IronWave>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(ironWave);
                var waveProbe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                ironWave.AddCapability(waveProbe, allowMerge: false);
                int waveBefore = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(ironWave, ctx.PrimaryEnemy);
                Check(waveBefore - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 14 : 10)
                    && waveProbe.BlockAtAttackStart == (upgraded ? 14 : 10), "native IronWave block resolves before attack");

                await ctx.Reset();
                var boomerang = await ctx.Add<SwordBoomerang>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(boomerang);
                int boomerangBefore = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
                await ctx.Play(boomerang);
                Check(boomerangBefore - ctx.Enemies.Sum(enemy => enemy.CurrentHp) == (upgraded ? 24 : 18),
                    "native upgrade increases random hit count, not the per-hit amount");

                await ctx.Reset();
                var adrenaline = await ctx.Add<Adrenaline>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(adrenaline);
                await ctx.AddFillerCards(PileType.Draw, 5);
                int energyBefore = player.PlayerCombatState!.Energy;
                await Pay(adrenaline);
                Check(player.PlayerCombatState.Energy == energyBefore && PileType.Hand.GetPile(player).Cards.Count == 0
                    && adrenaline.Pile?.Type == PileType.Discard, "empty native skill no longer grants energy/draw/exhaust");
            }

            await ctx.Reset();
            foreach (bool upgraded in new[] { false, true })
            {
                foreach (Type type in new[] { typeof(BallLightning), typeof(ColdSnap), typeof(MeteorStrike), typeof(Glacier) })
                {
                    await ctx.Reset();
                    var card = await ctx.Add(type, PileType.Hand, upgraded);
                    HumilityCardProfiles.ApplyKnown(card);
                    var orbsBefore = player.PlayerCombatState!.OrbQueue.Orbs.ToArray();
                    int hpBefore = ctx.PrimaryEnemy.CurrentHp;
                    await ctx.Play(card, card.Type == CardType.Attack ? ctx.PrimaryEnemy : null);
                    Check(player.PlayerCombatState.OrbQueue.Orbs.SequenceEqual(orbsBefore), "rewritten card does not channel or evoke: " + type.Name);
                    if (card.Type == CardType.Attack)
                        Check(hpBefore - ctx.PrimaryEnemy.CurrentHp == card.DynamicVars.Damage.BaseValue * 2, "orb attack retains doubled damage: " + type.Name);
                    else Check(ctx.Self.Block == (upgraded ? 18 : 12), "Glacier retains direct block only");
                }

                await ctx.Reset();
                var hologram = await ctx.Add<Hologram>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(hologram);
                await ctx.AddFillerCards(PileType.Discard, 3);
                await ctx.Play(hologram);
                Check(ctx.Self.Block == (upgraded ? 10 : 6) && PileType.Hand.GetPile(player).Cards.Count == 0
                    && PileType.Discard.GetPile(player).Cards.Count == 4, "Hologram gives block without selection or exhaust");

                await ctx.Reset();
                var battery = await ctx.Add<ChargeBattery>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(battery);
                await ctx.Play(battery);
                Check(ctx.Self.Block == (upgraded ? 20 : 14) && !ctx.Self.HasPower<EnergyNextTurnPower>(), "ChargeBattery removes next-turn energy");

                await ctx.Reset();
                var hyperbeam = await ctx.Add<Hyperbeam>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(hyperbeam);
                var beamHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
                await ctx.Play(hyperbeam);
                Check(beamHp.All(pair => pair.Value - pair.Key.CurrentHp == (upgraded ? 60 : 48))
                    && !ctx.Self.HasPower<HyperbeamFocusDownPower>(), "Hyperbeam retains all-enemy damage without focus loss");

                await ctx.Reset();
                var stack = await ctx.Add<Stack>(PileType.Hand, upgraded);
                HumilityCardProfiles.ApplyKnown(stack);
                await ctx.AddFillerCards(PileType.Discard, 2);
                await ctx.ApplyPower<DexterityPower>(ctx.Self, 3);
                Check(Text(stack).Contains($"{(upgraded ? 13 : 7)}点格挡"), "Stack preview reads live discard count and native Dexterity");
                await ctx.AddFillerCards(PileType.Discard, 3);
                Check(Text(stack).Contains($"{(upgraded ? 19 : 13)}点格挡"), "Stack preview updates after discard count changes");
                await ctx.Play(stack);
                Check(ctx.Self.Block == (upgraded ? 19 : 13), "Stack execution matches live preview before moving itself to discard");

                await ctx.Reset();
                var claw = await ctx.Add<Claw>(PileType.Hand, upgraded);
                var untouchedClaw = await ctx.Add<Claw>(PileType.Hand);
                claw.DynamicVars.Damage.BaseValue += 4;
                HumilityCardProfiles.ApplyKnown(claw);
                int clawHp = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(claw, ctx.PrimaryEnemy);
                Check(clawHp - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 16 : 14)
                    && untouchedClaw.DynamicVars.Damage.BaseValue == 3, "Claw preserves existing growth but does not grow other claws");
            }

            await ctx.Reset();
            int previousOrbCapacity = player.PlayerCombatState!.OrbQueue.Capacity;
            try
            {
                OrbCmd.RemoveSlots(player, previousOrbCapacity);
                await OrbCmd.AddSlots(player, 3);
                foreach (bool upgraded in new[] { false, true })
                {
                    OrbCmd.RemoveSlots(player, 3);
                    await OrbCmd.AddSlots(player, 3);
                    var barrage = await ctx.Add<Barrage>(PileType.Hand, upgraded);
                    HumilityCardProfiles.ApplyKnown(barrage);
                    int hpBefore = ctx.PrimaryEnemy.CurrentHp;
                    await ctx.Play(barrage, ctx.PrimaryEnemy);
                    Check(hpBefore == ctx.PrimaryEnemy.CurrentHp, "Barrage with zero orbs emits no attack");
                    for (int i = 0; i < 3; i++)
                        await OrbCmd.Channel<MegaCrit.Sts2.Core.Models.Orbs.FrostOrb>(choice, player);
                    await CardPileCmd.Add(barrage, PileType.Hand, skipVisuals: true);
                    var probe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                    barrage.AddCapability(probe, allowMerge: false);
                    hpBefore = ctx.PrimaryEnemy.CurrentHp;
                    await ctx.Play(barrage, ctx.PrimaryEnemy);
                    Check(hpBefore - ctx.PrimaryEnemy.CurrentHp == (upgraded ? 42 : 30)
                        && probe.BeforeCount == 1 && probe.AfterCount == 1, "Barrage live orb count retains one three-hit native command");
                }
            }
            finally
            {
                OrbCmd.RemoveSlots(player, player.PlayerCombatState.OrbQueue.Capacity);
                await OrbCmd.AddSlots(player, previousOrbCapacity);
            }

            await ctx.Reset();
            var bodySlam = await ctx.Add<BodySlam>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(bodySlam);
            await CreatureCmd.GainBlock(ctx.Self, 7, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null);
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 3);
            Check(Text(bodySlam).Contains("造成17点伤害"), "native calculated damage preview doubles current block before Strength");
            await CreatureCmd.GainBlock(ctx.Self, 5, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null);
            Check(Text(bodySlam).Contains("造成27点伤害"), "calculated damage is live, not frozen at rewrite time");
            int slamBefore = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(bodySlam, ctx.PrimaryEnemy);
            Check(slamBefore - ctx.PrimaryEnemy.CurrentHp == 27, "native calculated damage execution matches preview");

            await ctx.Reset();
            var shiv = await ctx.Add<Shiv>(PileType.Hand);
            Check(HumilityExtractedCards.Get(shiv).Program?.Effects.Single().Target == HumilityTarget.CurrentCardTarget,
                "generated split target branches retain live external targeting");
            HumilityExtractedCards.Apply(shiv);
            Check(!Text(shiv).Contains("所有敌人"), "initial rewritten Shiv remains single target");
            await ctx.ApplyPower<FanOfKnivesPower>(ctx.Self, 1);
            Check(Text(shiv).Contains("对所有敌人造成8点伤害"), "external FanOfKnives updates rewritten description");
            var shivHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
            await ctx.Play(shiv);
            Check(shivHp.All(pair => pair.Value - pair.Key.CurrentHp == 8), "external power changes rewritten Shiv to all enemies");
            await PowerCmd.Remove(ctx.Self.GetPower<FanOfKnivesPower>()!);
            await CardPileCmd.Add(shiv, PileType.Hand, skipVisuals: true);
            Check(!Text(shiv).Contains("所有敌人"), "removing external power restores single-target description");
            shivHp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
            await ctx.Play(shiv, ctx.PrimaryEnemy);
            Check(shivHp.All(pair => pair.Value - pair.Key.CurrentHp == (pair.Key == ctx.PrimaryEnemy ? 8 : 0)),
                "same rewritten instance returns to single-target damage after power removal");

            await ctx.Reset();
            var kick = await ctx.Add<KinglyKick>(PileType.Draw);
            HumilityCardProfiles.ApplyKnown(kick);
            int oldCost = kick.EnergyCost.GetWithModifiers(CostModifiers.Local);
            await CardPileCmd.Draw(choice, 1, player);
            Check(kick.EnergyCost.GetWithModifiers(CostModifiers.Local) == oldCost, "original AfterCardDrawn cost trigger suppressed");
            var ordinary = await ctx.Add<KinglyKick>(PileType.Draw);
            int ordinaryCost = ordinary.EnergyCost.GetWithModifiers(CostModifiers.Local);
            await CardPileCmd.Draw(choice, 1, player);
            Check(ordinary.EnergyCost.GetWithModifiers(CostModifiers.Local) == ordinaryCost - 1, "unmodified instance retains native draw trigger");

            await ctx.Reset();
            var modified = await ctx.Add<PommelStrike>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(modified);
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 3);
            await ctx.ApplyPower<VulnerablePower>(ctx.PrimaryEnemy, 1);
            string targeted = Regex.Replace(modified.GetDescriptionForPile(PileType.Hand, ctx.PrimaryEnemy), @"\[[^\]]*\]", "");
            Check(targeted.Contains("造成31点伤害"), "targeted preview applies Strength and Vulnerable after base doubling");
            int modifiedBefore = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(modified, ctx.PrimaryEnemy);
            Check(modifiedBefore - ctx.PrimaryEnemy.CurrentHp == 31, "actual native damage agrees with projected preview");

            await ctx.Reset();
            var goopy = await ctx.Add<DefendIronclad>(PileType.Hand);
            CardCmd.Enchant<Goopy>(goopy, 1);
            HumilityCardProfiles.ApplyKnown(goopy);
            Check(goopy.Keywords.Contains(CardKeyword.Exhaust), "Goopy exhaust keyword preserved");
            await ctx.Play(goopy);
            Check(goopy.Pile?.Type == PileType.Exhaust && goopy.Enchantment!.Amount == 2,
                "native enchanted exhaust and AfterCardPlayed remain active");

            await ctx.Reset();
            var shining = await ctx.Add<ShiningStrike>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(shining);
            int oldStars = player.PlayerCombatState!.Stars;
            await ctx.Play(shining, ctx.PrimaryEnemy);
            Check(shining.Pile?.Type == PileType.Discard && player.PlayerCombatState.Stars == oldStars,
                "derived original return-to-draw-pile and star effects removed");

            await ctx.Reset();
            var conditional = await ctx.Add<Uppercut>(PileType.Hand);
            conditional.AddKeyword(CardKeyword.Retain);
            conditional.BaseReplayCount = 2;
            HumilityCardProfiles.ApplyKnown(conditional);
            Check(!conditional.Keywords.Contains(CardKeyword.Retain) && conditional.BaseReplayCount == 0, "old keywords/intrinsic replay removed");
            await ctx.Play(conditional, ctx.PrimaryEnemy);
            Check(!ctx.PrimaryEnemy.Powers.OfType<WeakPower>().Any() && !ctx.PrimaryEnemy.Powers.OfType<VulnerablePower>().Any(), "original status effects removed");

            await ctx.Reset();
            var retained = await ctx.Add<StrikeIronclad>(PileType.Hand);
            CardCmd.Enchant<Steady>(retained, 1);
            HumilityCardProfiles.ApplyKnown(retained);
            Check(retained.Keywords.Contains(CardKeyword.Retain), "Steady's native retain survives without re-enchanting");
            HumilityRewriteCapability.Apply(retained, new([]));
            Check(HumilityRewriteCapability.Find(retained)!.Program.AmountMultiplier == 4, "second application scales existing program, ignores replacement input");
            var clone = (CardModel)retained.MutableClone();
            await CardPileCmd.Add(clone, PileType.Hand, skipVisuals: true);
            Check(HumilityRewriteCapability.Find(clone)?.Program.AmountMultiplier == 4, "native mutable clone retains independent rewrite capability");
            HumilityRewriteCapability.Apply(clone, new([]));
            Check(HumilityRewriteCapability.Find(retained)!.Program.AmountMultiplier == 4
                && HumilityRewriteCapability.Find(clone)!.Program.AmountMultiplier == 8, "clone rewrite independent of original");
            var restored = ModelCapabilityRegistry.Create<HumilityRewriteCapability>();
            restored.LoadState(HumilityRewriteCapability.Find(retained)!.SaveState(), 1);
            Check(restored.Program.Save().ToJsonString() == HumilityRewriteCapability.Find(retained)!.Program.Save().ToJsonString(), "framework capability JSON retains program");

            foreach (int x in new[] { 0, 1, 3 })
            foreach (bool chemical in new[] { false, true })
            {
                await ctx.Reset();
                foreach (var old in player.Relics.OfType<ChemicalX>().ToArray()) await RelicCmd.Remove(old);
                if (chemical) await RelicCmd.Obtain(ModelDb.Relic<ChemicalX>().ToMutable(), player);
                var whirlwind = await ctx.Add<Whirlwind>(PileType.Hand);
                HumilityCardProfiles.ApplyKnown(whirlwind);
                var probe = ModelCapabilityRegistry.Create<HumilityAttackProbeCapability>();
                whirlwind.AddCapability(probe, allowMerge: false);
                await PlayerCmd.SetEnergy(x, player);
                int effectiveX = Hook.ModifyXValue(combat, whirlwind, x);
                var hp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
                await Pay(whirlwind);
                Check(hp.All(pair => pair.Value - pair.Key.CurrentHp == 10 * effectiveX), "X + native ChemicalX modifier preserved for every enemy");
                Check(probe.BeforeCount == (effectiveX > 0 ? 1 : 0) && probe.AfterCount == probe.BeforeCount,
                    "multi-hit remains one native attack, zero X emits none");
            }
            foreach (var old in player.Relics.OfType<ChemicalX>().ToArray()) await RelicCmd.Remove(old);
            await ctx.Reset();
            var dualX = await ctx.Add<AllHopeLost>(PileType.Hand);
            HumilityCardProfiles.ApplyKnown(dualX);
            CardCmd.Enchant<Glam>(dualX, 1);
            await PlayerCmd.SetEnergy(3, player);
            await Data.Desire.Set(player, 4);
            int dualBefore = ctx.PrimaryEnemy.CurrentHp;
            await Pay(dualX);
            Check(dualBefore - ctx.PrimaryEnemy.CurrentHp == 12 * 4 * 3 * 2, "paid dual X and native replay use same resource values");
            Check(Data.Desire.Get(player) == 0 && player.PlayerCombatState!.Energy == 0, "native payments occur once");

            await ctx.Reset();
            var awakening = (HumilityRouteRelic)ModelDb.Relic<HumilityRouteRelic>().ToMutable();
            awakening.Stage = 3;
            await RelicCmd.Obtain(awakening, player);
            try
            {
                foreach (int stage in new[] { 2, 3, 4 })
                foreach (var scenario in new (Type Type, bool Rewrite, bool Swift, int OwnDraw, bool Pure)[]
                {
                    (typeof(StrikeIronclad), false, false, 0, true),
                    (typeof(DefendIronclad), false, false, 0, true),
                    (typeof(PommelStrike), false, false, 1, false),
                    (typeof(PommelStrike), true, false, 0, true),
                    (typeof(StrikeIronclad), false, true, 1, true),
                    (typeof(BladeDance), true, true, 1, false),
                    (typeof(Shiv), false, false, 0, false),
                    (typeof(Shiv), true, false, 0, true),
                })
                {
                    await ctx.Reset();
                    awakening.Stage = stage;
                    var card = await ctx.Add(scenario.Type, PileType.Hand);
                    if (scenario.Swift) CardCmd.Enchant<Swift>(card, 1);
                    if (scenario.Rewrite) HumilityExtractedCards.Apply(card);
                    Check(HumilityAwakening.IsPure(card) == scenario.Pure,
                        "original/rewritten/keyword/enchantment purity: " + scenario.Type.Name);
                    await ctx.AddFillerCards(PileType.Draw, 10);
                    await ctx.Play(card, card.TargetType == TargetType.AnyEnemy ? ctx.PrimaryEnemy : null);
                    int expected = scenario.OwnDraw + (stage >= 3 && scenario.Pure ? 2 : 0);
                    Check(PileType.Hand.GetPile(player).Cards.Count == expected,
                        "actual awakening relic hook draws two only for qualifying cards at active stage");
                }
            }
            finally { await RelicCmd.Remove(awakening); }
            Check(HumilityRewriteCapability.Find(ModelDb.Card<StrikeIronclad>()) == null, "canonical card never rewritten");
            MaidenSuccubusMod.Logger.Info($"[DS27HumilityRuntimeTest] PASS {checks} assertions; runtime layer only, selector adapters still pending.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27HumilityRuntimeTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}

[RegisterModelCapability(StableEntryStem = "humility_test_attack_probe")]
public sealed class HumilityAttackProbeCapability : CardCapability
{
    internal int BeforeCount { get; private set; }
    internal int AfterCount { get; private set; }
    internal decimal? DelayedAmplificationSample { get; private set; }
    internal int? BlockAtAttackStart { get; private set; }
    public override Task BeforeAttack(AttackCommand command)
    {
        if (Owner != null && command.CardPlay?.Card == Owner)
        {
            BeforeCount++;
            BlockAtAttackStart = Owner.Owner.Creature.Block;
            DelayedAmplificationSample = MaidenSuccubus.Core.Transformation.TransformationCmd
                .ApplyAmplificationToDelayedValue(Owner.Owner.Creature, Owner, 10);
        }
        return Task.CompletedTask;
    }
    public override Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        if (command.CardPlay?.Card == Owner) AfterCount++;
        return Task.CompletedTask;
    }
}
#endif
