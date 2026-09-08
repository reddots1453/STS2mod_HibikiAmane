param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

$contractPath = Join-Path $ProjectDir "docs\content_contract_20260824.json"
$catalogPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestCatalog.cs"
$runnerPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestRunner.cs"
$consolePath = Join-Path $ProjectDir "src\ConsoleCommands\CardEffectTestConsoleCmd.cs"
$hotkeyPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestHotkey.cs"
$intentFactoryPath = Join-Path $ProjectDir "src\Core\Intents\IntentMoveFactory.cs"
$cardsPath = Join-Path $ProjectDir "src\Cards"

$contract = Get-Content -Raw -Encoding UTF8 -LiteralPath $contractPath | ConvertFrom-Json
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath
$runner = Get-Content -Raw -Encoding UTF8 -LiteralPath $runnerPath
$console = Get-Content -Raw -Encoding UTF8 -LiteralPath $consolePath
$hotkey = Get-Content -Raw -Encoding UTF8 -LiteralPath $hotkeyPath
$intentFactory = Get-Content -Raw -Encoding UTF8 -LiteralPath $intentFactoryPath
$cardSources = (Get-ChildItem -LiteralPath $cardsPath -Filter "*.cs" -File |
    ForEach-Object { Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName }) -join "`n"

$expected = @(
    $contract.cards.PSObject.Properties.Value |
        ForEach-Object { $_ } |
        Sort-Object -Unique
)
if ($expected.Count -ne 204) {
    throw "Content contract must contain exactly 204 unique cards; found $($expected.Count)."
}

$registrationStart = $catalog.IndexOf("private static void RegisterNeutral", [StringComparison]::Ordinal)
$registrationEnd = $catalog.IndexOf("// Generic executable probes", [StringComparison]::Ordinal)
if ($registrationStart -lt 0 -or $registrationEnd -le $registrationStart) {
    throw "Cannot locate the card-effect registration region."
}
$registration = $catalog.Substring($registrationStart, $registrationEnd - $registrationStart)

$actual = [System.Collections.Generic.List[string]]::new()
$genericCalls = [regex]::Matches(
    $registration,
    '(?m)^\s*[A-Za-z][A-Za-z0-9_]*<(?<card>[A-Za-z0-9_.]+)')
foreach ($match in $genericCalls) {
    $name = $match.Groups['card'].Value.Split('.')[-1]
    if ($expected -contains $name) {
        $actual.Add($name)
    }
}

$probeCalls = [regex]::Matches(
    $registration,
    '(?m)^\s*(?<probe>[A-Za-z][A-Za-z0-9_]*Probe)\(\);')
foreach ($match in $probeCalls) {
    $probe = [regex]::Escape($match.Groups['probe'].Value)
    $definition = [regex]::Match(
        $catalog,
        "private\s+static\s+void\s+$probe\(\)\s*=>\s*[A-Za-z][A-Za-z0-9_]*<(?<card>[A-Za-z0-9_.]+)>")
    if (!$definition.Success) {
        throw "Probe $($match.Groups['probe'].Value) does not expose its registered card type."
    }
    $actual.Add($definition.Groups['card'].Value.Split('.')[-1])
}

# These two UI-only generated cards are registered through named configuration
# probes rather than methods ending in "Probe".
if ($registration -match '(?m)^\s*ConfigureEnchantmentChoice\(\);') {
    $actual.Add('EnchantmentChoiceCard')
}
if ($registration -match '(?m)^\s*ConfigureQuestChoice\(\);') {
    $actual.Add('FourthRouteQuestChoice')
}

$duplicates = @($actual | Group-Object | Where-Object Count -ne 1 | ForEach-Object Name)
$missing = @($expected | Where-Object { $actual -notcontains $_ })
$extra = @($actual | Where-Object { $expected -notcontains $_ } | Sort-Object -Unique)
if ($actual.Count -ne 204 -or $duplicates.Count -gt 0 -or
    $missing.Count -gt 0 -or $extra.Count -gt 0) {
    throw "Card-effect catalog mismatch: registrations=$($actual.Count); duplicates=[$($duplicates -join ',')]; missing=[$($missing -join ',')]; extra=[$($extra -join ',')]."
}

$pending = @([regex]::Matches($registration, 'Pending<(?<card>[A-Za-z0-9_.]+)>') |
    ForEach-Object { $_.Groups['card'].Value.Split('.')[-1] } |
    Sort-Object)
$expectedPending = @('ClimaxBanCurse', 'DreamMist', 'HypnosisCurse')
if (($pending -join ',') -ne ($expectedPending -join ',')) {
    throw "Only ClimaxBanCurse, DreamMist, and HypnosisCurse may be DESIGN_PENDING; found [$($pending -join ',')]."
}

$forbidden = @(
    'GetMethod\s*\(',
    'GetMethods\s*\(',
    'expectedToExist',
    'delegated behavior marker',
    'implementation is present',
    'Assert[A-Za-z0-9_]*Marker'
)
foreach ($pattern in $forbidden) {
    if ($catalog -match $pattern) {
        throw "Card-effect tests contain a forbidden placeholder assertion: $pattern"
    }
}

if ($runner -notmatch 'EffectAssertionCount\s*<\s*scenario\.MinimumEffectAssertions') {
    throw "Runtime minimum-effect-assertion gate is missing."
}
if ($runner -notmatch 'expected\.Length\s*!=\s*204') {
    throw "Runtime 204-card identity gate is missing."
}
if ($console -notmatch 'ms_test_cards' -or $console -notmatch 'confirm') {
    throw "Destructive console command confirmation gate is missing."
}
if ($hotkey -notmatch 'Key\.F10' -or
    $hotkey -notmatch 'CombatManager\.Instance\.IsInProgress' -or
    $hotkey -notmatch 'MaidenSuccubusCharacter' -or
    $hotkey -notmatch 'CardEffectTestRunner\.Run\(player, "all"\)') {
    throw "Manual-entry F10 card-effect test trigger is missing or insufficiently guarded."
}
if ($intentFactory -notmatch 'new\s+MoveState\("STUNNED"' -or
    $intentFactory -notmatch '!creature\.IsStunned') {
    throw "Mod stun bridge must use the engine-recognized STUNNED id and force a blocked transient replacement."
}
if ($cardSources -match 'CreatureCmd\.Stun\s*\(') {
    throw "Card source bypasses IntentMoveFactory.Stun and can silently fail on a transient erotic intent."
}
if ($catalog -notmatch 'DesireWhipProbe' -or
    $catalog -notmatch 'control-intent target stunned') {
    throw "DesireWhip must verify its conditional control/invasion-intent stun, not damage alone."
}

Write-Host "Validated card-effect tests: 204 exact registrations, 201 executable cards, 3 DESIGN_PENDING cards, guarded manual-entry F10 trigger, no method-presence placeholders."
