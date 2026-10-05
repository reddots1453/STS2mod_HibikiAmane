param(
    [int[]]$Indexes = @(50, 58, 73, 74, 75, 76, 78),
    [int[]]$Variants = @(1, 2, 3),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI",
    [string]$OriginalRoot = "D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img\pictures"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV2_20260913"
$referenceRoot = Join-Path $targetDir "references"
$outputRoot = Join-Path $ComfyRoot "output"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$corruptPath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久3_无损.png"
$fullDamagePath = Join-Path $modRoot "图片素材\变身形态\邪瘴天衣\魔装耐久1_严重破损.png"

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, polished anime game illustration, clean thin colored lineart, controlled two-step cel shading, restrained highlights, crisp focal subject, coherent adult anatomy, natural joints, correct limb attachment, deliberate readable silhouette"
$identity = "celesphonia, hibikiamane, solo adult woman, mature adult body, aqua eyes, blonde hair fading to pale pink, long hair, one side ponytail with a simple blue ribbon, no hair rings"
$layout = "horizontal card illustration, focal action in the central upper safe area, simple dark gradient and restrained bokeh only, no identifiable place, no scenery, no floor, no horizon, no text, no letters, no logo, no watermark, no frame, no border, no interface, no card-shaped object"
$negative = "worst quality, low quality, lowres, blurry subject, depth of field blur on body, jpeg artifacts, sketch, thick black outlines, overpainted glossy fantasy art, excessive bloom, excessive particles, photorealistic, 3d, chibi, child, teenager, young-looking, loli, schoolgirl, male, multiple women, duplicate person, duplicate torso, extra arms, third arm, extra legs, disconnected limbs, floating limbs, twisted shoulder, broken elbow, broken wrist, dislocated hip, extra fingers, six fingers, four fingers, missing fingers, fused fingers, malformed hands, giant hands, tiny hands, long neck, duplicate breasts, asymmetrical eyes, cross-eyed, concrete room, bedroom, dungeon, street, building, literal garden, landscape, wall, floor, horizon, text, letters, logo, watermark, frame, border, UI, playing card, tarot card, panel"

$cards = @(
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; composition = (Join-Path $referenceRoot "Eve_13_0001.png"); compositionName = "Eve_13_0001.png"; outfit = $corruptPath; outfitName = "魔装耐久3_无损.png"; outfitWeight = 0.30; compositionWeight = 0.68
        common = "intimate upper-body crop, exact black purple and gold corrupted magical outfit, one small round transparent vial containing vivid luminous pink aphrodisiac dew held beside her parted lips, pink fragrant mist, flushed half-lidded sensual expression, only one hand near the face, other arm outside the crop, no blue liquid, no nozzle, no squeeze bottle"
        v1 = "front three-quarter portrait, head gently tilted, five fingertips naturally curved around the narrow glass neck"
        v2 = "slight high-angle portrait, pink droplet suspended between vial mouth and lower lip, embarrassed intoxicated gaze"
        v3 = "clean side-three-quarter bust, lips about to sip the pink dew, vial silhouette completely readable"
    }
    [pscustomobject]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; composition = (Join-Path $referenceRoot "Eve_42_0001.png"); compositionName = "Eve_42_0001.png"; outfit = $fullDamagePath; outfitName = "魔装耐久1_严重破损.png"; outfitWeight = 0.34; compositionWeight = 0.76
        common = "adult erotic battle aftermath, stable standing three-quarter waist-up composition, severely damaged black purple and gold corrupted magical outfit, wrists held together behind her back, two shoulders clearly connected, purple curse sigils on torso, every sigil emits a thin cyan protective shield arc, flushed ecstatic trance, sweat, teary half-lidded eyes, no second person, no penetration"
        v1 = "camera slightly above eye level, torso leaning forward a little, curse sigils and cyan shield both clearly readable"
        v2 = "close waist-up portrait, shoulders drawn back by one loose luminous restraint behind her, overwhelmed blissful expression"
        v3 = "front three-quarter medium crop, balanced upright pose, purple debuff marks transforming into one translucent cyan barrier"
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; composition = (Join-Path $modRoot "图片素材\完成版卡图\006_ObstructingShot_妨碍射击.png"); compositionName = "006_ObstructingShot_妨碍射击.png"; outfit = $corruptPath; outfitName = "魔装耐久3_无损.png"; outfitWeight = 0.40; compositionWeight = 0.82
        common = "rear over-the-shoulder combat view, exact black purple and gold corrupted magical outfit, face completely hidden, one anatomically continuous arm extends forward and grips a simple whip handle, one long pink-black energy whip curves in a single S shape and strikes one dark monster silhouette, bright impact stun ring around the monster, no second visible arm"
        v1 = "whip curves from the hand across the center toward a monster at the right edge, strong readable action line"
        v2 = "camera close behind her shoulder, whip impact slightly above the target, restrained pink sparks"
        v3 = "dynamic rear three-quarter crop, torso twist remains subtle and anatomically plausible, single clear whip arc"
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; composition = (Join-Path $referenceRoot "Eve_18_0001.png"); compositionName = "Eve_18_0001.png"; outfit = $fullDamagePath; outfitName = "魔装耐久1_严重破损.png"; outfitWeight = 0.34; compositionWeight = 0.78
        common = "adult erotic magical tableau, stable reclining side view with torso and thighs clearly connected, severely damaged black purple and gold corrupted magical outfit, one arm raised beside her head and the other resting along her waist, pink-purple flower-shaped magic halo behind her, three small faceless enemy silhouettes entranced at the outer edge, blissful inviting expression, symbolic paradise, no penetration"
        v1 = "reclining diagonally from lower right to upper left, legs kept together, clean silhouette"
        v2 = "slight high-angle reclining portrait, floral desire aura radiates outward in three concentric rings"
        v3 = "side-on floating pose, body forms a gentle single curve, abstract petals and three dark silhouettes remain secondary"
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; composition = (Join-Path $modRoot "图片素材\完成版卡图\056_SemenConversion_精液变换.png"); compositionName = "056_SemenConversion_精液变换.png"; outfit = $stylePath; outfitName = "065_ReflectiveBarrier_反射屏障.png"; outfitWeight = 0.0; compositionWeight = 0.58
        common = "object-only magical alchemy, one cracked matte-black curse orb at upper left releases a thick pearly-white essence stream, the stream enters one green-gold crystal heart at center and becomes vivid green vitality light, clear left-to-right conversion from curse into maximum life, no person, no body, no hands, no mouth, no bottle"
        v1 = "black orb and green heart connected by one continuous pearly ribbon, simple triangular composition"
        v2 = "green life crystal centered slightly above the middle, shattered curse shell fragments contained near the left edge"
        v3 = "one black curse sphere dissolving from the top while pearly essence descends into a bright green-gold life sigil"
    }
    [pscustomobject]@{
        index = 76; class = "BiteInvader"; title = "咬"; composition = (Join-Path $referenceRoot "Eve_13_0001.png"); compositionName = "Eve_13_0001.png"; outfit = $fullDamagePath; outfitName = "魔装耐久1_严重破损.png"; outfitWeight = 0.30; compositionWeight = 0.62
        common = "tight adult head-and-shoulders crop, damaged black purple magical outfit visible only at collar and shoulder, one thick black-purple restraining tentacle crosses beside her mouth, her teeth clearly clamp and sever the tentacle, one correct five-fingered hand grips the tentacle below the bite, angry defiant aqua eyes, seven small violet weakening seals trail along the recoiling end, no gore, no phallic anatomy"
        v1 = "clear side-three-quarter facial profile, mouth and severed tentacle are the central focal point"
        v2 = "slight low angle close-up, jaw visibly closed on the tentacle, shoulder and neck connection unobstructed"
        v3 = "intense portrait crop, tentacle enters from lower right and recoils toward upper left, only one hand visible"
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; composition = (Join-Path $referenceRoot "Eve_20_0001.png"); compositionName = "Eve_20_0001.png"; outfit = $corruptPath; outfitName = "魔装耐久3_无损.png"; outfitWeight = 0.32; compositionWeight = 0.84
        common = "stable frontal three-quarter adult torso and upper thighs, arms extended outward but cropped before the hands, living glossy black-purple tendrils visibly interweave into one continuous purple-black bodysuit over chest waist hips and thighs, gold trim and purple rose details, tendrils are structural seams and armor plates rather than floating nearby, a second translucent armor silhouette behind her represents the copied layer, sensual transformation, no penetration"
        v1 = "symmetrical iconic presentation, armor integration clearly visible across waist and hips"
        v2 = "slight low-angle medium crop, tendrils close across the torso like articulated living plate armor"
        v3 = "front three-quarter view, one translucent duplicate armor shell offset behind the continuous primary suit"
    }
)

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value))
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

$sharedUploads = @(
    @{ source = $stylePath; subfolder = "MaidenSuccubus/EroticRedrawV2/style"; name = "065_ReflectiveBarrier_反射屏障.png" },
    @{ source = $corruptPath; subfolder = "MaidenSuccubus/EroticRedrawV2/outfit"; name = "魔装耐久3_无损.png" },
    @{ source = $fullDamagePath; subfolder = "MaidenSuccubus/EroticRedrawV2/outfit"; name = "魔装耐久1_严重破损.png" },
    @{ source = (Join-Path $modRoot "图片素材\完成版卡图\006_ObstructingShot_妨碍射击.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "006_ObstructingShot_妨碍射击.png" },
    @{ source = (Join-Path $modRoot "图片素材\完成版卡图\056_SemenConversion_精液变换.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "056_SemenConversion_精液变换.png" },
    @{ source = (Join-Path $referenceRoot "Eve_13_0001.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "Eve_13_0001.png" },
    @{ source = (Join-Path $referenceRoot "Eve_18_0001.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "Eve_18_0001.png" },
    @{ source = (Join-Path $referenceRoot "Eve_20_0001.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "Eve_20_0001.png" },
    @{ source = (Join-Path $referenceRoot "Eve_42_0001.png"); subfolder = "MaidenSuccubus/EroticRedrawV2/composition"; name = "Eve_42_0001.png" }
)

foreach ($upload in $sharedUploads) {
    if (-not (Test-Path -LiteralPath $upload.source)) { throw "Missing reference: $($upload.source)" }
    $form = @{ image = Get-Item -LiteralPath $upload.source; subfolder = $upload.subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-PromptGraph($job) {
    $positive = @($quality, $(if ($job.index -eq 75) { "" } else { $identity }), $job.common, $job.variantPrompt, $layout) -join ", "
    $prefix = "{0:D3}_{1}_redraw_v2_{2:D2}" -f $job.index, $job.class, $job.variant
    $compositionRemote = "MaidenSuccubus/EroticRedrawV2/composition/$($job.compositionName)"
    $outfitRemote = "MaidenSuccubus/EroticRedrawV2/outfit/$($job.outfitName)"
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.86; strength_clip = 0.86 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticRedrawV2/style/065_ReflectiveBarrier_反射屏障.png" } }
        "9" = @{ class_type = "LoadImage"; inputs = @{ image = $compositionRemote } }
        "10" = @{ class_type = "IPAdapterStyleComposition"; inputs = @{ model = @("2", 0); ipadapter = @("6", 0); image_style = @("8", 0); image_composition = @("9", 0); weight_style = 0.24; weight_composition = [double]$job.compositionWeight; expand_style = $false; combine_embeds = "average"; start_at = 0.0; end_at = 0.76; embeds_scaling = "V only"; clip_vision = @("7", 0) } }
        "13" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 1000; height = 760; batch_size = 1 } }
        "14" = @{ class_type = "KSampler"; inputs = @{ model = @("12", 0); seed = [int64]$job.seed; steps = 32; cfg = 5.1; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = @("13", 0); denoise = 1.0 } }
        "15" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("14", 0); vae = @("1", 2) } }
        "16" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticRedrawV2/$prefix"; images = @("15", 0) } }
    }

    if ($job.outfitWeight -gt 0) {
        $graph["11"] = @{ class_type = "LoadImage"; inputs = @{ image = $outfitRemote } }
        $graph["12"] = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("10", 0); ipadapter = @("6", 0); image = @("11", 0); weight = [double]$job.outfitWeight; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.62; embeds_scaling = "V only"; clip_vision = @("7", 0) } }
    }
    else {
        $graph["12"] = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("10", 0); ipadapter = @("6", 0); image = @("8", 0); weight = 0.01; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.01; embeds_scaling = "V only"; clip_vision = @("7", 0) } }
    }
    return $graph
}

function Get-CompletedHistory([string]$promptId) {
    try { $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 20 }
    catch { return $null }
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$jobs = @()
foreach ($card in $cards) {
    if ($card.index -notin $Indexes) { continue }
    foreach ($variant in $Variants) {
        if ($variant -notin 1, 2, 3) { throw "Unknown variant $variant" }
        $jobs += [pscustomobject]@{
            index = $card.index; class = $card.class; title = $card.title
            compositionName = $card.compositionName; outfitName = $card.outfitName
            compositionWeight = $card.compositionWeight; outfitWeight = $card.outfitWeight
            common = $card.common; variant = $variant; variantPrompt = [string]$card.("v$variant")
            seed = Get-StableSeed "$($card.class):redraw-v2-20260913:v$variant"
        }
    }
}

$clientId = [guid]::NewGuid().ToString()
$generated = 0
foreach ($job in $jobs) {
    $target = Join-Path $targetDir ("{0:D3}_{1}_redraw_v2_{2:D2}.png" -f $job.index, $job.class, $job.variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"; continue }
    $body = @{ prompt = (New-PromptGraph $job); client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected [$($job.index):$($job.variant)]: $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) -> $promptId"
    $history = $null
    while ($null -eq $history) { Start-Sleep -Seconds 2; $history = Get-CompletedHistory $promptId }
    $images = @($history.outputs."16".images)
    if ($images.Count -eq 0) { throw "ComfyUI completed without output for [$($job.index):$($job.variant)]" }
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
    method = "fresh text-to-image with IPAdapter SDXL style and composition references; rejected candidates are not reused"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    outputSize = "1000x760"
    jobs = @($jobs | Select-Object index, class, title, variant, seed, compositionName, compositionWeight, outfitName, outfitWeight, common, variantPrompt)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v2.json") -Encoding utf8

Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
