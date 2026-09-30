param([string]$ProjectDir=(Split-Path $PSScriptRoot -Parent),[switch]$Apply)
$ErrorActionPreference='Stop'
$game=[IO.Path]::GetFullPath((Join-Path $ProjectDir '../../..'))
$installed=Join-Path $game 'mods/MaidenSuccubus'
& python -B (Join-Path $PSScriptRoot 'ValidateUiLocalization20260930.py') --project-dir $ProjectDir
if($LASTEXITCODE -ne 0){throw 'UI localization preflight failed.'}
$resources=@('localization/zhs/intents.json','localization/zhs/static_hover_tips.json','localization/zhs/card_library.json','localization/zhs/powers.json')
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
foreach($name in @('MaidenSuccubus.dll','MaidenSuccubus.pdb')){
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
Write-Host "Verified DLL/PDB and complete UI localization tables in both runtime mirrors. Backup: $backup"
