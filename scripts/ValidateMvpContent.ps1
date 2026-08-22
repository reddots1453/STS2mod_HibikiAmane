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
    MSNeutralCardPool = 36
    MSCorruptCardPool = 52
    MSHolyCardPool = 43
    MSScriptureCardPool = 6
    MSInvasionCursePool = 17
    MSGeneratedCardPool = 22
}
$formalPoolIdentityHashes = @{
    MSNeutralCardPool = "77844c337365a4972197d0223f47e9f059766830d1821219ccbf95fb497d0464"
    MSCorruptCardPool = "3eda58611787d33279cdc81c22a876f97f54537cf9f8265c14a4d4930a7bd030"
    MSHolyCardPool = "89e778384dc9a7f3f3b1bda17fe237fd8171095142b091f97e1e05e27de89443"
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

Write-Host "Validated MVP content: neutral=36, corrupt=52, holy=43, scriptures=6, invasion-curses=17, generated=22, relics=22, enchantments=8."
