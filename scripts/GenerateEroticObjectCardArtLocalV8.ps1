param(
    [int[]]$Indexes = @(50, 73, 74, 75, 78),
    [int[]]$Variants = @(1, 2),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV8_20260919"
$intermediateDir = Join-Path $targetDir "intermediate"
$workflowDir = Join-Path $targetDir "workflow_api"
$baseDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV2_20260913\composition_bases"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $intermediateDir, $workflowDir | Out-Null

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"

$quality = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "premium anime game illustration", "clean thin colored lineart", "controlled two-step cel shading",
    "restrained soft gradient", "crisp focal edges", "precise object construction", "readable silhouette"
) -join ", "

$layout = @(
    "1000 by 760 horizontal card illustration", "one clear focal action centered slightly above the middle",
    "important forms inside the central eighty percent safe area", "simple abstract gradient and mist background",
    "no concrete location", "no architecture", "no floor", "no horizon", "no text", "no letters",
    "no logo", "no watermark", "no frame", "no border", "no interface", "no card-shaped object"
) -join ", "

$negative = @(
    "worst quality", "low quality", "lowres", "blurry focal object", "out of focus", "jpeg artifacts",
    "rough sketch", "crude icon", "flat vector diagram", "minimalist logo", "abstract symbol",
    "photorealistic", "3d render", "chibi", "human", "woman", "girl", "person", "hand", "fingers",
    "text", "letters", "runes", "logo", "watermark", "frame", "border", "UI", "playing card",
    "tarot card", "panel", "concrete room", "laboratory", "bedroom", "street", "building"
) -join ", "

$cards = @(
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; base = "050_EcstasyDewHighDetail_base.png"; semanticDenoise = 0.42; styleWeight = 0.22; polishDenoise = 0.30
        common = "(one single ornate glass potion bottle:1.6), (exactly one bottle:1.5), solo object, small elegant faceted perfume ampoule with a narrow open neck, delicate gold filigree around the glass shoulder, luminous rose-gold aphrodisiac liquid with layered transparency and crisp reflections, three bright dew drops floating upward from the open neck, soft fragrant pink mist, intimate warm pink amber and dark violet palette, premium fantasy item close-up, dark navy-violet abstract background, no stopper, no second bottle, no multiple bottles, no blue liquid, no magic circle, no medallion, no jewelry"
        variants = @("bottle tilted diagonally from lower left toward upper right, stopper near the upper right", "upright bottle slightly left of center, three droplets form a gentle rising curve")
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; base = "073_DesireWhipMonsterClean_base.png"; semanticDenoise = 0.52; styleWeight = 0.18; polishDenoise = 0.30
        common = "(a thick black-purple leather whip striking a red hooded tentacle monster:1.5), dynamic monster combat scene, one whip curves from lower left and cracks across the monster on the right, monster recoils backward, limp purple tentacles, compact cyan stun spark only at the impact point, strong diagonal motion, close action crop, dark violet abstract battle mist, no sword, no spear, no staff, no laser, no crosshair, no targeting reticle, no empty scene"
        variants = @("wide S-curve whip fills the foreground and the monster occupies the upper-right third", "closer impact crop, whip bends sharply around recoiling tentacles, sparse speed sparks")
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; base = ""; semanticDenoise = 1.0; styleWeight = 0.18; polishDenoise = 0.28
        common = "(a dense field filled with many distinct luminous pink and violet flowers:1.6), surreal pleasure garden, dozens of layered lotus-like blossoms at different depths, intoxicating sparkling pink pollen mist flows across the whole garden, a few small dark monster silhouettes only at the distant outer edges, lush organic petals, strong depth, dark violet shadows between flowers, no single central icon, no heart symbol, no circle symbol, no empty field, no radial logo"
        variants = @("high overhead view into a hypnotic flower vortex, monsters arranged around the rim", "low oblique view across layered petals, a wave of pollen visibly reaches the monster silhouettes")
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; base = "075_SemenAppetiteCrystalHeart_base.png"; semanticDenoise = 0.32; styleWeight = 0.23; polishDenoise = 0.30
        common = "object-only magical transformation, one jagged black-purple curse crystal on the left cracks and dissolves into a flowing arc of thick translucent pearly-white liquid essence, the liquid essence is visibly absorbed by one luminous red-gold anatomical magical heart on the right, green-gold vitality veins spread outward through the heart, clear left-to-right cause and effect, dark violet abstract mist background, sensual wet material highlights, clean colored outlines, simplified controlled cel shading, restrained glow, no mouth, no tongue, no face, no full person, no hand, no bottle, no flat white tube, no solid rod, no medical diagram, no heart icon, the heart is a dimensional organic magic object"
        variants = @("the curse crystal shatters into violet fragments while the pearly essence pours into the upper chamber of the glowing heart", "closer diagonal composition, pearly droplets bridge the dissolving curse crystal and the heart, compact green-gold life flare at absorption")
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; base = ""; semanticDenoise = 1.0; styleWeight = 0.20; polishDenoise = 0.30
        common = "(empty feminine black-purple living magical bodysuit:1.5), hollow neck opening, hollow arm openings and hollow thigh openings clearly visible, glossy violet tentacles weave themselves into layered chest waist hip and thigh armor plates, gold trim and magenta heart gem details, a second translucent duplicate garment is being knitted behind it, sensual curving garment silhouette, no body inside, no skin, no mannequin, no face, no human limbs, no conventional metal armor"
        variants = @("three-quarter view, opaque front garment complete while the rear duplicate is half-woven", "two separate hollow garments connected by one looping living tendril, front garment centered")
    }
)

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

function New-BaseNodes([string]$positive) {
    [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("1", 1); stop_at_clip_layer = -2 } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = $positive } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = $negative } }
    }
}

function New-SemanticGraph($job) {
    $positive = @($quality, $job.common, $job.variantPrompt, $layout) -join ", "
    $graph = New-BaseNodes $positive
    if ($job.base) {
        $graph["8"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV8/base/$($job.base)" } }
        $graph["9"] = @{ class_type = "ImageScale"; inputs = @{ image = @("8", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        $graph["10"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("9", 0); vae = @("1", 2) } }
    }
    else {
        $graph["10"] = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 1000; height = 760; batch_size = 1 } }
    }
    $graph["11"] = @{ class_type = "KSampler"; inputs = @{
        model = @("1", 0); seed = [int64]$job.seed; steps = 36; cfg = 6.2; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = @("3", 0); negative = @("4", 0); latent_image = @("10", 0); denoise = [double]$job.semanticDenoise
    } }
    $graph["12"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("11", 0); vae = @("1", 2) } }
    $graph["40"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV8/intermediate/{0:D3}_{1}_v8_{2:D2}_stageA" -f $job.index, $job.class, $job.variant); images = @("12", 0) } }
    $graph
}

function New-PolishGraph($job) {
    $positive = @($quality, $job.common, $job.variantPrompt, $layout) -join ", "
    $graph = New-BaseNodes $positive
    $graph["5"] = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
    $graph["6"] = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
    $graph["7"] = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV8/style/065_ReflectiveBarrier_反射屏障.png" } }
    $graph["8"] = @{ class_type = "IPAdapterAdvanced"; inputs = @{
        model = @("1", 0); ipadapter = @("5", 0); image = @("7", 0); weight = [double]$job.styleWeight
        weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.62
        embeds_scaling = "V only"; clip_vision = @("6", 0)
    } }
    $graph["9"] = @{ class_type = "LoadImage"; inputs = @{ image = ("MaidenSuccubus/EroticLocalV8/intermediate/{0:D3}_{1}_v8_{2:D2}_stageA.png" -f $job.index, $job.class, $job.variant) } }
    $graph["10"] = @{ class_type = "ImageScale"; inputs = @{ image = @("9", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    $graph["11"] = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("10", 0); vae = @("1", 2) } }
    $graph["12"] = @{ class_type = "KSampler"; inputs = @{
        model = @("8", 0); seed = [int64]($job.seed + 97); steps = 28; cfg = 5.0; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
        positive = @("3", 0); negative = @("4", 0); latent_image = @("11", 0); denoise = [double]$job.polishDenoise
    } }
    $graph["13"] = @{ class_type = "VAEDecode"; inputs = @{ samples = @("12", 0); vae = @("1", 2) } }
    $graph["40"] = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV8/{0:D3}_{1}_LOCAL_COMFYUI_v8_{2:D2}" -f $job.index, $job.class, $job.variant); images = @("13", 0) } }
    $graph
}

function Invoke-ComfyGraph($graph, [string]$label, [string]$clientId) {
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 35 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected $label`: $($response.node_errors | ConvertTo-Json -Depth 10 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  $label -> $promptId"
    $deadline = (Get-Date).AddMinutes(15)
    $failures = 0
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        try {
            $root = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 15
            $failures = 0
        }
        catch {
            $failures++
            if ($failures -ge 3) { throw "ComfyUI stopped responding during $label" }
            continue
        }
        $property = $root.PSObject.Properties[$promptId]
        if ($null -eq $property) { continue }
        $images = @($property.Value.outputs."40".images)
        if ($images.Count -eq 0) { throw "ComfyUI completed without output for $label" }
        return $images[0]
    }
    throw "Timed out waiting for $label"
}

function Copy-ComfyOutput($image, [string]$destination) {
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Upload-ComfyImage $stylePath "MaidenSuccubus/EroticLocalV8/style" "065_ReflectiveBarrier_反射屏障.png"
foreach ($card in $cards) {
    if ($card.base) { Upload-ComfyImage (Join-Path $baseDir $card.base) "MaidenSuccubus/EroticLocalV8/base" $card.base }
}

$jobs = @()
foreach ($card in $cards) {
    if ($card.index -notin $Indexes) { continue }
    foreach ($variant in $Variants) {
        if ($variant -notin 1, 2) { throw "Unknown variant: $variant" }
        $jobs += [pscustomobject]@{
            index = $card.index; class = $card.class; title = $card.title; base = $card.base; semanticDenoise = [double]$card.semanticDenoise; common = $card.common
            variant = $variant; variantPrompt = [string]$card.variants[$variant - 1]
            styleWeight = [double]$card.styleWeight; polishDenoise = [double]$card.polishDenoise
            seed = Get-StableSeed "$($card.class):local-v8-20260919:v$variant"
        }
    }
}

$clientId = [guid]::NewGuid().ToString()
$generated = 0
foreach ($job in $jobs) {
    $targetName = "{0:D3}_{1}_{2}_LOCAL_COMFYUI_v8_{3:D2}.png" -f $job.index, $job.class, $job.title, $job.variant
    $target = Join-Path $targetDir $targetName
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"; continue }

    $stageAPath = Join-Path $intermediateDir ("{0:D3}_{1}_v8_{2:D2}_stageA.png" -f $job.index, $job.class, $job.variant)
    $stageAGraph = New-SemanticGraph $job
    $stageAGraph | ConvertTo-Json -Depth 35 | Set-Content -LiteralPath (Join-Path $workflowDir ("{0:D3}_{1}_v8_{2:D2}_stageA_api.json" -f $job.index, $job.class, $job.variant)) -Encoding utf8
    $stageAImage = Invoke-ComfyGraph $stageAGraph "[$($job.index):$($job.variant):A] semantics" $clientId
    Copy-ComfyOutput $stageAImage $stageAPath
    Upload-ComfyImage $stageAPath "MaidenSuccubus/EroticLocalV8/intermediate" (Split-Path -Leaf $stageAPath)

    $stageBGraph = New-PolishGraph $job
    $stageBGraph | ConvertTo-Json -Depth 35 | Set-Content -LiteralPath (Join-Path $workflowDir ("{0:D3}_{1}_v8_{2:D2}_stageB_api.json" -f $job.index, $job.class, $job.variant)) -Encoding utf8
    $stageBImage = Invoke-ComfyGraph $stageBGraph "[$($job.index):$($job.variant):B] style" $clientId
    Copy-ComfyOutput $stageBImage $target
    $generated++
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($generated/$($jobs.Count))"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o"); pipeline = "LOCAL_COMFYUI_V8_TWO_STAGE_OBJECT"; outputSize = "1000x760"
    checkpoint = $checkpoint; styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    method = "stage A pure semantic txt2img; stage B low-denoise IP-Adapter style polish"
    jobs = @($jobs | Select-Object index, class, title, variant, seed, base, semanticDenoise, styleWeight, polishDenoise, common, variantPrompt)
} | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v8.json") -Encoding utf8

Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V8_TWO_STAGE_OBJECT generated_this_run=$generated selected=$($jobs.Count)"
