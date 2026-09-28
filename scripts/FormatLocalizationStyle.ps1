param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectDir = (Split-Path -Parent $PSScriptRoot),

    [switch]$Write
)

$ErrorActionPreference = "Stop"

$localizationDir = Join-Path $ProjectDir "MaidenSuccubus/localization/zhs"
$paths = @(
    (Join-Path $localizationDir "cards.json"),
    (Join-Path $localizationDir "powers.json"),
    (Join-Path $localizationDir "enchantments.json"),
    (Join-Path $localizationDir "static_hover_tips.json"),
    (Join-Path $localizationDir "card_keywords.json"),
    (Join-Path $localizationDir "relics.json"),
    (Join-Path $localizationDir "intents.json")
)

$mechanicTermsPath = Join-Path $PSScriptRoot "localization_gold_terms.txt"
$mechanicTerms = Get-Content -LiteralPath $mechanicTermsPath -Encoding UTF8 |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object { $_.Length } -Descending |
    Select-Object -Unique
$fullStop = [string][char]0x3002

$termPattern = ($mechanicTerms | ForEach-Object { [regex]::Escape($_) }) -join "|"
$protectedPattern = '(\[(?:gold|purple|red|blue|green|aqua|orange|pink)\].*?\[/(?:gold|purple|red|blue|green|aqua|orange|pink)\]|\[img\].*?\[/img\])'
$entryPattern = '^(?<prefix>\s*"(?<key>[^"]+)"\s*:\s*")(?<value>(?:\\.|[^"])*)"(?<suffix>\s*,?\s*)$'
$specialColorsPath = Join-Path $PSScriptRoot "localization_special_colors.txt"
$specialColors = @(Get-Content -LiteralPath $specialColorsPath -Encoding UTF8 |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object { ,($_ -split '\|', 2) })
$specialColorByTerm = @{}
foreach ($item in $specialColors) {
    $specialColorByTerm[$item[0]] = $item[1]
}
$specialPattern = ($specialColors | ForEach-Object {
    [regex]::Escape($_[0])
}) -join "|"

function Normalize-SpecialColors([string]$value) {
    foreach ($item in $specialColors) {
        $term = [regex]::Escape($item[0])
        $color = $item[1]
        $value = [regex]::Replace(
            $value,
            "\[(?:gold|purple|pink)\]$term\[/(?:gold|purple|pink)\]",
            "[$color]$($item[0])[/$color]")
    }
    return $value
}

function Add-SpecialHighlight([string]$value) {
    $parts = [regex]::Split($value, $protectedPattern)
    for ($index = 0; $index -lt $parts.Count; $index++) {
        if ([regex]::IsMatch($parts[$index], '^\[(gold|purple|red|blue|green|aqua|orange|pink|img)\]')) {
            continue
        }
        $parts[$index] = [regex]::Replace(
            $parts[$index],
            $specialPattern,
            { param($match)
                $color = $specialColorByTerm[$match.Value]
                "[$color]$($match.Value)[/$color]"
            })
    }
    return $parts -join ""
}

function Add-MechanicHighlight([string]$value) {
    $parts = [regex]::Split($value, $protectedPattern)
    for ($index = 0; $index -lt $parts.Count; $index++) {
        if ([regex]::IsMatch($parts[$index], '^\[(gold|purple|red|blue|green|aqua|orange|pink|img)\]')) {
            continue
        }
        $parts[$index] = [regex]::Replace(
            $parts[$index],
            $termPattern,
            { param($match) "[gold]$($match.Value)[/gold]" })
    }
    return $parts -join ""
}

$changedFiles = @()
foreach ($path in $paths) {
    $isCardFile = [System.IO.Path]::GetFileName($path) -eq "cards.json"
    $lines = [System.IO.File]::ReadAllLines($path)
    $changed = $false

    for ($index = 0; $index -lt $lines.Count; $index++) {
        $match = [regex]::Match($lines[$index], $entryPattern)
        if (-not $match.Success) {
            continue
        }

        $key = $match.Groups["key"].Value
        if ($key -notmatch '\.(description|smartDescription|extraCardText)$') {
            continue
        }

        $value = $match.Groups["value"].Value
        $value = $value -replace '\[/(gold|purple|red|blue|green|aqua|orange|pink)\]\1', '[/$1]'
        $value = Normalize-SpecialColors $value
        $value = Add-SpecialHighlight $value
        $value = Add-MechanicHighlight $value
        if ($isCardFile -and $key.EndsWith(".description")) {
            # A combat-only suffix owns its newline; adding another outside the
            # condition creates an empty line in combat and a trailing line in Deck.
            $sentencePattern = [regex]::Escape($fullStop) + '(?!\\n|\{(?:InCombat|ShowRemaining):\\n)(?=.)'
            $value = [regex]::Replace($value, $sentencePattern, $fullStop + '\n')
        }

        $newLine = $match.Groups["prefix"].Value + $value + '"' + $match.Groups["suffix"].Value
        if ($newLine -cne $lines[$index]) {
            $lines[$index] = $newLine
            $changed = $true
        }
    }

    if ($changed) {
        $changedFiles += $path
        if ($Write) {
            [System.IO.File]::WriteAllLines(
                $path,
                $lines,
                [System.Text.UTF8Encoding]::new($false))
        }
    }
}

if ($changedFiles.Count -eq 0) {
    Write-Host "Localization rich-text style is already normalized."
    exit 0
}

if (-not $Write) {
    $changedFiles | ForEach-Object { Write-Host "needs-format: $_" }
    Write-Error "Localization formatting drift detected. Run this script with -Write and review the diff."
}

$changedFiles | ForEach-Object { Write-Host "formatted: $_" }
