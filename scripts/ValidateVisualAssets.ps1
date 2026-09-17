param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

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

foreach ($value in -5..5) {
    $state = if ($value -lt 0) {
        "neg$(-$value)"
    } elseif ($value -gt 0) {
        "pos$value"
    } else {
        "zero"
    }
    $file = "corruption_balance_$state.png"
    Assert-ExactCopy (Find-ReviewedAsset $file) (
        Join-Path $runtime "ui\corruption\$file")
}

foreach ($value in 0..10) {
    $file = "desire_meter_{0:D2}.png" -f $value
    Assert-ExactCopy (Find-ReviewedAsset $file) (
        Join-Path $runtime "ui\desire_meter\$file")
}

$coreCopies = @(
    @("hibiki_amane_character_icon_128.png", "ui\core\hibiki_amane_character_icon_128.png"),
    @("hibiki_amane_character_icon_outline_128.png", "ui\core\hibiki_amane_character_icon_outline_128.png"),
    @("magic_energy_cost_icon_128.png", "ui\core\magic_energy_cost_icon_128.png"),
    @("desire_resource_icon_32.png", "ui\core\desire_resource_icon_32.png"),
    @("desire_resource_icon_128.png", "ui\core\desire_resource_icon_128.png"),
    @("route_holy_wing_v3.png", "ui\route_marks\route_holy_wing_v3.png"),
    @("route_corrupt_wing_v3.png", "ui\route_marks\route_corrupt_wing_v3.png")
)
foreach ($copy in $coreCopies) {
    Assert-ExactCopy (Find-ReviewedAsset $copy[0]) (Join-Path $runtime $copy[1])
}

foreach ($size in @("64x64", "256x256")) {
    $runtimeDir = Join-Path $runtime "powers\$size"
    $runtimeFiles = @(Get-ChildItem -LiteralPath $runtimeDir -Filter "*.png" -File)
    if ($runtimeFiles.Count -ne 68) {
        throw "Expected 68 runtime $size power/mechanism icons, found $($runtimeFiles.Count)."
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
# Keep hover hitboxes aligned with the five visible ticks in the 512x144 art.
$tickMatches = [regex]::Matches($corruptionCode, '\((-?\d+), (\d+)f\)')
$expectedTicks = @(@(-5, 104), @(-3, 165), @(0, 256), @(3, 347), @(5, 408))
if ($tickMatches.Count -ne $expectedTicks.Count -or
    $corruptionCode -notmatch 'tick.ArtworkX / 512f \* MeterWidth' -or
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
        $center = $x / 512 * 256 * $scale
        $halfWidth = $hoverWidth / 2 * $scale
        if ($halfWidth -le 0 -or $center - $halfWidth -lt 0 -or
            $center + $halfWidth -gt 256 * $scale) {
            throw "Corruption hover tick $value lies outside the meter."
        }
        if ($i -gt 0 -and
            ($x - $expectedTicks[$i - 1][1]) / 512 * 256 * $scale -le 2 * $halfWidth) {
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
    $characterCode -notmatch 'IconPath: RuntimeTextureAssets\.PrepareResource\(' -or
    $characterCode -notmatch 'ui/core/hibiki_amane_character_icon_128\.png' -or
    $characterCode -notmatch 'user://maiden_succubus_top_bar_icon\.tres') {
    throw "Character profile must route the reviewed Maiden icon through RitsuLib Ui.IconPath for the top bar."
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
if ($routeCode -notmatch 'route_holy_wing_v3\.png' -or
    $routeCode -notmatch 'route_corrupt_wing_v3\.png') {
    throw "Route overlays are not wired to both reviewed V3 wings."
}
if ($powerCode -notmatch 'RegisterPowerIconTextureProvider' -or
    $powerCode -notmatch 'RegisterPowerBigIconTextureProvider') {
    throw "Power icon providers are not registered for both icon sizes."
}

Write-Host "Validated visual assets: 11 corruption states, 11 desire states, 7 core/route icons, and 68 paired power/mechanism icons."
