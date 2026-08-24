param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

function ConvertTo-ScreamingSnake([string]$Name) {
    return ([regex]::Replace($Name, "(?<!^)([A-Z])", '_${1}')).ToUpperInvariant()
}

function Read-JsonHashtable([string]$Path) {
    $json = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    $result = @{}
    foreach ($property in $json.PSObject.Properties) {
        $result[$property.Name] = $property.Value
    }
    return $result
}

function Find-RegisteredTypes([string]$Root, [string]$Pattern) {
    $source = (Get-ChildItem -LiteralPath $Root -Recurse -Filter *.cs |
        Get-Content -Raw) -join "`n"
    return [regex]::Matches($source, $Pattern) |
        ForEach-Object { $_.Groups[1].Value }
}

function Assert-Count([string]$Name, [object[]]$Items, [int]$Expected) {
    if ($Items.Count -ne $Expected) {
        throw "$Name registration count is $($Items.Count); expected $Expected."
    }
}

function Assert-Localization(
    [string]$Kind,
    [object[]]$Types,
    [hashtable]$Localization,
    [string[]]$Suffixes
) {
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($type in $Types) {
        $prefix = "MAIDEN_SUCCUBUS_${Kind}_$(ConvertTo-ScreamingSnake $type)"
        foreach ($suffix in $Suffixes) {
            $key = "$prefix.$suffix"
            if (!$Localization.ContainsKey($key) -or
                [string]::IsNullOrWhiteSpace([string]$Localization[$key])) {
                $missing.Add($key)
            }
        }
    }
    if ($missing.Count -gt 0) {
        throw "Missing or empty localization keys:`n$($missing -join "`n")"
    }
}

function Assert-IdentityHash(
    [string]$Name,
    [object[]]$Items,
    [string]$ExpectedHash
) {
    $identity = (@($Items | Sort-Object) -join "`n")
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($identity)
        $actualHash = ([System.BitConverter]::ToString(
            $sha.ComputeHash($bytes))).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
    if ($actualHash -ne $ExpectedHash) {
        throw "$Name identities changed. Actual types:`n$($Items -join "`n")"
    }
}

$cardsRoot = Join-Path $ProjectDir "src\Cards"
$relicsRoot = Join-Path $ProjectDir "src\Relics"
$enchantmentsRoot = Join-Path $ProjectDir "src\Enchantments"
$locRoot = Join-Path $ProjectDir "MaidenSuccubus\localization\zhs"

$poolExpectations = [ordered]@{
    MSNeutralCardPool = 40
    MSCorruptCardPool = 63
    MSHolyCardPool = 51
    MSScriptureCardPool = 6
    MSInvasionCursePool = 17
    MSGeneratedCardPool = 27
}
$formalPoolIdentityHashes = @{
    MSNeutralCardPool = "68d0bab1030e93f26cc9398ac10d17b0ac7304c34151d85c2e39b38ad9d15ac5"
    MSCorruptCardPool = "796192f8fd70937011d9b3e7786234ea047cf7f3cc55c4433c3bf3e378ac34e2"
    MSHolyCardPool = "16b4265367bb1b6e23788480bf69c56b8dd3f733e8777c15546f116939218f9c"
}
$allCards = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $poolExpectations.GetEnumerator()) {
    $pattern = "\[RegisterCard\(typeof\($($entry.Key)\)\)\]\s*(?:public\s+)?sealed\s+class\s+(\w+)"
    [object[]]$types = @(Find-RegisteredTypes $cardsRoot $pattern)
    Assert-Count $entry.Key $types $entry.Value
    $allCards.AddRange([string[]]$types)
    if ($formalPoolIdentityHashes.ContainsKey($entry.Key)) {
        Assert-IdentityHash $entry.Key $types $formalPoolIdentityHashes[$entry.Key]
    }
}

[object[]]$relics = @(Find-RegisteredTypes $relicsRoot '\[RegisterRelic\([^\]]+\)\]\s*(?:public\s+)?sealed\s+class\s+(\w+)')
[object[]]$enchantments = @(Find-RegisteredTypes $enchantmentsRoot '\[RegisterEnchantment\]\s*(?:public\s+)?sealed\s+class\s+(\w+)')
Assert-Count "registered relic" $relics 22
Assert-Count "registered enchantment" $enchantments 8

$cardLoc = Read-JsonHashtable (Join-Path $locRoot "cards.json")
$relicLoc = Read-JsonHashtable (Join-Path $locRoot "relics.json")
$enchantmentLoc = Read-JsonHashtable (Join-Path $locRoot "enchantments.json")
Assert-Localization "CARD" $allCards.ToArray() $cardLoc @("title", "description")
Assert-Localization "RELIC" $relics $relicLoc @("title", "description", "flavor")
Assert-Localization "ENCHANTMENT" $enchantments $enchantmentLoc @("title", "description", "extraCardText")

Write-Host "Validated forward-merge content: neutral=40, corrupt=63, holy=51, scriptures=6, invasion-curses=17, generated=27, relics=22, enchantments=8."
