param(
    [int[]]$Indexes = @(41, 42, 50, 58, 73, 74, 75, 76, 78),
    [int[]]$Variants = @(1, 2),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$candidateDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticCardBatch_20260913"
$firstBatchDir = Join-Path $modRoot "图片素材\第一批卡图"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$holyPath = Join-Path $modRoot "图片素材\变身形态\无垢天衣\魔装耐久3_无损.png"
$corruptPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久3_无损.png"
$corruptDamagedPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久1_严重破损.png"
$outputRoot = Join-Path $ComfyRoot "output"

New-Item -ItemType Directory -Force -Path $candidateDir | Out-Null

$references = @(
    @{ source = $stylePath; subfolder = "MaidenSuccubus/EroticCardBatch/style" }
    @{ source = $holyPath; subfolder = "MaidenSuccubus/EroticCardBatch/holy" }
    @{ source = $corruptPath; subfolder = "MaidenSuccubus/EroticCardBatch/corrupt" }
    @{ source = $corruptDamagedPath; subfolder = "MaidenSuccubus/EroticCardBatch/corrupt_fulldamage" }
)

foreach ($entry in $references) {
    if (-not (Test-Path -LiteralPath $entry.source)) { throw "Missing reference: $($entry.source)" }
    $form = @{
        image = Get-Item -LiteralPath $entry.source
        subfolder = $entry.subfolder
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

$quality = "masterpiece, best quality, amazing quality, very aesthetic, absurdres, newest, polished premium anime game illustration, clean delicate colored lineart, smooth two-step cel shading, subtle gradients, rich but controlled color, restrained highlights, crisp central subject, coherent adult anatomy, detailed natural hands, visually readable card illustration"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, mature body, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, no hair rings"
$layout = "1000 by 760 horizontal card art, focal subject in the upper-middle central safe area, important anatomy and props inside the central eighty percent, uncluttered silhouette, no text, no border, no interface, no card, no card-shaped object"
$abstractScene = "only a plain or softly blurred abstract gradient background with restrained bokeh and magical particles, no identifiable location, no room, no architecture, no landscape, no floor, no horizon"
$negative = "worst quality, low quality, lowres, blurry, depth-of-field blur on the subject, jpeg artifacts, rough sketch, thick black outline, flat unfinished coloring, over-rendered glossy fantasy painting, overexposed bloom, excessive particles, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple heroines, duplicate person, extra arms, extra legs, extra fingers, six fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicate breasts, asymmetrical eyes, cross-eyed, concrete room, dungeon, bedroom, street, building, forest, garden scenery, landscape, wall, floor, horizon, text, letters, logo, watermark, border, user interface, playing card, tarot card, card-shaped panel"

$cards = @(
    [pscustomobject]@{
        index = 41; class = "RestraintEvasion"; title = "拘束回避"; base = "041_RestraintEvasion_拘束回避_v01.png"; denoise = 0.82; outfit = "holy/魔装耐久3_无损.png"; outfitWeight = 0.62
        common = "adult heroine in the exact intact white blue and gold magical-girl outfit, luminous ropes and dark tendrils narrowly missing her body, a thin curved cyan guard streak tracing along one forearm without covering any part of her body, dignified non-nude defensive action, heroine occupies most of the frame"
        v1 = "dynamic rear three-quarter view, twisting aerial dodge, face only in partial profile, one arm sweeping a defensive arc, determined expression"
        v2 = "dramatic side view, low evasive slide with both hands clearly visible, ropes snapping past above her, focused alert expression"
    }
    [pscustomobject]@{
        index = 42; class = "ChastityDefense"; title = "贞操防御"; base = "042_ChastityDefense_贞操防御_v01.png"; denoise = 0.78; outfit = "holy/魔装耐久3_无损.png"; outfitWeight = 0.66
        common = "adult heroine in the exact intact white blue and gold magical-girl outfit, a transparent white and cyan oval barrier outlined by thin light surrounding her without hiding her body, black-purple invasive tendrils visibly stopped outside the barrier, modest covered costume, resolute magical defense, heroine fully unobstructed"
        v1 = "front three-quarter medium shot, arms crossed before her chest to sustain the shield, stern determined eyes"
        v2 = "high-angle three-quarter view, heroine kneeling on one knee inside a luminous protective seal, one palm raised, calm defiant expression"
    }
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; base = "050_EcstasyDew_销魂露_v01.png"; denoise = 0.78; outfit = "corrupt/魔装耐久3_无损.png"; outfitWeight = 0.60
        common = "adult heroine in the exact black purple and gold corrupted magical outfit, holding one tiny glass vial of glowing pink aphrodisiac dew near her parted lips, two luminous pink desire droplets spiraling from it, flushed sensual but non-explicit presentation"
        v1 = "intimate three-quarter bust portrait, vial held delicately between five-fingered gloved hand and lips, half-lidded curious eyes, deep blush"
        v2 = "high-angle close view, heroine looking upward while pink mist curls around her throat and collarbone, surprised aroused expression, vial centered and readable"
    }
    [pscustomobject]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; base = "058_MasochisticTrance_被虐的恍惚_v01.png"; denoise = 0.72; outfit = "corrupt_fulldamage/魔装耐久1_严重破损.png"; outfitWeight = 0.70
        common = "explicit adult erotic battle aftermath, adult heroine in the exact severely destroyed black purple and gold corrupted transformation outfit, exposed mature breasts and vulva consistent with the supplied full-damage reference, loose glowing restraints around wrists and thighs, several red-purple curse marks on skin, a translucent protective aura forming from every debuff, no sexual partner"
        v1 = "high-angle close three-quarter view, heroine kneeling with shoulders pulled back by loose restraints, both hands anatomically visible, flushed ecstatic trance, sweat, teary half-lidded eyes, parted lips"
        v2 = "side three-quarter medium shot, adult heroine arching backward against floating restraint ribbons while a cyan shield blooms around her, overwhelmed blissful expression, readable full body silhouette"
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; base = "073_DesireWhip_欲望鞭挞_v01.png"; denoise = 0.72; outfit = "corrupt/魔装耐久3_无损.png"; outfitWeight = 0.64
        common = "adult heroine in the exact black purple and gold corrupted magical outfit wielding one glowing pink-black energy whip, the whip striking and stunning one abstract dark captor silhouette marked with a restraint symbol, sensual confident combat action, no concrete enemy anatomy"
        v1 = "over-the-shoulder rear three-quarter action view, heroine cracking the whip forward in a clear S-curve, face not visible, dynamic torso twist"
        v2 = "low side angle, heroine lunging with one hand controlling the whip and the other hand open for balance, fierce teasing smile, all five fingers correct"
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; base = "074_PleasureGarden_淫乐园_v01.png"; denoise = 0.82; outfit = "corrupt_fulldamage/魔装耐久1_严重破损.png"; outfitWeight = 0.58
        common = "explicit adult erotic magical tableau, adult heroine in the exact severely destroyed black purple and gold corrupted transformation outfit, exposed mature breasts and vulva consistent with the supplied full-damage reference, abstract pink-purple floral magic and luminous vine motifs radiating from her, three distant faceless enemy silhouettes entranced by desire, symbolic forbidden paradise rather than a literal garden"
        v1 = "high top-down view, heroine floating on her back at the center of a circular flower-like magic sigil, arms open, blissful inviting expression, elegant radial composition"
        v2 = "centered frontal full-body pose, one leg slightly bent and both hands lifting streams of floral desire magic, confident seductive smile, symmetrical icon-like composition"
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; base = "075_SemenAppetite_精液食粮_v01.png"; denoise = 0.84; outfit = "corrupt/魔装耐久3_无损.png"; outfitWeight = 0.56
        common = "adult erotic forbidden alchemy, adult heroine in the exact black purple and gold corrupted magical outfit absorbing luminous pearly-white essence from a dissolving black curse orb, the essence flowing into a bright green-gold life aura around her torso, visual metaphor for converting a semen curse into maximum vitality, no card or card-shaped object"
        v1 = "intimate side-profile close-up, pearly essence touching her tongue and lips before turning into green life sparks, flushed eager expression, one anatomically correct hand holding the dissolving orb"
        v2 = "high-angle three-quarter medium shot, pearly ribbon of essence entering a glowing sigil at her abdomen while the dark curse shell breaks apart, satisfied relieved expression"
    }
    [pscustomobject]@{
        index = 76; class = "BiteInvader"; title = "咬"; base = "076_BiteInvader_咬_v01.png"; denoise = 0.74; outfit = "corrupt_fulldamage/魔装耐久1_严重破损.png"; outfitWeight = 0.62
        common = "adult heroine in the exact severely destroyed black purple and gold corrupted transformation outfit, fiercely biting through one thick black-purple restraining tendril near her shoulder, seven small violet weakening seals appearing along the recoiling tendril, erotic peril transformed into aggressive escape, no gore"
        v1 = "tight three-quarter portrait, mouth and bite clearly readable, both shoulders anatomically connected, one hand gripping the tendril, angry defiant eyes"
        v2 = "rear three-quarter medium view, heroine turning her head over her shoulder to bite the tendril pulling across her upper arm, face in clear profile, dynamic struggle"
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; base = "078_TentacleArmor_淫触魔衣_v01.png"; denoise = 0.68; outfit = "corrupt/魔装耐久3_无损.png"; outfitWeight = 0.54
        common = "adult heroine wearing living black-purple tentacle armor, elegant glossy tendrils weaving themselves into a revealing but strategically covered magical bodysuit with gold trim, a second layer of tentacles forming behind her to symbolize the copied armor, sensual transformation with no penetration and no sexual partner"
        v1 = "front three-quarter full-body view, two hands held away from the body as the living armor wraps around waist chest and thighs, startled aroused expression"
        v2 = "rear three-quarter view, heroine glancing over her shoulder while living armor closes across her back and hips, confident seductive expression, clear silhouette"
    }
)

foreach ($card in $cards) {
    $basePath = Join-Path $firstBatchDir $card.base
    if (-not (Test-Path -LiteralPath $basePath)) { throw "Missing base composition: $basePath" }
    $form = @{
        image = Get-Item -LiteralPath $basePath
        subfolder = "MaidenSuccubus/EroticCardBatch/base"
        type = "input"
        overwrite = "true"
    }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value))
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function New-PromptGraph($job) {
    $prefix = "{0:D3}_{1}_erotic_img2img_v{2:D2}" -f $job.index, $job.class, $job.variant
    $positive = @($quality, $identity, $job.common, $job.variantPrompt, $layout, $abstractScene) -join ", "
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.88; strength_clip = 0.88 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticCardBatch/base/$($job.base)" } }
        "7" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "8" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "9" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticCardBatch/$($job.outfit)" } }
        "10" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("9", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "11" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("2", 0); ipadapter = @("7", 0); image = @("10", 0); clip_vision = @("8", 0); weight = [double]$job.outfitWeight; weight_type = "strong style transfer"; combine_embeds = "average"; start_at = 0.0; end_at = 0.82; embeds_scaling = "V only" } }
        "12" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticCardBatch/style/065_ReflectiveBarrier_反射屏障.png" } }
        "13" = @{ class_type = "PrepImageForClipVision"; inputs = @{ image = @("12", 0); interpolation = "LANCZOS"; crop_position = "pad"; sharpening = 0.0 } }
        "14" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("11", 0); ipadapter = @("7", 0); image = @("13", 0); clip_vision = @("8", 0); weight = 0.22; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.72; embeds_scaling = "V only" } }
        "15" = @{ class_type = "KSampler"; inputs = @{ model = @("14", 0); seed = [int64]$job.seed; steps = 38; cfg = 5.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = @("23", 0); denoise = [double]$job.denoise } }
        "16" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("15", 0); vae = @("1", 2) } }
        "17" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, adult woman, symmetrical aqua eyes, natural expressive face, blonde hair with pale pink gradient, crisp cel-shaded facial details" } }
        "18" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, young-looking, asymmetrical eyes, cross-eyed, deformed face, extra eyes, duplicate face, bad anatomy, blurred face" } }
        "19" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "20" = @{ class_type = "FaceDetailer"; inputs = @{ image = @("16", 0); model = @("14", 0); clip = @("3", 0); vae = @("1", 2); guide_size = 560.0; guide_size_for = $true; max_size = 1100.0; seed = [int64]($job.seed + 193); steps = 18; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("17", 0); negative = @("18", 0); denoise = 0.22; feather = 5; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 8; bbox_crop_factor = 3.0; sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = @("19", 0); wildcard = ""; cycle = 1 } }
        "21" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticBatch/$prefix"; images = @("20", 0) } }
        "22" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "23" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("22", 0); vae = @("1", 2) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$jobs = @()
foreach ($card in $cards) {
    if ($Indexes.Count -gt 0 -and $card.index -notin $Indexes) { continue }
    foreach ($variant in $Variants) {
        if ($variant -notin 1, 2) { throw "Unknown variant $variant" }
        $jobs += [pscustomobject]@{
            index = $card.index
            class = $card.class
            title = $card.title
            base = $card.base
            denoise = $card.denoise
            outfit = $card.outfit
            outfitWeight = $card.outfitWeight
            common = $card.common
            variant = $variant
            variantPrompt = [string]$card.("v$variant")
            seed = Get-StableSeed "$($card.class):erotic-batch-20260913:v$variant"
        }
    }
}

$clientId = [guid]::NewGuid().ToString()
$generated = 0
foreach ($job in $jobs) {
    $target = Join-Path $candidateDir ("{0:D3}_{1}_erotic_img2img_v{2:D2}.png" -f $job.index, $job.class, $job.variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) {
        Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"
        continue
    }
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
    $images = @($history.outputs."21".images)
    if ($images.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$images[0].filename)) {
        throw "ComfyUI completed without output for [$($job.index):$($job.variant)] $($job.title)"
    }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    $generated++
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($generated/$($jobs.Count))"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    checkpoint = "waiNSFWIllustrious_v140.safetensors"
    lora = "celesphonia-1.8.safetensors"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    size = "1000x760"
    jobs = @($jobs | Select-Object index, class, title, base, denoise, outfit, outfitWeight, variant, seed, common, variantPrompt)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $candidateDir "generation_spec.json") -Encoding utf8

Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
