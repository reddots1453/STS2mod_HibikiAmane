param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir
)

$ErrorActionPreference = "Stop"
$testDir = Join-Path $ProjectDir "src/Debugging/ControlIntents"
$runnerPath = Join-Path $testDir "ControlIntentTestRunner.cs"
$contextPath = Join-Path $testDir "ControlIntentTestContext.cs"
$hotkeyPath = Join-Path $testDir "ControlIntentTestHotkey.cs"
$consolePath = Join-Path $ProjectDir "src/ConsoleCommands/ControlIntentTestConsoleCmd.cs"

foreach ($path in @($runnerPath, $contextPath, $hotkeyPath, $consolePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing control-intent test component: $path"
    }
}

$runner = Get-Content -Raw -LiteralPath $runnerPath
$context = Get-Content -Raw -LiteralPath $contextPath
$hotkey = Get-Content -Raw -LiteralPath $hotkeyPath
$console = Get-Content -Raw -LiteralPath $consolePath

$scenarioNames = @(
    "lifecycle_threshold_dispatch",
    "forced_intent_ignores_natural_cooldown",
    "natural_consecutive_limit_and_saved_state",
    "default_invasion_curse",
    "fossil_invasion_stun_once",
    "native_stun_over_pending_erotic",
    "desire_intent_visual_deduplication",
    "catalog_intent_visual_components",
    "intent_metadata_and_exact_block",
    "insufficient_block_stress_projection",
    "high_desire_bypasses_block",
    "control_type_projection_matrix",
    "original_state_and_paid_escape",
    "zero_cost_escape_is_zero",
    "multi_source_priority_and_no_overflow",
    "source_death_releases_and_rebinds",
    "catalog_intent_to_recovery"
    "terror_eel_recovery_state"
)
foreach ($name in $scenarioNames) {
    if (-not $runner.Contains('new("' + $name + '"')) {
        throw "Control-intent suite is missing scenario: $name"
    }
}

$requiredTokens = @(
    "IntentMoveFactory.CreateControl",
    "IntentMoveFactory.CreateInvasion",
    "IntentMoveFactory.ForceStun",
    "AddFossilStalker",
    "AddTerrorEel",
    "PerformMove()",
    "ControlQuery.GetProjection",
    "ResistanceGloves",
    "SpendResources",
    "CreatureCmd.Kill",
    "IntentMoveFactory.TryForceControl",
    "MAIDENSUCCUBUS_RECOVERY",
    "WaitAsync",
    "WriteReport"
)
foreach ($token in $requiredTokens) {
    if (-not ($runner.Contains($token) -or $context.Contains($token))) {
        throw "Control-intent suite lost required runtime assertion path: $token"
    }
}

foreach ($forbidden in @("Task.CompletedTask", "TODO", "placeholder", "expectedToExist")) {
    if ($runner.Contains($forbidden) -or $context.Contains($forbidden)) {
        throw "Control-intent suite contains forbidden placeholder pattern: $forbidden"
    }
}

if (-not $hotkey.Contains("Key.F10") -or -not $hotkey.Contains("Key.Ctrl")) {
    throw "Control-intent suite must retain the Ctrl+F10 manual-combat trigger."
}
if ((-not $console.Contains('CmdName => "ms_test_control"')) -or (-not $console.Contains('"confirm"'))) {
    throw "Control-intent console command lost its explicit destructive confirmation gate."
}

Write-Host "Control-intent test contract: $($scenarioNames.Count) exact runtime scenarios passed structural validation."
