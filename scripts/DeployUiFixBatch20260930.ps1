param([string]$ProjectDir=(Split-Path $PSScriptRoot -Parent),[switch]$Apply)
$ErrorActionPreference='Stop'
$game=[IO.Path]::GetFullPath((Join-Path $ProjectDir '../../..'))
$installed=Join-Path $game 'mods/MaidenSuccubus'
& python -B (Join-Path $PSScriptRoot 'ValidateUiLocalization20260930.py') --project-dir $ProjectDir
if($LASTEXITCODE -ne 0){throw 'UI localization preflight failed.'}
$resources=@('localization/zhs/intents.json','localization/zhs/static_hover_tips.json','localization/zhs/card_library.json','localization/zhs/powers.json','localization/zhs/characters.json')
# Use only the accepted formal card manifest; preflight never copies assets.
$formal=Join-Path $ProjectDir ([regex]::Unescape("\u56fe\u7247\u7d20\u6750/\u5b8c\u6210\u7248\u5361\u56fe"))
$manifest=Get-Content -LiteralPath (Join-Path $formal 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$cards=@($manifest.items)+@([pscustomobject]@{class='default';file=$manifest.default.file;sha256=$manifest.default.sha256})
foreach($card in $cards){
    $resource="images/cards/$($card.class).png"
    foreach($source in @((Join-Path $formal $card.file),(Join-Path $ProjectDir "MaidenSuccubus/$resource"))){
        if((Get-FileHash -LiteralPath $source).Hash -ne $card.sha256){throw "Formal card preflight mismatch: $source"}
    }
    $resources+=$resource
}
$relicIcons=Join-Path $ProjectDir 'MaidenSuccubus/images/relics/icons'
$relicManifest=Get-Content -LiteralPath (Join-Path $relicIcons 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($relicManifest.entries.Count -ne 49){throw 'Relic icon profile count mismatch.'}
foreach($entry in $relicManifest.entries){
    foreach($part in @('small','big','outline')){
        $resource="images/relics/icons/$($entry.asset)_$part.png"
        if((Get-FileHash -LiteralPath (Join-Path $ProjectDir "MaidenSuccubus/$resource")).Hash -ne $entry.("${part}_sha256")){
            throw "Relic icon preflight mismatch: $resource"
        }
        $resources+=$resource
    }
}
$resources+='images/relics/icons/manifest.json'
$enchantIcons=Join-Path $ProjectDir 'MaidenSuccubus/images/enchantments'
$enchantManifest=Get-Content -LiteralPath (Join-Path $enchantIcons 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($enchantManifest.entries.Count -ne 9){throw 'Enchantment icon count mismatch.'}
foreach($entry in $enchantManifest.entries){
    $resource="images/enchantments/$($entry.asset).png"
    if((Get-FileHash -LiteralPath (Join-Path $ProjectDir "MaidenSuccubus/$resource")).Hash -ne $entry.source_sha256){
        throw "Enchantment icon preflight mismatch: $resource"
    }
    $resources+=$resource
}
$resources+='images/enchantments/manifest.json'
$selectV4=Join-Path $ProjectDir ([regex]::Unescape("\u56fe\u7247\u7d20\u6750/\u9009\u89d2\u754c\u9762/V4\u539f\u4f5c\u753b\u98ce_20261001"))
$selectManifest=Get-Content -LiteralPath (Join-Path $selectV4 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($selectManifest.assets.Count -ne 3){throw 'Character-select V4 asset count mismatch.'}
foreach($asset in $selectManifest.assets){
    $resource="images/ui/character_select/$($asset.file)"
    foreach($source in @((Join-Path $selectV4 $asset.file),(Join-Path $ProjectDir "MaidenSuccubus/$resource"))){
        if((Get-FileHash -LiteralPath $source).Hash -ne $asset.sha256){throw "Character-select V4 preflight mismatch: $source"}
    }
    $resources+=$resource
}
if(-not $Apply){Write-Host 'Preflight passed. Apply requires explicit deployment authorization; no installed files changed.';return}
if(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'Game is running; keep the repair pending until the user exits.'}
$backup=Join-Path $ProjectDir ('obj/ui-deploy-backup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$verified=@()
function CopyVerified([string]$source,[string]$target,[string]$key){
    if(Test-Path -LiteralPath $target){
        $saved=Join-Path $backup $key
        New-Item -ItemType Directory -Path (Split-Path $saved) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $saved
    }
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    $hash=(Get-FileHash -LiteralPath $source).Hash
    if((Get-FileHash -LiteralPath $target).Hash -ne $hash){throw "Deployment hash mismatch: $key"}
    [pscustomobject]@{path=$target;sha256=$hash}
}
foreach($name in @('MaidenSuccubus.dll','MaidenSuccubus.pdb','MaidenSuccubus.json')){
    $verified+=CopyVerified (Join-Path $ProjectDir $name) (Join-Path $installed $name) "installed/$name"
}
foreach($resource in $resources){
    $source=Join-Path $ProjectDir "MaidenSuccubus/$resource"
    $verified+=CopyVerified $source (Join-Path $installed $resource) "installed/$resource"
    $verified+=CopyVerified $source (Join-Path $game "MaidenSuccubus/$resource") "loose/$resource"
}
foreach($root in @($installed,(Join-Path $game 'MaidenSuccubus'))){
    & python -B (Join-Path $PSScriptRoot 'ValidateUiLocalization20260930.py') --project-dir $ProjectDir --installed-root $root
    if($LASTEXITCODE -ne 0){throw "Installed UI tables do not match: $root"}
}
$verified | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $backup 'verified.json') -Encoding UTF8
Write-Host "Verified DLL/PDB, complete UI localization tables and accepted formal card arts in both runtime mirrors. Backup: $backup"
