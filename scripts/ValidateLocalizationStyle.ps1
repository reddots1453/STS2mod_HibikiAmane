param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir
)

$ErrorActionPreference = "Stop"
$localizationDir = Join-Path $ProjectDir "MaidenSuccubus/localization/zhs"
$failures = [System.Collections.Generic.List[string]]::new()

function Read-LocTable([string]$name) {
    $path = Join-Path $localizationDir "$name.json"
    try {
        $object = Get-Content -Raw -LiteralPath $path -Encoding UTF8 | ConvertFrom-Json
        $table = @{}
        foreach ($property in $object.PSObject.Properties) {
            $table[$property.Name] = $property.Value
        }
        return $table
    }
    catch {
        $failures.Add("$name.json cannot be parsed: $($_.Exception.Message)")
        return @{}
    }
}

function Test-RichText([string]$tableName, [hashtable]$table) {
    $tags = @("gold", "purple", "red", "blue", "green", "aqua", "orange", "pink", "img")
    foreach ($entry in $table.GetEnumerator()) {
        $text = [string]$entry.Value
        foreach ($tag in $tags) {
            $open = ([regex]::Matches($text, "\[$tag\]")).Count
            $close = ([regex]::Matches($text, "\[/$tag\]")).Count
            if ($open -ne $close) {
                $failures.Add("$tableName/$($entry.Key): unbalanced [$tag] tags ($open/$close)")
            }
        }
    }
}

function Test-TitleDescriptionPairs([string]$tableName, [hashtable]$table) {
    $stems = @{}
    foreach ($key in $table.Keys) {
        if ($key -match '^(?<stem>.*)\.(?<suffix>title|description|smartDescription)$') {
            if (-not $stems.ContainsKey($Matches.stem)) {
                $stems[$Matches.stem] = @{}
            }
            $stems[$Matches.stem][$Matches.suffix] = $true
        }
    }
    foreach ($stem in $stems.Keys) {
        if ($tableName -eq "relics" -and
            $stem -match 'MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_STAGE(?:_[1-4])?$') {
            continue
        }
        foreach ($required in @("title", "description")) {
            if (-not $stems[$stem].ContainsKey($required)) {
                $failures.Add("$tableName/${stem}: missing $required")
            }
        }
    }
}

$cards = Read-LocTable "cards"
$powers = Read-LocTable "powers"
$keywords = Read-LocTable "card_keywords"
$hoverTips = Read-LocTable "static_hover_tips"
$enchantments = Read-LocTable "enchantments"
$relics = Read-LocTable "relics"
$intents = Read-LocTable "intents"
$tablesByName = @{
    "cards" = $cards
    "powers" = $powers
    "card_keywords" = $keywords
    "static_hover_tips" = $hoverTips
    "enchantments" = $enchantments
    "relics" = $relics
    "intents" = $intents
}

$formatContractPath = Join-Path $PSScriptRoot "localization_format_contract_20260914.json"
$formatContract = Get-Content -Raw -Encoding UTF8 -LiteralPath $formatContractPath |
    ConvertFrom-Json
foreach ($tableProperty in $formatContract.PSObject.Properties) {
    $tableName = $tableProperty.Name
    if (-not $tablesByName.ContainsKey($tableName)) {
        $failures.Add("format contract references unknown table: $tableName")
        continue
    }
    foreach ($entry in $tableProperty.Value.PSObject.Properties) {
        $actualText = $tablesByName[$tableName][$entry.Name]
        if ($null -eq $actualText) {
            $failures.Add("$tableName/$($entry.Name): required DesignDoc format entry is missing")
        }
        elseif (-not [string]::Equals(
                [string]$entry.Value,
                [string]$actualText,
                [StringComparison]::Ordinal)) {
            $failures.Add("$tableName/$($entry.Name): differs from DesignDoc format contract")
        }
    }
}

foreach ($pair in @(
    @("cards", $cards),
    @("powers", $powers),
    @("card_keywords", $keywords),
    @("static_hover_tips", $hoverTips),
    @("enchantments", $enchantments),
    @("relics", $relics),
    @("intents", $intents)
)) {
    Test-RichText $pair[0] $pair[1]
    Test-TitleDescriptionPairs $pair[0] $pair[1]
}

foreach ($pair in $tablesByName.GetEnumerator()) {
    foreach ($entry in $pair.Value.GetEnumerator()) {
        foreach ($match in [regex]::Matches(
                [string]$entry.Value,
                '\{(?<selector>[A-Za-z_][A-Za-z0-9_]*):show:')) {
            if ($match.Groups["selector"].Value -ne "IfUpgraded") {
                $failures.Add(
                    "$($pair.Key)/$($entry.Key): show formatter only accepts IfUpgradedVar; " +
                    "use the standard boolean branch syntax for $($match.Groups['selector'].Value)")
            }
        }
    }
}

$specialColorTerms = @{}
$specialColorsPath = Join-Path $PSScriptRoot "localization_special_colors.txt"
Get-Content -LiteralPath $specialColorsPath -Encoding UTF8 |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object {
        $parts = $_ -split '\|', 2
        $specialColorTerms[$parts[0]] = $parts[1]
    }
foreach ($pair in @(
    @("cards", $cards),
    @("powers", $powers),
    @("card_keywords", $keywords),
    @("static_hover_tips", $hoverTips),
    @("enchantments", $enchantments),
    @("relics", $relics),
    @("intents", $intents)
)) {
    foreach ($entry in $pair[1].GetEnumerator()) {
        if ($entry.Key -notmatch '\.(description|smartDescription|extraCardText)$') {
            continue
        }
        $text = [string]$entry.Value
        foreach ($term in $specialColorTerms.Keys |
            Sort-Object { $_.Length } -Descending) {
            $wrongColors = @("gold", "purple", "pink") |
                Where-Object { $_ -ne $specialColorTerms[$term] }
            foreach ($wrong in $wrongColors) {
                if ($text -match "\[$wrong\]$([regex]::Escape($term))\[/$wrong\]") {
                    $failures.Add("$($pair[0])/$($entry.Key): $term must use [$($specialColorTerms[$term])] color")
                }
            }
        }
    }
}

$mechanicTermsPath = Join-Path $PSScriptRoot "localization_gold_terms.txt"
$mechanicTerms = Get-Content -LiteralPath $mechanicTermsPath -Encoding UTF8 |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object { $_.Length } -Descending |
    Select-Object -Unique
$fullStop = [string][char]0x3002

foreach ($entry in $cards.GetEnumerator()) {
    if (-not $entry.Key.EndsWith(".description")) {
        continue
    }
    $text = [string]$entry.Value
    $sentenceCount = ([regex]::Matches($text, [regex]::Escape($fullStop))).Count
    if ($sentenceCount -gt 1 -and $text -notmatch "`n") {
        $failures.Add("cards/$($entry.Key): independent sentences must be split across lines")
    }

    $unhighlighted = [regex]::Replace(
        $text,
        '\[(gold|purple|red|blue|green|aqua|orange|pink)\].*?\[/\1\]|\[img\].*?\[/img\]',
        "")
    foreach ($term in $mechanicTerms) {
        if ($unhighlighted.Contains($term)) {
            $failures.Add("cards/$($entry.Key): mechanic term is not [gold]: $term")
            break
        }
    }
}

$staticPowerVariableAllowList = @{
    "MAIDEN_SUCCUBUS_POWER_MAGIC_ARMOR_POWER.description" = @("Chance")
    "MAIDEN_SUCCUBUS_POWER_CONTROL_POWER.description" = @("Source", "ControlType", "Amount")
    "MAIDEN_SUCCUBUS_POWER_GUARDIAN_SCRIPTURE_POWER.description" = @("Block")
    "MAIDEN_SUCCUBUS_POWER_ULTIMATE_FLARE_POWER.description" = @("Damage")
}

foreach ($entry in $powers.GetEnumerator()) {
    if (-not $entry.Key.EndsWith(".description")) {
        continue
    }
    $allowed = @("energyPrefix", "singleStarIcon")
    if ($staticPowerVariableAllowList.ContainsKey($entry.Key)) {
        $allowed += $staticPowerVariableAllowList[$entry.Key]
    }
    foreach ($match in [regex]::Matches([string]$entry.Value, '\{(?<name>[A-Za-z_][A-Za-z0-9_]*)')) {
        if ($match.Groups["name"].Value -notin $allowed) {
            $failures.Add("powers/$($entry.Key): unresolved canonical placeholder {$($match.Groups['name'].Value)}")
        }
    }
}

$smartPowerVariableAllowList = @{
    "MAIDEN_SUCCUBUS_POWER_ABNORMAL_ADAPTATION_POWER.smartDescription" = @("RemainingTriggers")
    "MAIDEN_SUCCUBUS_POWER_GUARDIAN_SCRIPTURE_POWER.smartDescription" = @("Block")
}

foreach ($entry in $powers.GetEnumerator()) {
    if (-not $entry.Key.EndsWith(".smartDescription")) {
        continue
    }
    $allowed = @("Amount", "energyPrefix", "singleStarIcon")
    if ($smartPowerVariableAllowList.ContainsKey($entry.Key)) {
        $allowed += $smartPowerVariableAllowList[$entry.Key]
    }
    foreach ($match in [regex]::Matches([string]$entry.Value, '\{(?<name>[A-Za-z_][A-Za-z0-9_]*)')) {
        if ($match.Groups["name"].Value -notin $allowed) {
            $failures.Add("powers/$($entry.Key): unregistered smart placeholder {$($match.Groups['name'].Value)}")
        }
    }
}

$formatScript = Join-Path $ProjectDir "scripts/FormatLocalizationStyle.ps1"
& $formatScript -ProjectDir $ProjectDir
if ($LASTEXITCODE -ne 0) {
    $failures.Add("localization rich-text formatter reports drift")
}

$upgradeBindingScript = Join-Path $ProjectDir "scripts/AuditUpgradeTextBindings.py"
& python $upgradeBindingScript
if ($LASTEXITCODE -ne 0) {
    $failures.Add("upgrade scalar description binding audit failed")
}

if ($failures.Count -gt 0) {
    $failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Localization style contract passed: cards, keywords, powers, enchantments, relics, intents and hover tips."
