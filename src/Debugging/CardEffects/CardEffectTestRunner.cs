#if DEBUG
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class CardEffectTestRunner
{
    private const int ExpectedCardCount = 227;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<string> Run(Player player, string requestedCard)
    {
        if (!await Gate.WaitAsync(0))
            return "Card effect test suite is already running.";

        try
        {
            CombatState? combat = CombatManager.Instance.DebugOnlyGetState();
            if (!CombatManager.Instance.IsInProgress || combat == null)
                return "Start a disposable combat before running card effect tests.";
            if (combat.Players.Count != 1)
                return "Card effect tests require a single-player disposable combat.";
            if (player.Character.GetType().Name != "MaidenSuccubusCharacter")
                return "Use the MaidenSuccubus character for this test suite.";

            ValidateCatalog();
            CardEffectSpec[] selected = SelectSpecs(requestedCard);
            CardEffectTestReport report = new()
            {
                StartedAt = DateTimeOffset.Now,
                RequestedCard = requestedCard,
                ContractCardCount = selected.Length,
            };

            CardEffectTestContext context = new(combat, player);
            await context.PrepareSuite();

            foreach (CardEffectSpec spec in selected)
            {
                CardEffectCardResult cardResult = new()
                {
                    CardId = spec.CardId,
                    Status = spec.IsDesignPending
                        ? CardEffectTestStatus.DesignPending
                        : CardEffectTestStatus.Passed,
                    PendingReason = spec.PendingReason,
                };
                report.Cards.Add(cardResult);

                if (spec.IsDesignPending)
                {
                    MaidenSuccubusMod.Logger.Info(
                        $"[CardEffectTest] DESIGN_PENDING {spec.CardId}: {spec.PendingReason}");
                    continue;
                }

                foreach (CardEffectScenario scenario in spec.Scenarios)
                {
                    CardEffectScenarioResult scenarioResult = new()
                    {
                        Name = scenario.Name,
                        Upgraded = scenario.Upgraded,
                        Status = CardEffectTestStatus.Passed,
                    };
                    cardResult.Scenarios.Add(scenarioResult);
                    context.BeginScenario(scenarioResult);

                    try
                    {
                        await context.Reset();
                        CardModel card = context.Create(spec.CardType, scenario.Upgraded);
                        DesignSyncNeutralContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncNeutralTextContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncHolyContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncHolyTextContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncCombatTextContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncRemainingTextContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncStarterTextContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncCardBatchSixContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncTextBatchContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncEnchantmentInputContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncChainCopyContract.Validate(context, card, scenario.Upgraded);
                        DesignSyncShatterRandomContract.Validate(context, card, scenario.Upgraded);
                        await scenario.Execute(context, card);

                        if (scenarioResult.EffectAssertionCount < scenario.MinimumEffectAssertions)
                        {
                            scenarioResult.Assertions.Add(new CardEffectAssertionResult
                            {
                                Name = "minimum effect assertion gate",
                                Passed = false,
                                Expected = scenario.MinimumEffectAssertions.ToString(),
                                Actual = scenarioResult.EffectAssertionCount.ToString(),
                            });
                        }

                        if (Iteration2CardEffectContract.Contains(spec.CardType)
                            && scenarioResult.NumericEffectAssertionCount == 0)
                        {
                            scenarioResult.Assertions.Add(new CardEffectAssertionResult
                            {
                                Name = "iteration-two numeric effect assertion gate",
                                Passed = false,
                                Expected = "at least 1 numeric effect assertion",
                                Actual = "0",
                            });
                        }

                        if (scenarioResult.Assertions.Count == 0
                            || scenarioResult.Assertions.Any(assertion => !assertion.Passed))
                        {
                            scenarioResult.Status = CardEffectTestStatus.Failed;
                            cardResult.Status = CardEffectTestStatus.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        scenarioResult.Status = CardEffectTestStatus.Failed;
                        scenarioResult.Error = ex.ToString();
                        cardResult.Status = CardEffectTestStatus.Failed;
                    }

                    string outcome = scenarioResult.Status == CardEffectTestStatus.Passed
                        ? "PASS"
                        : "FAIL";
                    MaidenSuccubusMod.Logger.Info(
                        $"[CardEffectTest] {outcome} {spec.CardId}/{scenario.Name} "
                        + $"assertions={scenarioResult.Assertions.Count}");
                }
            }

            await context.Reset();
            report.FinishedAt = DateTimeOffset.Now;
            string reportPath = await WriteReport(report);
            string summary = $"Card effects: {report.Passed} passed, {report.Failed} failed, "
                + $"{report.DesignPending} design-pending. Report: {reportPath}";
            if (report.Success)
                MaidenSuccubusMod.Logger.Info("[CardEffectTest] " + summary);
            else
                MaidenSuccubusMod.Logger.Error("[CardEffectTest] " + summary);
            return summary;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static CardEffectSpec[] SelectSpecs(string requestedCard)
    {
        if (string.Equals(requestedCard, "all", StringComparison.OrdinalIgnoreCase))
            return CardEffectTestCatalog.All.ToArray();

        if (string.Equals(requestedCard, "ds27-transformation", StringComparison.OrdinalIgnoreCase))
            return CardEffectTestCatalog.All.Where(spec =>
                spec.CardType == typeof(Cards.Transform)
                || spec.CardType == typeof(Cards.LightPowerRelease)).ToArray();

        if (string.Equals(requestedCard, "ds27-batch6", StringComparison.OrdinalIgnoreCase))
        {
            CardEffectSpec[] batch = CardEffectTestCatalog.All
                .Where(spec => DesignSyncCardBatchSixContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != DesignSyncCardBatchSixContract.Entries.Length || batch.Length != 9)
                throw new InvalidOperationException("DS27 batch6 suite must cover all 9 design entries.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-holy", StringComparison.OrdinalIgnoreCase))
        {
            CardEffectSpec[] batch = CardEffectTestCatalog.All
                .Where(spec => DesignSyncHolyContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != DesignSyncHolyContract.Entries.Length || batch.Length != 10)
                throw new InvalidOperationException("DS27 holy suite must cover all 10 design entries.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-text", StringComparison.OrdinalIgnoreCase))
        {
            CardEffectSpec[] batch = CardEffectTestCatalog.All
                .Where(spec => DesignSyncTextBatchContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 12)
                throw new InvalidOperationException("DS27 text suite must cover all 12 design entries.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-enchantment-input", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncEnchantmentInputContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 4) throw new InvalidOperationException("DS27 enchantment input requires all four cards.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-chain-copy", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncChainCopyContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 3) throw new InvalidOperationException("DS27 chain/copy requires all three cards.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-shatter-random", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncShatterRandomContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 4) throw new InvalidOperationException("DS27 shatter/random requires all four cards.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-scripture-generation", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncScriptureGenerationContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 4) throw new InvalidOperationException("DS27 scripture generation requires all four cards.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-scriptures", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncScriptureContract.Types.Contains(spec.CardType)).ToArray();
            if (batch.Length != 6) throw new InvalidOperationException("DS27 scriptures requires all six independent scriptures.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-holy-text", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncHolyTextContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != 48 || batch.Length != DesignSyncHolyTextContract.Entries.Length)
                throw new InvalidOperationException("DS27 holy text requires all 48 cards and their existing effect scenarios.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-starter-text", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncStarterTextContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != 5 || batch.Length != DesignSyncStarterTextContract.Entries.Length)
                throw new InvalidOperationException("DS27 starter text requires five cards and existing effect scenarios.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-remaining-text", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncRemainingTextContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != 7 || batch.Length != DesignSyncRemainingTextContract.Entries.Length)
                throw new InvalidOperationException("DS27 remaining text requires seven cards and existing effect scenarios.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-combat-text", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncCombatTextContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != 8 || batch.Length != DesignSyncCombatTextContract.Entries.Length)
                throw new InvalidOperationException("DS27 combat text requires eight cards and their existing effect scenarios.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-neutral-text", StringComparison.OrdinalIgnoreCase))
        {
            var batch = CardEffectTestCatalog.All.Where(spec => DesignSyncNeutralTextContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != 30 || batch.Length != DesignSyncNeutralTextContract.Entries.Length)
                throw new InvalidOperationException("DS27 neutral text requires all 30 cards and their existing effect scenarios.");
            return batch;
        }

        if (string.Equals(requestedCard, "ds27-neutral", StringComparison.OrdinalIgnoreCase))
        {
            CardEffectSpec[] batch = CardEffectTestCatalog.All
                .Where(spec => DesignSyncNeutralContract.Contains(spec.CardType)).ToArray();
            if (batch.Length != DesignSyncNeutralContract.Entries.Length || batch.Length != 14)
                throw new InvalidOperationException("DS27 neutral suite must cover all 14 design entries.");
            return batch;
        }

        if (string.Equals(requestedCard, "iteration2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedCard, "changed", StringComparison.OrdinalIgnoreCase))
        {
            return CardEffectTestCatalog.All
                .Where(spec => Iteration2CardEffectContract.Contains(spec.CardType))
                .ToArray();
        }

        CardEffectSpec? selected = CardEffectTestCatalog.All.FirstOrDefault(spec =>
            string.Equals(spec.CardId, requestedCard, StringComparison.OrdinalIgnoreCase));
        if (selected == null)
            throw new ArgumentException($"Unknown MaidenSuccubus card id: {requestedCard}");
        return [selected];
    }

    private static void ValidateCatalog()
    {
        CardEffectSpec[] specs = CardEffectTestCatalog.All.ToArray();
        string[] duplicateIds = specs.GroupBy(spec => spec.CardId, StringComparer.Ordinal)
            .Where(group => group.Count() != 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateIds.Length > 0)
            throw new InvalidOperationException(
                "Duplicate card effect specifications: " + string.Join(", ", duplicateIds));

        Type[] runtimeCards =
        [
            .. ModelDb.CardPool<MSNeutralCardPool>().AllCards.Select(card => card.GetType()),
            .. ModelDb.CardPool<MSCorruptCardPool>().AllCards.Select(card => card.GetType()),
            .. ModelDb.CardPool<MSHolyCardPool>().AllCards.Select(card => card.GetType()),
            .. ModelDb.CardPool<MSInvasionCursePool>().AllCards.Select(card => card.GetType()),
            .. ModelDb.CardPool<MSGeneratedCardPool>().AllCards.Select(card => card.GetType()),
        ];
        string[] expected = runtimeCards.Select(type => type.Name)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] actual = specs.Select(spec => spec.CardId)
            .Order(StringComparer.Ordinal).ToArray();
        if (expected.Length != ExpectedCardCount
            || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            string[] missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
            string[] extra = actual.Except(expected, StringComparer.Ordinal).ToArray();
            throw new InvalidOperationException(
                $"Card effect catalog mismatch (runtime={expected.Length}, catalog={actual.Length}); "
                + $"missing=[{string.Join(",", missing)}], extra=[{string.Join(",", extra)}]");
        }

        string[] pending = specs.Where(spec => spec.IsDesignPending)
            .Select(spec => spec.CardId).Order(StringComparer.Ordinal).ToArray();
        string[] expectedPending =
        [
            nameof(Cards.Curses.ClimaxBanCurse),
            nameof(Cards.Curses.HypnosisCurse),
        ];
        Array.Sort(expectedPending, StringComparer.Ordinal);
        if (!pending.SequenceEqual(expectedPending, StringComparer.Ordinal))
            throw new InvalidOperationException(
                "Only ClimaxBanCurse and HypnosisCurse may be DESIGN_PENDING.");

        if (Iteration2CardEffectContract.CardTypes.Count
                != Iteration2CardEffectContract.ExpectedCardCount)
        {
            throw new InvalidOperationException(
                $"Iteration-two contract count mismatch: "
                + $"expected={Iteration2CardEffectContract.ExpectedCardCount}, "
                + $"actual={Iteration2CardEffectContract.CardTypes.Count}.");
        }

        Type[] missingIteration2 = Iteration2CardEffectContract.CardTypes
            .Except(specs.Select(spec => spec.CardType))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        if (missingIteration2.Length > 0)
        {
            throw new InvalidOperationException(
                "Iteration-two cards missing executable specifications: "
                + string.Join(", ", missingIteration2.Select(type => type.Name)));
        }

        string[] pendingIteration2 = specs
            .Where(spec => spec.IsDesignPending
                && Iteration2CardEffectContract.Contains(spec.CardType))
            .Select(spec => spec.CardId)
            .ToArray();
        if (pendingIteration2.Length > 0)
        {
            throw new InvalidOperationException(
                "Iteration-two cards cannot be design-pending: "
                + string.Join(", ", pendingIteration2));
        }

        foreach (CardEffectSpec spec in specs.Where(spec => !spec.IsDesignPending))
        {
            if (spec.Scenarios.Count == 0)
                throw new InvalidOperationException($"{spec.CardId} has no executable scenarios.");
            if (spec.Scenarios.Any(scenario => scenario.MinimumEffectAssertions <= 0))
                throw new InvalidOperationException($"{spec.CardId} permits a zero-assertion scenario.");

            bool hasBase = spec.Scenarios.Any(scenario => !scenario.Upgraded);
            bool hasUpgrade = spec.Scenarios.Any(scenario => scenario.Upgraded);
            if (!hasBase || (spec.UpgradePolicy == CardUpgradePolicy.BaseAndUpgraded && !hasUpgrade))
                throw new InvalidOperationException($"{spec.CardId} is missing base/upgraded coverage.");
        }
    }

    private static async Task<string> WriteReport(CardEffectTestReport report)
    {
        string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Environment.CurrentDirectory;
        string reportDir = Path.Combine(assemblyDir, "card-effect-test-results");
        Directory.CreateDirectory(reportDir);
        string timestamp = report.StartedAt.ToString("yyyyMMdd-HHmmss");
        string path = Path.Combine(reportDir, $"card-effects-{timestamp}.json");
        string json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(path, json);
        await File.WriteAllTextAsync(Path.Combine(reportDir, "latest.json"), json);
        return path;
    }
}
#endif
