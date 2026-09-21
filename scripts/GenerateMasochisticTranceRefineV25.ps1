param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV25_20260921"
$workflowDir = Join-Path $targetDir "workflow_api"
$compositionPath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV6_20260914\058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png"
$outfitPath = Join-Path $targetDir "style_corrupt_succubus_subject_crop.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$maskPath = Join-Path $targetDir "058_MasochisticTrance_躯干衣装区域_inpaint_mask.png"
$outputRoot = Join-Path $ComfyRoot "output"

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$characterLora = "celesphonia-1.8.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"
$openPoseControlNet = "NoobAI\noobaiXLControlnet_openposeModel.safetensors"

New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

Add-Type -AssemblyName System.Drawing
$mask = [System.Drawing.Bitmap]::new(1000, 760)
try {
    $g = [System.Drawing.Graphics]::FromImage($mask)
    try {
        $g.Clear([System.Drawing.Color]::Black)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $body = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(350, 285),
            [System.Drawing.Point]::new(640, 285),
            [System.Drawing.Point]::new(630, 585),
            [System.Drawing.Point]::new(355, 585)
        )
        $g.FillPolygon([System.Drawing.Brushes]::White, $body)
    }
    finally { $g.Dispose() }
    $mask.Save($maskPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $mask.Dispose() }

$quality = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "polished Japanese anime game illustration", "clean thin colored lineart",
    "controlled two-step cel shading", "restrained highlights", "crisp focal details",
    "coherent adult anatomy", "correct limb attachment", "readable silhouette"
) -join ", "

$identity = @(
    "celesphonia", "hibiki amane", "one adult woman", "mature adult body", "cyan aqua eyes",
    "long blonde hair fading to pale pink at the ends", "one side ponytail tied with a blue accessory",
    "recognizable adult Hibiki Amane", "no hair rings"
) -join ", "

$outfit = @(
    "unmistakable severely damaged corrupted-route Jasho Ten'i torso armor from the outfit reference",
    "black and deep-purple corrupted magical-girl armor with gold trim", "purple rose decorations",
    "torn black chest armor remnants", "purple corset waist armor with branching gold thorn motif",
    "torn pale-lavender side skirt panels", "exposed shoulders and upper arms remain unchanged",
    "battle-damaged and erotic but visibly wearing the referenced armor",
    "all key outfit identifiers remain readable", "not a generic swimsuit", "not plain lingerie"
) -join ", "

$pose = @(
    "solo full-body frontal composition", "adult woman fills the central card area",
    "both arms raised straight above the head", "wrists crossed together above the crown",
    "both hands fully visible", "one iron chain loops around both wrist restraints and continues upward",
    "natural shoulders and elbows", "legs held close and lightly crossed at the knees",
    "a second loose iron chain curves around the lower legs without passing through the body",
    "one person only", "no captor", "no penetration"
) -join ", "

$expression = @(
    "masochistic trance", "pain and pleasure visibly intertwined", "dreamy intoxicated gaze",
    "half-lidded unfocused cyan eyes", "glazed pupils", "deeply flushed cheeks",
    "slightly parted trembling lips", "faint involuntary pleased smile",
    "one small tear at the eye corner", "light sweat", "relaxed facial muscles",
    "surrendering to the sensation", "not frightened", "not cheerful", "not blank-faced"
) -join ", "

$layout = @(
    "1000 by 760 horizontal card illustration", "character occupies about eighty percent of canvas height",
    "central upper safe-area focus", "abstract dark plum and muted violet background",
    "faint purple curse haze and restrained cyan defensive aura", "no concrete location",
    "no room", "no dungeon", "no floor", "no horizon", "no text", "no logo",
    "no watermark", "no frame", "no interface", "no card-shaped object"
) -join ", "

$negative = @(
    "worst quality", "low quality", "lowres", "blurry focal subject", "out of focus body",
    "jpeg artifacts", "rough sketch", "thick black outlines", "painterly", "glossy 3d",
    "photorealistic", "plastic skin", "chibi", "child", "teenager", "young-looking", "loli",
    "multiple women", "second person", "captor", "duplicate body", "duplicate torso",
    "extra arms", "third arm", "missing arm", "floating limb", "twisted shoulder",
    "broken elbow", "broken wrist", "dislocated hip", "extra legs", "missing leg",
    "extra fingers", "six fingers", "four fingers", "missing fingers", "fused fingers",
    "malformed hands", "hands behind head", "hidden hands", "chain through skin",
    "chain fused to hand", "chain fused to hair", "chain fused to leg", "chain as limb",
    "plain black swimsuit", "plain lingerie", "blue white holy costume", "cyan costume", "white costume", "wrong costume",
    "missing purple corset", "missing gloves", "missing rose ornament", "missing side skirt panels",
    "missing boots", "completely nude", "only boots", "eyes closed", "wide frightened eyes",
    "fear", "crying in terror", "angry expression", "blank expression", "cheerful smile",
    "oversaturated neon colors", "yellow skin", "posterized colors", "harsh black outline",
    "cross-eyed", "asymmetrical eyes", "room", "bedroom", "dungeon", "wall", "floor",
    "horizon", "text", "logo", "watermark", "frame", "UI"
) -join ", "

$variantSpecs = @{
    1 = [ordered]@{ seed = 583101; loraStrength = 0.44; styleWeight = 0.0; compositionWeight = 0.0; outfitWeight = 0.82; poseWeight = 0.82; denoise = 0.54; prompt = "purple corset waist armor with gold thorn branches, black torn chest armor edges, small purple rose ornament" }
    2 = [ordered]@{ seed = 583107; loraStrength = 0.42; styleWeight = 0.0; compositionWeight = 0.0; outfitWeight = 0.94; poseWeight = 0.86; denoise = 0.60; prompt = "faithful damaged purple corset and pale-lavender torn hip panels, gold trim, black armor fragments" }
    3 = [ordered]@{ seed = 583113; loraStrength = 0.38; styleWeight = 0.0; compositionWeight = 0.0; outfitWeight = 1.04; poseWeight = 0.88; denoise = 0.66; prompt = "exact torso costume from reference, purple corset, branching gold motif, black torn breast armor remnants, rose decoration" }
    4 = [ordered]@{ seed = 583119; loraStrength = 0.34; styleWeight = 0.0; compositionWeight = 0.0; outfitWeight = 1.12; poseWeight = 0.90; denoise = 0.72; prompt = "maximum outfit fidelity inside mask only, deep-purple black and gold damaged magical armor, no blue fabric" }
    5 = [ordered]@{ seed = 583127; loraStrength = 0.38; styleWeight = 0.0; compositionWeight = 0.0; outfitWeight = 0.96; poseWeight = 0.88; denoise = 0.61; prompt = "faithful magenta-black-gold corrupted succubus armor, heart and rose accents, ruffled trim, open chest, angular waist armor" }
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

function New-DetailerInputs($imageNode, $positiveNode, $negativeNode, $detectorNode, [int64]$seed, [double]$denoise, [double]$guideSize) {
    return @{
        image = $imageNode; model = @("12", 0); clip = @("3", 0); vae = @("1", 2)
        guide_size = $guideSize; guide_size_for = $true; max_size = 1100.0; seed = $seed
        steps = 18; cfg = 4.8; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = $positiveNode; negative = $negativeNode; denoise = $denoise
        feather = 5; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.45
        bbox_dilation = 8; bbox_crop_factor = 2.8; sam_detection_hint = "none"; sam_dilation = 0
        sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7
        sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = $detectorNode
        wildcard = ""; cycle = 1
    }
}

function New-Graph([int]$variant, $spec) {
    $positive = @($quality, $identity, $outfit, $pose, $expression, $spec.prompt, $layout) -join ", "
    $loraStrength = if ($null -ne $spec.loraStrength) { [double]$spec.loraStrength } else { 0.68 }
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $characterLora; strength_model = $loraStrength; strength_clip = $loraStrength } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV24/style/065_ReflectiveBarrier_反射屏障.png" } }
        "9" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV24/composition/058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png" } }
        "10" = @{ class_type = "IPAdapterStyleComposition"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image_style = @("8", 0); image_composition = @("9", 0)
            weight_style = [double]$spec.styleWeight; weight_composition = [double]$spec.compositionWeight
            expand_style = $false; combine_embeds = "average"; start_at = 0.0; end_at = 0.82
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "11" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV24/outfit/style_corrupt_succubus_subject_crop.png" } }
        "12" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image = @("11", 0); weight = [double]$spec.outfitWeight
            weight_type = "linear"; combine_embeds = "average"; start_at = 0.0; end_at = 0.72
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "13" = @{ class_type = "OpenposePreprocessor"; inputs = @{ image = @("9", 0); detect_hand = "enable"; detect_body = "enable"; detect_face = "enable"; resolution = 768; scale_stick_for_xinsr_cn = "disable" } }
        "14" = @{ class_type = "ControlNetLoader"; inputs = @{ control_net_name = $openPoseControlNet } }
        "15" = @{ class_type = "ControlNetApplyAdvanced"; inputs = @{
            positive = @("4", 0); negative = @("5", 0); control_net = @("14", 0); image = @("13", 0)
            strength = [double]$spec.poseWeight; start_percent = 0.0; end_percent = 0.92; vae = @("1", 2)
        } }
        "16" = @{ class_type = "LoadImageMask"; inputs = @{ image = "MaidenSuccubus/EroticLocalV25/mask/058_MasochisticTrance_躯干衣装区域_inpaint_mask.png"; channel = "red" } }
        "19" = @{ class_type = "InpaintModelConditioning"; inputs = @{ positive = @("15", 0); negative = @("15", 1); vae = @("1", 2); pixels = @("9", 0); mask = @("16", 0); noise_mask = $true } }
        "17" = @{ class_type = "KSampler"; inputs = @{
            model = @("12", 0); seed = [int64]$spec.seed; steps = 30; cfg = 4.6; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("19", 0); negative = @("19", 1); latent_image = @("19", 2); denoise = [double]$spec.denoise
        } }
        "18" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("17", 0); vae = @("1", 2) } }
        "21" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "adult Hibiki Amane, half-lidded unfocused cyan eyes, flushed cheeks, parted lips, faint involuntary pleased smile mixed with pain, one small tear, polished cel-shaded face" } }
        "22" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, young-looking, frightened, terrified, angry, cheerful, blank expression, eyes closed, deformed face, cross-eyed, duplicate face, blurry face" } }
        "23" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "24" = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("18", 0) @("21", 0) @("22", 0) @("23", 0) ([int64]($spec.seed + 193)) 0.22 560.0 }
        "25" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "two crossed restrained wrists, two natural adult female hands, exactly five fingers on each hand, clean joints, black-purple gloves with gold cuffs, chain separate from skin, crisp cel shading" } }
        "26" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "extra hand, missing hand, extra fingers, six fingers, four fingers, fused fingers, broken wrist, malformed hand, chain fused to hand" } }
        "27" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/hand_yolov8s.pt" } }
        "28" = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("24", 0) @("25", 0) @("26", 0) @("27", 0) ([int64]($spec.seed + 389)) 0.16 448.0 }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV25/058_MasochisticTrance_torso_v25_{0:D2}" -f $variant); images = @("18", 0) } }
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

function Copy-ComfyOutput($image, [string]$destination) {
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Clear-ComfyModels
Upload-ComfyImage $stylePath "MaidenSuccubus/EroticLocalV24/style"
Upload-ComfyImage $compositionPath "MaidenSuccubus/EroticLocalV24/composition"
Upload-ComfyImage $outfitPath "MaidenSuccubus/EroticLocalV24/outfit"
Upload-ComfyImage $maskPath "MaidenSuccubus/EroticLocalV25/mask"

$clientId = [guid]::NewGuid().ToString()
$jobs = @()
foreach ($variant in $Variants) {
    if (-not $variantSpecs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $variantSpecs[$variant]
    $targetName = "058_MasochisticTrance_被虐的恍惚_躯干衣装_LOCAL_v25_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $graph = New-Graph $variant $spec
    $graph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_torso_v25_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $jobs += [ordered]@{ variant = $variant; seed = $spec.seed; loraStrength = $spec.loraStrength; styleWeight = $spec.styleWeight; compositionWeight = $spec.compositionWeight; outfitWeight = $spec.outfitWeight; poseWeight = $spec.poseWeight; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [58:$variant] 被虐的恍惚"; continue }
    Clear-ComfyModels
    $image = Invoke-ComfyGraph $graph "[58:${variant}] full-redraw" $clientId
    Copy-ComfyOutput $image $target
    Write-Host "DONE [58:$variant] 被虐的恍惚"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    pipeline = "LOCAL_COMFYUI_V25_TORSO_ONLY_OUTFIT_INPAINT"
    outputSize = "1000x760"
    compositionReference = "EroticLocalV6_20260914/058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png"
    outfitReference = "变身形态/邪瘴天衣/魔装耐久1_严重破损.png"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    mask = "058_MasochisticTrance_躯干衣装区域_inpaint_mask.png"
    status = "awaiting-player-review"
    jobs = $jobs
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v25.json") -Encoding utf8

Clear-ComfyModels
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V25_TORSO_ONLY_OUTFIT_INPAINT selected=$($Variants.Count)"
