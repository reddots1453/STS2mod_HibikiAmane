param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

$contractPath = Join-Path $ProjectDir "docs\content_contract_20260824.json"
$catalogPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestCatalog.cs"
$iteration2ContractPath = Join-Path $ProjectDir "src\Debugging\CardEffects\Iteration2CardEffectContract.cs"
$runnerPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestRunner.cs"
$consolePath = Join-Path $ProjectDir "src\ConsoleCommands\CardEffectTestConsoleCmd.cs"
$hotkeyPath = Join-Path $ProjectDir "src\Debugging\CardEffects\CardEffectTestHotkey.cs"
$intentFactoryPath = Join-Path $ProjectDir "src\Core\Intents\IntentMoveFactory.cs"
$holyPowersPath = Join-Path $ProjectDir "src\Powers\Iteration1HolyPowers.cs"
$neutralBatchPath = Join-Path $ProjectDir "src\Cards\MvpNeutralCardsBatch.cs"
$cardsPath = Join-Path $ProjectDir "src\Cards"

$contract = Get-Content -Raw -Encoding UTF8 -LiteralPath $contractPath | ConvertFrom-Json
$catalog = Get-Content -Raw -Encoding UTF8 -LiteralPath $catalogPath
$chainCopyContract = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Debugging\CardEffects\DesignSyncChainCopyContract.cs")
$iteration2Contract = Get-Content -Raw -Encoding UTF8 -LiteralPath $iteration2ContractPath
$runner = Get-Content -Raw -Encoding UTF8 -LiteralPath $runnerPath
$console = Get-Content -Raw -Encoding UTF8 -LiteralPath $consolePath
$hotkey = Get-Content -Raw -Encoding UTF8 -LiteralPath $hotkeyPath
$intentFactory = Get-Content -Raw -Encoding UTF8 -LiteralPath $intentFactoryPath
$holyPowers = Get-Content -Raw -Encoding UTF8 -LiteralPath $holyPowersPath
$neutralBatch = Get-Content -Raw -Encoding UTF8 -LiteralPath $neutralBatchPath
$neutralPowers = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Powers\MvpNeutralUtilityPowers.cs")
$cardSources = (Get-ChildItem -LiteralPath $cardsPath -Filter "*.cs" -File |
    ForEach-Object { Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName }) -join "`n"

$expected = @(
    $contract.cards.PSObject.Properties.Value |
        ForEach-Object { $_ } |
        Sort-Object -Unique
)
if ($expected.Count -ne 227) {
    throw "Content contract must contain exactly 227 unique cards; found $($expected.Count)."
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
if ($actual.Count -ne $expected.Count -or $duplicates.Count -gt 0 -or
    $missing.Count -gt 0 -or $extra.Count -gt 0) {
    throw "Card-effect catalog mismatch: registrations=$($actual.Count); duplicates=[$($duplicates -join ',')]; missing=[$($missing -join ',')]; extra=[$($extra -join ',')]."
}

$pending = @([regex]::Matches($registration, 'Pending<(?<card>[A-Za-z0-9_.]+)>') |
    ForEach-Object { $_.Groups['card'].Value.Split('.')[-1] } |
    Sort-Object)
$expectedPending = @('HypnosisCurse')
if (($pending -join ',') -ne ($expectedPending -join ',')) {
    throw "Only HypnosisCurse may be DESIGN_PENDING; found [$($pending -join ',')]."
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
if ($runner -notmatch 'Iteration2CardEffectContract\.Contains\(spec\.CardType\)' -or
    $runner -notmatch 'NumericEffectAssertionCount\s*==\s*0' -or
    $runner -notmatch 'requestedCard,\s*"iteration2"') {
    throw "Focused iteration-two selection or numeric-effect assertion gate is missing."
}
$iteration2Types = @([regex]::Matches(
    $iteration2Contract,
    'typeof\(Cards\.(?:Curses\.)?(?<card>[A-Za-z0-9_]+)\)') |
    ForEach-Object { $_.Groups['card'].Value })
$iteration2Duplicates = @($iteration2Types | Group-Object |
    Where-Object Count -ne 1 | ForEach-Object Name)
if ($iteration2Contract -notmatch 'ExpectedCardCount\s*=\s*68' -or
    $iteration2Types.Count -ne 68 -or $iteration2Duplicates.Count -gt 0) {
    throw "Iteration-two changed-card contract must contain 68 unique card types; found $($iteration2Types.Count), duplicates=[$($iteration2Duplicates -join ',')]."
}
if ($iteration2Types -notcontains 'DemonStaff') {
    throw "The changed DemonStaff implementation must be covered by Shift+F10."
}
$missingIteration2Registrations = @($iteration2Types |
    Where-Object { $actual -notcontains $_ })
if ($missingIteration2Registrations.Count -gt 0) {
    throw "Iteration-two changed cards are missing runtime tests: [$($missingIteration2Registrations -join ',')]."
}
if ($runner -notmatch 'ExpectedCardCount\s*=\s*227' -or
    $runner -notmatch 'expected\.Length\s*!=\s*ExpectedCardCount') {
    throw "Runtime 227-card identity gate is missing."
}
if ($console -notmatch 'ms_test_cards' -or $console -notmatch 'confirm') {
    throw "Destructive console command confirmation gate is missing."
}
if ($hotkey -notmatch 'Key\.F10' -or
    $hotkey -notmatch 'shiftNow\s*\?\s*"iteration2"\s*:\s*"all"' -or
    $hotkey -notmatch 'CombatManager\.Instance\.IsInProgress' -or
    $hotkey -notmatch 'MaidenSuccubusCharacter' -or
    $hotkey -notmatch 'CardEffectTestRunner\.Run\(player, requested\)') {
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
if ($neutralBatch -notmatch '(?s)class FlashStab.*?CardCmd\.PreviewCardPileAdd\(\s*await CardPileCmd\.AddGeneratedCardToCombat\(\s*copy,\s*PileType\.Draw,\s*Owner,\s*CardPilePosition\.Random\),\s*2\.2f\)') {
    throw "FlashStab must preview its generated copy entering a random draw-pile position, matching Anger-style feedback."
}
if ($neutralPowers -notmatch 'description\.Add\("Damage",\s*\(int\)Damage\)' -or
    $neutralPowers -match 'description\.Add\("Damage",\s*Damage\)') {
    throw "Magic Star Bomb power must format delayed damage as an integer."
}
if ($catalog -notmatch 'projected card original effect is suppressed' -or
    $catalog -notmatch 'projected card pays one escape point' -or
    $catalog -notmatch 'projected card resolves to discard') {
    throw "Control projection must verify effect suppression, exact escape payment, and result pile."
}
if ($catalog -notmatch 'holy variation route identity' -or
    $catalog -notmatch 'corrupt variation route identity' -or
    $catalog -notmatch 'holy variation remains unsealed' -or
    $catalog -notmatch 'corrupt variation remains unsealed') {
    throw "Transform and DarkElement variations must verify route identity and current-route seal compatibility."
}
if ($catalog -notmatch 'CustomVariants<SummonThunder>\(DesignSyncChainCopyContract\.Thunder, 33\)' -or
    $chainCopyContract -notmatch 'await ctx\.Play\(ctx\.Create<SummonThunder>\(upgraded\)' -or
    $chainCopyContract -notmatch 'ctx\.AssertDamage\(scenario \+ " selected target", initial, initialHp, damage\)' -or
    $chainCopyContract -notmatch 'ctx\.AssertDamage\(scenario \+ " lowest-health survivor receives all pending hits", sink, sinkHp, damage \* sinkHits\)' -or
    $chainCopyContract -notmatch 'ctx\.AssertPower\(scenario \+ " exactly one accepted armor payment"' -or
    $chainCopyContract -notmatch '"chain\+release" \? 2 : 0' -or
    $chainCopyContract -notmatch 'ApplyPower<MinionPower>\(initial, 1\)' -or
    $chainCopyContract -notmatch 'ApplyPower<MinionPower>\(minion, 1\)') {
    throw "SummonThunder must execute and verify selected-target damage, lowest-health release, payment, chained kills, combined triggers and native minion exclusions."
}
if ($holyPowers -notmatch '_pendingRestores' -or
    $holyPowers -notmatch 'AfterCardChangedPiles' -or
    $holyPowers -notmatch 'oldPileType\s*!=\s*PileType\.Play') {
    throw "BattleTechniqueReplay must restore only after the played projection leaves PileType.Play."
}

Write-Host "Validated card-effect tests: 227 exact registrations, 226 executable cards, 1 DESIGN_PENDING card, 68-card iteration-two numeric suite, guarded manual-entry F10 trigger, no method-presence placeholders."
