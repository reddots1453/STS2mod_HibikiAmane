param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV23_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$basePath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV22_20260919\058_MasochisticTrance_被虐的恍惚_邪瘴天衣_LOCAL_v22_03.png"
$outfitPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久1_严重破损.png"
$maskPath = Join-Path $targetDir "058_MasochisticTrance_衣装与手腕_inpaint_mask.png"
$outputRoot = Join-Path $ComfyRoot "output"

New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

Add-Type -AssemblyName System.Drawing
$mask = [System.Drawing.Bitmap]::new(1000, 760)
try {
    $g = [System.Drawing.Graphics]::FromImage($mask)
    try {
        $g.Clear([System.Drawing.Color]::Black)
        $white = [System.Drawing.Brushes]::White
        $leftArm = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(476, 12), [System.Drawing.Point]::new(536, 12),
            [System.Drawing.Point]::new(548, 270), [System.Drawing.Point]::new(493, 270)
        )
        $rightArm = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(548, 12), [System.Drawing.Point]::new(610, 12),
            [System.Drawing.Point]::new(608, 274), [System.Drawing.Point]::new(553, 274)
        )
        $torso = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(452, 220), [System.Drawing.Point]::new(642, 220),
            [System.Drawing.Point]::new(625, 442), [System.Drawing.Point]::new(470, 442)
        )
        $g.FillPolygon($white, $leftArm)
        $g.FillPolygon($white, $rightArm)
        $g.FillEllipse($white, 485, 0, 130, 110)
        $g.FillPolygon($white, $torso)
    }
    finally { $g.Dispose() }
    $mask.Save($maskPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $mask.Dispose() }

$quality = "masterpiece, best quality, polished anime game illustration, clean thin colored lineart, controlled two-step cel shading, coherent adult anatomy, crisp focal details"
$positiveCore = @(
    "celesphonia", "hibiki amane", "one adult woman", "mature adult body", "aqua eyes",
    "long blonde hair fading to pale pink", "one side ponytail with blue ribbon",
    "preserve the exact pose scale legs boots face hair and abstract dark violet background from the base image",
    "replace only masked clothing and arms with the exact severely damaged corrupted magical-girl armor from the outfit reference",
    "long detached black-purple gloves with gold trim", "purple rose ornament",
    "torn purple corset waist armor with branching gold lines", "small torn black chest armor remnants",
    "adult topless battle-damaged state", "exposed breasts remain anatomically natural",
    "both forearms fully visible", "two natural hands", "wrists crossed above the head",
    "one loose iron chain clearly wraps around both crossed wrists without merging into skin",
    "open half-lidded unfocused aqua eyes", "glazed pupils", "flushed cheeks", "parted trembling lips",
    "pain and pleasure intertwined", "lost ecstatic trance", "small tear", "light sweat"
) -join ", "
$negative = @(
    "worst quality", "low quality", "blurry", "photorealistic", "3d", "chibi", "child", "teenager", "young-looking", "loli",
    "extra arms", "missing arm", "floating limb", "broken elbow", "broken wrist", "extra fingers", "six fingers", "four fingers", "fused fingers", "malformed hands",
    "hands behind head", "hidden hands", "chain through skin", "chain as limb", "chain fused to hand",
    "blue white holy costume", "plain black swimsuit", "bra", "modest chest armor", "missing purple corset", "missing long gloves",
    "eyes closed", "serene expression", "frightened expression", "angry expression", "blank expression",
    "new background", "room", "wall", "floor", "text", "logo", "watermark", "frame", "UI"
) -join ", "

$variantSpecs = @{
    1 = [ordered]@{ seed = 580231; outfitWeight = 0.48; denoise = 0.72; prompt = "clean gold-trimmed gloves, readable crossed hands, restrained purple corset" }
    2 = [ordered]@{ seed = 580237; outfitWeight = 0.56; denoise = 0.78; prompt = "strong outfit fidelity, purple rose and gold branch motifs, dazed tearful smile" }
    3 = [ordered]@{ seed = 580241; outfitWeight = 0.64; denoise = 0.82; prompt = "strongest corrupted armor details, chain loop clearly separate from wrists, intense ecstatic trance" }
    4 = [ordered]@{ seed = 580253; outfitWeight = 0.52; denoise = 0.75; prompt = "balanced clean cel shading, five fingers on each hand, half-lidded unfocused gaze" }
}

function Upload-ComfyImage([string]$source, [string]$subfolder) {
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing input: $source" }
    $form = @{ image = Get-Item -LiteralPath $source; subfolder = $subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function Clear-ComfyModels {
    Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
    Start-Sleep -Seconds 2
}

function New-Graph([int]$variant, $spec) {
    $positive = @($quality, $positiveCore, $spec.prompt) -join ", "
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.46; strength_clip = 0.46 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV23/outfit/魔装耐久1_严重破损.png" } }
        "9" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image = @("8", 0); weight = [double]$spec.outfitWeight
            weight_type = "linear"; combine_embeds = "average"; start_at = 0.0; end_at = 0.74
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "10" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV23/base/058_MasochisticTrance_被虐的恍惚_邪瘴天衣_LOCAL_v22_03.png" } }
        "11" = @{ class_type = "LoadImageMask"; inputs = @{ image = "MaidenSuccubus/EroticLocalV23/mask/058_MasochisticTrance_衣装与手腕_inpaint_mask.png"; channel = "red" } }
        "12" = @{ class_type = "InpaintModelConditioning"; inputs = @{ positive = @("4", 0); negative = @("5", 0); vae = @("1", 2); pixels = @("10", 0); mask = @("11", 0); noise_mask = $true } }
        "13" = @{ class_type = "KSampler"; inputs = @{
            model = @("9", 0); seed = [int64]$spec.seed; steps = 28; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("12", 0); negative = @("12", 1); latent_image = @("12", 2); denoise = [double]$spec.denoise
        } }
        "14" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("13", 0); vae = @("1", 2) } }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV23/058_MasochisticTrance_corrupt_v23_{0:D2}" -f $variant); images = @("14", 0) } }
    }
}

function Invoke-ComfyGraph($graph, [string]$label, [string]$clientId) {
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 40 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected $label`: $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  $label -> $promptId"
    $deadline = (Get-Date).AddMinutes(20)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $historyRoot = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 20
        $property = $historyRoot.PSObject.Properties[$promptId]
        if ($null -eq $property) { continue }
        $images = @($property.Value.outputs."40".images)
        if ($images.Count -eq 0) { throw "ComfyUI completed without output for $label" }
        return $images[0]
    }
    throw "Timed out after 20 minutes while running $label"
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Clear-ComfyModels
Upload-ComfyImage $basePath "MaidenSuccubus/EroticLocalV23/base"
Upload-ComfyImage $outfitPath "MaidenSuccubus/EroticLocalV23/outfit"
Upload-ComfyImage $maskPath "MaidenSuccubus/EroticLocalV23/mask"

$clientId = [guid]::NewGuid().ToString()
$jobs = @()
foreach ($variant in $Variants) {
    if (-not $variantSpecs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $variantSpecs[$variant]
    $targetName = "058_MasochisticTrance_被虐的恍惚_邪瘴天衣_LOCAL_v23_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $graph = New-Graph $variant $spec
    $graph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_corrupt_v23_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $jobs += [ordered]@{ variant = $variant; seed = $spec.seed; outfitWeight = $spec.outfitWeight; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [58:$variant] 被虐的恍惚"; continue }
    Clear-ComfyModels
    $image = Invoke-ComfyGraph $graph "[58:${variant}] localized-outfit-and-wrist-inpaint" $clientId
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE [58:$variant] 被虐的恍惚"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    pipeline = "LOCAL_COMFYUI_V23_MASKED_OUTFIT_AND_WRIST_INPAINT"
    outputSize = "1000x760"
    baseCandidate = "EroticLocalV22_20260919/058_MasochisticTrance_被虐的恍惚_邪瘴天衣_LOCAL_v22_03.png"
    outfitReference = "变身形态/邪瘴天衣/魔装耐久1_严重破损.png"
    mask = "058_MasochisticTrance_衣装与手腕_inpaint_mask.png"
    status = "awaiting-player-review"
    jobs = $jobs
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v23.json") -Encoding utf8

Clear-ComfyModels
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V23_MASKED_OUTFIT_AND_WRIST_INPAINT selected=$($Variants.Count)"
