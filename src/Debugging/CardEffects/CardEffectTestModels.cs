#if DEBUG
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Debugging.CardEffects;

internal enum CardEffectTestStatus
{
    Passed,
    Failed,
    DesignPending,
}

internal enum CardUpgradePolicy
{
    BaseAndUpgraded,
    NotUpgradable,
}

internal sealed record CardEffectScenario(
    string Name,
    bool Upgraded,
    int MinimumEffectAssertions,
    Func<CardEffectTestContext, CardModel, Task> Execute);

internal sealed record CardEffectSpec(
    Type CardType,
    CardUpgradePolicy UpgradePolicy,
    IReadOnlyList<CardEffectScenario> Scenarios,
    string? PendingReason = null)
{
    public string CardId => CardType.Name;
    public bool IsDesignPending => PendingReason != null;
}

internal sealed class CardEffectAssertionResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public required string Expected { get; init; }
    public required string Actual { get; init; }
}

internal sealed class CardEffectScenarioResult
{
    public required string Name { get; init; }
    public required bool Upgraded { get; init; }
    public required CardEffectTestStatus Status { get; set; }
    public string? Error { get; set; }
    public List<CardEffectAssertionResult> Assertions { get; } = [];

    [JsonIgnore]
    public int EffectAssertionCount { get; set; }

    public int NumericEffectAssertionCount { get; set; }
}

internal sealed class CardEffectCardResult
{
    public required string CardId { get; init; }
    public required CardEffectTestStatus Status { get; set; }
    public string? PendingReason { get; init; }
    public List<CardEffectScenarioResult> Scenarios { get; } = [];
}

internal sealed class CardEffectTestReport
{
    public string SchemaVersion { get; init; } = "1";
    public string Baseline { get; init; } = "DesignDoc state 2026-09-16";
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }
    public required string RequestedCard { get; init; }
    public required int ContractCardCount { get; init; }
    public List<CardEffectCardResult> Cards { get; } = [];
    public int Passed => Cards.Count(card => card.Status == CardEffectTestStatus.Passed);
    public int Failed => Cards.Count(card => card.Status == CardEffectTestStatus.Failed);
    public int DesignPending => Cards.Count(card => card.Status == CardEffectTestStatus.DesignPending);
    public bool Success => Failed == 0 && Passed + DesignPending == ContractCardCount;
}
#endif
