param(
    [int[]]$Indexes = @(),
    [int[]]$Variants = @(),
    [switch]$Force,
    [switch]$PrepareOnly,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V2"
$configPath = Join-Path $targetDir "v2_generation_config.json"
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
$sourceManifestPath = Join-Path $targetDir ([string]$config.sourceManifest)
$sourceManifestPath = [System.IO.Path]::GetFullPath($sourceManifestPath)
$sourceManifest = Get-Content -Raw -LiteralPath $sourceManifestPath | ConvertFrom-Json
$referenceDir = Join-Path $targetDir "_references"
$cards = @($sourceManifest.cards)
$variantSpecs = @($config.variants)

if ($Indexes.Count -gt 0) {
    $cards = @($cards | Where-Object { [int]$_.index -in $Indexes })
}
if ($Variants.Count -gt 0) {
    $variantSpecs = @($variantSpecs | Where-Object { [int]$_.variant -in $Variants })
}
if ($cards.Count -eq 0) { throw "No cards selected." }
if ($variantSpecs.Count -eq 0) { throw "No variants selected." }

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = $sha.ComputeHash($bytes)
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function Get-CleanVisualConcept([string]$concept) {
    $replacements = [ordered]@{
        "six fading card-shaped runes" = "six fading arcane motes"
        "six fading card-shaped runes and frost motes" = "six fading geometric frost motes"
        "a single card-rune drawn toward her" = "a single electric rune drawn toward her"
        "catching two luminous card-shaped sigils" = "catching two luminous technique sigils"
        "glowing enchanted cards orbit her" = "glowing enchantment sigils orbit her"
        "sleepy card-shaped lights" = "sleepy starlike lights"
        "a card-shaped light returns to her hand" = "a cool blue spark returns to orbit around her"
        "card lights drift back" = "soft light motes drift back"
        "card-shaped spark" = "white-gold spark"
        "one discarded card" = "one fading memory fragment"
        "a glowing deck" = "a bright spiral portal"
        "selected card-shaped runes from a shadowed discard pile toward a bright draw pile" = "selected wind runes from a shadowed vortex toward a bright spiral portal"
        "an enemy intent and two cards, one card becoming upgraded and glowing in her hand" = "an enemy attack pattern and two technique glyphs, one becoming refined and radiant"
        "three floating cards" = "three floating memory shards"
        "two card-shaped shadows immediately flying back toward her hand" = "two black afterimages snapping back into orbit around her"
        "the same spell card" = "the same spell sigil"
        "curse and status cards dissolve from around her into a clean rotating hand of cards" = "curse and status sigils dissolve from around her into a clean rotating halo"
        "two card sigils" = "two alchemical sparks"
        "one attack card into burning fragments" = "one crimson attack sigil into burning fragments"
        "three new luminous cards into her hands" = "three new luminous technique glyphs into orbit around her"
        "devours two cards and spits their attacks back out repeatedly" = "devours two attack echoes and spits their force back out repeatedly"
        "mist-filled deck" = "mist-filled portal"
        "two chosen glowing cards" = "two chosen technique glyphs"
        "spell cards from other heroes" = "foreign spell glyphs from other heroes"
        "exhausted card ashes" = "ashes of exhausted techniques"
        "spectral attack cards" = "spectral attack echoes"
        "countless spectral attack cards" = "countless spectral attack echoes"
        "one tiny black strike" = "one tiny black strike sigil"
        "spectral attack cards igniting" = "spectral attack echoes igniting"
        "one burned card" = "one burned technique glyph"
        "the card becoming whole in her hand" = "the glyph becoming whole inside her palm"
        "a curse card dissolving" = "a curse sigil dissolving"
        "selected cards peel away from a bright draw deck into a dark discard vortex" = "selected luminous techniques peel away from a bright spiral portal into a dark vortex"
    }
    $clean = $concept
    foreach ($entry in $replacements.GetEnumerator()) {
        $clean = $clean.Replace([string]$entry.Key, [string]$entry.Value)
    }
    if ($clean -match "continuous translucent cyan force-field") {
        $clean = "an enormous continuous transparent cyan hexagonal energy plane spans from the top edge to the bottom edge; the heroine presses both open palms flat against this wall; a bright golden beam arriving from offscreen strikes its center and visibly ricochets away as a separate cyan beam"
    }
    return $clean
}

function Get-NegativeText([string]$route, [string]$concept) {
    $negative = [string]$config.negative
    if ($route -eq "堕落") { $negative += ", white armor, silver bodice, white cyan costume, blue white magical outfit, school uniform, face veil, ninja mask" }
    if ($route -eq "圣洁") { $negative += ", black purple costume, succubus outfit" }
    if ($concept -match "continuous translucent cyan force-field") { $negative += ", orb, spherical creature, cute round monster, mascot creature, physical round shield" }
    return $negative
}

function Get-RouteGenerationSettings([string]$route) {
    if ($route -eq "堕落") {
        return @{ loraModel = 0.52; loraClip = 0.58; ipWeight = 0.72; ipWeightType = "linear"; endAt = 0.72 }
    }
    return @{ loraModel = 0.8; loraClip = 0.8; ipWeight = [double]$config.output.styleWeight; ipWeightType = "style transfer"; endAt = [double]$config.output.referenceEndAt }
}

function Get-StyleReference([string]$route, [int]$variant) {
    switch ($route) {
        "圣洁" { return "holy_v{0:D2}.png" -f $variant }
        "堕落" { return "corrupt_v{0:D2}.png" -f $variant }
        default { return "neutral_v{0:D2}.png" -f $variant }
    }
}

function Send-Reference([string]$filename) {
    $path = Join-Path $referenceDir $filename
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing reference: $path" }
    $form = @{
        image = Get-Item -LiteralPath $path
        subfolder = "MaidenSuccubus/V2Refs"
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function Get-PromptText($card, $variantSpec) {
    $cleanConcept = Get-CleanVisualConcept ([string]$card.concept)
    $routeStyle = [string]$config.routeStyles.([string]$card.route)
    $intensity = [string]$config.intensity.([string]$card.intensity)
    $positive = @(
        [string]$config.positiveQuality
        [string]$config.positiveIdentity
        "((the scene must visibly and unambiguously depict this exact action: $cleanConcept):1.45)"
        $routeStyle
        $intensity
        [string]$variantSpec.camera
        [string]$variantSpec.movement
        [string]$variantSpec.expression
        [string]$variantSpec.lighting
        [string]$config.positiveComposition
        "no frame, no border, no text"
    ) -join ", "

    if ($positive -match "(?i)\b(card|cards|deck)\b|hand of cards|draw pile|discard pile") {
        throw "Positive prompt still contains forbidden literal object wording for $($card.title) variant $($variantSpec.variant): $positive"
    }
    return $positive
}

function New-PromptGraph($job) {
    $output = $config.output
    $settings = Get-RouteGenerationSettings ([string]$job.route)
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = [string]$output.lora; strength_model = [double]$settings.loraModel; strength_clip = [double]$settings.loraClip } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 1); text = $job.positive } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 1); text = [string]$job.negative } }
        "5" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = [int]$output.width; height = [int]$output.height; batch_size = 1 } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = [string]$output.ipAdapter } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = [string]$output.clipVision } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/V2Refs/$($job.styleReference)" } }
        "9" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("8", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "10" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image = @("9", 0); clip_vision = @("7", 0)
            weight = [double]$settings.ipWeight; weight_type = [string]$settings.ipWeightType; combine_embeds = "average"
            start_at = 0.0; end_at = [double]$settings.endAt; embeds_scaling = "V only"
        } }
        "11" = @{ class_type = "KSampler"; inputs = @{
            model = @("10", 0); seed = [int64]$job.seed; steps = [int]$output.steps; cfg = [double]$output.cfg
            sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
            positive = @("3", 0); negative = @("4", 0); latent_image = @("5", 0); denoise = 1.0
        } }
        "12" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("11", 0); vae = @("1", 2) } }
        "13" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = $job.comfyPrefix; images = @("12", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$jobs = @()
foreach ($card in $cards) {
    foreach ($variantSpec in $variantSpecs) {
        $variant = [int]$variantSpec.variant
        $folderName = "{0:D3}_{1}_{2}" -f [int]$card.index, [string]$card.class, [string]$card.title
        $relativeFile = Join-Path $folderName ("v{0:D2}.png" -f $variant)
        $targetPath = Join-Path $targetDir $relativeFile
        $jobs += [pscustomobject]@{
            index = [int]$card.index
            route = [string]$card.route
            class = [string]$card.class
            title = [string]$card.title
            intensity = [string]$card.intensity
            variant = $variant
            positive = Get-PromptText $card $variantSpec
            negative = Get-NegativeText ([string]$card.route) ([string]$card.concept)
            styleReference = Get-StyleReference ([string]$card.route) $variant
            seed = Get-StableSeed "$($sourceManifest.designDocSha256):$($card.index):$($card.class):v2:$variant"
            relativeFile = $relativeFile
            targetPath = $targetPath
            comfyPrefix = "MaidenSuccubus/CardArt/FirstBatchV2/$folderName/v{0:D2}" -f $variant
        }
    }
}

$catalogPath = Join-Path $targetDir "v2_prompt_catalog.json"
$catalog = [ordered]@{
    schemaVersion = 1
    designDocSha256 = [string]$sourceManifest.designDocSha256
    width = [int]$config.output.width
    height = [int]$config.output.height
    jobs = @($jobs | Select-Object index, route, class, title, intensity, variant, seed, styleReference, relativeFile, positive, negative)
}
$catalog | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $catalogPath -Encoding utf8
Write-Host "Prepared $($jobs.Count) jobs and prompt catalog: $catalogPath"

if ($PrepareOnly) { exit 0 }

$requiredReferences = @($jobs.styleReference | Sort-Object -Unique)
foreach ($reference in $requiredReferences) { Send-Reference $reference }

$clientId = [guid]::NewGuid().ToString()
$outputRoot = Join-Path $ComfyRoot "output"
$statusPath = Join-Path $targetDir "v2_generation_status.json"
$completedCount = 0

foreach ($job in $jobs) {
    if ((Test-Path -LiteralPath $job.targetPath) -and -not $Force) {
        Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"
        continue
    }
    $parent = Split-Path -Parent $job.targetPath
    New-Item -ItemType Directory -Path $parent -Force | Out-Null

    $graph = New-PromptGraph $job
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 24 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) {
        throw "ComfyUI rejected [$($job.index):$($job.variant)] $($job.title): $($response.node_errors | ConvertTo-Json -Depth 10 -Compress)"
    }

    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) -> $promptId"
    $history = $null
    while ($null -eq $history) {
        Start-Sleep -Seconds 2
        $history = Get-CompletedHistory $promptId
    }

    $images = @($history.outputs."13".images)
    if ($images.Count -eq 0) {
        $status = $history.status | ConvertTo-Json -Depth 8 -Compress
        throw "ComfyUI completed without output for [$($job.index):$($job.variant)] $($job.title): $status"
    }
    $image = $images[0]
    $sourcePath = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $sourcePath -Destination $job.targetPath -Force
    $completedCount++

    $status = [ordered]@{
        updatedAt = (Get-Date).ToString("o")
        selectedJobs = $jobs.Count
        generatedThisRun = $completedCount
        lastCompleted = @{ index = $job.index; variant = $job.variant; title = $job.title; file = $job.relativeFile; promptId = $promptId }
        existing = @(Get-ChildItem -LiteralPath $targetDir -Recurse -File -Filter "v??.png").Count
    }
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($completedCount/$($jobs.Count))"
}

Write-Host "COMPLETE generated_this_run=$completedCount selected=$($jobs.Count) status=$statusPath"
