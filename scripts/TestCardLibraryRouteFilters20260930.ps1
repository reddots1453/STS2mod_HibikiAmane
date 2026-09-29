param([string]$ProjectDir = (Join-Path $PSScriptRoot '..'))

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($ProjectDir)
Add-Type -Path @(
    (Join-Path $root 'src\Core\Routes\RouteCardKind.cs'),
    (Join-Path $root 'src\UI\MaidenRouteFilterRules.cs'))

$kinds = [MaidenSuccubus.Core.Routes.RouteCardKind]
$checked = 0
foreach ($neutral in @($false, $true)) {
    foreach ($corrupt in @($false, $true)) {
        foreach ($holy in @($false, $true)) {
            foreach ($route in [System.Enum]::GetValues($kinds)) {
                $any = $neutral -or $corrupt -or $holy
                $expected = !$any -or
                    ($route -eq $kinds::Neutral -and $neutral) -or
                    ($route -eq $kinds::Corrupt -and $corrupt) -or
                    ($route -eq $kinds::Holy -and $holy)
                $actual = [MaidenSuccubus.UI.MaidenRouteFilterRules]::Allows(
                    $route, $neutral, $corrupt, $holy)
                if ($actual -ne $expected) {
                    throw "Route filter mismatch: route=$route N=$neutral C=$corrupt H=$holy"
                }
                $checked++
            }
        }
    }
}

$localization = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $root 'MaidenSuccubus\localization\zhs\card_library.json') | ConvertFrom-Json
foreach ($name in @('CORRUPT', 'HOLY', 'NEUTRAL')) {
    foreach ($suffix in @('', '_TIP')) {
        $key = "MAIDEN_ROUTE_$name$suffix"
        if ([string]::IsNullOrWhiteSpace($localization.$key)) {
            throw "Missing card-library route localization: $key"
        }
    }
}

$ui = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $root 'src\Patches\CardLibraryRoutePoolPatch.cs')
$contracts = @{
    nativeCheckbox = 'prototype\.Duplicate\(\(int\)Node\.DuplicateFlags\.Scripts\)'
    ownMaterials = 'IsolateMaterials\(filter\)'
    maidenOnly = '_maidenPool\.Visible && _maidenPool\.IsSelected'
    nativeRefresh = '"UpdateFilter"'
    allPoolFallback = '____poolFilters\[entry\.Value\] = IsMaidenSuccubusCompendiumCard;'
    resetOnOpen = 'CardLibraryRouteFilterOpenPatch[\s\S]*?ResetAndRefresh\(\)'
}
foreach ($name in $contracts.Keys) {
    if ($ui -notmatch $contracts[$name]) {
        throw "Missing library UI contract: $name"
    }
}

Write-Host "Card-library route filter validated: $checked combinations; 6 localization keys; $($contracts.Count) UI contracts."
