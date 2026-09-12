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
Assert-NotContains "escape capability-owned target" $escape 'nameof\(CardModel\.TargetType\)'
Assert-NotContains "escape capability-owned result" $escape 'GetResultLocationForCardPlay'

$control = Read-Text "src\Core\Control\ControlQuery.cs"
Assert-Contains "escape projection identity" $control 'ModelCapabilities\.TryGet\(card'
Assert-Contains "escape capability creation" $control 'ModelCapabilityRegistry\.Create<EscapeProjectionCapability>\(\)'
Assert-Contains "escape capability attachment" $control 'card\.AddCapability\(existing, allowMerge: false\)'
Assert-NotContains "escape projection side table" $control 'ConditionalWeakTable<CardModel,'
Assert-NotContains "escape getter-driven rebuild" $control 'RefreshCard\('

$escapeCapability = Read-Text "src\Core\Control\EscapeProjectionCapability.cs"
Assert-Contains "escape capability registration" $escapeCapability '\[RegisterModelCapability\('
Assert-Contains "escape capability play dispatch" $escapeCapability 'CardPlayCapability'
Assert-Contains "escape capability target" $escapeCapability 'ICardPropertyContributor'
Assert-Contains "escape capability result" $escapeCapability 'ICardPlayResultContributor'
Assert-Contains "escape capability overlay" $escapeCapability 'ICardOverlayContributor'
Assert-Contains "escape capability hover" $escapeCapability 'ICardHoverTipContributor'
Assert-Contains "escape capability suppression" $escapeCapability 'BeforeOwnerCardOnPlay'

$controlPower = Read-Text "src\Powers\ControlPower.cs"
Assert-NotContains "escape effect dispatch ownership" $controlPower 'ICardOnPlayHookListener|BeforeCardOnPlay\('
Assert-NotContains "escape refresh lifecycle" $controlPower 'override Task AfterApplied'

$controlCmd = Read-Text "src\Commands\ControlCmd.cs"
Assert-NotContains "escape refresh ownership" $controlCmd 'EscapeCardVisuals\.Refresh'

$legacyEscapeVisualPatch = Join-Path $ProjectDir "src\Patches\EscapeCardVisualPatch.cs"
if (Test-Path -LiteralPath $legacyEscapeVisualPatch) {
    throw "Legacy global EscapeCardVisualPatch must remain removed."
}

$originalState = Read-Text "src\Patches\EscapeOriginalStateAccessPatches.cs"
Assert-Contains "escape save preservation" $originalState 'nameof\(CardModel\.ToSerializable\)'
Assert-Contains "escape enchantment preservation" $originalState 'nameof\(CardCmd\.Enchant\)'
Assert-Contains "escape affliction preservation" $originalState 'nameof\(CardCmd\.Afflict\)'

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
Assert-NotContains "scripture source-card hover previews" $allSource 'HoverTipFactory\.FromCard<(Guardian|Nimble|Punishment|Wisdom|Vitality|Bliss)Scripture>'
$scripturePreviewHelper = Join-Path $ProjectDir "src\Cards\Scriptures\ScriptureCardPreview.cs"
if (Test-Path -LiteralPath $scripturePreviewHelper) {
    throw "Scripture card-preview helper must remain removed: $scripturePreviewHelper"
}

$desireResource = Read-Text "src\Core\Desire\DesireResource.cs"
$desireMeter = Read-Text "src\UI\DesireMeter.cs"
Assert-Contains "combat desire UI" $desireResource 'RegisterCombatUi\('
Assert-Contains "combat desire UI" $desireResource 'AlwaysShowInCombatUiForCharacter<MaidenSuccubusCharacter>'
Assert-Contains "persistent left-side desire UI" $desireMeter 'displayed along the left side in and out of combat'
Assert-Contains "combat transition refreshes left-side desire UI" $desireMeter 'private\s+void\s+OnCombatVisibilityChanged\(bool\s+_\)'
Assert-NotContains "combat must not hide left-side desire UI" $desireMeter 'Visible\s*=\s*!inCombat'
Assert-NotContains "combat must not suppress left-side desire UI refresh" $desireMeter 'CombatManager\.Instance\.IsInProgress'

$fourthRouteScreen = Read-Text "src\UI\FourthRouteSelectionScreen.cs"
Assert-Contains "fourth-route map modal" $fourthRouteScreen 'IScreenContext'
Assert-Contains "fourth-route map modal" $fourthRouteScreen 'NModalContainer\.Instance'
Assert-Contains "fourth-route map modal" $fourthRouteScreen 'container\.Add\(screen\)'
Assert-Contains "fourth-route input blocking" $fourthRouteScreen 'AddBlockingScreen\(this\)'
Assert-Contains "fourth-route input cleanup" $fourthRouteScreen 'RemoveBlockingScreen\(this\)'
Assert-NotContains "fourth-route map-hidden overlay" $fourthRouteScreen 'NOverlayStack\.Instance'

$fourthRoutePatch = Read-Text "src\Patches\FourthRouteQuestSelectionPatch.cs"
Assert-Contains "fourth-route travel suspension" $fourthRoutePatch 'map\.SetTravelEnabled\(false\)'
Assert-Contains "fourth-route travel restoration" $fourthRoutePatch 'currentMap\.SetTravelEnabled\(true\)'

$restSiteSealPatch = Read-Text "src\Patches\RestSiteSealPatch.cs"
$sacrificeOption = Read-Text "src\RestSite\SacrificeRestSiteOption.cs"
$twinSoulChalice = Read-Text "src\Relics\TwinSoulChalice.cs"
Assert-Contains "unified sacrifice option registration" $restSiteSealPatch 'new\s+SacrificeRestSiteOption\(__0\)'
Assert-Contains "unified sacrifice sealed-card source" $sacrificeOption 'CombatSealQuery\.GetSealedDeckCards\(Owner\)'
Assert-Contains "unified sacrifice removes all confirmed sealed cards" $sacrificeOption 'CardPileCmd\.RemoveFromDeck\(confirmed\)'
Assert-Contains "unified sacrifice advances fourth route" $sacrificeOption 'FourthRouteProgressService\.AdvanceStage\(Owner,\s*2\)'
Assert-NotContains "fourth-route relic must not add a second sacrifice option" $twinSoulChalice 'TryModifyRestSiteOptions|FourthRouteSacrificeOption'
foreach ($legacyRestSiteOption in @(
    "src\RestSite\FourthRouteSacrificeOption.cs",
    "src\RestSite\RemoveSealedCardsRestSiteOption.cs")) {
    if (Test-Path -LiteralPath (Join-Path $ProjectDir $legacyRestSiteOption)) {
        throw "Legacy split rest-site action must remain removed: $legacyRestSiteOption"
    }
}

$scenePath = Join-Path $ProjectDir "MaidenSuccubus\scenes\maiden_succubus_character.tscn"
if (!(Test-Path -LiteralPath $scenePath)) {
    throw "Character scene is missing: $scenePath"
}

Write-Host "Validated structural contracts: isolated build/deploy gates, instance-attached Escape capability with preserved original state, event-driven UI, simultaneous left-side and combat desire UI, stable intent state, shared derivative pool, map-safe fourth-route modal, unified sacrifice/sealed-card rest-site action, character scene."
