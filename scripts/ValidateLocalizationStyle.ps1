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

foreach ($pair in @(
    @("cards", $cards),
    @("powers", $powers),
    @("card_keywords", $keywords),
    @("static_hover_tips", $hoverTips),
    @("enchantments", $enchantments)
)) {
    Test-RichText $pair[0] $pair[1]
    Test-TitleDescriptionPairs $pair[0] $pair[1]
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

if ($failures.Count -gt 0) {
    $failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Localization style contract passed: cards, keywords, powers, enchantments and hover tips."
