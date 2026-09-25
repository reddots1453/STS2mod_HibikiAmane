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
Assert-Contains "escape overlay is reloaded after projection changes" $escapeVisuals 'node\.Call\(NCard\.MethodName\.Reload\)'

$pickupEnchantment = Read-Text "src\Commands\PickupEnchantmentCmd.cs"
$combatEnchantment = Read-Text "src\Commands\CombatEnchantmentCmd.cs"
$enchantmentVfx = Read-Text "src\Commands\EnchantmentVfxCmd.cs"
$strengthCards = Read-Text "src\Cards\MvpStrengthCards.cs"
$neutralCardsBatch3 = Read-Text "src\Cards\MvpNeutralCardsBatch3.cs"
$cardEffectCatalog = Read-Text "src\Debugging\CardEffects\CardEffectTestCatalog.cs"
$iteration2ExpansionCards = Read-Text "src\Cards\Iteration2ExpansionCards.cs"
$advancedCards = Read-Text "src\Cards\MvpAdvancedCards.cs"
Assert-Contains "pickup enchantment model update" $pickupEnchantment 'CardCmd\.Enchant'
Assert-Contains "shared enchantment vanilla vfx" $enchantmentVfx 'NCardEnchantVfx\.Create\(previewCard\)'
Assert-Contains "shared enchantment preview container" $enchantmentVfx 'container\.AddChildSafely\(vfx\)'
Assert-Contains "projected enchantment vfx uses original-state clone" $enchantmentVfx 'SuppressPresentation\(\)[\s\S]*?MutableClone\(\)[\s\S]*?RemoveCapability<EscapeProjectionCapability>'
Assert-Contains "enchantment vfx is de-duplicated" $enchantmentVfx 'ActiveCards\.Add\(card\)[\s\S]*?TreeExited'
Assert-Contains "pickup enchantment uses deferred shared preview" $pickupEnchantment 'EnchantmentVfxCmd\.PreviewAfterCardPickup\(card\)'
Assert-Contains "pickup enchantment waits for ordinary card preview" $enchantmentVfx 'PreviewAfterCardPickupAsync[\s\S]*?GetChildren\(\)[\s\S]*?OfType<NCard>\(\)[\s\S]*?ReferenceEquals\(node\.Model,\s*card\)[\s\S]*?Preview\(card\)'
Assert-Contains "combat enchantment accesses original projected state" $combatEnchantment 'using\s*\(ControlQuery\.SuppressPresentation\(\)\)'
Assert-Contains "combat enchantment shared preview" $combatEnchantment 'AfterCombatEnchantmentApplied\(card\);[\s\S]*?EnchantmentVfxCmd\.Preview\(card\)[\s\S]*?return applied'
Assert-Contains "Kifuda Adroit is audited for temporary combat use" $combatEnchantment 'typeof\(T\)\s*!=\s*typeof\(Adroit\)'
Assert-Contains "dark storm pickup enchantment preview" $strengthCards 'PickupEnchantmentCmd\.EnchantAndPreview<Glam>'
Assert-Contains "magic sword pickup enchantment preview" $neutralCardsBatch3 'PickupEnchantmentCmd\.EnchantAndPreview<ChargeEnchantment>'
Assert-Contains "surf uses final displayed energy cost" $neutralCardsBatch3 'Surf[\s\S]*?GetWithModifiers\(CostModifiers\.All\)'
Assert-NotContains "surf avoids patched resource-payment cost" $neutralCardsBatch3 'Surf[\s\S]*?GetAmountToSpend\(\)[\s\S]*?public sealed class PressBack'
Assert-Contains "surf modified-cost effect coverage" $cardEffectCatalog 'SurfProbe[\s\S]*?TezcatarasEmber[\s\S]*?SetThisCombat\(1\)[\s\S]*?X cost accumulates as zero[\s\S]*?unplayable cost accumulates as zero'
Assert-Contains "gale sword pickup enchantment preview" $iteration2ExpansionCards 'PickupEnchantmentCmd\.EnchantAndPreview<Swift>'
Assert-Contains "shining sword pickup enchantment preview" $iteration2ExpansionCards 'PickupEnchantmentCmd\.EnchantAndPreview<Vigorous>'
Assert-Contains "Yarus memory pickup enchantment preview" $advancedCards 'PickupEnchantmentCmd\.EnchantAndPreview\('

$battleReplayPower = Read-Text "src\Powers\Iteration1HolyPowers.cs"
$battleReplayCard = Read-Text "src\Cards\Iteration1HolyCards.cs"
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
Assert-Contains "battle replay installs its listener when added during combat" $battleReplayCard 'AfterCardEnteredCombat\(CardModel card\)[\s\S]*?card == this[\s\S]*?EnsureReplayPower'

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

$holyCardsExpanded = Read-Text "src\Cards\HolyCardsExpanded.cs"
Assert-Contains "Sun Dance costs zero" $holyCardsExpanded 'SunDance\(\)[\s\S]*?:\s*base\(0,\s*CardType\.Skill,\s*CardRarity\.Common,\s*TargetType\.Self\)'
Assert-Contains "Sun Dance upgrades to retain without a cost change" $holyCardsExpanded 'SunDance[\s\S]*?OnUpgrade\(\)\s*=>\s*AddKeyword\(CardKeyword\.Retain\)'

$intent = Read-Text "src\Core\Intents\IntentMoveFactory.cs"
Assert-NotContains "erotic intent ids" $intent 'Interlocked\.Increment'
Assert-Contains "erotic intent ids" $intent '\$"MAIDENSUCCUBUS_\{kind\}"'

$intentModels = Read-Text "src\Core\Intents\MaidenSuccubusIntents.cs"
$customUiIconPatches = Read-Text "src\Patches\CustomUiIconPatches.cs"
$intentLocalization = Read-Text "MaidenSuccubus\localization\zhs\intents.json"
Assert-Contains "desire intent canonical title" $intentLocalization '"MAIDENSUCCUBUS_DESIRE\.title"\s*:\s*"\u6B32\u671B\u653B\u51FB"'
Assert-Contains "desire intent centered amount label" $intentModels 'DesireGainIntent[\s\S]*?ExtraIconAmountLabelSpec\.PlainCustom\('
Assert-NotContains "desire intent reserved vanilla corner" $intentModels 'DesireGainIntent[\s\S]*?ExtraIconAmountLabelCorner\.BottomRight'
Assert-Contains "custom intent sprite uses stable public update entry" $customUiIconPatches 'HarmonyPatch\(typeof\(NIntent\),\s*nameof\(NIntent\.UpdateIntent\)\)'
Assert-NotContains "custom intent sprite must not rely on inline-prone private update" $customUiIconPatches 'HarmonyPatch\(typeof\(NIntent\),\s*"UpdateVisuals"\)'
foreach ($intentType in @(
    "ControlIntent", "InvasionIntent", "DesireGainIntent", "TearClothingIntent")) {
    Assert-Contains "$intentType disables vanilla intent animation" $intentModels (
        'class\s+' + $intentType + '[\s\S]*?GetAnimation\([\s\S]*?\)\s*=>\s*null!;')
}
$intentFactory = Read-Text "src\Core\Intents\IntentMoveFactory.cs"
Assert-Contains "desire attack derives supplemental icons from effect parser" $intentFactory 'BuildDesireIntents[\s\S]*?BuildSupplementalIntents\([\s\S]*?EroticIntentKind\.Desire'
Assert-Contains "invasion stun is queued after an executing move" $intentFactory 'if\s*\(monster\.IsPerformingMove\)[\s\S]*?current\.FollowUpState\s*=\s*stun'
Assert-Contains "duplicate stun scheduling is coalesced" $intentFactory 'ForceStun\(MonsterModel monster\)[\s\S]*?if\s*\(!ShouldQueueStun\(current\)\)[\s\S]*?return;'
Assert-Contains "stun cannot follow itself by state id" $intentFactory 'ShouldQueueStun\(MoveState current\)[\s\S]*?current\.FollowUpStateId\s*==\s*"STUNNED"'
Assert-Contains "stun cannot follow itself by move instance" $intentFactory 'ShouldQueueStun\(MoveState current\)[\s\S]*?current\.FollowUpState is not MoveState followUp[\s\S]*?!IsStunMove\(followUp\)'

$energyFormatter = Read-Text "src\Localization\MaidenEnergyIconsFormatter.cs"
$desireFormatter = Read-Text "src\Localization\MaidenDesireIconsFormatter.cs"
$formatterRegistration = Read-Text "src\Localization\MaidenLocalizationFormatters.cs"
$energyFormatterPatch = Read-Text "src\Patches\MaidenEnergyFormatterPatch.cs"
$modInitializer = Read-Text "src\MaidenSuccubusMod.cs"
$energyCardLocalization = Read-Text "MaidenSuccubus\localization\zhs\cards.json"
Assert-Contains "maiden inline energy formatter owns a distinct name" $energyFormatter 'get\s*=>\s*"maidenEnergyIcons"'
Assert-Contains "maiden inline energy formatter uses font-sized resource" $energyFormatter 'magic_energy_cost_icon_32\.png'
Assert-Contains "maiden inline energy formatter follows vanilla image markup" $energyFormatter '\[img\]\{MaidenEnergyIconAssets\.TextIconResourcePath\}\[/img\]'
Assert-NotContains "maiden inline energy formatter must not bypass autosize measurement" $energyFormatter '\[img=[^\]]+'
Assert-Contains "maiden inline formatter registration reads Smart.Default" $formatterRegistration 'Smart\.Default'
Assert-Contains "maiden inline formatter registration targets LocManager active formatter" $formatterRegistration 'AccessTools\.Field\(typeof\(LocManager\),\s*"_smartFormatter"\)'
Assert-Contains "maiden inline formatter registration is idempotent" $formatterRegistration 'GetFormatterExtensions\(\)\.Any'
Assert-Contains "maiden inline energy formatter registration" $formatterRegistration 'new\s+MaidenEnergyIconsFormatter\(\)'
Assert-Contains "maiden inline formatter registers during mod init" $modInitializer 'MaidenLocalizationFormatters\.Register\(\)'
Assert-Contains "maiden inline formatter re-registers after localization reload" $energyFormatterPatch 'MaidenLocalizationFormatters\.Register'
Assert-Contains "maiden inline formatter registers after LocManager construction" $energyFormatterPatch 'HarmonyPatch\(typeof\(LocManager\),\s*nameof\(LocManager\.Initialize\)\)'
Assert-NotContains "maiden inline formatter must not patch exception-filtered SmartFormat" $energyFormatterPatch 'nameof\(LocManager\.SmartFormat\)'
Assert-NotContains "maiden localization must not use vanilla energy formatter" $energyCardLocalization ':energyIcons\('
Assert-Contains "maiden inline desire formatter owns a distinct name" $desireFormatter 'get\s*=>\s*"maidenDesireIcons"'
Assert-Contains "maiden inline desire formatter uses font-sized resource" $desireFormatter 'desire_resource_icon_32\.png'
Assert-Contains "maiden inline desire formatter follows vanilla image markup" $desireFormatter '\$"\[img\]\{TextIconResourcePath\}\[/img\]"'
Assert-NotContains "maiden inline desire formatter must not bypass autosize measurement" $desireFormatter '\[img=[^\]]+'
Assert-Contains "maiden inline desire formatter registration" $formatterRegistration 'new\s+MaidenDesireIconsFormatter\(\)'
Assert-Contains "card localization uses desire icons" $energyCardLocalization ':maidenDesireIcons\('
foreach ($cardKey in @("ECSTASY_DEW", "BLASPHEMOUS_DESIRE", "TRANQUILIZER", "PURIFICATION_ORB")) {
    Assert-Contains "$cardKey uses deterministic desire icon text" $energyCardLocalization (
        'MAIDEN_SUCCUBUS_CARD_' + $cardKey + '\.description"\s*:\s*"[^\"]*\{DesireIcons\}')
}
Assert-Contains "All Hope Lost has a symbolic non-combat description" $energyCardLocalization 'ALL_HOPE_LOST\.description"\s*:\s*"\u9020\u6210\{InCombat:\{Damage:diff\(\)\}\|6X\}\u70B9\u4F24\u5BB3\{InCombat:\{Hits:diff\(\)\}\|Y\}\u6B21'
Assert-Contains "Soul Fuenika names the selected-card copy" $energyCardLocalization 'SOUL_FUENIKA\.description"\s*:\s*"[^"]*\u9009\u62E9\u7684\u724C\u7684\u590D\u5236'
$exhaustCards = Read-Text "src\Cards\MvpExhaustCards.cs"
Assert-Contains "destruction reaction selects newly drawn cards from the hand UI" $exhaustCards 'class\s+DestructionReaction[\s\S]*?CardSelectCmd\.FromHand'
Assert-NotContains "exhaust cards must not fall back to detached simple-grid selection" $exhaustCards 'CardSelectCmd\.FromSimpleGrid'
$chainDestructionPowers = Read-Text "src\Powers\MvpExhaustPowers2.cs"
Assert-Contains "chain destruction exposes an orbit-style remaining counter" $chainDestructionPowers 'class\s+ChainDestructionPower[\s\S]*?DisplayAmount\s*=>\s*4\s*-\s*ExhaustProgress\s*%\s*4'
Assert-Contains "chain destruction arms a separate visible replay state" $chainDestructionPowers 'PowerCmd\.Apply<ChainDestructionReplayPower>'
$permanentCardCmd = Read-Text "src\Commands\PermanentCardCmd.cs"
Assert-Contains "generated permanent-growth copies may omit a deck version" $permanentCardCmd 'TryModifyDeckVersion[\s\S]*?if\s*\(deckCard\s*==\s*null\)[\s\S]*?return\s+false'
$desireWord = [regex]::Unescape('\u6b32\u671b')
$pointWord = [regex]::Unescape('\u70b9')
$consumeWord = [regex]::Unescape('\u6d88\u8017')
$spendWord = [regex]::Unescape('\u82b1\u8d39')
Assert-NotContains "numeric card desire units must use icons" $energyCardLocalization "(?:\d+|\{[^}]+\})(?:$pointWord)?\[pink\]$desireWord\[/pink\]"
Assert-NotContains "card desire costs must use icons" $energyCardLocalization "(?:$consumeWord\[/gold\]|$spendWord)\[pink\]$desireWord\[/pink\]"

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
$cardLocalization = Read-Text "MaidenSuccubus\localization\zhs\cards.json"
Assert-Contains "card-library Ritsu ordering" $cardLibraryRoutePool '\[HarmonyAfter\("com\.ritsukage\.sts2-RitsuLib\.framework-character-assets"\)\]'
Assert-Contains "card-library final priority" $cardLibraryRoutePool '\[HarmonyPriority\(Priority\.Last\)\]'
Assert-Contains "card-library route cards" $cardLibraryRoutePool 'card\s+is\s+IMaidenSuccubusRouteCard'
Assert-NotContains "card-library starter-basic special case" $cardLibraryRoutePool 'MaidenStrike\s+or\s+MaidenDefend'
Assert-NotContains "card-library generated-pool leak" $cardLibraryRoutePool 'MSGeneratedCardPool'
Assert-Contains "generated-card library visibility defaults hidden" $generatedCards 'bool\s+shouldShowInCardLibrary\s*=\s*false'
Assert-Contains "generated-card library visibility forwarded" $generatedCards ':\s*base\(cost,\s*type,\s*rarity,\s*target,\s*shouldShowInCardLibrary\)'
Assert-Contains "Maiden Strike belongs to character card pool" $basicCards '\[RegisterCard\(typeof\(MSNeutralCardPool\)\)\]\s*public\s+sealed\s+class\s+MaidenStrike\s*:\s*MSNeutralCard'
Assert-Contains "Maiden Defend belongs to character card pool" $basicCards '\[RegisterCard\(typeof\(MSNeutralCardPool\)\)\]\s*public\s+sealed\s+class\s+MaidenDefend\s*:\s*MSNeutralCard'
Assert-NotContains "starter basics must not use derivative pool" $basicCards '\[RegisterCard\(typeof\(MSGeneratedCardPool\)\)\][\s\S]{0,80}(MaidenStrike|MaidenDefend)'
Assert-Contains "Maiden Strike remains a basic Strike" $basicCards 'MaidenStrike[\s\S]*?CanonicalTags\s*=>\s*\[CardTag\.Strike\][\s\S]*?CardRarity\.Basic'
Assert-Contains "Maiden Defend remains a basic Defend" $basicCards 'MaidenDefend[\s\S]*?CanonicalTags\s*=>\s*\[CardTag\.Defend\][\s\S]*?CardRarity\.Basic'
Assert-Contains "Transform canonical-safe description" $basicCards 'Transform[\s\S]*?AddExtraArgsToDescription[\s\S]*?IsMutable\s*&&\s*Owner\?\.RunState'
Assert-Contains "Dark Element canonical-safe holy variation" $basicCards 'DarkElement[\s\S]*?IsMutable[\s\S]*?CorruptionQuery\.Get\(runState\)\s*<=\s*-3'
Assert-Contains "Transform variation changes route identity" $basicCards 'Transform[\s\S]*?override\s+RouteCardKind\s+RouteKind[\s\S]*?CorruptionQuery\.Get\(runState\)\s*>=\s*3[\s\S]*?RouteCardKind\.Corrupt[\s\S]*?RouteCardKind\.Holy'
Assert-Contains "Dark Element variation changes route identity" $basicCards 'DarkElement[\s\S]*?override\s+RouteCardKind\s+RouteKind\s*=>\s*IsHolyVariation[\s\S]*?RouteCardKind\.Holy[\s\S]*?RouteCardKind\.Corrupt'
Assert-Contains "Dark Element corrupt release repeats damage" $basicCards 'DarkElement[\s\S]*?if\s*\(!IsHolyVariation\)[\s\S]*?DamageCmd\.Attack[\s\S]*?else[\s\S]*?CreatureCmd\.GainBlock'
Assert-Contains "Dark Element minus-three localization" $cardLocalization 'MAIDEN_SUCCUBUS_CARD_DARK_ELEMENT\.description"\s*:\s*"[^"]*\u9B54\u529B\u89E3\u653E[^"]*\u9020\u6210\{Damage:diff\(\)\}[^"]*\u5815\u843D\u503C[^\"]*≤-3[^\"]*\u5815\u843D\u503C[^\"]*＞-3'
Assert-Contains "Dark Element zero-corruption effect coverage" $cardEffectCatalog 'DarkElementProbe[\s\S]*?Set\(runState,\s*0\)[\s\S]*?zero corruption remains the corrupt base form[\s\S]*?zero-corruption magic release repeats amplified damage'

$frameworkSelfTests = Read-Text "src\Debugging\FrameworkSelfTests.cs"
Assert-Contains "route probability self-test magnitude 2" $frameworkSelfTests 'AssertProbabilities\(2,\s*0\.08m,\s*0\.20m,\s*0\.72m\)'
Assert-Contains "route probability self-test magnitude 4" $frameworkSelfTests 'AssertProbabilities\(4,\s*0\.00m,\s*0\.45m,\s*0\.55m\)'
Assert-Contains "duplicate stun scheduling self-test" $frameworkSelfTests 'AssertStunSchedulingIsIdempotent\(\)'
Assert-Contains "route probability self-test magnitude 5" $frameworkSelfTests 'AssertProbabilities\(5,\s*0\.00m,\s*0\.65m,\s*0\.35m\)'
$modInitializer = Read-Text "src\MaidenSuccubusMod.cs"
Assert-Contains "runtime self-tests cannot block patch installation" $modInitializer 'RunFrameworkSelfTestsWithoutBlockingInitialization\(\)'
Assert-Contains "runtime self-test failure is logged" $modInitializer 'Framework self-tests failed; continuing mod initialization'
$iterationOneNeutralCards = Read-Text "src\Cards\Iteration1NeutralCards.cs"
Assert-Contains "Bath next-turn energy uses EnergyVar" $iterationOneNeutralCards 'new\s+EnergyVar\("NextEnergy",\s*2\)'

$desireResource = Read-Text "src\Core\Desire\DesireResource.cs"
$desireMeter = Read-Text "src\UI\DesireMeter.cs"
$desireFacade = Read-Text "src\Data\Desire.cs"
$desireResourceRules = Read-Text "src\Core\Desire\DesireResourceRules.cs"
$desirePersistence = Read-Text "src\Core\Desire\DesirePersistenceCoordinator.cs"
$temptationMeter = Read-Text "src\UI\TemptationMeter.cs"
Assert-Contains "combat desire UI" $desireResource 'RegisterCombatUi\('
Assert-Contains "combat desire UI" $desireResource 'AlwaysShowInCombatUiForCharacter<MaidenSuccubusCharacter>'
Assert-Contains "persistent left-side desire UI" $desireMeter 'displayed along the left side in and out of combat'
Assert-Contains "combat transition refreshes left-side desire UI" $desireMeter 'private\s+void\s+OnCombatVisibilityChanged\(bool\s+_\)'
Assert-Contains "combat transition refreshes temptation UI" $temptationMeter 'OnCombatVisibilityChanged\(bool\s+_\)[\s\S]*?CallDeferred\(nameof\(Refresh\)\)'
Assert-Contains "temptation meter retries until local player is published" $temptationMeter '_player\s*==\s*null\s*&&\s*_initialRefreshAttempts\+\+\s*<\s*4'
Assert-Contains "temptation meter remains visible outside combat" $temptationMeter 'Visible\s*=\s*_player\?\.Character\s+is\s+MaidenSuccubusCharacter'
Assert-NotContains "temptation meter must not be combat-only" $temptationMeter 'Visible\s*=\s*inCombat'
Assert-NotContains "combat must not hide left-side desire UI" $desireMeter 'Visible\s*=\s*!inCombat'
Assert-NotContains "combat must not suppress left-side desire UI refresh" $desireMeter 'CombatManager\.Instance\.IsInProgress'
Assert-Contains "desire has per-player cross-combat storage" $desireFacade 'PlayerRunSavedData<DesireAmountState>\s+AmountHandle'
Assert-Contains "non-combat desire reads persistent value" $desireFacade 'if\s*\(HasCombatState\(player\)\)[\s\S]*?AmountHandle\.Get\(player\)'
Assert-Contains "combat desire updates persistent value" $desireResourceRules 'RememberCombatValue\([\s\S]*?context\.NewAmount'
Assert-Contains "combat start restores persistent desire" $desirePersistence 'SubscribeLifecycle<CombatStartingEvent>[\s\S]*?SecondaryResourcePersistence\.RestoreSnapshot'
Assert-Contains "combat start explicitly refreshes restored desire" $desirePersistence 'DesireEvents\.Publish\(new DesireChanged\([\s\S]*?finalValue,[\s\S]*?finalValue'
Assert-Contains "combat end captures desire" $desirePersistence 'SubscribeLifecycle<CombatEndedEvent>[\s\S]*?RememberCombatValue'

$characterModel = Read-Text "src\Characters\MaidenSuccubusCharacter.cs"
Assert-Contains "character hook rechecks cross-combat desire" $characterModel 'BeforeCombatStart[\s\S]*?DesirePersistenceCoordinator\.RestoreForCombat'

$characterVisuals = Read-Text "src\UI\MaidenSuccubusCreatureVisuals.cs"
Assert-Contains "combat feedback preserves surrounded facing" $characterVisuals 'WithCurrentFacing\(scale\)[\s\S]*?WithCurrentFacing\(RestScale\)'
Assert-Contains "high-desire warning uses smooth pixel-distance falloff" $characterVisuals 'edge_pixels[\s\S]*?SCREEN_PIXEL_SIZE[\s\S]*?smoothstep'
Assert-Contains "high-desire warning expands inward" $characterVisuals 'shader_parameter/glow_width_px[\s\S]*?220f[\s\S]*?280f'
Assert-NotContains "high-desire warning must not use rectangular edge strips" $characterVisuals 'AddEdgeRect'

$sidebarRail = Read-Text "src\UI\MaidenSidebarRail.cs"
Assert-Contains "resource widgets share a vertical rail" $sidebarRail 'TemptationPosition\s*=\s*new\(7f,\s*8f\)[\s\S]*?DesirePosition\s*=\s*new\(7f,\s*88f\)'
Assert-Contains "resource rail hides for settings" $sidebarRail 'SettingsOpened\s*\+=\s*OnSettingsOpened[\s\S]*?Visible\s*=\s*!open'
Assert-Contains "resource rail clears hover tips while hidden" $sidebarRail 'NHoverTipSet\.Remove\(child\)'

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
