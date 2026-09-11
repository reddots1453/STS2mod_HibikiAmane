#if DEBUG
namespace MaidenSuccubus.Debugging.ControlIntents;

internal sealed class ControlIntentAssertionResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public required string Expected { get; init; }
    public required string Actual { get; init; }
}

internal sealed class ControlIntentScenarioResult
{
    public required string Name { get; init; }
    public required string Requirement { get; init; }
    public bool Passed { get; set; }
    public string? LastCheckpoint { get; set; }
    public string? Error { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public List<ControlIntentAssertionResult> Assertions { get; } = [];
}

internal sealed class ControlIntentTestReport
{
    public string SchemaVersion { get; init; } = "1";
    public string Baseline { get; init; } =
        "DesignDoc SYS-CTL-001/002 and SYS-DES-002B, 2026-08-24 implementation baseline";
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }
    public List<ControlIntentScenarioResult> Scenarios { get; } = [];
    public int Passed => Scenarios.Count(scenario => scenario.Passed);
    public int Failed => Scenarios.Count(scenario => !scenario.Passed);
    public bool Success => Scenarios.Count > 0 && Failed == 0;
}
#endif
