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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.ConsoleCommands;

// Uses the production reviewed profiles. It does NOT claim that the production
// HumilityLesson selector has all card adapters yet.
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
            Check(!HumilityCardProfiles.TryGet(ctx.Create<Surf>(), out _), "pending Surf has no guessed profile");
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
    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.CardPlay?.Card == Owner) BeforeCount++;
        return Task.CompletedTask;
    }
    public override Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        if (command.CardPlay?.Card == Owner) AfterCount++;
        return Task.CompletedTask;
    }
}
#endif
