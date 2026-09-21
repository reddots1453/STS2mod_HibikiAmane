param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV22_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$compositionPath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV6_20260914\058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png"
$outfitPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久1_严重破损.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$characterLora = "celesphonia-1.8.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"
$openPoseControlNet = "NoobAI\noobaiXLControlnet_openposeModel.safetensors"

New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$outfitCanvasPath = Join-Path $targetDir "058_MasochisticTrance_邪瘴天衣_img2img_base.png"
Add-Type -AssemblyName System.Drawing
$sourceOutfit = [System.Drawing.Image]::FromFile($outfitPath)
try {
    $canvas = [System.Drawing.Bitmap]::new(1000, 760)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(31, 18, 56))
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $height = 720
            $width = [int][math]::Round($sourceOutfit.Width * $height / $sourceOutfit.Height)
            $x = [int][math]::Round((1000 - $width) / 2)
            $graphics.DrawImage($sourceOutfit, $x, 20, $width, $height)
        }
        finally { $graphics.Dispose() }
        $canvas.Save($outfitCanvasPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $canvas.Dispose() }
}
finally { $sourceOutfit.Dispose() }

$quality = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "polished anime game illustration", "clean thin colored lineart", "controlled two-step cel shading",
    "crisp focal details", "coherent adult anatomy", "correct limb attachment", "readable silhouette"
) -join ", "

$identity = @(
    "celesphonia", "hibiki amane", "one adult woman", "mature adult body", "aqua eyes",
    "long blonde hair fading to pale pink", "one side ponytail tied with a simple blue ribbon", "no hair rings"
) -join ", "

$outfit = @(
    "the exact severely damaged corrupted magical-girl armor shown in the outfit reference",
    "torn black and deep-purple chest armor", "purple rose ornament", "purple corset waist armor with branching gold lines",
    "detached long black-purple gloves with gold trim", "purple and gold armored thigh-high boots",
    "adult topless battle-damaged state with both breasts exposed as in the reference",
    "torn-away crotch armor exposes her adult vulva as in the reference",
    "damaged open sections match the reference", "no modest replacement fabric", "no blue-white holy costume", "no plain black swimsuit"
) -join ", "

$pose = @(
    "solo large full-body frontal composition", "adult woman occupies about seventy-five percent of canvas height and forty percent of canvas width",
    "standing centered", "both arms raised over her head", "both forearms and both hands fully visible in front of the hair",
    "wrists visibly crossed together above the crown and never hidden behind the head", "exactly two shoulders and two continuous arms",
    "legs crossed at the knees", "one loose iron chain arcs around the crossed wrists",
    "one separate loose iron chain wraps around the knees", "chains remain outside the body and never become limbs",
    "no captor", "no second person", "no penetration"
) -join ", "

$expression = @(
    "pain and pleasure intertwined", "lost ecstatic trance", "open unfocused half-lidded aqua eyes",
    "glazed pupils", "deeply flushed cheeks", "slightly parted trembling lips", "faint blissful smile",
    "one small tear at the eye corner", "light sweat", "consciousness fading into rapture",
    "not eyes closed", "not serene", "not frightened", "not angry", "not blank-faced"
) -join ", "

$layout = @(
    "1000 by 760 horizontal card illustration", "subject fills the central upper safe area",
    "head hands torso hips and knees visible", "simple abstract dark violet background",
    "restrained purple curse wisps and faint cyan defensive aura", "((featureless abstract dark violet void background:1.45))",
    "((no room and no walls:1.45))", "no concrete location", "no floor", "no horizon",
    "no text", "no logo", "no watermark", "no frame", "no border", "no interface", "no card-shaped object"
) -join ", "

$negative = @(
    "worst quality", "low quality", "lowres", "blurry focal subject", "out of focus body", "jpeg artifacts",
    "rough sketch", "thick black outlines", "painterly impasto", "glossy 3d", "photorealistic", "chibi",
    "child", "teenager", "young-looking", "loli", "school uniform", "multiple women", "second person", "captor",
    "duplicate body", "duplicate torso", "extra arms", "third arm", "missing arm", "floating limb",
    "twisted shoulder", "broken elbow", "broken wrist", "dislocated hip", "extra legs", "missing leg",
    "extra fingers", "six fingers", "four fingers", "missing fingers", "fused fingers", "malformed hands",
    "chain through skin", "chain fused to arm", "chain fused to leg", "chain as limb", "detached chain fragments",
    "plain black swimsuit", "blue white magical-girl dress", "wrong costume", "missing purple corset", "missing boots",
    "covered breasts", "bra", "modest chest armor", "covered crotch", "hands behind head", "hidden hands", "tiny person", "distant subject",
    "eyes closed", "serene expression", "frightened expression", "angry expression", "blank expression", "cross-eyed", "asymmetrical eyes",
    "room", "bedroom", "dungeon", "wall", "floor", "horizon", "text", "logo", "watermark", "frame", "UI"
) -join ", "

$variantSpecs = @{
    1 = [ordered]@{ seedKey = "v22-a"; outfitWeight = 0.44; poseWeight = 1.00; denoise = 0.72; prompt = "clean crossed wrists, flower-and-gold outfit details, dazed tender smile" }
    2 = [ordered]@{ seedKey = "v22-b"; outfitWeight = 0.52; poseWeight = 1.00; denoise = 0.78; prompt = "separated arm contours, intensely glazed half-lidded eyes" }
    3 = [ordered]@{ seedKey = "v22-c"; outfitWeight = 0.60; poseWeight = 0.96; denoise = 0.82; prompt = "restrained chain arcs, bliss and pain visibly mixed" }
    4 = [ordered]@{ seedKey = "v22-d"; outfitWeight = 0.48; poseWeight = 1.00; denoise = 0.76; prompt = "anatomically clean hands and shoulders, tearful ecstatic trance" }
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [int64]([BitConverter]::ToUInt64($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0) -band 0x001FFFFFFFFFFFFF) }
    finally { $sha.Dispose() }
}

function Upload-ComfyImage([string]$source, [string]$subfolder, [string]$name) {
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing input: $source" }
    $form = @{ image = Get-Item -LiteralPath $source; subfolder = $subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function Clear-ComfyModels {
    Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
    Start-Sleep -Seconds 2
}

function New-DetailerInputs($imageNode, $positiveNode, $negativeNode, $detectorNode, [int64]$seed, [double]$denoise, [double]$guideSize) {
    return @{
        image = $imageNode; model = @("12", 0); clip = @("3", 0); vae = @("1", 2)
        guide_size = $guideSize; guide_size_for = $true; max_size = 1100.0; seed = $seed
        steps = 16; cfg = 4.8; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = $positiveNode; negative = $negativeNode; denoise = $denoise
        feather = 5; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.45
        bbox_dilation = 8; bbox_crop_factor = 2.8; sam_detection_hint = "none"; sam_dilation = 0
        sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7
        sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = $detectorNode
        wildcard = ""; cycle = 1
    }
}

function New-Graph([int]$variant, $spec, [int64]$seed) {
    $positive = @($quality, $identity, $outfit, $pose, $expression, $spec.prompt, $layout) -join ", "
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $characterLora; strength_model = 0.42; strength_clip = 0.42 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV22/style/065_ReflectiveBarrier_反射屏障.png" } }
        "9" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV22/composition/058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png" } }
        "10" = @{ class_type = "IPAdapterStyleComposition"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image_style = @("8", 0); image_composition = @("9", 0)
            weight_style = 0.01; weight_composition = 0.01
            expand_style = $false; combine_embeds = "average"; start_at = 0.0; end_at = 0.76
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "11" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV22/outfit/058_MasochisticTrance_邪瘴天衣_img2img_base.png" } }
        "12" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("10", 0); ipadapter = @("6", 0); image = @("11", 0); weight = [double]$spec.outfitWeight
            weight_type = "linear"; combine_embeds = "average"; start_at = 0.0; end_at = 0.76
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "13" = @{ class_type = "OpenposePreprocessor"; inputs = @{ image = @("9", 0); detect_hand = "enable"; detect_body = "enable"; detect_face = "enable"; resolution = 768; scale_stick_for_xinsr_cn = "disable" } }
        "14" = @{ class_type = "ControlNetLoader"; inputs = @{ control_net_name = $openPoseControlNet } }
        "15" = @{ class_type = "ControlNetApplyAdvanced"; inputs = @{
            positive = @("4", 0); negative = @("5", 0); control_net = @("14", 0); image = @("13", 0)
            strength = [double]$spec.poseWeight; start_percent = 0.0; end_percent = 0.86; vae = @("1", 2)
        } }
        "16" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("11", 0); vae = @("1", 2) } }
        "17" = @{ class_type = "KSampler"; inputs = @{
            model = @("2", 0); seed = $seed; steps = 30; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("15", 0); negative = @("15", 1); latent_image = @("16", 0); denoise = [double]$spec.denoise
        } }
        "18" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("17", 0); vae = @("1", 2) } }
        "21" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "adult woman, ecstatic trance, half-lidded unfocused aqua eyes, flushed cheeks, parted lips, one small tear, crisp cel-shaded face" } }
        "22" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, young-looking, frightened, angry, blank expression, deformed face, cross-eyed, duplicate face, blurred face" } }
        "23" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "24" = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("18", 0) @("21", 0) @("22", 0) @("23", 0) ([int64]($seed + 193)) 0.20 560.0 }
        "25" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "crossed wrists, two natural adult female hands, exactly five fingers on each hand, clean joints, crisp cel shading" } }
        "26" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "extra hand, missing hand, extra fingers, six fingers, four fingers, fused fingers, broken wrist, malformed hand, chain fused to hand" } }
        "27" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/hand_yolov8s.pt" } }
        "28" = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("24", 0) @("25", 0) @("26", 0) @("27", 0) ([int64]($seed + 389)) 0.14 448.0 }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV22/058_MasochisticTrance_corrupt_v22_{0:D2}" -f $variant); images = @("18", 0) } }
    }
    return $graph
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

function Copy-ComfyOutput($image, [string]$destination) {
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Clear-ComfyModels
Upload-ComfyImage $stylePath "MaidenSuccubus/EroticLocalV22/style" "065_ReflectiveBarrier_反射屏障.png"
Upload-ComfyImage $compositionPath "MaidenSuccubus/EroticLocalV22/composition" "058_MasochisticTrance_v6_03.png"
Upload-ComfyImage $outfitCanvasPath "MaidenSuccubus/EroticLocalV22/outfit" "058_MasochisticTrance_邪瘴天衣_img2img_base.png"

$clientId = [guid]::NewGuid().ToString()
$jobs = @()
foreach ($variant in $Variants) {
    if (-not $variantSpecs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $variantSpecs[$variant]
    $seed = Get-StableSeed "MasochisticTrance:$($spec.seedKey):20260919"
    $targetName = "058_MasochisticTrance_被虐的恍惚_邪瘴天衣_LOCAL_v22_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $graph = New-Graph $variant $spec $seed
    $graph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_corrupt_v22_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $jobs += [ordered]@{ variant = $variant; seed = $seed; outfitWeight = $spec.outfitWeight; poseWeight = $spec.poseWeight; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [58:$variant] 被虐的恍惚"; continue }
    Clear-ComfyModels
    $image = Invoke-ComfyGraph $graph "[58:${variant}] corrupt-outfit-redraw" $clientId
    Copy-ComfyOutput $image $target
    Write-Host "DONE [58:$variant] 被虐的恍惚"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    pipeline = "LOCAL_COMFYUI_V22_OUTFIT_IMG2IMG_OPENPOSE_NO_IPADAPTER"
    outputSize = "1000x760"
    compositionReference = "EroticLocalV6_20260914/058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png"
    outfitReference = "变身形态/邪瘴天衣/魔装耐久1_严重破损.png"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    status = "awaiting-player-review"
    jobs = $jobs
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v22.json") -Encoding utf8

Clear-ComfyModels
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V22_OUTFIT_IMG2IMG_OPENPOSE_NO_IPADAPTER selected=$($Variants.Count)"
