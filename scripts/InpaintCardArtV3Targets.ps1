param(
    [int[]]$Indexes = @(),
    [int[]]$Variants = @(),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$pilotRoot = Join-Path $modRoot "图片素材\第一批卡图V3试制"
$targetDir = Join-Path $pilotRoot "targeted"
$configPath = Join-Path $pilotRoot "targeted_inpaint_config.json"
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

function Send-Image([string]$relativePath, [string]$subfolder) {
    $path = Join-Path $pilotRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing input: $path" }
    $form = @{
        image = Get-Item -LiteralPath $path
        subfolder = $subfolder
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
    return Split-Path -Leaf $relativePath
}

function New-PromptGraph($job) {
    $output = $config.output
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = [string]$output.lora; strength_model = [double]$output.loraModel; strength_clip = [double]$output.loraClip } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = [int]$output.clipSkip } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = [string]$job.negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/V3TargetBases/$($job.baseName)" } }
        "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = [int]$output.width; height = [int]$output.height; crop = "disabled" } }
        "8" = @{ class_type = "LoadImageMask"; inputs = @{ image = "MaidenSuccubus/V3TargetMasks/$($job.maskName)"; channel = "red" } }
        "9" = @{ class_type = "GrowMaskWithBlur"; inputs = @{ mask = @("8", 0); expand = 8; incremental_expandrate = 0.0; tapered_corners = $true; flip_input = $false; blur_radius = 10.0; lerp_alpha = 1.0; decay_factor = 1.0; fill_holes = $true } }
        "10" = @{ class_type = "VAEEncodeForInpaint"; inputs = @{ pixels = @("7", 0); vae = @("1", 2); mask = @("9", 0); grow_mask_by = 8 } }
        "11" = @{ class_type = "KSampler"; inputs = @{
            model = @("2", 0); seed = [int64]$job.seed; steps = [int]$output.steps; cfg = [double]$output.cfg
            sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
            positive = @("4", 0); negative = @("5", 0); latent_image = @("10", 0); denoise = [double]$job.denoise
        } }
        "12" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("11", 0); vae = @("1", 2) } }
        "13" = @{ class_type = "ImageCompositeMasked"; inputs = @{ destination = @("7", 0); source = @("12", 0); x = 0; y = 0; resize_source = $false; mask = @("9", 0) } }
        "14" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/V3Targeted/$($job.prefix)"; images = @("13", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Copy-Output($history, [string]$destination) {
    $images = @()
    foreach ($node in $history.outputs.PSObject.Properties) {
        if ($null -ne $node.Value.images) { $images += @($node.Value.images) }
    }
    if ($images.Count -eq 0) { throw "ComfyUI returned no image output." }
    $image = $images[-1]
    $source = Join-Path $ComfyRoot "output"
    if ($image.subfolder) { $source = Join-Path $source ([string]$image.subfolder) }
    $source = Join-Path $source ([string]$image.filename)
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing ComfyUI output: $source" }
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

[void](New-Item -ItemType Directory -Force -Path $targetDir)
$jobs = @()
foreach ($card in $config.cards) {
    if ($Indexes.Count -gt 0 -and $Indexes -notcontains [int]$card.index) { continue }
    $baseName = Send-Image ([string]$card.base) "MaidenSuccubus/V3TargetBases"
    $maskName = Send-Image ([string]$card.mask) "MaidenSuccubus/V3TargetMasks"
    foreach ($variant in $card.variants) {
        if ($Variants.Count -gt 0 -and $Variants -notcontains [int]$variant.variant) { continue }
        $prefix = "{0:D3}_{1}_t{2:D2}" -f [int]$card.index, [string]$card.class, [int]$variant.variant
        $jobs += [pscustomobject]@{
            index = [int]$card.index
            title = [string]$card.title
            baseName = $baseName
            maskName = $maskName
            prefix = $prefix
            denoise = [double]$variant.denoise
            seed = Get-StableSeed("V3-targeted|$prefix")
            positive = @([string]$config.quality, [string]$card.identity, "(($([string]$card.prompt)):1.5)", "preserve everything outside the mask exactly", "no text, no border, no interface") -join ", "
            negative = @([string]$config.negative, [string]$card.avoid) -join ", "
        }
    }
}

$count = 0
foreach ($job in $jobs) {
    $destination = Join-Path $targetDir "$($job.prefix).png"
    if ((Test-Path -LiteralPath $destination) -and -not $Force) {
        Write-Host "SKIP [$($job.index)] $($job.title) -> $destination"
        continue
    }
    $graph = New-PromptGraph $job
    $body = @{ prompt = $graph } | ConvertTo-Json -Depth 20 -Compress
    $queued = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    $promptId = [string]$queued.prompt_id
    Write-Host "RUN  [$($job.index)] $($job.title) denoise=$($job.denoise) -> $promptId"
    do {
        Start-Sleep -Seconds 2
        $history = Get-CompletedHistory $promptId
    } until ($null -ne $history)
    if ($history.status.status_str -ne "success") {
        $messages = $history.status.messages | ConvertTo-Json -Depth 10 -Compress
        throw "ComfyUI failed for $($job.prefix): $messages"
    }
    Copy-Output $history $destination
    $count++
    Write-Host "DONE [$($job.index)] $($job.title) -> $destination"
}
Write-Host "COMPLETE generated_this_run=$count selected=$($jobs.Count)"
