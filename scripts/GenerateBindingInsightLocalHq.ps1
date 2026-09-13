param(
    [int[]]$Variants = @(5, 6, 7),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$candidateDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\BindingInsight_LocalNSFW_20260913"
$basePath = Join-Path $candidateDir "019_BindingInsight_local_nsfw_v02.png"
$focusBasePath = Join-Path $candidateDir "019_BindingInsight_local_hq_fulldamage_v20.png"
$armorPath = Join-Path $modRoot "图片素材\变身形态\无垢天衣\魔装耐久1_严重破损.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"

$references = [ordered]@{
    "019_BindingInsight_local_nsfw_v02.png" = $basePath
    "019_BindingInsight_local_hq_fulldamage_v20.png" = $focusBasePath
    "魔装耐久1_严重破损.png" = $armorPath
    "065_ReflectiveBarrier_反射屏障.png" = $stylePath
}

foreach ($entry in $references.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $entry.Value)) { throw "Missing reference: $($entry.Value)" }
    $form = @{
        image = Get-Item -LiteralPath $entry.Value
        subfolder = "MaidenSuccubus/BindingInsightHQ"
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

$quality = "masterpiece, best quality, amazing quality, very aesthetic, absurdres, newest, polished premium anime game illustration, clean delicate colored lineart, refined smooth cel shading, subtle soft gradients, rich but controlled color depth, crisp focal details, restrained bloom, coherent adult anatomy, highly detailed face, highly detailed hands, detailed natural hemp rope fibers"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, mature body, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, no hair rings"
$composition = "preserve the supplied v02 composition: high-angle close three-quarter view, heroine leaning forward, hips back, ((both elbows bent sharply backward:1.35)), ((both forearms and both tied hands hidden behind her torso:1.40)), ((wrists bound together behind her back:1.35)), one vertical rope rising behind her, face and upper body dominant in the upper-middle, thighs visible at the lower edge"
$outfit = "((exact fully damaged transformation outfit from the supplied reference:1.45)), ((severely shredded white blue and gold magical-girl costume:1.35)), ((the chest cloth is ripped completely open:1.35)), ((bare breasts with clearly visible nipples:1.30)), torn abdomen panel exposing bare skin, ((the black crotch panel and panties are torn away:1.30)), ((bare vulva visible between the thighs:1.25)), retain the blue bow and green brooch, blue-gold waist armor remnants, detached white-blue sleeves, white-gold thigh armor and boots"
$erotic = "((explicit adult erotic shibari:1.35)), ((tight tan hemp rope harness around upper arms, under and around breasts, waist and thighs:1.35)), breast bondage, crotch rope, several readable functional knots, wrists securely tied together behind her back, rope indentation on skin, body straining helplessly against the bindings, deeply flushed face, sweat, teary half-lidded eyes, drool at parted lips, panting orgasmic expression, embarrassed arousal"
$scene = "pure abstract dark indigo to muted magenta gradient, soft bokeh and a few restrained cyan-gold energy sparks around one loosening wrist knot, no concrete location, no wall, no floor, no vertical frame, all essential anatomy inside the central eighty percent card-art safe area, no text, no border, no interface, no card or card-shaped object"
$positive = @($quality, $identity, $composition, $outfit, $erotic, $scene) -join ", "
$negative = "worst quality, low quality, lowres, blurry, jpeg artifacts, rough sketch, thick black outline, flat unfinished coloring, overexposed bloom, noisy particles, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple people, duplicate person, free hands, arms in front, arm reaching sideways, outstretched arm, hand beside body, visible free hand, unbound wrists, beads, pearls, bead chain, metal chain, handcuffs, bracelets, intact costume, intact bodice, white breast covering, bra, covered breasts, covered nipples, black panties, intact black crotch panel, covered crotch, censored, mosaic, black bar, extra arms, extra legs, extra fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicated breasts, rope replacing limbs, rope through body, concrete room, dungeon cell, furniture, architecture, landscape, wall edge, floor line, vertical frame, text, logo, watermark, border, interface, playing card, tarot card"

$variantSpecs = @{
    5 = @{ seed = 619284750331; denoise = 0.58; armorWeight = 0.68; styleWeight = 0.34 }
    6 = @{ seed = 864092317455; denoise = 0.66; armorWeight = 0.76; styleWeight = 0.38 }
    7 = @{ seed = 305771948226; denoise = 0.62; armorWeight = 0.82; styleWeight = 0.30 }
    8 = @{ seed = 701992486133; denoise = 0.78; armorWeight = 1.05; styleWeight = 0.34 }
    9 = @{ seed = 519846002771; denoise = 0.84; armorWeight = 1.18; styleWeight = 0.38 }
    10 = @{ seed = 947150283611; denoise = 0.81; armorWeight = 1.30; styleWeight = 0.30 }
    11 = @{ seed = 302975186441; denoise = 0.84; armorWeight = 0.42; styleWeight = 0.26 }
    12 = @{ seed = 875314902266; denoise = 0.90; armorWeight = 0.50; styleWeight = 0.30 }
    13 = @{ seed = 164220793508; denoise = 0.87; armorWeight = 0.58; styleWeight = 0.34 }
    14 = @{ seed = 302975186441; denoise = 0.84; armorWeight = 0.42; styleWeight = 0.26 }
    15 = @{ seed = 631087425944; denoise = 0.86; armorWeight = 0.46; styleWeight = 0.29 }
    16 = @{ seed = 426718935012; denoise = 0.88; armorWeight = 0.50; styleWeight = 0.31 }
    17 = @{ seed = 302975186441; denoise = 0.84; armorWeight = 0.42; styleWeight = 0.26 }
    18 = @{ seed = 875314902266; denoise = 0.88; armorWeight = 0.48; styleWeight = 0.29 }
    19 = @{ seed = 556920874113; denoise = 0.86; armorWeight = 0.45; styleWeight = 0.31 }
    20 = @{ seed = 302975186441; denoise = 0.84; armorWeight = 0.42; styleWeight = 0.28 }
    21 = @{ seed = 723156098244; denoise = 0.86; armorWeight = 0.45; styleWeight = 0.30 }
    22 = @{ seed = 280614935770; denoise = 0.88; armorWeight = 0.48; styleWeight = 0.32 }
    23 = @{ seed = 302975186441; denoise = 0.84; armorWeight = 0.42; styleWeight = 0.28; maskExpand = 4; maskBlur = 3.0 }
    24 = @{ seed = 419806237155; denoise = 0.46; armorWeight = 0.34; styleWeight = 0.34; maskExpand = 5; maskBlur = 3.0 }
    25 = @{ seed = 302975186441; denoise = 0.00; armorWeight = 0.00; styleWeight = 0.00; maskExpand = 5; maskBlur = 3.0; postSharpenOnly = $true }
    26 = @{ seed = 419806237155; denoise = 0.34; armorWeight = 0.30; styleWeight = 0.30; maskExpand = 4; maskBlur = 2.0 }
}

function New-PromptGraph([int]$variant, [hashtable]$spec) {
    $prefix = "019_BindingInsight_local_hq_fulldamage_v{0:D2}" -f $variant
    $useLocalInpaint = $variant -ge 11
    $useLatentNoiseMask = $variant -ge 17
    $useFocusRepair = $variant -ge 24
    $postSharpenOnly = $spec.ContainsKey("postSharpenOnly") -and [bool]$spec.postSharpenOnly
    $latentNode = if ($useLocalInpaint -and -not $useLatentNoiseMask) {
        @{ class_type = "VAEEncodeForInpaint"; inputs = @{ pixels = @("7", 0); vae = @("1", 2); mask = @("29", 0); grow_mask_by = 8 } }
    } else {
        @{ class_type = "VAEEncode"; inputs = @{ pixels = @("7", 0); vae = @("1", 2) } }
    }
    $armorWeightType = if ($useLocalInpaint) { "style transfer precise" } else { "strong style transfer" }
    $latentInput = if ($useLatentNoiseMask) { @("31", 0) } else { @("8", 0) }
    $faceInput = if ($variant -ge 14 -and -not $useLatentNoiseMask) { @("30", 0) } else { @("18", 0) }
    $faceSteps = if ($variant -ge 20) { 24 } else { 18 }
    $faceDenoise = if ($variant -ge 20) { 0.32 } else { 0.24 }
    if ($useFocusRepair) {
        $faceSteps = 18
        $faceDenoise = 0.18
    }
    $maskExpand = if ($spec.ContainsKey("maskExpand")) { [int]$spec.maskExpand } else { 12 }
    $maskBlur = if ($spec.ContainsKey("maskBlur")) { [double]$spec.maskBlur } else { 22.0 }
    $baseImageName = if ($useFocusRepair) { "MaidenSuccubus/BindingInsightHQ/019_BindingInsight_local_hq_fulldamage_v20.png" } else { "MaidenSuccubus/BindingInsightHQ/019_BindingInsight_local_nsfw_v02.png" }
    $positiveText = if ($useFocusRepair) {
        "$positive, localized focus repair, razor-sharp continuous character silhouette, crisp uninterrupted cel-shaded outlines around the left breast and right shoulder, clearly separated shoulder skin, sleeve, hair tip and background, every foreground body contour in perfect focus"
    } else {
        $positive
    }
    $maskOne = if ($useFocusRepair) { @{ width = 220; height = 300; x = 250; y = 440 } } else { @{ width = 690; height = 310; x = 310; y = 450 } }
    $maskTwo = if ($useFocusRepair) { @{ width = 300; height = 250; x = 440; y = 230 } } else { @{ width = 510; height = 245; x = 490; y = 265 } }
    $saveInput = if ($postSharpenOnly) { @("33", 0) } else { @("22", 0) }
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.88; strength_clip = 0.88 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positiveText } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = $baseImageName } }
        "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "8" = $latentNode
        "9" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "10" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "11" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/BindingInsightHQ/魔装耐久1_严重破损.png" } }
        "12" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("11", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "13" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("2", 0); ipadapter = @("9", 0); image = @("12", 0); clip_vision = @("10", 0); weight = [double]$spec.armorWeight; weight_type = $armorWeightType; combine_embeds = "average"; start_at = 0.0; end_at = 0.90; embeds_scaling = "V only" } }
        "14" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/BindingInsightHQ/065_ReflectiveBarrier_反射屏障.png" } }
        "15" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("14", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "16" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("13", 0); ipadapter = @("9", 0); image = @("15", 0); clip_vision = @("10", 0); weight = [double]$spec.styleWeight; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.80; embeds_scaling = "V only" } }
        "17" = @{ class_type = "KSampler"; inputs = @{ model = @("16", 0); seed = [int64]$spec.seed; steps = 36; cfg = 5.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = $latentInput; denoise = [double]$spec.denoise } }
        "18" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("17", 0); vae = @("1", 2) } }
        "19" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, adult woman, symmetrical aqua eyes, deeply flushed cheeks, teary half-lidded eyes, parted wet lips, refined expressive anime face, blonde hair with pale pink gradient" } }
        "20" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, young-looking, asymmetrical eyes, cross-eyed, deformed face, extra eyes, duplicate face, bad anatomy" } }
        "21" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "22" = @{ class_type = "FaceDetailer"; inputs = @{ image = $faceInput; model = @("16", 0); clip = @("3", 0); vae = @("1", 2); guide_size = 640.0; guide_size_for = $true; max_size = 1200.0; seed = [int64]($spec.seed + 193); steps = $faceSteps; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("19", 0); negative = @("20", 0); denoise = $faceDenoise; feather = 6; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 8; bbox_crop_factor = 3.0; sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = @("21", 0); wildcard = ""; cycle = 1 } }
        "23" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/BindingInsightLocalHQ/$prefix"; images = $saveInput } }
        "24" = @{ class_type = "SolidMask"; inputs = @{ value = 0.0; width = 1000; height = 760 } }
        "25" = @{ class_type = "SolidMask"; inputs = @{ value = 1.0; width = [int]$maskOne.width; height = [int]$maskOne.height } }
        "26" = @{ class_type = "MaskComposite"; inputs = @{ destination = @("24", 0); source = @("25", 0); x = [int]$maskOne.x; y = [int]$maskOne.y; operation = "add" } }
        "27" = @{ class_type = "SolidMask"; inputs = @{ value = 1.0; width = [int]$maskTwo.width; height = [int]$maskTwo.height } }
        "28" = @{ class_type = "MaskComposite"; inputs = @{ destination = @("26", 0); source = @("27", 0); x = [int]$maskTwo.x; y = [int]$maskTwo.y; operation = "add" } }
        "29" = @{ class_type = "GrowMaskWithBlur"; inputs = @{ mask = @("28", 0); expand = $maskExpand; incremental_expandrate = 0.0; tapered_corners = $true; flip_input = $false; blur_radius = $maskBlur; lerp_alpha = 1.0; decay_factor = 1.0; fill_holes = $true } }
        "30" = @{ class_type = "ImageCompositeMasked"; inputs = @{ destination = @("7", 0); source = @("18", 0); x = 0; y = 0; resize_source = $false; mask = @("29", 0) } }
        "31" = @{ class_type = "SetLatentNoiseMask"; inputs = @{ samples = @("8", 0); mask = @("29", 0) } }
        "32" = @{ class_type = "ImageSharpen"; inputs = @{ image = @("7", 0); sharpen_radius = 3; sigma = 1.10; alpha = 1.80 } }
        "33" = @{ class_type = "ImageCompositeMasked"; inputs = @{ destination = @("7", 0); source = @("32", 0); x = 0; y = 0; resize_source = $false; mask = @("29", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$clientId = [guid]::NewGuid().ToString()
foreach ($variant in $Variants) {
    if (-not $variantSpecs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $variantSpecs[$variant]
    $target = Join-Path $candidateDir ("019_BindingInsight_local_hq_fulldamage_v{0:D2}.png" -f $variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) {
        Write-Host "SKIP v$variant"
        continue
    }
    $graph = New-PromptGraph $variant $spec
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) {
        throw "ComfyUI rejected v$variant`: $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)"
    }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN v$variant -> $promptId"
    $history = $null
    while ($null -eq $history) {
        Start-Sleep -Seconds 2
        $history = Get-CompletedHistory $promptId
    }
    $images = @($history.outputs."23".images)
    if ($images.Count -eq 0 -or $null -eq $images[0] -or [string]::IsNullOrWhiteSpace([string]$images[0].filename)) {
        throw "ComfyUI completed without output for v$variant"
    }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE v$variant -> $target"
}

Write-Host "COMPLETE"
