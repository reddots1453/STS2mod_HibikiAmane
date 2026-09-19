param(
    [int[]]$Variants = @(1, 2, 3),
    [switch]$Force,
    [switch]$Upscale,
    [switch]$IgnoreMemoryGuard,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV6_20260914"
$workflowDir = Join-Path $targetDir "workflow_api"
$intermediateDir = Join-Path $targetDir "intermediate"
$basePath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV2_20260913\composition_bases\058_MasochisticTranceSolo_base.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$characterLora = "celesphonia-1.8.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"
$openPoseControlNet = "NoobAI\noobaiXLControlnet_openposeModel.safetensors"
$upscaleModel = "4x_foolhardy_Remacri.pth"

New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir, $intermediateDir | Out-Null

$quality = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "polished anime game illustration", "clean thin colored lineart", "controlled two-step cel shading",
    "soft restrained gradients", "crisp focal details", "coherent adult anatomy", "readable silhouette"
) -join ", "

$identity = @(
    "celesphonia", "hibiki amane", "one adult woman", "mature adult body", "aqua eyes",
    "long blonde hair fading to pale pink", "one side ponytail tied with a simple blue ribbon", "no hair rings"
) -join ", "

$subject = @(
    "solo", "full body", "front three-quarter view", "both arms raised together over her head",
    "wrists crossed above the crown", "two shoulders and exactly two arms", "legs crossed at the knees",
    "black high-cut damaged corrupted magical bodysuit with red trim",
    "loose iron chains arc around her crossed wrists and crossed knees without becoming limbs",
    "flushed ecstatic trance", "sweat", "teary half-lidded aqua eyes",
    "small violet curse wisps dissolve into a translucent cyan defensive aura around her silhouette",
    "no captor", "no second person", "no penetration"
) -join ", "

$layout = @(
    "1000 by 760 horizontal card illustration", "subject centered slightly above the middle",
    "all important anatomy inside the central eighty percent safe area",
    "simple abstract dark violet gradient and mist only", "no concrete background", "no architecture",
    "no floor", "no horizon", "no text", "no letters", "no logo", "no watermark",
    "no frame", "no border", "no interface", "no card-shaped object"
) -join ", "

$negative = @(
    "worst quality", "low quality", "lowres", "blurry focal subject", "out of focus body", "jpeg artifacts",
    "rough sketch", "thick black outlines", "painterly impasto", "photorealistic", "3d", "chibi",
    "child", "teenager", "young-looking", "loli", "multiple women", "second person", "captor",
    "duplicate body", "duplicate torso", "extra arms", "third arm", "missing arm", "floating limb",
    "twisted shoulder", "broken elbow", "broken wrist", "dislocated hip", "extra legs",
    "extra fingers", "six fingers", "four fingers", "missing fingers", "fused fingers", "malformed hands",
    "giant hands", "tiny hands", "duplicate breasts", "asymmetrical eyes", "cross-eyed", "concrete room",
    "bedroom", "dungeon", "street", "building", "wall", "floor", "horizon", "text", "letters",
    "logo", "watermark", "frame", "border", "UI", "playing card", "tarot card", "panel"
) -join ", "

$variantSpecs = @{
    1 = [ordered]@{ seedKey = "v6-a"; structureDenoise = 0.34; openPoseWeight = 0.90; polishDenoise = 0.30; styleWeight = 0.38; compositionWeight = 0.54; faceDenoise = 0.18; handDenoise = 0.12; prompt = "tight readable silhouette, restrained cyan aura, clean separated chain arcs" }
    2 = [ordered]@{ seedKey = "v6-b"; structureDenoise = 0.40; openPoseWeight = 0.96; polishDenoise = 0.34; styleWeight = 0.42; compositionWeight = 0.58; faceDenoise = 0.20; handDenoise = 0.14; prompt = "clear crossed-wrist pose, both arm contours separated, violet wisps absorbed at shoulders and waist" }
    3 = [ordered]@{ seedKey = "v6-c"; structureDenoise = 0.46; openPoseWeight = 0.86; polishDenoise = 0.38; styleWeight = 0.46; compositionWeight = 0.50; faceDenoise = 0.22; handDenoise = 0.15; prompt = "stronger ecstatic expression, compact cyan barrier flare, sparse highlights, anatomically clean limbs" }
    4 = [ordered]@{ seedKey = "v6-d-effect"; structureDenoise = 0.36; openPoseWeight = 0.92; polishDenoise = 0.42; styleWeight = 0.42; compositionWeight = 0.44; faceDenoise = 0.20; handDenoise = 0.13; prompt = "((several clearly visible violet curse wisps:1.45)), ((violet wisps flow inward and dissolve at her shoulders and waist:1.40)), ((one translucent cyan protective aura tightly outlines her body:1.35)), visible conversion from violet curses into cyan protection, sparse controlled magical effects, both arm contours separated" }
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [int64]([BitConverter]::ToUInt64($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0) -band 0x001FFFFFFFFFFFFF) }
    finally { $sha.Dispose() }
}

function Get-AvailableMemoryGb {
    Add-Type -AssemblyName Microsoft.VisualBasic
    $computerInfo = [Microsoft.VisualBasic.Devices.ComputerInfo]::new()
    return [math]::Round($computerInfo.AvailablePhysicalMemory / 1GB, 2)
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

function New-BaseNodes([string]$positiveText) {
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $characterLora; strength_model = 0.74; strength_clip = 0.74 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positiveText } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
    }
}

function New-DetailerInputs($imageNode, $positiveNode, $negativeNode, $detectorNode, [int64]$seed, [double]$denoise, [double]$guideSize) {
    return @{
        image = $imageNode; model = @("2", 0); clip = @("3", 0); vae = @("1", 2)
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

function New-StructureGraph([int]$variant, $spec, [int64]$seed) {
    $positive = @($quality, $identity, $subject, $spec.prompt, $layout) -join ", "
    $graph = New-BaseNodes $positive
    $graph["9"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV6/base/058_MasochisticTranceSolo_base.png" } }
    $graph["10"] = @{ class_type = "ImageScale"; inputs = @{ image = @("9", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    $graph["11"] = @{ class_type = "OpenposePreprocessor"; inputs = @{ image = @("10", 0); detect_hand = "enable"; detect_body = "enable"; detect_face = "enable"; resolution = 768; scale_stick_for_xinsr_cn = "disable" } }
    $graph["12"] = @{ class_type = "ControlNetLoader"; inputs = @{ control_net_name = $openPoseControlNet } }
    $graph["13"] = @{ class_type = "ControlNetApplyAdvanced"; inputs = @{
        positive = @("4", 0); negative = @("5", 0); control_net = @("12", 0); image = @("11", 0)
        strength = [double]$spec.openPoseWeight; start_percent = 0.0; end_percent = 0.82; vae = @("1", 2)
    } }
    $graph["14"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("10", 0); vae = @("1", 2) } }
    $graph["15"] = @{ class_type = "KSampler"; inputs = @{
        model = @("2", 0); seed = $seed; steps = 28; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = @("13", 0); negative = @("13", 1); latent_image = @("14", 0); denoise = [double]$spec.structureDenoise
    } }
    $graph["16"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("15", 0); vae = @("1", 2) } }
    $graph["40"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV6/intermediate/058_MasochisticTrance_v6_{0:D2}_stageA_structure" -f $variant); images = @("16", 0) } }
    return $graph
}

function New-PolishGraph([int]$variant, $spec, [int64]$seed) {
    $positive = @($quality, $identity, $subject, $spec.prompt, $layout) -join ", "
    $graph = New-BaseNodes $positive
    $graph["6"] = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
    $graph["7"] = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
    $graph["8"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV6/style/065_ReflectiveBarrier_反射屏障.png" } }
    $graph["9"] = @{ class_type = "LoadImage"; inputs = @{ image = ("MaidenSuccubus/EroticLocalV6/intermediate/058_MasochisticTrance_v6_{0:D2}_stageA_structure.png" -f $variant) } }
    $graph["10"] = @{ class_type = "IPAdapterStyleComposition"; inputs = @{
        model = @("2", 0); ipadapter = @("6", 0); image_style = @("8", 0); image_composition = @("9", 0)
        weight_style = [double]$spec.styleWeight; weight_composition = [double]$spec.compositionWeight
        expand_style = $false; combine_embeds = "average"; start_at = 0.0; end_at = 0.70
        embeds_scaling = "V only"; clip_vision = @("7", 0)
    } }
    $graph["11"] = @{ class_type = "ImageScale"; inputs = @{ image = @("9", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    $graph["12"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("11", 0); vae = @("1", 2) } }
    $graph["13"] = @{ class_type = "KSampler"; inputs = @{
        model = @("10", 0); seed = [int64]($seed + 97); steps = 30; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = @("4", 0); negative = @("5", 0); latent_image = @("12", 0); denoise = [double]$spec.polishDenoise
    } }
    $graph["14"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("13", 0); vae = @("1", 2) } }
    $graph["21"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, adult woman, symmetrical aqua eyes, natural ecstatic adult face, crisp clean cel-shaded facial details" } }
    $graph["22"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, teenager, young-looking, asymmetrical eyes, cross-eyed, deformed face, extra eyes, duplicate face, blurred face" } }
    $graph["23"] = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
    $graph["24"] = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("14", 0) @("21", 0) @("22", 0) @("23", 0) ([int64]($seed + 193)) ([double]$spec.faceDenoise) 560.0 }
    $graph["25"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, crossed wrists, natural adult female hands, anatomically correct fingers, five fingers on each hand, clean cel shading" } }
    $graph["26"] = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "extra hand, missing hand, extra fingers, six fingers, four fingers, fused fingers, broken wrist, malformed hand, twisted fingers" } }
    $graph["27"] = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/hand_yolov8s.pt" } }
    $graph["28"] = @{ class_type = "FaceDetailer"; inputs = New-DetailerInputs @("24", 0) @("25", 0) @("26", 0) @("27", 0) ([int64]($seed + 389)) ([double]$spec.handDenoise) 448.0 }
    $graph["29"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV6/058_MasochisticTrance_LOCAL_COMFYUI_v6_{0:D2}_stageB_raw" -f $variant); images = @("14", 0) } }
    $graph["40"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV6/058_MasochisticTrance_LOCAL_COMFYUI_v6_{0:D2}" -f $variant); images = @("28", 0) } }
    return $graph
}

function New-UpscaleGraph([int]$variant, [int64]$seed) {
    $positive = @($quality, $identity, $subject, $layout) -join ", "
    $graph = New-BaseNodes $positive
    $graph["8"] = @{ class_type = "LoadImage"; inputs = @{ image = ("MaidenSuccubus/EroticLocalV6/intermediate/058_MasochisticTrance_v6_{0:D2}_stageB_detail.png" -f $variant) } }
    $graph["9"] = @{ class_type = "UpscaleModelLoader"; inputs = @{ model_name = $upscaleModel } }
    $graph["10"] = @{ class_type = "UltimateSDUpscale"; inputs = @{
        image = @("8", 0); model = @("2", 0); positive = @("4", 0); negative = @("5", 0); vae = @("1", 2)
        upscale_by = 1.5; seed = [int64]($seed + 587); steps = 12; cfg = 4.2
        sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; denoise = 0.10; upscale_model = @("9", 0)
        mode_type = "Linear"; tile_width = 512; tile_height = 512; mask_blur = 8; tile_padding = 32
        seam_fix_mode = "None"; seam_fix_denoise = 0.10; seam_fix_width = 64; seam_fix_mask_blur = 8
        seam_fix_padding = 16; force_uniform_tiles = $true; tiled_decode = $false; batch_size = 1
    } }
    $graph["11"] = @{ class_type = "ImageScale"; inputs = @{ image = @("10", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    $graph["40"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV6/058_MasochisticTrance_LOCAL_COMFYUI_v6_{0:D2}_upscaled" -f $variant); images = @("11", 0) } }
    return $graph
}

function Invoke-ComfyGraph($graph, [string]$label, [string]$clientId) {
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 40 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected $label`: $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  $label -> $promptId"
    $deadline = (Get-Date).AddMinutes(20)
    $connectionFailures = 0
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        try {
            $historyRoot = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 20
            $connectionFailures = 0
        }
        catch {
            $connectionFailures++
            if ($connectionFailures -ge 3) { throw "ComfyUI stopped responding while running $label. The process probably exhausted RAM/VRAM." }
            continue
        }
        $property = $historyRoot.PSObject.Properties[$promptId]
        if ($null -eq $property) { continue }
        $history = $property.Value
        $images = @($history.outputs."40".images)
        if ($images.Count -eq 0) { throw "ComfyUI completed without output for $label`: $($history.status | ConvertTo-Json -Depth 8 -Compress)" }
        return $images[0]
    }
    throw "Timed out after 20 minutes while running $label"
}

function Copy-ComfyOutput($image, [string]$destination) {
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

$comfyAlreadyRunning = $false
try {
    [void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
    $comfyAlreadyRunning = $true
}
catch {
    $comfyAlreadyRunning = $false
}

if ($comfyAlreadyRunning) {
    Clear-ComfyModels
}

$freeGb = Get-AvailableMemoryGb
$requiredFreeGb = if ($comfyAlreadyRunning) { 5.0 } else { 7.0 }
if ($freeGb -lt $requiredFreeGb -and -not $IgnoreMemoryGuard) {
    $heavy = Get-Process -Name SlayTheSpire2, eu4 -ErrorAction SilentlyContinue | Select-Object ProcessName, Id, @{Name="WorkingSetGB";Expression={[math]::Round($_.WorkingSet64 / 1GB, 2)}}
    $heavyText = if ($heavy) { $heavy | Format-Table -AutoSize | Out-String } else { "No known game process found." }
    throw "Only $freeGb GB physical RAM is available; controlled local generation needs at least $requiredFreeGb GB free in the current ComfyUI state. Close memory-heavy games/apps and retry. Detected:`n$heavyText"
}

if (-not $comfyAlreadyRunning) { throw "ComfyUI is not running at $ComfyUrl" }
Upload-ComfyImage $stylePath "MaidenSuccubus/EroticLocalV6/style" "065_ReflectiveBarrier_反射屏障.png"
Upload-ComfyImage $basePath "MaidenSuccubus/EroticLocalV6/base" "058_MasochisticTranceSolo_base.png"

$clientId = [guid]::NewGuid().ToString()
$jobs = @()
$generated = 0
foreach ($variant in $Variants) {
    if (-not $variantSpecs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $variantSpecs[$variant]
    $seed = Get-StableSeed "MasochisticTrance:$($spec.seedKey):20260914"
    $targetName = "058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $stageAPath = Join-Path $intermediateDir ("058_MasochisticTrance_v6_{0:D2}_stageA_structure.png" -f $variant)
    $stageBPath = Join-Path $intermediateDir ("058_MasochisticTrance_v6_{0:D2}_stageB_detail.png" -f $variant)

    $structureGraph = New-StructureGraph $variant $spec $seed
    $polishGraph = New-PolishGraph $variant $spec $seed
    $structureGraph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_v6_{0:D2}_stageA_api.json" -f $variant)) -Encoding utf8
    $polishGraph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_v6_{0:D2}_stageB_api.json" -f $variant)) -Encoding utf8

    $jobs += [ordered]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; variant = $variant; seed = $seed
        engine = "LOCAL_COMFYUI"; checkpoint = $checkpoint; lora = $characterLora
        pipeline = "two-stage: OpenPose structure -> IP-Adapter style/composition + face/hand detail"
        structureDenoise = $spec.structureDenoise; openPoseWeight = $spec.openPoseWeight
        polishDenoise = $spec.polishDenoise; styleWeight = $spec.styleWeight; compositionWeight = $spec.compositionWeight
        faceDenoise = $spec.faceDenoise; handDenoise = $spec.handDenoise; upscale = [bool]$Upscale; output = $targetName
    }

    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [58:$variant] 被虐的恍惚"; continue }

    Clear-ComfyModels
    $stageAImage = Invoke-ComfyGraph $structureGraph "[58:${variant}:A] structure" $clientId
    Copy-ComfyOutput $stageAImage $stageAPath
    Upload-ComfyImage $stageAPath "MaidenSuccubus/EroticLocalV6/intermediate" (Split-Path -Leaf $stageAPath)

    Clear-ComfyModels
    $stageBImage = Invoke-ComfyGraph $polishGraph "[58:${variant}:B] style-and-detail" $clientId
    Copy-ComfyOutput $stageBImage $stageBPath
    $finalImage = $stageBImage

    if ($Upscale) {
        Upload-ComfyImage $stageBPath "MaidenSuccubus/EroticLocalV6/intermediate" (Split-Path -Leaf $stageBPath)
        Clear-ComfyModels
        $upscaleGraph = New-UpscaleGraph $variant $seed
        $upscaleGraph | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $workflowDir ("058_MasochisticTrance_v6_{0:D2}_stageC_upscale_api.json" -f $variant)) -Encoding utf8
        $finalImage = Invoke-ComfyGraph $upscaleGraph "[58:${variant}:C] local-upscale" $clientId
    }

    Copy-ComfyOutput $finalImage $target
    $generated++
    Write-Host "DONE [58:$variant] 被虐的恍惚 ($generated/$($Variants.Count))"
}

$manifestJobs = @()
foreach ($variantKey in ($variantSpecs.Keys | Sort-Object)) {
    $manifestSpec = $variantSpecs[$variantKey]
    $manifestTargetName = "058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_{0:D2}.png" -f $variantKey
    if (-not (Test-Path -LiteralPath (Join-Path $targetDir $manifestTargetName))) { continue }
    $manifestSeed = Get-StableSeed "MasochisticTrance:$($manifestSpec.seedKey):20260914"
    $manifestJobs += [ordered]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; variant = $variantKey; seed = $manifestSeed
        engine = "LOCAL_COMFYUI"; checkpoint = $checkpoint; lora = $characterLora
        pipeline = "two-stage: OpenPose structure -> IP-Adapter style/composition + face/hand detail"
        structureDenoise = $manifestSpec.structureDenoise; openPoseWeight = $manifestSpec.openPoseWeight
        polishDenoise = $manifestSpec.polishDenoise; styleWeight = $manifestSpec.styleWeight; compositionWeight = $manifestSpec.compositionWeight
        faceDenoise = $manifestSpec.faceDenoise; handDenoise = $manifestSpec.handDenoise; output = $manifestTargetName
    }
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o"); pipeline = "LOCAL_COMFYUI_CONTROLLED_V6_TWO_STAGE"; outputSize = "1000x760"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    compositionReference = "EroticRedrawV2_20260913/composition_bases/058_MasochisticTranceSolo_base.png"
    controls = @("stage A: Celesphonia LoRA + NoobAI OpenPose", "stage B: Celesphonia LoRA + IP-Adapter style/composition + face detailer + hand detailer", "optional stage C: Remacri + Ultimate SD Upscale")
    jobs = $manifestJobs
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v6.json") -Encoding utf8

Clear-ComfyModels
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_CONTROLLED_V6_TWO_STAGE generated_this_run=$generated selected=$($Variants.Count) upscale=$([bool]$Upscale)"
