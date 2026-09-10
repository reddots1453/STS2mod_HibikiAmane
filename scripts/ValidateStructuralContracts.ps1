param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

function Read-Text([string]$RelativePath) {
    return Get-Content -Raw -Encoding UTF8 -LiteralPath (
        Join-Path $ProjectDir $RelativePath)
}

function Assert-Contains(
    [string]$Name,
    [string]$Text,
    [string]$Pattern
) {
    if ($Text -notmatch $Pattern) {
        throw "$Name structural contract is missing pattern: $Pattern"
    }
}

function Assert-NotContains(
    [string]$Name,
    [string]$Text,
    [string]$Pattern
) {
    if ($Text -match $Pattern) {
        throw "$Name violates structural contract with pattern: $Pattern"
    }
}

$project = Read-Text "MaidenSuccubus.csproj"
Assert-Contains "build gate" $project '<ValidateMod Condition="''\$\(ValidateMod\)'' == ''''">false</ValidateMod>'
Assert-Contains "deploy gate" $project '<DeployMod Condition="''\$\(DeployMod\)'' == ''''">false</DeployMod>'

$escape = Read-Text "src\Patches\EscapeCardProjectionPatches.cs"
Assert-NotContains "escape projection" $escape 'AppDomain\.CurrentDomain\.GetAssemblies'
Assert-NotContains "escape projection" $escape 'EscapeEffectPatcher|Harmony\.Patch\('

$control = Read-Text "src\Core\Control\ControlQuery.cs"
Assert-Contains "escape projection identity" $control 'ConditionalWeakTable<CardModel,'
Assert-Contains "escape projection reentrancy" $control 'if \(_resolvingProjection \|\| !card\.IsMutable\)'

$controlPower = Read-Text "src\Powers\ControlPower.cs"
Assert-Contains "escape effect dispatch" $controlPower 'ICardOnPlayHookListener'
Assert-Contains "escape effect dispatch" $controlPower 'BeforeCardOnPlay\(BeforeCardOnPlayContext context\)'
Assert-Contains "escape effect dispatch" $controlPower 'ReferenceEquals\(projection\.Control, this\)'
Assert-NotContains "escape refresh lifecycle" $controlPower 'override Task AfterApplied'

$controlCmd = Read-Text "src\Commands\ControlCmd.cs"
Assert-NotContains "escape refresh ownership" $controlCmd 'EscapeCardVisuals\.Refresh'

$escapeVisuals = Read-Text "src\UI\EscapeCardVisuals.cs"
Assert-Contains "escape visual reentrancy" $escapeVisuals '\[ThreadStatic\]\s+private static bool _refreshing'
Assert-Contains "escape visual reentrancy" $escapeVisuals 'if \(_refreshing\)'

$intent = Read-Text "src\Core\Intents\IntentMoveFactory.cs"
Assert-NotContains "erotic intent ids" $intent 'Interlocked\.Increment'
Assert-Contains "erotic intent ids" $intent '\$"MAIDENSUCCUBUS_\{kind\}"'

$runtimePower = Read-Text "src\Powers\EroticIntentRuntimePower.cs"
foreach ($field in @(
    "ForceStun", "ControlDisabled", "DesireIntentUses", "ControlIntentUses",
    "InvasionIntentUses", "LastNaturalRollTurn")) {
    Assert-Contains "intent persistence" $runtimePower (
        '\[SavedProperty\]\s+public\s+[^\r\n]+\s+' + $field + '\s*\{')
}

$allSource = (Get-ChildItem -LiteralPath (Join-Path $ProjectDir "src") -Recurse -Filter *.cs |
    Get-Content -Raw) -join "`n"
Assert-NotContains "event-driven runtime" $allSource 'new\s+Timer\s*\('
Assert-NotContains "scripture pool" $allSource 'MSScriptureCardPool'

$desireResource = Read-Text "src\Core\Desire\DesireResource.cs"
$desireMeter = Read-Text "src\UI\DesireMeter.cs"
Assert-Contains "combat desire UI" $desireResource 'RegisterCombatUi\('
Assert-Contains "combat desire UI" $desireResource 'AlwaysShowInCombatUiForCharacter<MaidenSuccubusCharacter>'
Assert-Contains "noncombat desire UI" $desireMeter 'Character-only vertical desire meter displayed along the left side'
Assert-Contains "noncombat desire UI" $desireMeter 'Visible\s*=\s*!inCombat'

$scenePath = Join-Path $ProjectDir "MaidenSuccubus\scenes\maiden_succubus_character.tscn"
if (!(Test-Path -LiteralPath $scenePath)) {
    throw "Character scene is missing: $scenePath"
}

Write-Host "Validated structural contracts: isolated build/deploy gates, exact escape projection through RitsuLib OnPlay dispatch, event-driven UI, dual-context desire UI, stable intent state, shared derivative pool, character scene."
