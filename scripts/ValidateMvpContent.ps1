param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

function ConvertTo-ScreamingSnake([string]$Name) {
    # Preserve acronym and terminal Roman-numeral runs. CounterBarrierII must
    # resolve to COUNTER_BARRIER_II, matching RitsuLib/runtime registration.
    $value = [regex]::Replace($Name, '([a-z0-9])([A-Z])', '$1_$2')
    $value = [regex]::Replace($value, '([A-Z]+)([A-Z][a-z])', '$1_$2')
    return $value.ToUpperInvariant()
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

function Assert-ExactSet(
    [string]$Name,
    [object[]]$Actual,
    [object[]]$Expected
) {
    [string[]]$actualSet = @($Actual | ForEach-Object { [string]$_ } |
        Sort-Object -Unique)
    [string[]]$expectedSet = @($Expected | ForEach-Object { [string]$_ } |
        Sort-Object -Unique)
    [string[]]$missing = @($expectedSet | Where-Object { $_ -notin $actualSet })
    [string[]]$unexpected = @($actualSet | Where-Object { $_ -notin $expectedSet })
    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw "$Name differs from the content contract.`n" +
            "Missing: $($missing -join ', ')`n" +
            "Unexpected: $($unexpected -join ', ')"
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
            $allowsEmptyDescription = $Kind -eq "CARD" -and
                $type -eq "DrowsyStatus" -and $suffix -eq "description"
            if (!$Localization.ContainsKey($key) -or
                (!$allowsEmptyDescription -and
                    [string]::IsNullOrWhiteSpace([string]$Localization[$key]))) {
                $missing.Add($key)
            }
        }
    }
    if ($missing.Count -gt 0) {
        throw "Missing or empty localization keys:`n$($missing -join "`n")"
    }
}

$cardsRoot = Join-Path $ProjectDir "src\Cards"
$relicsRoot = Join-Path $ProjectDir "src\Relics"
$enchantmentsRoot = Join-Path $ProjectDir "src\Enchantments"
$locRoot = Join-Path $ProjectDir "MaidenSuccubus\localization\zhs"
$contractPath = Join-Path $ProjectDir "docs\content_contract_20260824.json"
$contract = Get-Content -Raw -Encoding UTF8 -LiteralPath $contractPath |
    ConvertFrom-Json

$poolExpectations = $contract.cards.PSObject.Properties
$allCards = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $poolExpectations) {
    $pattern = "\[RegisterCard\(typeof\($($entry.Name)\)\)\]\s*(?:public\s+)?sealed\s+class\s+(\w+)"
    [object[]]$types = @(Find-RegisteredTypes $cardsRoot $pattern)
    Assert-ExactSet $entry.Name $types @($entry.Value)
    $allCards.AddRange([string[]]$types)
}

$cardSource = (Get-ChildItem -LiteralPath $cardsRoot -Recurse -Filter *.cs |
    Get-Content -Raw) -join "`n"
[string[]]$registeredPools = @(
    [regex]::Matches($cardSource, '\[RegisterCard\(typeof\((\w+)\)\)\]') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
Assert-ExactSet "registered card pools" $registeredPools @(
    $poolExpectations | ForEach-Object { $_.Name })

[object[]]$relics = @(Find-RegisteredTypes $relicsRoot '\[RegisterRelic\([^\]]+\)\]\s*(?:public\s+)?sealed\s+class\s+(\w+)')
[object[]]$enchantments = @(Find-RegisteredTypes $enchantmentsRoot '\[RegisterEnchantment\]\s*(?:public\s+)?sealed\s+class\s+(\w+)')
Assert-ExactSet "registered relics" $relics @($contract.relics)
Assert-ExactSet "registered enchantments" $enchantments @($contract.enchantments)

$cardLoc = Read-JsonHashtable (Join-Path $locRoot "cards.json")
$relicLoc = Read-JsonHashtable (Join-Path $locRoot "relics.json")
$enchantmentLoc = Read-JsonHashtable (Join-Path $locRoot "enchantments.json")
Assert-Localization "CARD" $allCards.ToArray() $cardLoc @("title", "description")
Assert-Localization "RELIC" $relics $relicLoc @("title", "description", "flavor")
Assert-Localization "ENCHANTMENT" $enchantments $enchantmentLoc @("title", "description", "extraCardText")

$poolSummary = @($poolExpectations | ForEach-Object {
    "$($_.Name)=$(@($_.Value).Count)"
}) -join ", "
Write-Host "Validated content contract '$($contract.baseline)': $poolSummary, relics=$(@($contract.relics).Count), enchantments=$(@($contract.enchantments).Count)."
