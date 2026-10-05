param(
    [int[]]$Variants = @(),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$root = Join-Path $modRoot "图片素材\第一批卡图V3试制"
$targetDir = Join-Path $root "dark_element_outfit"
$config = Get-Content -Raw -LiteralPath (Join-Path $root "dark_element_outfit_config.json") | ConvertFrom-Json
$referencePath = Join-Path $root ([string]$config.reference)

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value))
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function Get-History([string]$promptId) {
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
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

if (-not (Test-Path -LiteralPath $referencePath)) { throw "Missing outfit reference: $referencePath" }
[void](New-Item -ItemType Directory -Force -Path $targetDir)
$form = @{ image = Get-Item -LiteralPath $referencePath; subfolder = "MaidenSuccubus/DarkElement"; type = "input"; overwrite = "true" }
[void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
$referenceName = Split-Path -Leaf $referencePath

foreach ($candidate in $config.variants) {
    if ($null -eq $candidate.base) { continue }
    $basePath = Join-Path $root ([string]$candidate.base)
    if (-not (Test-Path -LiteralPath $basePath)) { throw "Missing variant base: $basePath" }
    $baseForm = @{ image = Get-Item -LiteralPath $basePath; subfolder = "MaidenSuccubus/DarkElement"; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $baseForm)
}

foreach ($variant in $config.variants) {
    $number = [int]$variant.variant
    if ($Variants.Count -gt 0 -and $Variants -notcontains $number) { continue }
    $destination = Join-Path $targetDir ("049_DarkElement_o{0:D2}.png" -f $number)
    if ((Test-Path -LiteralPath $destination) -and -not $Force) { Write-Host "SKIP $destination"; continue }
    $seed = Get-StableSeed("DarkElement-original-outfit-v$number")
    $action = if ($null -ne $variant.action) { [string]$variant.action } else { [string]$config.action }
    $positive = @([string]$config.quality, [string]$config.identity, [string]$config.outfit, $action, [string]$variant.camera, [string]$variant.expression, [string]$config.composition, "no text, no border, no interface") -join ", "
    $output = $config.output
    $loraModel = if ($null -ne $variant.loraModel) { [double]$variant.loraModel } else { [double]$output.loraModel }
    $loraClip = if ($null -ne $variant.loraClip) { [double]$variant.loraClip } else { [double]$output.loraClip }
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1",0); clip = @("1",1); lora_name = [string]$output.lora; strength_model = $loraModel; strength_clip = $loraClip } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2",1); stop_at_clip_layer = [int]$output.clipSkip } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3",0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3",0); text = [string]$config.negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/DarkElement/$referenceName" } }
    }
    $sourceModel = @("2",0)
    if ([string]$variant.mode -in @("ipadapter", "hybrid")) {
        $graph["7"] = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = [string]$output.ipAdapter } }
        $graph["8"] = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = [string]$output.clipVision } }
        $graph["9"] = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("6",0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        $graph["10"] = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2",0); ipadapter = @("7",0); image = @("9",0); clip_vision = @("8",0)
            weight = [double]$variant.weight; weight_type = [string]$variant.weightType; combine_embeds = "average"
            start_at = 0.0; end_at = [double]$variant.endAt; embeds_scaling = "V only"
        } }
        $sourceModel = @("10",0)
        if ([string]$variant.mode -eq "ipadapter") {
            $graph["11"] = @{ class_type = "EmptyLatentImage"; inputs = @{ width = [int]$output.width; height = [int]$output.height; batch_size = 1 } }
        }
        else {
            $baseName = Split-Path -Leaf ([string]$variant.base)
            $graph["20"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/DarkElement/$baseName" } }
            $graph["21"] = @{ class_type = "ImageScale"; inputs = @{ image = @("20",0); upscale_method = "lanczos"; width = [int]$output.width; height = [int]$output.height; crop = "disabled" } }
            $graph["22"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("21",0); vae = @("1",2) } }
        }
    }
    else {
        $graph["11"] = @{ class_type = "ImageScale"; inputs = @{ image = @("6",0); upscale_method = "lanczos"; width = [int]$output.width; height = [int]$output.height; crop = "disabled" } }
        $graph["12"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("11",0); vae = @("1",2) } }
    }
    $latent = if ([string]$variant.mode -eq "ipadapter") { @("11",0) } elseif ([string]$variant.mode -eq "hybrid") { @("22",0) } else { @("12",0) }
    $denoise = if ([string]$variant.mode -eq "ipadapter") { 1.0 } else { [double]$variant.denoise }
    $graph["13"] = @{ class_type = "KSampler"; inputs = @{
        model = $sourceModel; seed = $seed; steps = [int]$output.steps; cfg = [double]$output.cfg
        sampler_name = [string]$output.sampler; scheduler = [string]$output.scheduler
        positive = @("4",0); negative = @("5",0); latent_image = $latent; denoise = $denoise
    } }
    $graph["14"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("13",0); vae = @("1",2) } }
    $graph["15"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3",0); text = [string]$config.facePositive } }
    $graph["16"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3",0); text = [string]$config.faceNegative } }
    $graph["17"] = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
    $graph["18"] = @{ class_type = "FaceDetailer"; inputs = @{
        image = @("14",0); model = @("2",0); clip = @("3",0); vae = @("1",2); guide_size = 512.0; guide_size_for = $true; max_size = 1024.0
        seed = [int64]($seed + 193); steps = 12; cfg = 5.5; sampler_name = "euler_ancestral"; scheduler = "normal"; positive = @("15",0); negative = @("16",0)
        denoise = 0.18; feather = 5; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 10; bbox_crop_factor = 3.0
        sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"
        drop_size = 10; bbox_detector = @("17",0); wildcard = ""; cycle = 1
    } }
    $graph["19"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/DarkElementOutfit/049_DarkElement_o$('{0:D2}' -f $number)"; images = @("18",0) } }
    $body = @{ prompt = $graph } | ConvertTo-Json -Depth 20 -Compress
    $queued = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    $promptId = [string]$queued.prompt_id
    Write-Host "RUN  v$number mode=$($variant.mode) -> $promptId"
    do { Start-Sleep -Seconds 2; $history = Get-History $promptId } until ($null -ne $history)
    if ($history.status.status_str -ne "success") { throw ($history.status.messages | ConvertTo-Json -Depth 10 -Compress) }
    Copy-Output $history $destination
    Write-Host "DONE v$number -> $destination"
}
