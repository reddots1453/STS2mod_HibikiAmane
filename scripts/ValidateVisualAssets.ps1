param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"
$ProjectDir = (Resolve-Path -LiteralPath $ProjectDir).Path

function Assert-ExactCopy([string]$Source, [string]$Destination) {
    if (!(Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Visual source asset is missing: $Source"
    }
    if (!(Test-Path -LiteralPath $Destination -PathType Leaf)) {
        throw "Runtime visual asset is missing: $Destination"
    }
    $sourceHash = Get-Sha256 $Source
    $destinationHash = Get-Sha256 $Destination
    if ($sourceHash -ne $destinationHash) {
        throw "Runtime visual asset differs from reviewed source: $Destination"
    }
}

function Get-Sha256([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($sha.ComputeHash($stream)).Replace("-", "")
    } finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Assert-ManifestCopy(
    [string]$Source,
    [string]$Destination,
    [string]$ExpectedHash
) {
    Assert-ExactCopy $Source $Destination
    $actualHash = Get-Sha256 $Source
    if ($actualHash -ne $ExpectedHash.ToUpperInvariant()) {
        throw "Formal visual source hash differs from manifest: $Source"
    }
}

$runtime = Join-Path $ProjectDir "MaidenSuccubus\images"
$reviewedAssetsByName = @{}
Get-ChildItem -LiteralPath $ProjectDir -Recurse -File | Where-Object {
    !$_.FullName.StartsWith($runtime, [System.StringComparison]::OrdinalIgnoreCase) -and
    $_.FullName -notmatch '[\\/]\.review[\\/]'
} | ForEach-Object {
    if (!$reviewedAssetsByName.ContainsKey($_.Name)) {
        $reviewedAssetsByName[$_.Name] = @()
    }
    $reviewedAssetsByName[$_.Name] += $_.FullName
}

function Find-ReviewedAsset([string]$FileName) {
    $matches = @($reviewedAssetsByName[$FileName])
    if ($matches.Count -ne 1) {
        throw "Expected one reviewed source named '$FileName', found $($matches.Count)."
    }
    return $matches[0]
}

$cardArtRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u5b8c\u6210\u7248\u5361\u56fe")
$cardArtSource = Join-Path $ProjectDir $cardArtRelativePath
$cardArtManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $cardArtSource "manifest.json") | ConvertFrom-Json
$cardRuntime = Join-Path $runtime "cards"
$cardRuntimeFiles = @(Get-ChildItem -LiteralPath $cardRuntime -Filter "*.png" -File)
if ($cardRuntimeFiles.Count -lt ($cardArtManifest.items.Count + 1)) {
    throw "Expected at least $($cardArtManifest.items.Count) dedicated formal card arts plus one default runtime art."
}
$seenCardClasses = @{}
foreach ($item in $cardArtManifest.items) {
    if ($seenCardClasses.ContainsKey($item.class)) {
        throw "Duplicate C# class in formal card-art manifest: $($item.class)"
    }
    $seenCardClasses[$item.class] = $true
    Assert-ManifestCopy (
        Join-Path $cardArtSource $item.file) (
        Join-Path $cardRuntime "$($item.class).png") $item.sha256
}
Assert-ManifestCopy (
    Join-Path $cardArtSource $cardArtManifest.default.file) (
    Join-Path $cardRuntime "default.png") $cardArtManifest.default.sha256

# Every accepted PNG in the formal root must be mapped to a concrete card
# class, and every runtime PNG must be accounted for by the same manifest.
$formalFiles = @{}
foreach ($item in $cardArtManifest.items) { $formalFiles[$item.file] = $true }
$formalFiles[$cardArtManifest.default.file] = $true
foreach ($file in Get-ChildItem -LiteralPath $cardArtSource -Filter '*.png' -File) {
    if (!$formalFiles.ContainsKey($file.Name)) {
        throw "Unmapped accepted formal card art: $($file.Name)"
    }
}
if (@(Get-ChildItem -LiteralPath $cardArtSource -Filter '*.png' -File).Count -ne $formalFiles.Count -or
    $cardRuntimeFiles.Count -ne ($cardArtManifest.items.Count + 1)) {
    throw "Formal card source/runtime counts do not match the manifest."
}
foreach ($file in $cardRuntimeFiles) {
    if ($file.BaseName -ne 'default' -and !$seenCardClasses.ContainsKey($file.BaseName)) {
        throw "Runtime card art has no accepted manifest entry: $($file.Name)"
    }
}

$corruptionRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u5815\u843d\u503c\u5929\u5e73")
$corruptionSource = Join-Path $ProjectDir $corruptionRelativePath
$corruptionManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $corruptionSource "manifest.json") | ConvertFrom-Json
foreach ($state in $corruptionManifest.states.psobject.Properties.Value) {
    Assert-ManifestCopy (
        Join-Path $corruptionSource $state.file) (
        Join-Path $runtime "ui\corruption\$($state.file)") $state.sha256
}

foreach ($value in 0..10) {
    $file = "desire_meter_{0:D2}.png" -f $value
    Assert-ExactCopy (Find-ReviewedAsset $file) (
        Join-Path $runtime "ui\desire_meter\$file")
}

$coreCopies = @(
    @("hibiki_amane_character_icon_128.png", "ui\core\hibiki_amane_character_icon_128.png"),
    @("hibiki_amane_character_icon_outline_128.png", "ui\core\hibiki_amane_character_icon_outline_128.png"),
    @("hibiki_amane_character_icon_256.png", "ui\core\hibiki_amane_character_icon_256.png"),
    @("hibiki_amane_character_icon_outline_256.png", "ui\core\hibiki_amane_character_icon_outline_256.png"),
    @("magic_energy_cost_icon_32.png", "ui\core\magic_energy_cost_icon_32.png"),
    @("magic_energy_cost_icon_128.png", "ui\core\magic_energy_cost_icon_128.png"),
    @("desire_resource_icon_32.png", "ui\core\desire_resource_icon_32.png"),
    @("desire_resource_icon_128.png", "ui\core\desire_resource_icon_128.png"),
    @("route_holy_angel_wing_v4.png", "ui\route_marks\route_holy_angel_wing_v4.png"),
    @("route_corrupt_succubus_wing_v4.png", "ui\route_marks\route_corrupt_succubus_wing_v4.png")
)
foreach ($copy in $coreCopies) {
    Assert-ExactCopy (Find-ReviewedAsset $copy[0]) (Join-Path $runtime $copy[1])
}
foreach ($legacyRouteMark in @(
    "ui\route_marks\route_holy_wing_v3.png",
    "ui\route_marks\route_corrupt_wing_v3.png"
)) {
    if (Test-Path -LiteralPath (Join-Path $runtime $legacyRouteMark) -PathType Leaf) {
        throw "Legacy V3 route mark must not remain in the runtime package: $legacyRouteMark"
    }
}

$characterSelectRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u9009\u89d2\u754c\u9762/" +
    "V4\u539f\u4f5c\u753b\u98ce_20261001/\u4fdd\u7559\u7684\u524d\u7248\u517c\u5bb9\u8d44\u6e90")
foreach ($file in @(
    "hibiki_amane_char_select_bg_v02_2561x1201.png",
    "hibiki_amane_character_icon_v02_256.png",
    "hibiki_amane_character_icon_outline_v02_256.png"
)) {
    Assert-ExactCopy (Join-Path $ProjectDir "$characterSelectRelativePath\$file") (
        Join-Path $runtime "ui\character_select\$file")
}
if (Test-Path -LiteralPath (
    Join-Path $runtime "ui\character_select\hibiki_amane_char_select_bg_v01_2561x1201.png") `
    -PathType Leaf) {
    throw "Legacy V1 character-select background must not remain in the runtime package."
}

$characterV4Source = Join-Path $ProjectDir ([regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u9009\u89d2\u754c\u9762/V4\u539f\u4f5c\u753b\u98ce_20261001"))
$characterV4Manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $characterV4Source "manifest.json") | ConvertFrom-Json
if ($characterV4Manifest.assets.Count -ne 3) {
    throw "Expected three reviewed V4 character-select assets."
}
foreach ($asset in $characterV4Manifest.assets) {
    Assert-ManifestCopy (Join-Path $characterV4Source $asset.file) (
        Join-Path $runtime "ui\character_select\$($asset.file)") $asset.sha256
}

$schoolUniformRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u53d8\u8eab\u5f62\u6001/" +
    "\u6821\u670d\u5f62\u6001/\u666e\u901a\u6821\u670d.png")
$schoolUniformSource = Join-Path $ProjectDir $schoolUniformRelativePath
Assert-ExactCopy $schoolUniformSource (
    Join-Path $runtime "character\character_normal.png")

$intentRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u72b6\u6001\u56fe\u6807/" +
    "\u6b63\u5f0f\u7d20\u6750/\u610f\u56fe\u56fe\u6807")
$intentSource = Join-Path $ProjectDir $intentRelativePath
$intentNames = @(
    "desire_gain_intent",
    "restraint_attack_intent",
    "restraint_power_intent",
    "restraint_skill_intent",
    "tear_clothing_intent"
)
foreach ($size in @("64x64", "256x256")) {
    $suffix = if ($size -eq "256x256") { "_big" } else { "" }
    foreach ($name in $intentNames) {
        $file = "$name$suffix.png"
        Assert-ExactCopy (Join-Path $intentSource "$size\$file") (
            Join-Path $runtime "intents\$size\$file")
    }
}

$temptationRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u72b6\u6001\u56fe\u6807/" +
    "\u6b63\u5f0f\u7d20\u6750/\u8bf1\u60d1\u5ea6UI/" +
    "temptation_lipstick_64.png")
Assert-ExactCopy (Join-Path $ProjectDir $temptationRelativePath) (
    Join-Path $runtime "ui\temptation\temptation_lipstick_64.png")

$worldRelativePath = [regex]::Unescape(
    "\u56fe\u7247\u7d20\u6750/\u706b\u5806\u4e0e\u5546\u5e97\u89d2\u8272/" +
    "\u6b63\u5f0f\u7d20\u6750")
$worldSource = Join-Path $ProjectDir $worldRelativePath
foreach ($file in @("hibiki_amane_rest_site.png", "hibiki_amane_merchant.png")) {
    Assert-ExactCopy (Join-Path $worldSource $file) (
        Join-Path $runtime "character\$file")
}

foreach ($size in @("64x64", "256x256")) {
    $runtimeDir = Join-Path $runtime "powers\$size"
    $runtimeFiles = @(Get-ChildItem -LiteralPath $runtimeDir -Filter "*.png" -File)
    if ($runtimeFiles.Count -ne 72) {
        throw "Expected 72 runtime $size power/mechanism icons, found $($runtimeFiles.Count)."
    }
    foreach ($runtimeFile in $runtimeFiles) {
        Assert-ExactCopy (Find-ReviewedAsset $runtimeFile.Name) $runtimeFile.FullName
    }
}

$corruptionCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\CorruptionMeter.cs")
$desireCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\DesireMeter.cs")
$routeCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\RouteCardVisuals.cs")
$powerCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\PowerIconAssets.cs")
$characterCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Characters\MaidenSuccubusCharacter.cs")
if ($corruptionCode -notmatch 'corruption_balance_\{state\}\.png') {
    throw "Corruption meter is not wired to reviewed state textures."
}
# Keep hover hitboxes aligned with the five visible ticks in the 2172x724 art.
$tickMatches = [regex]::Matches($corruptionCode, '\((-?\d+), (\d+)f\)')
$expectedTicks = @(@(-5, 560), @(-3, 770), @(0, 1086), @(3, 1401), @(5, 1611))
if ($tickMatches.Count -ne $expectedTicks.Count -or
    $corruptionCode -notmatch 'MeterHeight = 85f' -or
    $corruptionCode -notmatch 'tick.ArtworkX / 2172f \* MeterWidth' -or
    $corruptionCode -notmatch 'centerX - HoverWidth / 2f, HoverTop' -or
    $corruptionCode -match 'SegmentCount') {
    throw "Corruption hover zones must use artwork tick centers, not canvas segments."
}
$hoverWidth = [double]([regex]::Match($corruptionCode, 'HoverWidth = ([\d.]+)f').Groups[1].Value)
for ($i = 0; $i -lt $expectedTicks.Count; $i++) {
    $value = [int]$tickMatches[$i].Groups[1].Value
    $x = [double]$tickMatches[$i].Groups[2].Value
    if ($value -ne $expectedTicks[$i][0] -or $x -ne $expectedTicks[$i][1]) {
        throw "Corruption hover tick $i differs from the reviewed artwork."
    }
    # Check screen-scaled centers and non-overlap at common UI scales.
    foreach ($scale in @(0.75, 1.0, 1.25, 1.5, 2.0)) {
        $center = $x / 2172 * 256 * $scale
        $halfWidth = $hoverWidth / 2 * $scale
        if ($halfWidth -le 0 -or $center - $halfWidth -lt 0 -or
            $center + $halfWidth -gt 256 * $scale) {
            throw "Corruption hover tick $value lies outside the meter."
        }
        if ($i -gt 0 -and
            ($x - $expectedTicks[$i - 1][1]) / 2172 * 256 * $scale -le 2 * $halfWidth) {
            throw "Corruption hover tick $value overlaps its neighbor."
        }
    }
}
Write-Host "Validated corruption hover geometry: -5/-3/0/+3/+5, five UI scales."
$libraryCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Patches\CardLibraryRoutePoolPatch.cs")
if ($libraryCode -notmatch 'pair.Key is MaidenSuccubusCharacter' -or
    $libraryCode -notmatch 'entry.Value.GetNodeOrNull<TextureRect>\("Image"\)' -or
    $libraryCode -notmatch 'ui/core/hibiki_amane_character_icon_128.png' -or
    $libraryCode -notmatch 'image.Texture = texture;') {
    throw "Compendium character filter must assign the reviewed Maiden icon to its own Image."
}
if ($characterCode -notmatch 'new CharacterUiAssetSet\(' -or
    $characterCode -notmatch 'string iconPath = RuntimeTextureAssets\.PrepareResource\(' -or
    $characterCode -notmatch 'IconPath: iconPath' -or
    $characterCode -notmatch 'ui/core/hibiki_amane_character_icon_128\.png' -or
    $characterCode -notmatch 'user://maiden_succubus_character_icon\.tres' -or
    $characterCode -notmatch 'CharacterSelectBgPath: characterSelectBgPath' -or
    $characterCode -notmatch 'CharacterSelectIconPath: characterSelectIconPath' -or
    $characterCode -notmatch 'CharacterSelectLockedIconPath: characterSelectLockedIconPath' -or
    $characterCode -notmatch 'hibiki_amane_select_bg_v04_2561x1201\.png' -or
    $characterCode -notmatch 'user://maiden_succubus_character_select_bg_v04\.tres') {
    throw "Character profile must route the reviewed Maiden icon through RitsuLib Ui.IconPath for the top bar."
}
$cardArtCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\CardArtAssets.cs")
$allCardFiles = Get-ChildItem -LiteralPath (Join-Path $ProjectDir "src\Cards") `
    -Filter "*.cs" -Recurse
$allCardCode = ($allCardFiles | ForEach-Object {
            Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName
        }) -join "`n"
if ($cardArtCode -notmatch 'cards/\{cardType\.Name\}\.png' -or
    $cardArtCode -notmatch 'cards/default\.png' -or
    $cardArtCode -match 'PrepareResource' -or
    $allCardCode -match 'card_portraits/ironclad/bash\.png') {
    throw "Maiden card art must resolve by class without serializing user resources in the compendium hot path."
}
$cardPresentationCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Patches\CardArtPresentationPatch.cs")
if ($cardPresentationCode -notmatch 'HarmonyPatch\(typeof\(CardModel\), nameof\(CardModel\.Portrait\), MethodType\.Getter\)' -or
    $cardPresentationCode -notmatch '__result = texture;' -or
    $cardPresentationCode -notmatch 'HarmonyPatch\(typeof\(NCard\), "Reload"\)' -or
    $cardPresentationCode -notmatch 'HarmonyPatch\(typeof\(NInspectCardScreen\), "UpdateCardDisplay"\)' -or
    $cardPresentationCode -notmatch 'AccessTools\.DeclaredField\(typeof\(NCard\), "_portrait"\)' -or
    $cardPresentationCode -notmatch 'portrait\.Texture = texture;' -or
    $cardPresentationCode -match 'SetDeferred') {
    throw "Card art presentation must replace CardModel.Portrait and synchronously cover normal cards and the inspect-card HD view."
}
$characterSelectPatchCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Patches\CharacterSelectVisualPatch.cs")
if ($characterSelectPatchCode -notmatch 'NCharacterSelectButton' -or
    $characterSelectPatchCode -notmatch 'hibiki_amane_select_normal_v04_132x195\.png' -or
    $characterSelectPatchCode -notmatch 'hibiki_amane_select_locked_v04_132x195\.png' -or
    $characterSelectPatchCode -notmatch '"CharacterSelectIconPath", MethodType\.Getter' -or
    $characterSelectPatchCode -notmatch '"CharacterSelectLockedIconPath", MethodType\.Getter' -or
    $characterSelectPatchCode -notmatch 'char_select_ironclad\.png' -or
    $characterSelectPatchCode -notmatch 'char_select_ironclad_locked\.png' -or
    $characterSelectPatchCode -notmatch '__instance is MaidenSuccubusCharacter' -or
    $characterSelectPatchCode -notmatch '__result = VanillaSelectIcon;' -or
    $characterSelectPatchCode -notmatch '__result = VanillaLockedSelectIcon;' -or
    $characterSelectPatchCode -notmatch 'nameof\(CharacterModel\.CharacterSelectIcon\), MethodType\.Getter' -or
    $characterSelectPatchCode -notmatch 'nameof\(CharacterModel\.CharacterSelectLockedIcon\), MethodType\.Getter' -or
    $characterSelectPatchCode -notmatch 'ResourceLoader\.Load<CompressedTexture2D>') {
    throw "Character-select buttons need compressed vanilla init paths and the reviewed 132x195 V4 Maiden overlays."
}
$thresholdPowerCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Powers\EroticIntentThresholdPowers.cs")
if ($thresholdPowerCode -notmatch 'PowerStackType\.Counter') {
    throw "Erotic intent threshold Powers must expose their threshold amount on the icon."
}
$intentCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\UI\MaidenIntentIconAssets.cs")
foreach ($binding in @(
    "restraint_attack_intent.png",
    "restraint_skill_intent.png",
    "restraint_power_intent.png",
    "desire_gain_intent.png",
    "tear_clothing_intent.png"
)) {
    if ($intentCode -notmatch [regex]::Escape($binding)) {
        throw "Formal intent icon binding is missing: $binding"
    }
}
# The approved shared vertical rail replaced the former side-by-side (56,34)
# label. Validate structural/layout invariants instead of pinning that old pixel.
& python (Join-Path $ProjectDir "scripts\ValidateSidebarLayout20260927.py") --project-dir $ProjectDir
if ($LASTEXITCODE -ne 0) {
    throw "Vertical sidebar source contracts failed (exit $LASTEXITCODE)."
}
foreach ($scene in @("maiden_succubus_rest_site.tscn", "maiden_succubus_merchant.tscn")) {
    if (!(Test-Path -LiteralPath (Join-Path $ProjectDir "MaidenSuccubus\scenes\$scene") -PathType Leaf)) {
        throw "Character world scene is missing: $scene"
    }
}
if ($characterCode -notmatch 'MerchantAnimPath: MerchantScenePath' -or
    $characterCode -notmatch 'RestSiteAnimPath: RestSiteScenePath' -or
    $characterCode -notmatch 'character/hibiki_amane_merchant\.png' -or
    $characterCode -notmatch 'character/hibiki_amane_rest_site\.png') {
    throw "Character profile must bind both formal merchant and rest-site assets."
}
if ($desireCode -notmatch 'desire_meter_\{state:00\}\.png') {
    throw "Desire meter is not wired to reviewed state textures."
}
if ($desireCode -notmatch 'Text\s*=\s*"0"' -or
    $desireCode -notmatch '_valueLabel\.Text\s*=\s*value\.ToString\(\)' -or
    $desireCode -match 'Text\s*=\s*"0/10"' -or
    $desireCode -match '\$"\{value\}/\{max\}"') {
    throw "Desire meter must display only the current integer value, without /10."
}
$escapeVisualCode = Get-Content -LiteralPath (Join-Path $ProjectDir "src\UI\EscapeCardVisuals.cs") -Raw
if ($escapeVisualCode -notmatch 'ModelDb\.Affliction<Bound>\(\)\.CreateOverlay\(\)' -or
    $escapeVisualCode -match 'Label\s+chains\s*=\s*new') {
    throw "Escape cards must reuse the vanilla Bound overlay instead of a font-positioned chain label."
}
if ($routeCode -notmatch 'maiden_route_wing_v4' -or
    $routeCode -notmatch 'route_holy_angel_wing_v4\.png' -or
    $routeCode -notmatch 'route_corrupt_succubus_wing_v4\.png' -or
    $routeCode -match 'route_(holy|corrupt)_wing_v3\.png' -or
    $routeCode -notmatch 'new Vector2\(-222f, -255f\)' -or
    $routeCode -notmatch 'new Vector2\(124f, -248f\)' -or
    $routeCode -notmatch 'new Vector2\(92f, 92f\)' -or
    $routeCode -notmatch 'new Vector2\(84f, 90f\)' -or
    $routeCode -notmatch 'ExpandMode = TextureRect\.ExpandModeEnum\.IgnoreSize' -or
    $routeCode -notmatch 'StretchMode = TextureRect\.StretchModeEnum\.KeepAspectCentered' -or
    $routeCode -notmatch 'MouseFilter = Control\.MouseFilterEnum\.Ignore' -or
    $routeCode -notmatch 'ClipContents = false' -or
    $routeCode -notmatch 'ZIndex = 0') {
    throw "Route overlays are not wired to the reviewed V4 wings and geometry."
}
if ($powerCode -notmatch 'RegisterPowerIconTextureProvider' -or
    $powerCode -notmatch 'RegisterPowerBigIconTextureProvider') {
    throw "Power icon providers are not registered for both icon sizes."
}
foreach ($binding in @(
    @("CorruptRobePower", "corrupt_robe"),
    @("HolyFlamePower", "holy_flame"),
    @("OpeningPrayerPower", "opening_prayer"),
    @("WetPower", "wet")
)) {
    $pattern = '\["' + [regex]::Escape($binding[0]) + '"\]\s*=\s*"' +
        [regex]::Escape($binding[1]) + '"'
    if ($powerCode -notmatch $pattern) {
        throw "Iteration-two power icon binding is missing: $($binding[0])."
    }
}
$holyPowerCode = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $ProjectDir "src\Powers\Iteration2HolyPowers.cs")
if ($holyPowerCode -match
    'HolyFlamePower[\s\S]*?IsVisibleInternal\s*=>\s*false') {
    throw "Holy Flame must remain a visible Power state."
}

$relicArtRoot = Join-Path $ProjectDir ([regex]::Unescape("\u56fe\u7247\u7d20\u6750/\u9057\u7269\u56fe\u6807"))
$relicV3 = @(Get-ChildItem -LiteralPath $relicArtRoot -Directory | Where-Object {
    $_.Name -like 'V3*_20261001' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'manifest.json'))
})
if ($relicV3.Count -ne 1) { throw "Expected exactly one reviewed V3 relic directory." }
$relicV3 = $relicV3[0].FullName
$relicSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $relicV3 'manifest.json') | ConvertFrom-Json
$relicRuntime = Join-Path $runtime 'relics\icons'
$relicBound = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $relicRuntime 'manifest.json') | ConvertFrom-Json
if ($relicSource.entries.Count -ne 43 -or $relicBound.entries.Count -ne 49) {
    throw "Expected 43 reviewed V3 icons and 49 runtime relic profiles."
}
foreach ($entry in $relicSource.entries) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($entry.file)
    Assert-ManifestCopy (Join-Path $relicV3 "512x512\$($entry.file)") (
        Join-Path $relicRuntime "${stem}_big.png") $entry.sha256
    Assert-ExactCopy (Join-Path $relicV3 "64x64\$($entry.file)") (
        Join-Path $relicRuntime "${stem}_small.png")
}
$seenRelicIcons = @{}
foreach ($entry in $relicBound.entries) {
    if ($seenRelicIcons.ContainsKey($entry.asset)) { throw "Duplicate relic icon: $($entry.asset)" }
    $seenRelicIcons[$entry.asset] = $true
    foreach ($part in @('small', 'big', 'outline')) {
        $target = Join-Path $relicRuntime "$($entry.asset)_$part.png"
        if (!(Test-Path -LiteralPath $target -PathType Leaf) -or
            (Get-Sha256 $target) -ne $entry.("${part}_sha256").ToUpperInvariant()) {
            throw "Missing or altered relic $part icon: $($entry.asset)"
        }
    }
    if ($entry.asset.StartsWith('legacy_', [StringComparison]::Ordinal)) {
        $source = Join-Path $relicArtRoot ($entry.source.Replace('/', '\'))
        Assert-ManifestCopy $source (Join-Path $relicRuntime "$($entry.asset)_big.png") $entry.source_sha256
    }
}

$enchantArtRoot = Join-Path $ProjectDir ([regex]::Unescape("\u56fe\u7247\u7d20\u6750/\u9644\u9b54\u56fe\u6807/V1\u8349\u56fe/\u72ec\u7acb\u56fe\u6807"))
$enchantRuntime = Join-Path $runtime 'enchantments'
$enchantManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $enchantRuntime 'manifest.json') | ConvertFrom-Json
if ($enchantManifest.entries.Count -ne 9) { throw "Expected nine enchantment icons." }
foreach ($entry in $enchantManifest.entries) {
    $file = "$($entry.asset).png"
    Assert-ManifestCopy (Join-Path $enchantArtRoot $file) (
        Join-Path $enchantRuntime $file) $entry.source_sha256
}

Write-Host "Validated visual assets: $($cardArtManifest.items.Count) card arts plus default and HD view, V4 character-select background/icons with V2 aliases, school-uniform/world portraits, 11 HD corruption states, 11 desire states, 10 intent icons, temptation UI, 10 core/route icons, 72 paired power/mechanism icons, 49 relic icon profiles and 9 enchantment icons."
