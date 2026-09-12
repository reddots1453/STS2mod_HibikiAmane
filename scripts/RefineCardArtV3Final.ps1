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
$pilotRoot = Join-Path $modRoot "图片素材\第一批卡图V3试制"
$baseDir = Join-Path $pilotRoot "final"
$targetDir = Join-Path $pilotRoot "refined"
$configPath = Join-Path $pilotRoot "refinement_config.json"
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = $sha.ComputeHash($bytes)
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function Send-BaseImage([string]$filename) {
    $path = Join-Path $baseDir $filename
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing V3 final base: $path" }
    $form = @{
        image = Get-Item -LiteralPath $path
        subfolder = "MaidenSuccubus/V3RefineBases"
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function Get-PromptText($card, $variant) {
    return @(
        [string]$config.quality
        [string]$card.identity
        [string]$card.outfit
        "(($([string]$variant.edit)):1.45)"
        [string]$config.composition
        "no text, no border, no interface"
    ) -join ", "
}

function New-PromptGraph($job) {
    $output = $config.output
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = [string]$output.lora; strength_model = [double]$output.loraModel; strength_clip = [double]$output.loraClip } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = [int]$output.clipSkip } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/V3RefineBases/$($job.base)" } }
        "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = [int]$output.width; height = [int]$output.height; crop = "disabled" } }
        "8" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("7", 0); vae = @("1", 2) } }
        "9" = @{ class_type = "KSampler"; inputs = @{
            model = @("2", 0); seed = [int64]$job.seed; steps = [int]$output.steps; cfg = [double]$output.cfg
            sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
            positive = @("4", 0); negative = @("5", 0); latent_image = @("8", 0); denoise = [double]$job.denoise
        } }
        "10" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("9", 0); vae = @("1", 2) } }
        "11" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$config.facePositive } }
        "12" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$config.faceNegative } }
        "13" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "14" = @{ class_type = "FaceDetailer"; inputs = @{
            image = @("10", 0); model = @("2", 0); clip = @("3", 0); vae = @("1", 2)
            guide_size = 512.0; guide_size_for = $true; max_size = 1024.0
            seed = [int64]($job.seed + 193); steps = 12; cfg = 5.5; sampler_name = "euler_ancestral"; scheduler = "normal"
            positive = @("11", 0); negative = @("12", 0); denoise = 0.18; feather = 5
            noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 10; bbox_crop_factor = 3.0
            sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0
            sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10
            bbox_detector = @("13", 0); wildcard = ""; cycle = 1
        } }
        "15" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/V3Refined/$($job.prefix)"; images = @("14", 0) } }
    }
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
        $prefix = "{0:D3}_{1}_r{2:D2}" -f [int]$card.index, [string]$card.class, [int]$variant.variant
        $jobs += [pscustomobject]@{
            index = [int]$card.index
            class = [string]$card.class
            title = [string]$card.title
            variant = [int]$variant.variant
            base = [string]$card.base
            denoise = [double]$variant.denoise
            seed = Get-StableSeed "$($config.designDocSha256):$($card.class):v3-refined:$($variant.variant)"
            positive = Get-PromptText $card $variant
            negative = @([string]$config.negative, [string]$variant.avoid) -join ", "
            prefix = $prefix
            outputPath = Join-Path $targetDir "$prefix.png"
        }
    }
}
if ($jobs.Count -eq 0) { throw "No refinement jobs selected." }

$catalogPath = Join-Path $pilotRoot "refinement_prompt_catalog.json"
[ordered]@{
    schemaVersion = 1
    designDocSha256 = [string]$config.designDocSha256
    jobs = @($jobs | Select-Object index, class, title, variant, base, denoise, seed, positive, negative, outputPath)
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $catalogPath -Encoding utf8
Write-Host "Prepared $($jobs.Count) V3 refinement jobs: $catalogPath"
if ($PrepareOnly) { exit 0 }

$requiredBases = @($jobs | Select-Object -ExpandProperty base -Unique)
foreach ($base in $requiredBases) { Send-BaseImage $base }

$clientId = [guid]::NewGuid().ToString()
$outputRoot = Join-Path $ComfyRoot "output"
$generated = 0

foreach ($job in $jobs) {
    if ((Test-Path -LiteralPath $job.outputPath) -and -not $Force) {
        Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"
        continue
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $job.outputPath) -Force | Out-Null
    $body = @{ prompt = (New-PromptGraph $job); client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) {
        throw "ComfyUI rejected [$($job.index):$($job.variant)] $($job.title): $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)"
    }

    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) denoise=$($job.denoise) -> $promptId"
    $history = $null
    while ($null -eq $history) {
        Start-Sleep -Seconds 2
        $history = Get-CompletedHistory $promptId
    }

    $images = @($history.outputs."15".images)
    if ($images.Count -eq 0) {
        throw "ComfyUI completed without output for [$($job.index):$($job.variant)] $($job.title): $($history.status | ConvertTo-Json -Depth 8 -Compress)"
    }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $job.outputPath -Force
    $generated++
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($generated/$($jobs.Count))"
}

Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
