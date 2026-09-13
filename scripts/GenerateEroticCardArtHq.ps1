param(
    [int[]]$Indexes = @(50, 58, 73, 74, 75, 76, 78),
    [int]$Version = 1,
    [switch]$Force,
    [switch]$LowMemory,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$conceptDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticCardConcepts_20260913"
$roughDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticCardBatch_20260913"
$firstBatchDir = Join-Path $modRoot "图片素材\第一批卡图"
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticCardHQ_20260913"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$corruptPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久3_无损.png"
$fullDamagePath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久1_严重破损.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, absurdres, newest, polished premium anime game illustration, clean delicate colored lineart, refined smooth cel shading, subtle soft gradients, rich but controlled color depth, crisp focal details, restrained bloom, coherent adult anatomy, highly detailed face, highly detailed five-fingered hands"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, mature body, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, no hair rings"
$layout = "preserve the supplied composition and decisive action, subject and props remain in the central eighty percent card-art safe area, abstract gradient and soft bokeh only, no identifiable location, no scenery, no text, no border, no interface, no card or card-shaped object"
$negative = "worst quality, low quality, lowres, blurry, depth-of-field blur on subject, jpeg artifacts, rough sketch, thick black outline, flat unfinished coloring, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple heroines, duplicate person, extra arms, extra legs, extra fingers, six fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicated breasts, comic, manga, panel, speech bubble, caption, decorative frame, concrete room, dungeon, bedroom, street, building, literal garden, landscape, wall, floor, horizon, text, letters, logo, watermark, border, interface, playing card, tarot card, card-shaped panel"

$jobs = @(
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; base = (Join-Path $conceptDir "050_EcstasyDew_concept_v03.png"); baseName = "050_EcstasyDew_concept_v03.png"; outfit = "corrupt"; denoise = 0.50; outfitWeight = 0.46
        prompt = "black purple and gold corrupted magical girl outfit, one small round transparent glass vial filled with vivid luminous pink aphrodisiac held directly at her parted lips in one correct hand, a pink liquid droplet at the vial mouth, drinking the pink potion, flushed half-lidded sensual expression, no blue liquid"
    }
    [pscustomobject]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; base = (Join-Path $roughDir "058_MasochisticTrance_erotic_img2img_v01.png"); baseName = "058_MasochisticTrance_erotic_img2img_v01.png"; outfit = "fulldamage"; denoise = 0.54; outfitWeight = 0.62
        prompt = "explicit adult erotic battle aftermath, exact severely destroyed black purple and gold corrupted outfit, exposed mature breasts and vulva consistent with the outfit reference, wrists restrained overhead by loose black-purple ribbons, glowing curse marks across skin, every curse mark feeding a thin cyan protective aura, ecstatic trance, sweat, teary half-lidded eyes, no partner, no penetration"
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; base = (Join-Path $conceptDir "073_DesireWhip_concept_v03.png"); baseName = "073_DesireWhip_concept_v03.png"; outfit = "corrupt"; denoise = 0.52; outfitWeight = 0.55
        prompt = "rear three-quarter action view, exact black purple and gold corrupted magical outfit, one long glowing pink-black energy whip held in her right hand and fully visible in a clear S curve, whip lashes one abstract black captor silhouette at the right edge, restrained impact sparks, face hidden by rear view"
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; base = (Join-Path $conceptDir "074_PleasureGarden_concept_v03.png"); baseName = "074_PleasureGarden_concept_v03.png"; outfit = "fulldamage"; denoise = 0.48; outfitWeight = 0.52
        prompt = "explicit adult top-down erotic tableau, exact severely destroyed black purple and gold corrupted outfit, exposed mature breasts and vulva consistent with outfit reference, heroine lying at the center of one abstract pink-purple flower-shaped magic circle, arms open, three small faceless enemy silhouettes around the outer circle, blissful inviting expression, no penetration"
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; base = (Join-Path $conceptDir "075_SemenAppetite_concept_v03.png"); baseName = "075_SemenAppetite_concept_v03.png"; outfit = "corrupt"; denoise = 0.58; outfitWeight = 0.48
        prompt = "exact black purple and gold corrupted magical outfit, side-profile close-up, a broken black curse orb held in one correct five-fingered hand, thick luminous pearly-white essence visibly streams from the orb into her open mouth, white fluid on lips, the swallowed essence transforms into green-gold healing sparks, flushed eager expression, no blue orb"
    }
    [pscustomobject]@{
        index = 76; class = "BiteInvader"; title = "咬"; base = (Join-Path $conceptDir "076_BiteInvader_concept_v03.png"); baseName = "076_BiteInvader_concept_v03.png"; outfit = "fulldamage"; denoise = 0.44; outfitWeight = 0.48
        prompt = "explicit adult heroine in severely destroyed corrupted magical outfit, side-profile close-up, teeth visibly clamped around one thick black-purple restraining tentacle, biting completely through it, one correct hand gripping the tentacle, seven violet weakening seals along the recoiling tentacle, angry defiant eyes, no gore"
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; base = (Join-Path $firstBatchDir "078_TentacleArmor_淫触魔衣_v01.png"); baseName = "078_TentacleArmor_淫触魔衣_v01.png"; outfit = "corrupt"; denoise = 0.54; outfitWeight = 0.44
        prompt = "adult heroine wearing living black-purple tentacle armor, multiple elegant glossy black-purple tendrils tightly wrap and visibly weave into the bodice waist hip guards and thigh armor, the tendrils themselves form a revealing purple-black protective suit with gold trim rather than floating separately, a second armor layer forms behind her, sensual transformation, no penetration"
    }
)

$sharedRefs = @(
    @{ source = $stylePath; subfolder = "MaidenSuccubus/EroticHQ/style" }
    @{ source = $corruptPath; subfolder = "MaidenSuccubus/EroticHQ/corrupt" }
    @{ source = $fullDamagePath; subfolder = "MaidenSuccubus/EroticHQ/fulldamage" }
)
foreach ($ref in $sharedRefs) {
    if (-not (Test-Path -LiteralPath $ref.source)) { throw "Missing reference: $($ref.source)" }
    $form = @{ image = Get-Item -LiteralPath $ref.source; subfolder = $ref.subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}
foreach ($job in $jobs) {
    if (-not (Test-Path -LiteralPath $job.base)) { throw "Missing base: $($job.base)" }
    $form = @{ image = Get-Item -LiteralPath $job.base; subfolder = "MaidenSuccubus/EroticHQ/base"; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-PromptGraph($job) {
    $positive = @($quality, $identity, $job.prompt, $layout) -join ", "
    $outfitRemote = if ($job.outfit -eq "fulldamage") { "MaidenSuccubus/EroticHQ/fulldamage/魔装耐久1_严重破损.png" } else { "MaidenSuccubus/EroticHQ/corrupt/魔装耐久3_无损.png" }
    $prefix = if ($LowMemory) { "{0:D3}_{1}_hq_lite_v{2:D2}" -f $job.index, $job.class, $Version } else { "{0:D3}_{1}_hq_v{2:D2}" -f $job.index, $job.class, $Version }
    if ($LowMemory) {
        return [ordered]@{
            "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
            "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.86; strength_clip = 0.86 } }
            "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
            "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
            "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
            "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticHQ/base/$($job.baseName)" } }
            "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
            "8" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("7", 0); vae = @("1", 2) } }
            "17" = @{ class_type = "KSampler"; inputs = @{ model = @("2", 0); seed = [int64](710000000000 + $job.index * 1009 + $Version * 100003); steps = 30; cfg = 5.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = @("8", 0); denoise = [double]$job.denoise } }
            "18" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("17", 0); vae = @("1", 2) } }
            "23" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticHQ/$prefix"; images = @("18", 0) } }
        }
    }
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.88; strength_clip = 0.88 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticHQ/base/$($job.baseName)" } }
        "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "8" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("7", 0); vae = @("1", 2) } }
        "9" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "10" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "11" = @{ class_type = "LoadImage"; inputs = @{ image = $outfitRemote } }
        "12" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("11", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "13" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("2", 0); ipadapter = @("9", 0); image = @("12", 0); clip_vision = @("10", 0); weight = [double]$job.outfitWeight; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.86; embeds_scaling = "V only" } }
        "14" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticHQ/style/065_ReflectiveBarrier_反射屏障.png" } }
        "15" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("14", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "16" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("13", 0); ipadapter = @("9", 0); image = @("15", 0); clip_vision = @("10", 0); weight = 0.28; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.78; embeds_scaling = "V only" } }
        "17" = @{ class_type = "KSampler"; inputs = @{ model = @("16", 0); seed = [int64](700000000000 + $job.index * 1009 + $Version * 100003); steps = 36; cfg = 5.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = @("8", 0); denoise = [double]$job.denoise } }
        "18" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("17", 0); vae = @("1", 2) } }
        "19" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, adult woman, symmetrical aqua eyes, natural expressive face, blonde hair with pale pink gradient, crisp refined cel-shaded face" } }
        "20" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, young-looking, asymmetrical eyes, cross-eyed, deformed face, extra eyes, duplicate face, bad anatomy, blurred face" } }
        "21" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "22" = @{ class_type = "FaceDetailer"; inputs = @{ image = @("18", 0); model = @("16", 0); clip = @("3", 0); vae = @("1", 2); guide_size = 600.0; guide_size_for = $true; max_size = 1200.0; seed = [int64](700000000193 + $job.index * 1009 + $Version * 100003); steps = 20; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("19", 0); negative = @("20", 0); denoise = 0.24; feather = 6; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 8; bbox_crop_factor = 3.0; sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = @("21", 0); wildcard = ""; cycle = 1 } }
        "23" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticHQ/$prefix"; images = @("22", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    try {
        $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 30
    }
    catch {
        return $null
    }
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$selected = @($jobs | Where-Object { $_.index -in $Indexes })
$clientId = [guid]::NewGuid().ToString()
$generated = 0
foreach ($job in $selected) {
    $targetName = if ($LowMemory) { "{0:D3}_{1}_hq_lite_v{2:D2}.png" -f $job.index, $job.class, $Version } else { "{0:D3}_{1}_hq_v{2:D2}.png" -f $job.index, $job.class, $Version }
    $target = Join-Path $targetDir $targetName
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [$($job.index)] $($job.title)"; continue }
    $body = @{ prompt = (New-PromptGraph $job); client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected [$($job.index)] $($job.title): $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index)] $($job.title) -> $promptId"
    $history = $null
    while ($null -eq $history) { Start-Sleep -Seconds 2; $history = Get-CompletedHistory $promptId }
    $images = @($history.outputs."23".images)
    if ($images.Count -eq 0) { throw "ComfyUI completed without output for [$($job.index)] $($job.title)" }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    $generated++
    Write-Host "DONE [$($job.index)] $($job.title) ($generated/$($selected.Count))"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    checkpoint = "waiNSFWIllustrious_v140.safetensors"
    lora = "celesphonia-1.8.safetensors"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    lowMemory = [bool]$LowMemory
    version = $Version
    size = "1000x760"
    jobs = @($selected | Select-Object index, class, title, baseName, outfit, denoise, outfitWeight, prompt)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec.json") -Encoding utf8

Write-Host "COMPLETE generated_this_run=$generated selected=$($selected.Count)"
