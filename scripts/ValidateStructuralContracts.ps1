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

$battleReplayPower = Read-Text "src\Powers\Iteration1HolyPowers.cs"
$battleReplayCapability = Read-Text "src\Core\Replay\BattleReplayOriginCapability.cs"
$battleReplayVisuals = Read-Text "src\UI\BattleReplayCardVisuals.cs"
Assert-Contains "battle replay instance marker" $battleReplayPower 'ModelCapabilityRegistry\.Create<BattleReplayOriginCapability>\(\)'
Assert-Contains "battle replay listener remains hidden" $battleReplayPower 'BattleTechniqueReplayPower[\s\S]*?protected override bool IsVisibleInternal => false;'
Assert-Contains "battle replay overlay registration" $battleReplayCapability '\[RegisterModelCapability\('
Assert-Contains "battle replay overlay ownership" $battleReplayCapability 'ICardOverlayContributor'
Assert-Contains "battle replay overlay factory" $battleReplayCapability 'maiden_battle_replay_shadow'
Assert-Contains "battle replay shadow visuals" $battleReplayVisuals 'CreateShadowOverlay'
Assert-Contains "battle replay edge shadow" $battleReplayVisuals 'AddEdgeBands'
Assert-NotContains "battle replay type-wide overlay" $battleReplayPower 'OverlayPath'
Assert-NotContains "battle replay persistent visual timer" $battleReplayVisuals 'new\s+Timer'

$scripturePowers = Read-Text "src\Powers\Scriptures\ScripturePowers.cs"
$guardianScriptureBlock = Read-Text "src\Powers\Scriptures\GuardianScriptureBlockVar.cs"
$cardEffectCatalog = Read-Text "src\Debugging\CardEffects\CardEffectTestCatalog.cs"
$powerLocalization = Read-Text "MaidenSuccubus\localization\zhs\powers.json"
Assert-Contains "guardian scripture powered block var" $scripturePowers 'GuardianScripturePower[\s\S]*?new\s+GuardianScriptureBlockVar\(\)'
Assert-Contains "guardian scripture canonical gain path" $scripturePowers 'GuardianScripturePower[\s\S]*?CreatureCmd\.GainBlock\(Owner,\s*DynamicVars\.Block,\s*null\)'
Assert-Contains "guardian scripture dynamic hook preview" $guardianScriptureBlock 'Hook\.ModifyBlock\('
Assert-Contains "guardian scripture powered move property" $guardianScriptureBlock 'ValueProp\.Move'
Assert-Contains "guardian scripture dexterity effect test" $cardEffectCatalog 'GuardianScripture[\s\S]*?ApplyPower<DexterityPower>\(ctx\.Self,\s*2\)[\s\S]*?GuardianScriptureBlockVar\.BaseBlock\s*\+\s*2'
Assert-Contains "guardian scripture dynamic power text" $powerLocalization 'GUARDIAN_SCRIPTURE_POWER\.smartDescription"\s*:\s*"[^"]*\{Block\}'
Assert-NotContains "guardian scripture hard-coded power text" $powerLocalization 'GUARDIAN_SCRIPTURE_POWER\.(description|smartDescription)"\s*:\s*"[^"]*\u83B73\u70B9'

$neutralCardsBatch2 = Read-Text "src\Cards\MvpNeutralCardsBatch2.cs"
$neutralUtilityPowers = Read-Text "src\Powers\MvpNeutralUtilityPowers.cs"
Assert-Contains "mental unity unpowered block preview" $neutralCardsBatch2 'MentalUnity[\s\S]*?new\s+BlockVar\(2,\s*ValueProp\.Unpowered\s*\|\s*ValueProp\.Move\)'
Assert-Contains "mental unity unpowered trigger" $neutralUtilityPowers 'MentalUnityPower[\s\S]*?CreatureCmd\.GainBlock\(beneficiary,\s*Amount,\s*ValueProp\.Unpowered,\s*null\)'
Assert-Contains "mental unity dexterity preview test" $cardEffectCatalog 'MentalUnityProbe[\s\S]*?ApplyPower<DexterityPower>\(ctx\.Self,\s*2\)[\s\S]*?UpdateCardPreview\([\s\S]*?runGlobalHooks:\s*true\)[\s\S]*?triggered unpowered block ignores dexterity'

$scripturePowers = Read-Text "src\Powers\Scriptures\ScripturePowers.cs"
$guardianScriptureBlock = Read-Text "src\Powers\Scriptures\GuardianScriptureBlockVar.cs"
$cardEffectCatalog = Read-Text "src\Debugging\CardEffects\CardEffectTestCatalog.cs"
$powerLocalization = Read-Text "MaidenSuccubus\localization\zhs\powers.json"
Assert-Contains "guardian scripture powered block var" $scripturePowers 'GuardianScripturePower[\s\S]*?new\s+GuardianScriptureBlockVar\(\)'
Assert-Contains "guardian scripture canonical gain path" $scripturePowers 'GuardianScripturePower[\s\S]*?CreatureCmd\.GainBlock\(Owner,\s*DynamicVars\.Block,\s*null\)'
Assert-Contains "guardian scripture dynamic hook preview" $guardianScriptureBlock 'Hook\.ModifyBlock\('
Assert-Contains "guardian scripture powered move property" $guardianScriptureBlock 'ValueProp\.Move'
Assert-Contains "guardian scripture dexterity effect test" $cardEffectCatalog 'GuardianScripture[\s\S]*?ApplyPower<DexterityPower>\(ctx\.Self,\s*2\)[\s\S]*?GuardianScriptureBlockVar\.BaseBlock\s*\+\s*2'
Assert-Contains "guardian scripture dynamic power text" $powerLocalization 'GUARDIAN_SCRIPTURE_POWER\.smartDescription"\s*:\s*"[^"]*\{Block\}'
Assert-NotContains "guardian scripture hard-coded power text" $powerLocalization 'GUARDIAN_SCRIPTURE_POWER\.(description|smartDescription)"\s*:\s*"[^"]*\u83B73\u70B9'

$purificationPower = Read-Text "src\Powers\PurificationPower.cs"
Assert-Contains "purification enumerates all owner powers" $purificationPower 'Owner\.Powers'
Assert-Contains "purification only considers stackable counters" $purificationPower 'power\.StackType\s*==\s*PowerStackType\.Counter'
Assert-Contains "purification follows current debuff type" $purificationPower 'power\.TypeForCurrentAmount\s*==\s*PowerType\.Debuff'
Assert-Contains "purification selects uniformly by status type" $purificationPower '\.GroupBy\(power\s*=>\s*power\.Id\)'
Assert-NotContains "purification hard-coded debuff catalog" $purificationPower 'AddIfPresent<(Vulnerable|Weak|Frail|Condemnation)Power>'

$holyCardsBatch3 = Read-Text "src\Cards\MvpHolyCardsBatch3.cs"
Assert-Contains "Forge Nimble uses Kifuda Adroit hover" $holyCardsBatch3 'ForgeNimble[\s\S]*?FromEnchantment<Adroit>\(3\)'
Assert-Contains "Forge Nimble selects Adroit-compatible cards" $holyCardsBatch3 'ForgeNimble[\s\S]*?ModelDb\.Enchantment<Adroit>\(\)\.CanEnchant\(card\)'
Assert-Contains "Forge Nimble applies Kifuda Adroit" $holyCardsBatch3 'ForgeNimble[\s\S]*?ApplyVanilla<Adroit>\(selected,\s*3\)'
Assert-NotContains "Forge Nimble must not use block-scaling Nimble" $holyCardsBatch3 'ForgeNimble[\s\S]*?(FromEnchantment|Enchantment|ApplyVanilla)<Nimble>'

$intent = Read-Text "src\Core\Intents\IntentMoveFactory.cs"
Assert-NotContains "erotic intent ids" $intent 'Interlocked\.Increment'
Assert-Contains "erotic intent ids" $intent '\$"MAIDENSUCCUBUS_\{kind\}"'

$intentModels = Read-Text "src\Core\Intents\MaidenSuccubusIntents.cs"
$intentLocalization = Read-Text "MaidenSuccubus\localization\zhs\intents.json"
Assert-Contains "desire intent canonical title" $intentLocalization '"MAIDENSUCCUBUS_DESIRE\.title"\s*:\s*"\u6B32\u671B\u653B\u51FB"'
Assert-Contains "desire intent centered amount label" $intentModels 'DesireGainIntent[\s\S]*?ExtraIconAmountLabelSpec\.PlainCustom\('
Assert-NotContains "desire intent reserved vanilla corner" $intentModels 'DesireGainIntent[\s\S]*?ExtraIconAmountLabelCorner\.BottomRight'

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

$cardLibraryRoutePool = Read-Text "src\Patches\CardLibraryRoutePoolPatch.cs"
$generatedCards = Read-Text "src\Cards\MvpGeneratedCards.cs"
$basicCards = Read-Text "src\Cards\MvpBasicCards.cs"
Assert-Contains "card-library Ritsu ordering" $cardLibraryRoutePool '\[HarmonyAfter\("com\.ritsukage\.sts2-RitsuLib\.framework-character-assets"\)\]'
Assert-Contains "card-library final priority" $cardLibraryRoutePool '\[HarmonyPriority\(Priority\.Last\)\]'
Assert-Contains "card-library route cards" $cardLibraryRoutePool 'card\s+is\s+IMaidenSuccubusRouteCard'
Assert-Contains "card-library starter basics" $cardLibraryRoutePool 'card\s+is\s+MaidenStrike\s+or\s+MaidenDefend'
Assert-NotContains "card-library generated-pool leak" $cardLibraryRoutePool 'MSGeneratedCardPool'
Assert-Contains "generated-card library visibility defaults hidden" $generatedCards 'bool\s+shouldShowInCardLibrary\s*=\s*false'
Assert-Contains "generated-card library visibility forwarded" $generatedCards ':\s*base\(cost,\s*type,\s*rarity,\s*target,\s*shouldShowInCardLibrary\)'
Assert-Contains "Maiden Strike enters card-library source list" $basicCards 'MaidenStrike\(\)[\s\S]*?shouldShowInCardLibrary:\s*true'
Assert-Contains "Maiden Defend enters card-library source list" $basicCards 'MaidenDefend\(\)[\s\S]*?shouldShowInCardLibrary:\s*true'
Assert-Contains "Transform canonical-safe description" $basicCards 'Transform[\s\S]*?AddExtraArgsToDescription[\s\S]*?IsMutable\s*&&\s*Owner\?\.RunState'
Assert-Contains "Dark Element canonical-safe corrupt description" $basicCards 'DarkElement[\s\S]*?AddExtraArgsToDescription[\s\S]*?!IsMutable\s*\|\|\s*Owner\?\.RunState\s+is\s+not\s+RunState'

$frameworkSelfTests = Read-Text "src\Debugging\FrameworkSelfTests.cs"
Assert-Contains "route probability self-test magnitude 2" $frameworkSelfTests 'AssertProbabilities\(2,\s*0\.08m,\s*0\.20m,\s*0\.72m\)'
Assert-Contains "route probability self-test magnitude 4" $frameworkSelfTests 'AssertProbabilities\(4,\s*0\.00m,\s*0\.45m,\s*0\.55m\)'
Assert-Contains "route probability self-test magnitude 5" $frameworkSelfTests 'AssertProbabilities\(5,\s*0\.00m,\s*0\.65m,\s*0\.35m\)'
$modInitializer = Read-Text "src\MaidenSuccubusMod.cs"
Assert-Contains "runtime self-tests cannot block patch installation" $modInitializer 'RunFrameworkSelfTestsWithoutBlockingInitialization\(\)'
Assert-Contains "runtime self-test failure is logged" $modInitializer 'Framework self-tests failed; continuing mod initialization'
$iterationOneNeutralCards = Read-Text "src\Cards\Iteration1NeutralCards.cs"
Assert-Contains "Bath next-turn energy uses EnergyVar" $iterationOneNeutralCards 'new\s+EnergyVar\("NextEnergy",\s*2\)'

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

Write-Host "Validated structural contracts: isolated build/deploy gates, instance-attached Escape capability with preserved original state, instance-attached Battle Replay shadow, event-driven UI, simultaneous left-side and combat desire UI, canonical and collision-safe desire intent display, stable intent state, ordered three-route compendium filter without generated-pool leakage, shared derivative pool, map-safe fourth-route modal, unified sacrifice/sealed-card rest-site action, character scene."
