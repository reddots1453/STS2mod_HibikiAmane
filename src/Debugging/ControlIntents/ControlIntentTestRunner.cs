#if DEBUG
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.ControlIntents;

internal static class ControlIntentTestRunner
{
    private const int ScenarioTimeoutSeconds = 30;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed record Scenario(
        string Name,
        string Requirement,
        Func<ControlIntentTestContext, Task> Execute);

    private static readonly IReadOnlyList<Scenario> Scenarios =
    [
        new("lifecycle_threshold_dispatch", "SYS-DES-INTENT-001", LifecycleThresholdDispatch),
        new("forced_intent_ignores_natural_cooldown", "SYS-DES-INTENT-001", ForcedIntentIgnoresNaturalCooldown),
        new("natural_consecutive_limit_and_saved_state", "SYS-DES-INTENT-001", NaturalConsecutiveLimitAndSavedState),
        new("default_invasion_curse", "SYS-INV-001", DefaultInvasionCurse),
        new("desire_intent_visual_deduplication", "SYS-DES-INTENT-001", DesireIntentVisualDeduplication),
        new("intent_metadata_and_exact_block", "SYS-CTL-001", IntentMetadataAndExactBlock),
        new("insufficient_block_stress_projection", "SYS-CTL-001", InsufficientBlockStressProjection),
        new("high_desire_bypasses_block", "SYS-DES-002B", HighDesireBypassesBlock),
        new("control_type_projection_matrix", "SYS-CTL-001", ControlTypeProjectionMatrix),
        new("original_state_and_paid_escape", "SYS-CTL-001", OriginalStateAndPaidEscape),
        new("zero_cost_escape_is_zero", "SYS-CTL-001", ZeroCostEscapeIsZero),
        new("multi_source_priority_and_no_overflow", "SYS-CTL-001", MultiSourcePriorityAndNoOverflow),
        new("source_death_releases_and_rebinds", "SYS-CTL-001", SourceDeathReleasesAndRebinds),
        new("catalog_intent_to_recovery", "SYS-CTL-001/002", CatalogIntentToRecovery),
    ];

    private static async Task LifecycleThresholdDispatch(ControlIntentTestContext ctx)
    {
        // Use the actual game dispatchers, not direct calls to the character
        // override or TryApplyNaturalErotic: those hid the missing subscription.
        ctx.AssertEqual("character subscribed exactly once", 1,
            ctx.Combat.IterateHookListeners().Count(model =>
                ReferenceEquals(model, ctx.Player.Character)));
        ctx.AssertEqual("run/combat dispatch does not duplicate character", 1,
            ctx.Player.RunState.IterateHookListeners(ctx.Combat).Count(model =>
                ReferenceEquals(model, ctx.Player.Character)));
        await MegaCrit.Sts2.Core.Hooks.Hook.BeforeCombatStart(
            ctx.Player.RunState, ctx.Combat);
        ctx.AssertTrue("combat-start creates temptation carrier",
            ctx.Self.HasPower<TemptationRuntimePower>());

        Creature enemy = await ctx.AddByrdonis();
        ctx.AssertEqual("summoned monster desire threshold", 25m,
            enemy.GetPower<DesireIntentThresholdPower>()?.Amount ?? -1m);
        ctx.AssertEqual("summoned monster control threshold", 40m,
            enemy.GetPower<ControlIntentThresholdPower>()?.Amount ?? -1m);
        var monster = enemy.Monster!;
        var originalMove = monster.NextMove;
        var choice = new BlockingPlayerChoiceContext();
        await Core.Temptation.Temptation.Modify(choice, ctx.Player,
            24 - Core.Temptation.Temptation.Get(ctx.Player));
        await MegaCrit.Sts2.Core.Hooks.Hook.AfterPlayerTurnStart(
            ctx.Combat, choice, ctx.Player);
        ctx.AssertReference("below threshold preserves original intent",
            originalMove, monster.NextMove);
        await Core.Temptation.Temptation.Modify(choice, ctx.Player, 1);
        ctx.AssertReference("mid-turn threshold crossing does not replace intent",
            originalMove, monster.NextMove);
        await MegaCrit.Sts2.Core.Hooks.Hook.AfterPlayerTurnStart(
            ctx.Combat, choice, ctx.Player);
        ctx.AssertTrue("exact threshold selects desire at turn start",
            monster.NextMove.StateId.StartsWith("MAIDENSUCCUBUS_DESIRE",
                StringComparison.Ordinal));
        ctx.AssertEqual("selection consumes exactly one use", 1,
            IntentAdapterRegistry.GetRuntime(monster).DesireIntentUses);
        await MegaCrit.Sts2.Core.Hooks.Hook.AfterPlayerTurnStart(
            ctx.Combat, choice, ctx.Player);
        ctx.AssertEqual("existing erotic intent is not selected twice", 1,
            IntentAdapterRegistry.GetRuntime(monster).DesireIntentUses);
    }

    private static async Task DesireIntentVisualDeduplication(
        ControlIntentTestContext ctx)
    {
        MonsterModel monster = ctx.PrimaryEnemy.Monster!;
        MoveState delayedHazard = IntentMoveFactory.CreateDesire(
            monster,
            new DesireIntentSpec(
                Desire: 1,
                Damage: 3,
                EffectText: "造成3点伤害，欲望增加1，将1张溶解液置入弃牌堆。"));
        ctx.AssertEqual("delayed hazard has only damage and desire icons", 2,
            delayedHazard.Intents.Count);
        ctx.AssertEqual("delayed hazard has one desire icon", 1,
            delayedHazard.Intents.Count(intent => intent is DesireGainIntent));
        ctx.AssertEqual("delayed hazard hides clothing status icon", 0,
            delayedHazard.Intents.Count(intent => intent is ClothingHazardIntent));

        MoveState directTear = IntentMoveFactory.CreateDesire(
            monster,
            new DesireIntentSpec(
                Desire: 1,
                Damage: 3,
                EffectText: "造成3点伤害，欲望增加1，撕裂衣服。"));
        ctx.AssertEqual("direct tear has damage, desire and tear icons", 3,
            directTear.Intents.Count);
        ctx.AssertEqual("direct tear has one tear icon", 1,
            directTear.Intents.Count(intent => intent is TearClothingIntent));
        await Task.Yield();
    }

    private static async Task ForcedIntentIgnoresNaturalCooldown(
        ControlIntentTestContext ctx)
    {
        var choice = new BlockingPlayerChoiceContext();
        await Core.Temptation.Temptation.Modify(choice, ctx.Player,
            100 - Core.Temptation.Temptation.Get(ctx.Player));

        MonsterModel desireMonster = (await ctx.AddByrdonis()).Monster!;
        EroticMonsterSpec desireSpec = EroticAttackCatalog.Get(desireMonster)!;
        IntentRuntimeState desireState = IntentAdapterRegistry.GetRuntime(desireMonster);
        desireState.ControlIntentUses = desireSpec.Control!.MaxUsesPerCombat;
        desireState.DesireCooldownThroughTurn = int.MaxValue;
        ctx.AssertTrue("natural desire respects cooldown",
            !IntentMoveFactory.TryApplyNaturalErotic(desireMonster, ctx.Player));
        ctx.AssertTrue("forced desire bypasses natural cooldown",
            IntentMoveFactory.TryForceErotic(desireMonster, ctx.Player));
        ctx.AssertTrue("forced move is desire",
            desireMonster.NextMove.StateId.StartsWith("MAIDENSUCCUBUS_DESIRE", StringComparison.Ordinal));
        ctx.AssertEqual("forced desire still consumes one total use", 1,
            desireState.DesireIntentUses);

        MonsterModel controlMonster = (await ctx.AddByrdonis()).Monster!;
        EroticMonsterSpec controlSpec = EroticAttackCatalog.Get(controlMonster)!;
        IntentRuntimeState controlState = IntentAdapterRegistry.GetRuntime(controlMonster);
        controlState.DesireIntentUses = controlSpec.Desire!.MaxUsesPerCombat;
        controlState.ControlCooldownThroughTurn = int.MaxValue;
        ctx.AssertTrue("natural control respects cooldown",
            !IntentMoveFactory.TryApplyNaturalErotic(controlMonster, ctx.Player));
        ctx.AssertTrue("forced control bypasses natural cooldown",
            IntentMoveFactory.TryForceControl(controlMonster, ctx.Player, 3));
        ctx.AssertEqual("forced control still consumes one total use", 1,
            controlState.ControlIntentUses);
        controlState.ControlIntentUses = controlSpec.Control!.MaxUsesPerCombat;
        ctx.AssertTrue("forced control still respects total-use cap",
            !IntentMoveFactory.TryForceControl(controlMonster, ctx.Player, 3));
    }

    private static async Task NaturalConsecutiveLimitAndSavedState(
        ControlIntentTestContext ctx)
    {
        var choice = new BlockingPlayerChoiceContext();
        await Core.Temptation.Temptation.Modify(choice, ctx.Player,
            100 - Core.Temptation.Temptation.Get(ctx.Player));
        Creature enemy = await ctx.AddByrdonis();
        MonsterModel monster = enemy.Monster!;
        await IntentAdapterRegistry.Initialize(monster);
        MoveState original = monster.NextMove;
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(monster);
        int originalRound = ctx.Combat.RoundNumber;
        try
        {
            runtime.LastEroticSelectionRound = originalRound - 1;
            runtime.ConsecutiveEroticSelectionRounds = 2;
            ctx.AssertTrue("third consecutive natural intent is blocked",
                !IntentMoveFactory.TryApplyNaturalErotic(monster, ctx.Player));
            ctx.AssertReference("blocked natural leaves original move", original,
                monster.NextMove);
            ctx.AssertTrue("forced intent bypasses consecutive cap",
                IntentMoveFactory.TryForceControl(monster, ctx.Player, 3));
            ctx.AssertEqual("forced intent still consumes total use", 1,
                runtime.ControlIntentUses);
            ctx.AssertEqual("same-round selection capped at two", 2,
                runtime.ConsecutiveEroticSelectionRounds);
            ctx.AssertEqual("forced round saved on carrier", originalRound,
                enemy.GetPower<EroticIntentRuntimePower>()?.LastEroticSelectionRound ?? -1);

            IntentAdapterRegistry.ResetRuntimeForTests(monster);
            runtime = IntentAdapterRegistry.GetRuntime(monster);
            ctx.AssertEqual("consecutive count restored from carrier", 2,
                runtime.ConsecutiveEroticSelectionRounds);
            monster.SetMoveImmediate(original, forceTransition: true);
            ctx.Combat.RoundNumber = originalRound + 1;
            ctx.AssertTrue("natural still blocked after forced round",
                !IntentMoveFactory.TryApplyNaturalErotic(monster, ctx.Player));
            ctx.Combat.RoundNumber = originalRound + 2;
            ctx.AssertTrue("natural resumes after a non-erotic round",
                IntentMoveFactory.TryApplyNaturalErotic(monster, ctx.Player));
            ctx.AssertEqual("gap resets consecutive count", 1,
                runtime.ConsecutiveEroticSelectionRounds);
        }
        finally
        {
            ctx.Combat.RoundNumber = originalRound;
        }
    }

    private static async Task DefaultInvasionCurse(
        ControlIntentTestContext ctx)
    {
        MonsterModel monster = (await ctx.AddByrdonis()).Monster!;
        HashSet<CardModel> deckBefore = ctx.Player.Deck.Cards.ToHashSet();
        bool applied = await InvasionCmd.Resolve(
            new BlockingPlayerChoiceContext(),
            monster,
            ctx.Player,
            new InvasionIntentSpec(1),
            deferCompletion: true);
        ctx.AssertTrue("default invasion resolves", applied);
        ctx.AssertEqual("default invasion adds one curse", deckBefore.Count + 1,
            ctx.Player.Deck.Cards.Count);
        SemenCurse curse = ctx.Player.Deck.Cards
            .Where(card => !deckBefore.Contains(card))
            .OfType<SemenCurse>()
            .Single();
        ctx.AssertEqual("default curse keeps source monster", monster.Id.Entry,
            curse.SourceMonsterId);
    }

    public static async Task<string> Run(Player player)
    {
        if (!await Gate.WaitAsync(0))
            return "Control-intent test suite is already running.";

        try
        {
            var combat = MegaCrit.Sts2.Core.Combat.CombatManager.Instance
                .DebugOnlyGetState();
            if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress
                || combat == null)
            {
                return "Start a disposable combat before running control-intent tests.";
            }
            if (combat.Players.Count != 1)
                return "Control-intent tests require a single-player disposable combat.";
            if (player.Character.GetType().Name != "MaidenSuccubusCharacter")
                return "Use the MaidenSuccubus character for this test suite.";

            ControlIntentTestReport report = new()
            {
                StartedAt = DateTimeOffset.Now,
            };
            ControlIntentTestContext context = new(combat, player);
            await context.PrepareSuite();

            bool timedOut = false;
            for (int scenarioIndex = 0;
                scenarioIndex < Scenarios.Count;
                scenarioIndex++)
            {
                Scenario scenario = Scenarios[scenarioIndex];
                ControlIntentScenarioResult result = new()
                {
                    Name = scenario.Name,
                    Requirement = scenario.Requirement,
                };
                report.Scenarios.Add(result);
                context.BeginScenario(result);
                Stopwatch stopwatch = Stopwatch.StartNew();
                MaidenSuccubusMod.Logger.Info(
                    $"[ControlIntentTest] BEGIN {scenario.Name} ({scenario.Requirement})");

                try
                {
                    await context.Reset();
                    context.Checkpoint("fixture reset complete");
                    await scenario.Execute(context).WaitAsync(
                        TimeSpan.FromSeconds(ScenarioTimeoutSeconds));
                    result.Passed = result.Assertions.Count > 0
                        && result.Assertions.All(assertion => assertion.Passed);
                    if (result.Assertions.Count == 0)
                        result.Error = "Scenario produced no assertions.";
                }
                catch (TimeoutException ex)
                {
                    timedOut = true;
                    result.Passed = false;
                    result.Error =
                        $"Timed out after {ScenarioTimeoutSeconds}s at "
                        + $"{result.LastCheckpoint ?? "<no checkpoint>"}. "
                        + ex;
                }
                catch (Exception ex)
                {
                    result.Passed = false;
                    result.Error = ex.ToString();
                }
                finally
                {
                    stopwatch.Stop();
                    result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                }

                string outcome = result.Passed ? "PASS" : "FAIL";
                MaidenSuccubusMod.Logger.Info(
                    $"[ControlIntentTest] {outcome} {scenario.Name} "
                    + $"assertions={result.Assertions.Count} "
                    + $"elapsedMs={result.ElapsedMilliseconds} "
                    + $"last={result.LastCheckpoint ?? "<none>"}");

                if (timedOut)
                {
                    for (int skippedIndex = scenarioIndex + 1;
                        skippedIndex < Scenarios.Count;
                        skippedIndex++)
                    {
                        Scenario skipped = Scenarios[skippedIndex];
                        report.Scenarios.Add(new ControlIntentScenarioResult
                        {
                            Name = skipped.Name,
                            Requirement = skipped.Requirement,
                            Passed = false,
                            Error = "Not run because a previous scenario timed out.",
                        });
                    }
                    break;
                }
            }

            try
            {
                if (!timedOut)
                    await context.Finish();
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Error(
                    "[ControlIntentTest] Final fixture cleanup failed: " + ex);
            }

            report.FinishedAt = DateTimeOffset.Now;
            string reportPath = await WriteReport(report);
            string summary =
                $"Control intents: {report.Passed} passed, {report.Failed} failed. "
                + $"Report: {reportPath}";
            if (report.Success)
                MaidenSuccubusMod.Logger.Info("[ControlIntentTest] " + summary);
            else
                MaidenSuccubusMod.Logger.Error("[ControlIntentTest] " + summary);
            return summary;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task IntentMetadataAndExactBlock(
        ControlIntentTestContext context)
    {
        Creature enemy = context.PrimaryEnemy;
        ControlIntentSpec spec = new(
            BlockRequired: 5,
            ControlType.Skill,
            EscapeRequired: 3,
            DisplayName: "自动测试拘束");
        IntentMoveFactory.SetTransient(
            enemy.Monster!,
            IntentMoveFactory.CreateControl(enemy.Monster!, spec));
        ControlIntent intent = enemy.Monster!.NextMove.Intents
            .OfType<ControlIntent>().Single();
        context.AssertEqual("stable transient state id",
            "MAIDENSUCCUBUS_CONTROL", enemy.Monster.NextMove.StateId);
        context.AssertEqual("displayed block requirement", 5, intent.BlockRequired);
        context.AssertEqual("displayed escape requirement", 3, intent.EscapeRequired);
        context.AssertEqual("displayed control type", ControlType.Skill, intent.ControlType);

        await CreatureCmd.GainBlock(
            context.Self, 5, ValueProp.Unpowered, null, fast: true);
        int hpBefore = context.Self.CurrentHp;
        context.Checkpoint("before exact-block MonsterModel.PerformMove");
        await enemy.Monster.PerformMove();
        context.Checkpoint("after exact-block MonsterModel.PerformMove");
        context.AssertEqual("exact block is consumed", 0, context.Self.Block);
        context.AssertEqual("control does not deal hp damage",
            hpBefore, context.Self.CurrentHp);
        context.AssertNoControl("exact block prevents control");
    }

    private static async Task InsufficientBlockStressProjection(
        ControlIntentTestContext context)
    {
        List<CardModel> cards = [];
        foreach (PileType pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            for (int index = 0; index < 6; index++)
            {
                cards.Add(await context.AddCard<StrikeIronclad>(pile));
                cards.Add(await context.AddCard<DefendIronclad>(pile));
                cards.Add(await context.AddCard<MagicResonance>(pile));
            }
            cards.Add(await context.AddCard<ResistanceGloves>(pile));
            cards.Add(await context.AddCard<RegenerativeMagicFiber>(pile));
        }

        Creature enemy = context.PrimaryEnemy;
        ControlIntentSpec spec = new(5, ControlType.Skill, 3);
        IntentMoveFactory.SetTransient(
            enemy.Monster!,
            IntentMoveFactory.CreateControl(enemy.Monster!, spec));
        await CreatureCmd.GainBlock(
            context.Self, 4, ValueProp.Unpowered, null, fast: true);
        int hpBefore = context.Self.CurrentHp;
        context.Checkpoint(
            $"before insufficient-block MonsterModel.PerformMove cards={cards.Count}");
        await enemy.Monster!.PerformMove();
        context.Checkpoint("after insufficient-block MonsterModel.PerformMove");

        ControlPower control = ControlQuery.GetInstances(context.Player).Single();
        context.AssertEqual("insufficient block remains untouched", 4, context.Self.Block);
        context.AssertEqual("control application does not deal hp damage",
            hpBefore, context.Self.CurrentHp);
        context.AssertEqual("applied escape amount", 3, control.Amount);
        context.AssertEqual("applied control type", ControlType.Skill, control.ControlType);
        int projected = cards.Count(card => ControlQuery.GetProjection(card) != null);
        int expectedProjected = cards.Count(card =>
            card.Type == CardType.Skill && card is not ResistanceGloves);
        context.AssertEqual("only non-portable skills are projected",
            expectedProjected, projected);
        context.AssertTrue("portable skill is never projected",
            cards.OfType<ResistanceGloves>()
                .All(card => ControlQuery.GetProjection(card) == null));
        context.AssertTrue("attack and power cards remain original",
            cards.Where(card => card.Type != CardType.Skill)
                .All(card => ControlQuery.GetProjection(card) == null));

        foreach (CardModel card in cards.Where(card =>
            ControlQuery.GetProjection(card) != null))
        {
            for (int iteration = 0; iteration < 8; iteration++)
            {
                _ = card.Title;
                _ = card.GetDescriptionForPile(card.Pile!.Type);
                _ = card.Keywords;
                _ = card.Enchantment;
                _ = card.Affliction;
                _ = card.TargetType;
            }
        }
        context.Checkpoint("repeated projection reads complete");
        await ControlCmd.Release(
            new BlockingPlayerChoiceContext(), context.Self);
        context.AssertTrue("all capabilities detach after direct release",
            cards.All(card => ControlQuery.GetProjection(card) == null));
    }

    private static async Task HighDesireBypassesBlock(
        ControlIntentTestContext context)
    {
        await Desire.Set(context.Player, 8);
        await CreatureCmd.GainBlock(
            context.Self, 99, ValueProp.Unpowered, null, fast: true);
        Creature enemy = context.PrimaryEnemy;
        IntentMoveFactory.SetTransient(
            enemy.Monster!,
            IntentMoveFactory.CreateControl(
                enemy.Monster!,
                new ControlIntentSpec(5, ControlType.Attack, 4)));
        context.Checkpoint("before high-desire MonsterModel.PerformMove");
        await enemy.Monster!.PerformMove();
        context.Checkpoint("after high-desire MonsterModel.PerformMove");

        ControlPower control = ControlQuery.GetInstances(context.Player).Single();
        context.AssertEqual("eight desire bypasses sufficient block", 4, control.Amount);
        context.AssertEqual("bypassed block is not consumed", 99, context.Self.Block);
        context.AssertEqual("bypassed control type", ControlType.Attack,
            control.ControlType);
    }

    private static async Task ControlTypeProjectionMatrix(
        ControlIntentTestContext context)
    {
        StrikeIronclad attack =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        DefendIronclad skill =
            await context.AddCard<DefendIronclad>(PileType.Draw);
        MagicResonance power =
            await context.AddCard<MagicResonance>(PileType.Discard);
        ResistanceGloves portableSkill =
            await context.AddCard<ResistanceGloves>(PileType.Hand);
        RegenerativeMagicFiber portablePower =
            await context.AddCard<RegenerativeMagicFiber>(PileType.Draw);
        CardModel[] all = [attack, skill, power, portableSkill, portablePower];

        foreach ((ControlType type, CardModel expected) in new[]
        {
            (ControlType.Attack, (CardModel)attack),
            (ControlType.Skill, (CardModel)skill),
            (ControlType.Power, (CardModel)power),
        })
        {
            ControlPower control = await context.ApplyControl(
                context.PrimaryEnemy, type, 3);
            context.AssertEqual($"{type} projects exactly one fixture",
                1, all.Count(card => ControlQuery.GetProjection(card) != null));
            context.AssertTrue($"{type} projects matching original instance",
                ControlQuery.GetProjection(expected) is { } projection
                && ReferenceEquals(projection.Control, control)
                && ReferenceEquals(projection.OriginalCard, expected));
            context.AssertTrue($"{type} leaves portable cards original",
                ControlQuery.GetProjection(portableSkill) == null
                && ControlQuery.GetProjection(portablePower) == null);
            await ControlCmd.Release(
                new BlockingPlayerChoiceContext(), context.Self);
            context.AssertTrue($"{type} restores every fixture",
                all.All(card => ControlQuery.GetProjection(card) == null));
        }
    }

    private static async Task OriginalStateAndPaidEscape(
        ControlIntentTestContext context)
    {
        DefendIronclad card = await context.AddCard<DefendIronclad>(
            PileType.Hand, upgraded: true);
        card.AddKeyword(CardKeyword.Retain);
        Glam enchantment = (Glam)ModelDb.Enchantment<Glam>().ToMutable();
        card.EnchantInternal(enchantment, 1);
        Bound affliction = (Bound)ModelDb.Affliction<Bound>().ToMutable();
        card.AfflictInternal(affliction, 1);
        affliction.AfterApplied();
        card.EnergyCost.SetThisCombat(2);

        string originalTitle = card.Title;
        TargetType originalTarget = card.TargetType;
        ControlPower control = await context.ApplyControl(
            context.PrimaryEnemy, ControlType.Skill, 5);
        EscapeProjection projection = ControlQuery.GetProjection(card)
            ?? throw new InvalidOperationException("Expected an escape projection.");
        context.AssertTrue("projection retains original card reference",
            ReferenceEquals(card, projection.OriginalCard));
        context.AssertEqual(
            "projection title",
            new LocString("cards", "MAIDENSUCCUBUS_ESCAPE.title")
                .GetFormattedText(),
            card.Title);
        context.AssertEqual("projection target is none", TargetType.None,
            card.TargetType);
        context.AssertEqual("projection hides original keywords", 0,
            card.Keywords.Count);
        context.AssertTrue("projection hides original enchantment",
            card.Enchantment == null);
        context.AssertTrue("projection hides original affliction",
            card.Affliction == null);
        string description = card.GetDescriptionForPile(PileType.Hand);
        context.AssertTrue("projection description includes paid escape value",
            description.Contains("2", StringComparison.Ordinal));
        context.AssertTrue("projection description names original card",
            description.Contains(originalTitle, StringComparison.Ordinal));
        var serialized = card.ToSerializable();
        context.AssertEqual("serialization keeps upgrade level", 1,
            serialized.CurrentUpgradeLevel);
        context.AssertTrue("serialization keeps original enchantment",
            serialized.Enchantment != null);

        int blockBefore = context.Self.Block;
        context.Checkpoint("before paid projected card play");
        int energySpent = await context.PlayPayingResources(card);
        context.Checkpoint("after paid projected card play");
        context.AssertEqual("fixture paid two energy", 2, energySpent);
        context.AssertEqual("original defend effect is suppressed",
            blockBefore, context.Self.Block);
        context.AssertEqual("escape reduces exact paid amount",
            5 - energySpent, control.Amount);
        context.AssertEqual("projected card resolves to discard",
            PileType.Discard, card.Pile?.Type ?? PileType.None);

        await PowerCmd.Remove(control);
        context.AssertTrue("same card instance remains in discard",
            PileType.Discard.GetPile(context.Player).Cards
                .Any(candidate => ReferenceEquals(candidate, card)));
        context.AssertEqual("original title restores", originalTitle, card.Title);
        context.AssertEqual("original target restores", originalTarget,
            card.TargetType);
        context.AssertTrue("original retain restores",
            card.Keywords.Contains(CardKeyword.Retain));
        context.AssertTrue("original enchantment restores",
            card.Enchantment is Glam);
        context.AssertTrue("original affliction restores",
            card.Affliction is Bound);
    }

    private static async Task ZeroCostEscapeIsZero(
        ControlIntentTestContext context)
    {
        DefendIronclad card =
            await context.AddCard<DefendIronclad>(PileType.Hand);
        card.EnergyCost.SetThisCombat(0);
        ControlPower control = await context.ApplyControl(
            context.PrimaryEnemy, ControlType.Skill, 3);
        int blockBefore = context.Self.Block;
        int energyBefore = context.Player.PlayerCombatState!.Energy;
        int spent = await context.PlayPayingResources(card);
        context.AssertEqual("zero-cost card spends zero energy", 0, spent);
        context.AssertEqual("player energy remains unchanged",
            energyBefore, context.Player.PlayerCombatState!.Energy);
        context.AssertEqual("zero-cost escape removes zero control", 3,
            control.Amount);
        context.AssertEqual("zero-cost projected original effect is suppressed",
            blockBefore, context.Self.Block);
        context.AssertEqual("zero-cost projection still resolves to discard",
            PileType.Discard, card.Pile?.Type ?? PileType.None);
    }

    private static async Task MultiSourcePriorityAndNoOverflow(
        ControlIntentTestContext context)
    {
        Creature left = await context.AddByrdonis();
        Creature right = await context.AddByrdonis();
        ControlPower leftControl = await context.ApplyControl(
            left, ControlType.Attack, 4);
        ControlPower rightControl = await context.ApplyControl(
            right, ControlType.Attack, 2);
        StrikeIronclad lowerCard =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        context.AssertTrue("lower remaining source is selected",
            ReferenceEquals(
                ControlQuery.GetProjection(lowerCard)?.Control,
                rightControl));
        await context.PlayPayingResources(lowerCard);
        context.AssertEqual("higher source is untouched", 4, leftControl.Amount);
        context.AssertEqual("lower source loses one", 1, rightControl.Amount);

        await ControlCmd.Release(
            new BlockingPlayerChoiceContext(), context.Self);
        leftControl = await context.ApplyControl(left, ControlType.Attack, 2);
        rightControl = await context.ApplyControl(right, ControlType.Attack, 2);
        StrikeIronclad tieCard =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        context.AssertTrue("left source wins equal remaining tie",
            ReferenceEquals(
                ControlQuery.GetProjection(tieCard)?.Control,
                leftControl));
        await context.PlayPayingResources(tieCard);
        context.AssertEqual("left tied source loses one", 1, leftControl.Amount);
        context.AssertEqual("right tied source is untouched", 2,
            rightControl.Amount);

        await ControlCmd.Release(
            new BlockingPlayerChoiceContext(), context.Self);
        leftControl = await context.ApplyControl(left, ControlType.Attack, 1);
        rightControl = await context.ApplyControl(right, ControlType.Attack, 2);
        StrikeIronclad overflowCard =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        overflowCard.EnergyCost.SetThisCombat(3);
        int spent = await context.PlayPayingResources(overflowCard);
        context.AssertEqual("overflow fixture spends three", 3, spent);
        context.AssertTrue("selected one-point source is removed",
            !ControlQuery.GetInstances(context.Player).Contains(leftControl));
        context.AssertEqual("overflow does not transfer to another source",
            2, rightControl.Amount);
    }

    private static async Task SourceDeathReleasesAndRebinds(
        ControlIntentTestContext context)
    {
        Creature first = await context.AddByrdonis();
        Creature second = await context.AddByrdonis();
        ControlPower firstControl = await context.ApplyControl(
            first, ControlType.Skill, 2);
        ControlPower secondControl = await context.ApplyControl(
            second, ControlType.Skill, 4);
        DefendIronclad card =
            await context.AddCard<DefendIronclad>(PileType.Hand);
        context.AssertTrue("card initially binds lower source",
            ReferenceEquals(
                ControlQuery.GetProjection(card)?.Control,
                firstControl));

        context.Checkpoint("before killing first control source");
        await CreatureCmd.Kill(first, force: true);
        context.Checkpoint("after killing first control source");
        context.AssertTrue("dead source control is removed",
            !ControlQuery.GetInstances(context.Player).Contains(firstControl));
        context.AssertTrue("card rebinds surviving source",
            ReferenceEquals(
                ControlQuery.GetProjection(card)?.Control,
                secondControl));

        await CreatureCmd.Kill(second, force: true);
        context.AssertNoControl("all source deaths clear control");
        context.AssertTrue("card restores when no source survives",
            ControlQuery.GetProjection(card) == null);
    }

    private static async Task CatalogIntentToRecovery(
        ControlIntentTestContext context)
    {
        Creature source = await context.AddByrdonis();
        MoveState original = source.Monster!.NextMove;
        bool forced = IntentMoveFactory.TryForceControl(
            source.Monster!, context.Player, escape: 3);
        context.AssertTrue("catalog control can be forced", forced);
        ControlIntent intent = source.Monster!.NextMove.Intents
            .OfType<ControlIntent>().Single();
        context.AssertEqual("Byrdonis block requirement", 12,
            intent.BlockRequired);
        context.AssertEqual("Byrdonis control type", ControlType.Attack,
            intent.ControlType);
        context.AssertEqual("Byrdonis escape requirement", 3,
            intent.EscapeRequired);

        context.Checkpoint("before catalog MonsterModel.PerformMove");
        await source.Monster.PerformMove();
        context.Checkpoint("after catalog MonsterModel.PerformMove");
        ControlPower control = ControlQuery.GetInstances(context.Player).Single();
        context.AssertTrue("catalog control records source creature",
            ReferenceEquals(source, control.Applier));
        context.AssertEqual("catalog control applies exact amount", 3,
            control.Amount);

        StrikeIronclad twoCost =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        twoCost.EnergyCost.SetThisCombat(2);
        int hpBefore = source.CurrentHp;
        await context.PlayPayingResources(twoCost);
        context.AssertEqual("projected attack deals no original damage",
            hpBefore, source.CurrentHp);
        context.AssertEqual("first escape leaves one", 1, control.Amount);

        StrikeIronclad oneCost =
            await context.AddCard<StrikeIronclad>(PileType.Hand);
        await context.PlayPayingResources(oneCost);
        context.AssertNoControl("second escape removes catalog control");
        context.AssertEqual("escape schedules stable recovery move",
            "MAIDENSUCCUBUS_RECOVERY",
            source.Monster.NextMove.StateId);
        context.AssertEqual("recovery mirrors Byrdonis PECK intent count",
            1, source.Monster.NextMove.Intents.Count);
        context.AssertEqual("played card restores original target",
            TargetType.AnyEnemy, oneCost.TargetType);
        context.AssertTrue("played card no longer has escape projection",
            ControlQuery.GetProjection(oneCost) == null);

        context.Checkpoint("before recovery move and original intent pop");
        await source.Monster.PerformMove();
        source.Monster.RollMove(context.Combat.PlayerCreatures);
        context.AssertReference("original intent resumes after recovery",
            original, source.Monster.NextMove);
    }

    private static async Task<string> WriteReport(ControlIntentTestReport report)
    {
        string assemblyDir = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location)
            ?? Environment.CurrentDirectory;
        string reportDir = Path.Combine(
            assemblyDir, "control-intent-test-results");
        Directory.CreateDirectory(reportDir);
        string timestamp = report.StartedAt.ToString("yyyyMMdd-HHmmss");
        string path = Path.Combine(
            reportDir, $"control-intents-{timestamp}.json");
        string json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(path, json);
        await File.WriteAllTextAsync(
            Path.Combine(reportDir, "latest.json"), json);
        return path;
    }
}
#endif
