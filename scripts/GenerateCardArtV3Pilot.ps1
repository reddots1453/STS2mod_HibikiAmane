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
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制"
$configPath = Join-Path $targetDir "pilot_config.json"
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
$referenceDir = Join-Path $modRoot "图片素材\第一批卡图V2\_references"

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = $sha.ComputeHash($bytes)
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function Get-PromptText($card, $variant) {
    $identityMode = switch ([string]$card.route) {
        "圣洁" { "holy" }
        "堕落" { "corrupt" }
        default {
            if ([string]$variant.outfit -match "magical girl") { "holy" } else { "normal" }
        }
    }
    $identity = [string]$config.identities.PSObject.Properties[$identityMode].Value
    return @(
        [string]$config.quality
        $identity
        [string]$variant.outfit
        "(($([string]$variant.action)):1.4)"
        [string]$variant.camera
        [string]$variant.expression
        [string]$variant.scene
        [string]$variant.palette
        [string]$config.composition
        "no text, no border, no interface"
    ) -join ", "
}

function Send-Reference([string]$filename) {
    $path = Join-Path $referenceDir $filename
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing reference: $path" }
    $form = @{
        image = Get-Item -LiteralPath $path
        subfolder = "MaidenSuccubus/V3PilotRefs"
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-PromptGraph($job) {
    $output = $config.output
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = [string]$output.lora; strength_model = [double]$output.loraModel; strength_clip = [double]$output.loraClip } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = [int]$output.clipSkip } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.negative } }
        "6" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = [int]$output.width; height = [int]$output.height; batch_size = 1 } }
    }

    $sourceModel = @("2", 0)
    if ($job.referenceMode -ne "none") {
        $graph["7"] = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = [string]$output.ipAdapter } }
        $graph["8"] = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = [string]$output.clipVision } }
        $graph["9"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/V3PilotRefs/$($job.reference)" } }
        $graph["10"] = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("9", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        $weightType = if ($job.referenceMode -eq "composition") { "composition precise" } else { "style transfer precise" }
        $graph["11"] = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2", 0); ipadapter = @("7", 0); image = @("10", 0); clip_vision = @("8", 0)
            weight = [double]$job.referenceWeight; weight_type = $weightType; combine_embeds = "average"
            start_at = 0.0; end_at = [double]$job.referenceEndAt; embeds_scaling = "V only"
        } }
        $sourceModel = @("11", 0)
    }

    $graph["12"] = @{ class_type = "KSampler"; inputs = @{
        model = $sourceModel; seed = [int64]$job.seed; steps = [int]$output.sourceSteps; cfg = [double]$output.sourceCfg
        sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
        positive = @("4", 0); negative = @("5", 0); latent_image = @("6", 0); denoise = 1.0
    } }
    $graph["13"] = @{ class_type = "KSampler"; inputs = @{
        model = @("2", 0); seed = [int64]($job.seed + 97); steps = [int]$output.refineSteps; cfg = [double]$output.refineCfg
        sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
        positive = @("4", 0); negative = @("5", 0); latent_image = @("12", 0); denoise = [double]$output.refineDenoise
    } }
    $graph["14"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("13", 0); vae = @("1", 2) } }
    $graph["15"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$config.facePositive } }
    $graph["16"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$config.faceNegative } }
    $graph["17"] = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
    $graph["18"] = @{ class_type = "FaceDetailer"; inputs = @{
        image = @("14", 0); model = @("2", 0); clip = @("3", 0); vae = @("1", 2)
        guide_size = 512.0; guide_size_for = $true; max_size = 1024.0
        seed = [int64]($job.seed + 193); steps = 12; cfg = 5.5; sampler_name = "euler_ancestral"; scheduler = "normal"
        positive = @("15", 0); negative = @("16", 0); denoise = 0.2; feather = 5
        noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 10; bbox_crop_factor = 3.0
        sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0
        sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10
        bbox_detector = @("17", 0); wildcard = ""; cycle = 1
    } }
    $graph["19"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("12", 0); vae = @("1", 2) } }
    $graph["20"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/V3Pilot/sources/$($job.prefix)"; images = @("19", 0) } }
    $graph["21"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/V3Pilot/final/$($job.prefix)"; images = @("18", 0) } }
    return $graph
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$jobs = @()
foreach ($card in @($config.cards)) {
    if ($Indexes.Count -gt 0 -and [int]$card.index -notin $Indexes) { continue }
    foreach ($variant in @($card.variants)) {
        if ($Variants.Count -gt 0 -and [int]$variant.variant -notin $Variants) { continue }
        $prefix = "{0:D3}_{1}_v{2:D2}" -f [int]$card.index, [string]$card.class, [int]$variant.variant
        $jobs += [pscustomobject]@{
            index = [int]$card.index
            class = [string]$card.class
            title = [string]$card.title
            route = [string]$card.route
            variant = [int]$variant.variant
            referenceMode = [string]$variant.referenceMode
            reference = [string]$variant.reference
            referenceWeight = [double]$variant.referenceWeight
            referenceEndAt = [double]$variant.referenceEndAt
            seed = Get-StableSeed "$($config.designDocSha256):$($card.class):v3-pilot:$($variant.variant)"
            positive = Get-PromptText $card $variant
            negative = [string]$config.negative
            prefix = $prefix
            sourcePath = Join-Path $targetDir "sources\$prefix.png"
            finalPath = Join-Path $targetDir "final\$prefix.png"
        }
    }
}
if ($jobs.Count -eq 0) { throw "No pilot jobs selected." }

$catalogPath = Join-Path $targetDir "pilot_prompt_catalog.json"
[ordered]@{
    schemaVersion = 1
    designDocSha256 = [string]$config.designDocSha256
    jobs = @($jobs | Select-Object index, class, title, route, variant, referenceMode, reference, referenceWeight, referenceEndAt, seed, positive, negative, sourcePath, finalPath)
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $catalogPath -Encoding utf8
Write-Host "Prepared $($jobs.Count) V3 pilot jobs: $catalogPath"
if ($PrepareOnly) { exit 0 }

$requiredReferences = @($jobs | Where-Object referenceMode -ne "none" | Select-Object -ExpandProperty reference -Unique)
foreach ($reference in $requiredReferences) { Send-Reference $reference }

$clientId = [guid]::NewGuid().ToString()
$outputRoot = Join-Path $ComfyRoot "output"
$statusPath = Join-Path $targetDir "pilot_status.json"
$generated = 0

foreach ($job in $jobs) {
    if ((Test-Path -LiteralPath $job.finalPath) -and -not $Force) {
        Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"
        continue
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $job.sourcePath) -Force | Out-Null
    New-Item -ItemType Directory -Path (Split-Path -Parent $job.finalPath) -Force | Out-Null

    $body = @{ prompt = (New-PromptGraph $job); client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) {
        throw "ComfyUI rejected [$($job.index):$($job.variant)] $($job.title): $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)"
    }

    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) -> $promptId"
    $history = $null
    while ($null -eq $history) {
        Start-Sleep -Seconds 2
        $history = Get-CompletedHistory $promptId
    }

    foreach ($outputSpec in @(@{ node = "20"; target = $job.sourcePath }, @{ node = "21"; target = $job.finalPath })) {
        $images = @($history.outputs.($outputSpec.node).images)
        if ($images.Count -eq 0) {
            throw "ComfyUI completed without node $($outputSpec.node) output for [$($job.index):$($job.variant)] $($job.title): $($history.status | ConvertTo-Json -Depth 8 -Compress)"
        }
        $image = $images[0]
        $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
        Copy-Item -LiteralPath $source -Destination $outputSpec.target -Force
    }

    $generated++
    [ordered]@{
        updatedAt = (Get-Date).ToString("o")
        selectedJobs = $jobs.Count
        generatedThisRun = $generated
        lastCompleted = @{ index = $job.index; variant = $job.variant; title = $job.title; promptId = $promptId }
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($generated/$($jobs.Count))"
}

Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
