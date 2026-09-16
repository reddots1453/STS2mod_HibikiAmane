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
if ($corruptionCode -notmatch 'corruption_balance_\{state\}\.png') {
    throw "Corruption meter is not wired to reviewed state textures."
}
if ($desireCode -notmatch 'desire_meter_\{state:00\}\.png') {
    throw "Desire meter is not wired to reviewed state textures."
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
