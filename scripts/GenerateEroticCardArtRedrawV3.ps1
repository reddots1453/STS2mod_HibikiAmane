param(
    [int[]]$Indexes = @(50, 73, 74, 75, 76, 78),
    [int[]]$Variants = @(1, 2),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV7_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$baseDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV2_20260913\composition_bases"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, newest, highres, polished premium anime game illustration, clean thin colored lineart, smooth controlled two-step cel shading, restrained soft gradients, crisp focal details, restrained highlights, coherent anatomy, precise material edges, deliberate readable silhouette, same clean cel-shaded visual language as the style reference"
$identity = "celesphonia, hibikiamane, solo adult woman, mature adult body, aqua eyes, long blonde hair fading to pale pink, one side ponytail tied with a simple blue ribbon, no hair rings"
$layout = "1000 by 760 horizontal card illustration, focal action centered slightly above the middle, all important forms inside the central eighty percent safe area, abstract dark gradient or mist background only, no identifiable place, no architecture, no floor, no horizon, no text, no letters, no logo, no watermark, no frame, no border, no interface, no card-shaped object"
$negative = "worst quality, low quality, lowres, blurry subject, depth-of-field blur on focal subject, jpeg artifacts, rough sketch, crude icon, flat vector diagram, thick black outlines, overpainted glossy fantasy art, excessive bloom, excessive particles, photorealistic, 3d, chibi, child, teenager, young-looking, loli, schoolgirl, multiple women, duplicate person, duplicate torso, extra arms, third arm, extra legs, disconnected limbs, floating limbs, twisted shoulder, broken elbow, broken wrist, dislocated hip, extra fingers, six fingers, four fingers, missing fingers, fused fingers, malformed hands, giant hands, tiny hands, long neck, duplicate breasts, asymmetrical eyes, cross-eyed, concrete room, bedroom, dungeon, street, building, wall, floor, horizon, text, letters, logo, watermark, frame, border, UI, playing card, tarot card, panel"

$cards = @(
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; base = ""; denoise = @(1.0, 1.0, 1.0); styleWeight = 0.08; face = $false; objectOnly = $true; removeBackground = $false
        common = "object-only fantasy item illustration, one unmistakable small glass perfume ampoule with a narrow open bottle neck, containing luminous rose-gold aphrodisiac dew, the removable stopper has a delicate crescent ornament and lies beside the bottle, three sparkling dew drops rise from the open neck and become a soft fragrant mist, intimate warm pink and amber palette, sensuous liquid surface, clearly a precious drinkable potion bottle, no magic circle, no pendant, no medallion, no jewelry, no person, no body, no face, no hand, no blue liquid"
        variants = @("ampoule tilted above a dark velvet-like abstract shadow, rising droplets are the focal point", "slight high angle over a shallow crystal saucer holding a single glowing dew drop", "close macro view of the crescent stopper and rose-gold liquid with a soft fragrant spiral")
    }
    [pscustomobject]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; base = "058_MasochisticTranceSolo_base.png"; denoise = @(0.32, 0.40, 0.48); styleWeight = 0.46; face = $true; objectOnly = $false; removeBackground = $false
        common = "preserve the exact anatomically coherent original-game solo adult heroine pose from the source: exactly one woman, both arms raised together over her head, two shoulders, two arms, two crossed legs, no second person; black high-cut damaged corrupted magical bodysuit, blonde hair with pale pink ends and one blue-ribbon side ponytail; loose iron chains arc around her raised wrists and crossed knees without creating extra limbs; flushed ecstatic trance, sweat, teary half-lidded aqua eyes; several small violet negative-status wisps are absorbed into one translucent cyan defensive aura tracing her silhouette, no floating icons, no captor, no penetration"
        variants = @("tight three-quarter-body polish, chain links and both arm silhouettes remain clearly separated", "violet curse wisps visibly dissolve into cyan shielding along her shoulders and waist", "subtle cyan barrier flare behind the single figure, restrained highlights and clean readable outline")
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; base = ""; denoise = @(1.0, 1.0, 1.0); styleWeight = 0.10; face = $false; objectOnly = $true; removeBackground = $false
        common = "dynamic close-up cel-shaded monster combat illustration filling most of the canvas, one red hooded Slay the Spire tentacle monster recoils on the right and one thick curving black-purple leather whip enters from the lower left, the pink-edged whip visibly cracks across the monster's tentacles, a compact cyan stun spark only at the point of impact, limp recoiling tendrils and strong left-to-right motion, no heroine, no human, no hand, no beam, no straight laser, no targeting reticle, no crosshair, no cyan circle, no emblem, no diagram"
        variants = @("the whip crack bends the monster backward with a compact cyan impact flash", "closer crop on severed momentum and limp tentacles, sparse speed sparks", "wide whip arc surrounds the monster once before a bright stun impact")
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; base = ""; denoise = @(1.0, 1.0, 1.0); styleWeight = 0.12; face = $false; objectOnly = $true; removeBackground = $false
        common = "environmental magical card illustration with no human figure, a surreal circular garden of enormous luminous pink-purple night flowers opens in concentric layers, dense intoxicating pollen mist floods outward, several small recognizable monster silhouettes at the outer ring turn toward the flowers with glowing pink eyes and heart-shaped desire auras, the whole battlefield is transformed into an erotic temptation zone, lush organic detail, strong depth, dark violet and hot pink palette, no woman, no body, no limbs, no literal architecture"
        variants = @("high overhead view into a hypnotic flower vortex with monsters around the rim", "low oblique view across layered petals, pollen wave visibly reaches three monster silhouettes", "one dominant open flower at center with multiple smaller carnivorous blossoms surrounding entranced enemies")
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; base = ""; denoise = @(1.0, 1.0, 1.0); styleWeight = 0.12; face = $false; objectOnly = $true; removeBackground = $false
        common = "object-only dark alchemical transformation illustration, a detailed cracked black curse crystal on the left dissolves into thick luminous pearly-white essence, the continuous essence stream is visibly absorbed by one organic red crystal heart on the right, green-gold life veins spread through the heart and make it grow larger, unmistakable conversion of a curse into permanent maximum vitality, layered depth, rich material texture, no icon, no diagram, no person, no body, no hands, no mouth, no bottle"
        variants = @("diagonal flow from shattered curse crystal at upper left into enlarged living heart at lower right", "close view of the white essence entering the heart while dark curse fragments evaporate behind", "heart centered above the middle, black crystal shell peeling away around a spiral of pearly essence")
    }
    [pscustomobject]@{
        index = 76; class = "BiteInvader"; title = "咬"; base = "076_BiteBarrierStyle_base.png"; denoise = @(0.25, 0.32, 0.40); styleWeight = 0.46; face = $true; objectOnly = $false; removeBackground = $false
        common = "preserve the accepted high-quality cel-shaded adult heroine close-up and the exact mouth-tentacle contact from the source; extremely tight head-and-shoulders action crop, absolutely no hands, no fingers and no forearms visible; exactly one thick pink-purple monster tentacle enters from the right and is clamped between her teeth, clear compressed bite mark, tentacle recoils sharply away, determined defiant aqua eyes and flushed cheeks, blonde hair fading pale pink with one blue-ribbon side ponytail, damaged black corrupted magical outfit visible only at the lower edge, replace the hexagonal barrier with simple abstract cyan and violet impact mist, no extra tentacles near the mouth, no gore, no phallic anatomy"
        variants = @("preserve the exact jaw contact while adding a compact cyan impact spark at the bite", "stronger recoil curve and tiny cyan stun sparks along the bitten tentacle", "close profile emphasis on clenched teeth and the compressed bite mark, both arms fully excluded")
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; base = ""; denoise = @(1.0, 1.0, 1.0); styleWeight = 0.12; face = $false; objectOnly = $true; removeBackground = $false
        common = "object-only living magical garment illustration, an empty feminine black-purple corrupted magical dress floats upright with clearly hollow neck arm and leg openings, multiple glossy organic tendrils weave themselves into layered chest waist hip and thigh armor plates, gold trim and magenta heart-gem details, behind it the tendrils simultaneously knit a second translucent duplicate garment, sensual curving silhouette but absolutely no person inside, no skin, no face, no human hands, no mannequin, no penetration, not conventional metal armor"
        variants = @("three-quarter view, front garment complete while the duplicate behind is half-woven from tendrils", "close view of tendrils crossing and becoming glossy fabric around the waist and hips", "two clearly separate empty garments connected by one looping living tendril, front one opaque and rear one spectral")
    }
)

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [int64]([BitConverter]::ToUInt64($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0) -band 0x001FFFFFFFFFFFFF) }
    finally { $sha.Dispose() }
}

$uploads = @(@{ source = $stylePath; subfolder = "MaidenSuccubus/EroticLocalV7/style"; name = "065_ReflectiveBarrier_反射屏障.png" })
foreach ($card in $cards) {
    if ($card.base) { $uploads += @{ source = (Join-Path $baseDir $card.base); subfolder = "MaidenSuccubus/EroticLocalV7/base"; name = $card.base } }
}
foreach ($upload in $uploads) {
    if (-not (Test-Path -LiteralPath $upload.source)) { throw "Missing input: $($upload.source)" }
    $form = @{ image = Get-Item -LiteralPath $upload.source; subfolder = $upload.subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-PromptGraph($job) {
    $subjectIdentity = if ($job.objectOnly) { "" } else { $identity }
    $positive = @($quality, $subjectIdentity, $job.common, $job.variantPrompt, $layout) -join ", "
    $latent = if ($job.base) { @("13", 0) } else { @("11", 0) }
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = $(if ($job.objectOnly) { 0.0 } else { 0.78 }); strength_clip = $(if ($job.objectOnly) { 0.0 } else { 0.78 }) } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = "ip-adapter-plus_sdxl_vit-h.safetensors" } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = "model.safetensors" } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV7/style/065_ReflectiveBarrier_反射屏障.png" } }
        "9" = @{ class_type = "IPAdapterAdvanced"; inputs = @{ model = @("2", 0); ipadapter = @("6", 0); image = @("8", 0); weight = [double]$job.styleWeight; weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = $(if ($job.objectOnly) { 0.55 } else { 0.80 }); embeds_scaling = "V only"; clip_vision = @("7", 0) } }
        "11" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 1000; height = 760; batch_size = 1 } }
        "14" = @{ class_type = "KSampler"; inputs = @{ model = @("9", 0); seed = [int64]$job.seed; steps = 34; cfg = 5.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("4", 0); negative = @("5", 0); latent_image = $latent; denoise = [double]$job.denoise } }
        "15" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("14", 0); vae = @("1", 2) } }
        "16" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "masterpiece, best quality, adult woman, symmetrical aqua eyes, natural expressive adult face, crisp clean cel-shaded facial details" } }
        "17" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = "child, teenager, young-looking, asymmetrical eyes, cross-eyed, deformed face, extra eyes, duplicate face, bad anatomy, blurred face" } }
        "18" = @{ class_type = "UltralyticsDetectorProvider"; inputs = @{ model_name = "bbox/face_yolov8m.pt" } }
        "19" = @{ class_type = "FaceDetailer"; inputs = @{ image = @("15", 0); model = @("9", 0); clip = @("3", 0); vae = @("1", 2); guide_size = 560.0; guide_size_for = $true; max_size = 1100.0; seed = [int64]($job.seed + 193); steps = 18; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"; positive = @("16", 0); negative = @("17", 0); denoise = 0.20; feather = 5; noise_mask = $true; force_inpaint = $true; bbox_threshold = 0.5; bbox_dilation = 8; bbox_crop_factor = 3.0; sam_detection_hint = "none"; sam_dilation = 0; sam_threshold = 0.93; sam_bbox_expansion = 0; sam_mask_hint_threshold = 0.7; sam_mask_hint_use_negative = "False"; drop_size = 10; bbox_detector = @("18", 0); wildcard = ""; cycle = 1 } }
        "20" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV7/{0:D3}_{1}_LOCAL_COMFYUI_v7_{2:D2}" -f $job.index, $job.class, $job.variant); images = $(if ($job.face) { @("19", 0) } else { @("15", 0) }) } }
    }
    if ($job.base) {
        $graph["10"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV7/base/$($job.base)" } }
        $graph["12"] = @{ class_type = "ImageScale"; inputs = @{ image = @("10", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        if ($job.objectOnly -or -not $job.removeBackground) {
            $graph["13"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("12", 0); vae = @("1", 2) } }
        }
        else {
            $graph["21"] = @{ class_type = "Image Rembg (Remove Background)"; inputs = @{ images = @("12", 0); transparency = $true; model = "isnet-anime"; post_processing = $true; only_mask = $false; alpha_matting = $false; alpha_matting_foreground_threshold = 240; alpha_matting_background_threshold = 10; alpha_matting_erode_size = 4; background_color = "none" } }
            $graph["22"] = @{ class_type = "ImageToMask"; inputs = @{ image = @("21", 0); channel = "alpha" } }
            $graph["23"] = @{ class_type = "EmptyImage"; inputs = @{ width = 1000; height = 760; batch_size = 1; color = 1181728 } }
            $graph["24"] = @{ class_type = "ImageCompositeMasked"; inputs = @{ destination = @("23", 0); source = @("21", 0); x = 0; y = 0; resize_source = $false; mask = @("22", 0) } }
            $graph["13"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("24", 0); vae = @("1", 2) } }
        }
    }
    return $graph
}

function Get-CompletedHistory([string]$promptId) {
    try { $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 20 } catch { return $null }
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Wait-CompletedHistory([string]$promptId, [string]$label) {
    $deadline = (Get-Date).AddMinutes(12)
    $connectionFailures = 0
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        try {
            $history = Get-CompletedHistory $promptId
            $connectionFailures = 0
        }
        catch {
            $history = $null
            $connectionFailures++
        }
        if ($null -ne $history) { return $history }
        try { [void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 4) }
        catch {
            $connectionFailures++
            if ($connectionFailures -ge 3) { throw "ComfyUI stopped responding during $label" }
        }
    }
    throw "Timed out waiting for $label"
}

$jobs = @()
foreach ($card in $cards) {
    if ($card.index -notin $Indexes) { continue }
    foreach ($variant in $Variants) {
        if ($variant -notin 1, 2, 3) { throw "Unknown variant $variant" }
        $jobs += [pscustomobject]@{
            index = $card.index; class = $card.class; title = $card.title; base = $card.base; styleWeight = [double]$card.styleWeight; face = $card.face; objectOnly = $card.objectOnly; removeBackground = $card.removeBackground
            common = $card.common; variant = $variant; variantPrompt = [string]$card.variants[$variant - 1]
            denoise = [double]$card.denoise[$variant - 1]; seed = Get-StableSeed "$($card.class):local-v7-20260919:v$variant"
        }
    }
}

function Clear-ComfyModels {
    Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
    Start-Sleep -Seconds 2
}

$clientId = [guid]::NewGuid().ToString()
$generated = 0
Clear-ComfyModels
foreach ($job in $jobs) {
    $target = Join-Path $targetDir ("{0:D3}_{1}_{2}_LOCAL_COMFYUI_v7_{3:D2}.png" -f $job.index, $job.class, $job.title, $job.variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"; continue }
    $graph = New-PromptGraph $job
    $graph | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $workflowDir ("{0:D3}_{1}_v7_{2:D2}_api.json" -f $job.index, $job.class, $job.variant)) -Encoding utf8
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected [$($job.index):$($job.variant)]: $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) -> $promptId"
    $history = Wait-CompletedHistory $promptId "[$($job.index):$($job.variant)] $($job.title)"
    $images = @($history.outputs."20".images)
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
    pipeline = "LOCAL_COMFYUI_V7_NATIVE_1000x760"
    method = "Local WAI NSFW Illustrious + Celesphonia LoRA when a heroine is present + IP-Adapter style lock to 065 + optional source composition + face detailer"
    styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    outputSize = "1000x760"
    jobs = @($jobs | Select-Object index, class, title, variant, seed, base, denoise, common, variantPrompt)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v7.json") -Encoding utf8

Clear-ComfyModels
Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
