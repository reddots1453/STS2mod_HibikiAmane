param([Parameter(Mandatory = $true)][string]$ProjectDir)

$ErrorActionPreference = "Stop"

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

function Assert-File([string]$Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required performance asset is missing: $Path"
    }
}

function Assert-ExactCopy([string]$Source, [string]$Runtime) {
    Assert-File $Source
    Assert-File $Runtime
    if ((Get-Sha256 $Source) -ne (Get-Sha256 $Runtime)) {
        throw "Runtime asset differs from curated manifest source: $Runtime"
    }
}

function Assert-Signature([string]$Path, [byte[]]$Expected) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt $Expected.Length) {
        throw "Performance asset is truncated: $Path"
    }
    for ($i = 0; $i -lt $Expected.Length; $i++) {
        if ($bytes[$i] -ne $Expected[$i]) {
            throw "Performance asset has an invalid file signature: $Path"
        }
    }
}

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
    if ($text -notmatch $Pattern) {
        throw "$Message ($Path)"
    }
}

$cgRootName = [regex]::Unescape("\u56fe\u7247\u7d20\u6750/\u8fc7\u573aCG")
$audioRootName = [regex]::Unescape("\u97f3\u9891\u7d20\u6750")
$cgRoot = Join-Path $ProjectDir $cgRootName
$audioRoot = Join-Path $ProjectDir $audioRootName
$cgManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $cgRoot "CG_MANIFEST.json") | ConvertFrom-Json
$audioManifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (
    Join-Path $audioRoot "AUDIO_MANIFEST.json") | ConvertFrom-Json

if ($cgManifest.assets.Count -ne 13) {
    throw "Expected exactly 13 curated CG assets, found $($cgManifest.assets.Count)."
}
if ($audioManifest.assets.Count -ne 16) {
    throw "Expected exactly 16 curated audio assets, found $($audioManifest.assets.Count)."
}

$runtimeCgRoot = Join-Path $ProjectDir "MaidenSuccubus\images\cutscenes"
$expectedCg = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($asset in $cgManifest.assets) {
    $folder = switch ($asset.role) {
        "control" { "control" }
        "invasion" { "invasion" }
        "rest_site_masturbation" { "rest_site_masturbation" }
        default { throw "Unsupported CG role in manifest: $($asset.role)" }
    }
    $source = Join-Path $cgRoot $asset.path
    $runtime = Join-Path (Join-Path $runtimeCgRoot $folder) ([System.IO.Path]::GetFileName($asset.path))
    Assert-ExactCopy $source $runtime
    Assert-Signature $runtime ([byte[]](0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
    [void]$expectedCg.Add([System.IO.Path]::GetFullPath($runtime))
}

$actualCg = @(Get-ChildItem -LiteralPath $runtimeCgRoot -Recurse -Filter "*.png" -File)
if ($actualCg.Count -ne $expectedCg.Count) {
    throw "Runtime cutscene directory must contain exactly the 13 manifest CGs; found $($actualCg.Count)."
}
foreach ($file in $actualCg) {
    if (!$expectedCg.Contains($file.FullName)) {
        throw "Runtime cutscene asset is not listed in the curated manifest: $($file.FullName)"
    }
}

$runtimeAudioRoot = Join-Path $ProjectDir "MaidenSuccubus\audio"
$expectedAudio = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($asset in $audioManifest.assets) {
    $name = [System.IO.Path]::GetFileName($asset.path)
    $folder = switch -Regex ($name) {
        '^(transformation_start|transformation_complete|magic_cast)\.ogg$' { "magic"; break }
        '^(desire_high|desire_full|heartbeat)\.ogg$' { "desire"; break }
        '^(masturbation_loop|climax)\.ogg$' { "rest_site"; break }
        default { "erotic_intents" }
    }
    $source = Join-Path $audioRoot $asset.path
    $runtime = Join-Path (Join-Path $runtimeAudioRoot $folder) $name
    Assert-ExactCopy $source $runtime
    Assert-Signature $runtime ([byte[]](0x4F, 0x67, 0x67, 0x53))
    [void]$expectedAudio.Add([System.IO.Path]::GetFullPath($runtime))
}

$actualAudio = @(Get-ChildItem -LiteralPath $runtimeAudioRoot -Recurse -Filter "*.ogg" -File)
if ($actualAudio.Count -ne $expectedAudio.Count) {
    throw "Runtime performance audio directory must contain exactly the 16 manifest OGGs; found $($actualAudio.Count)."
}
foreach ($file in $actualAudio) {
    if (!$expectedAudio.Contains($file.FullName)) {
        throw "Runtime audio asset is not listed in the curated manifest: $($file.FullName)"
    }
}

$presentation = Join-Path $ProjectDir "src\Presentation"
Assert-Contains (Join-Path $presentation "PerformanceSettings.cs") `
    'AdultCgEnabled\s*=>\s*false' `
    "Adult CG safe fallback must remain disabled"
Assert-Contains (Join-Path $presentation "PerformanceSettings.cs") `
    'AdultAudioEnabled\s*=>\s*false' `
    "Adult audio safe fallback must remain disabled"
Assert-Contains (Join-Path $presentation "PerformanceSettings.cs") `
    'SaveScope\.Global[\s\S]*?\.AsChildOf\(MaidenSuccubusMod\.ModId\)[\s\S]*?\.AddToggle\("adult_cg"[\s\S]*?\.AddToggle\("adult_audio"[\s\S]*?\.AddIntSlider\("normal_volume"[\s\S]*?\.AddIntSlider\("adult_volume"' `
    "Adult CG/audio must have independent, persisted main-menu controls and volume sliders"
Assert-Contains (Join-Path $ProjectDir "src\MaidenSuccubusMod.cs") `
    'PerformanceSettings\.Register' `
    "Performance settings must be registered during mod initialization"
Assert-Contains (Join-Path $presentation "CutscenePlaybackService.cs") `
    '!PerformanceSettings\.Current\.AdultCgEnabled\s*\r?\n\s*\|\|\s*!PerformanceAudience\.IsLocalMaiden\(player\)' `
    "Cutscenes must gate settings and local Maiden ownership before loading assets"
Assert-Contains (Join-Path $presentation "CutscenePlaybackService.cs") `
    'RuntimeTextureAssets\.Load\(relativePath\)' `
    "Loose runtime CGs must use the PNG buffer loader"
Assert-Contains (Join-Path $ProjectDir "src\Core\Desire\DesireResourceRules.cs") `
    'PerformanceAudioService\.PlayDesireMaximum\(\)' `
    "Desire maximum must trigger the replacement sound sequence"
Assert-Contains (Join-Path $presentation "PerformanceAudioService.cs") `
    'PlayDesireMaximum\(\)\s*=>\s*PlayOneShot\(PerformanceAudioCue\.Climax\)' `
    "Desire maximum must use only the climax cue"
Assert-Contains (Join-Path $presentation "PerformanceSettings.cs") `
    'WithVisibleOnHostSurfaces\(ModSettingsHostSurface\.All\)' `
    "CG and audio toggles must be visible from RitsuLib settings on every host surface"
Assert-Contains (Join-Path $ProjectDir "MaidenSuccubus.json") `
    '"description"\s*:\s*"playable character"' `
    "Mod description must match the requested RitsuLib listing text"
$removedAudio = @('MagicCast', 'DesireHigh', 'DesireFull', 'Heartbeat')
$activeAudio = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $presentation "PerformanceAssets.cs")
foreach ($cue in $removedAudio) {
    if ($activeAudio -match [regex]::Escape($cue)) { throw "Removed audio cue remains active: $cue" }
}
if ($activeAudio -match 'PerformanceLoopCue\.Masturbation') { throw "Masturbation loop remains active" }
Assert-Contains (Join-Path $presentation "PerformanceAudioService.cs") `
    'Loops\.TryGetValue\(cue' `
    "Loop audio must be singleton-backed"
Assert-Contains (Join-Path $presentation "PerformanceAudioService.cs") `
    'AudioStreamOggVorbis\.LoadFromBuffer\(bytes\)' `
    "Loose runtime OGGs must use the OGG buffer loader"
Assert-Contains (Join-Path $ProjectDir "src\Core\Intents\IntentMoveFactory.cs") `
    'ControlResolutionResult\.Applied[\s\S]*?PerformanceDirector\.PlayControlAsync' `
    "Control presentation must follow an Applied result"
Assert-Contains (Join-Path $ProjectDir "src\Core\Intents\IntentMoveFactory.cs") `
    'deferCompletion:\s*true\)[\s\S]*?if \(succeeded\)[\s\S]*?PerformanceDirector\.PlayInvasionAsync' `
    "Invasion presentation must follow successful curse resolution"
Assert-Contains (Join-Path $ProjectDir "src\RestSite\MasturbateRestSiteOption.cs") `
    'Desire\.Get\(Owner\)\s*<\s*5[\s\S]*?return false;[\s\S]*?Desire\.Set[\s\S]*?PerformanceDirector\.PlayMasturbationAsync' `
    "Masturbation presentation must follow the desire gate and rule settlement"
Assert-Contains (Join-Path $ProjectDir "src\Patches\PerformanceLifecyclePatch.cs") `
    'PerformanceDirector\.OnSceneTransition' `
    "Scene transitions must clean performance nodes and audio"
Assert-Contains (Join-Path $presentation "PerformanceDirector.cs") `
    'sceneGeneration\s*==\s*_sceneGeneration[\s\S]*?PerformanceAudioService\.PlayOneShot\(PerformanceAudioCue\.InvasionFinish\)' `
    "A cancelled invasion must not restart audio after scene cleanup"

Write-Host "Performance assets validated: 13 CGs, 16 OGGs, exact copies and structural safety gates."
