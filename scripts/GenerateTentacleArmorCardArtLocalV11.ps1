param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV11_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$basePath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV4_20260914\078_TentacleArmor_hifi_v4_01.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"
$positiveBase = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "premium anime fantasy game illustration", "clean thin colored lineart", "controlled two-step cel shading",
    "restrained soft gradients", "crisp focal edges", "readable object silhouette",
    "one empty feminine living magical bodysuit floating upright", "no wearer inside",
    "the garment is visibly woven from many glossy black-purple tentacles",
    "individual violet tendrils loop and interlock around the chest waist hips and thighs",
    "small tasteful suction cups visible on several tendrils", "gold trim", "one magenta heart gem at the chest",
    "a second translucent duplicate of the garment is half-knitted behind the first",
    "clear magical replication action", "hollow neck opening", "hollow arm openings", "hollow thigh openings",
    "sensual curving garment silhouette", "simple abstract violet mist background", "no concrete location",
    "one clear focal object centered slightly above the middle", "important forms inside the central safe area",
    "no text", "no letters", "no logo", "no watermark", "no frame", "no UI"
) -join ", "
$negative = @(
    "worst quality", "low quality", "lowres", "blurry", "rough sketch", "flat vector icon", "minimalist logo",
    "human", "woman", "girl", "person", "body inside", "skin", "head", "face", "mannequin", "human hands",
    "human fingers", "solid latex catsuit", "ordinary clothing", "metal armor", "robot", "three garments", "crowd",
    "extra limbs", "detached sleeves", "white outline", "sticker outline", "photorealistic", "3d render",
    "text", "letters", "logo", "watermark", "frame", "border", "UI", "concrete room", "floor", "horizon"
) -join ", "

$specs = @{
    1 = @{ denoise = 0.28; seedKey = "tentacle-armor-v11-hifi-a"; extra = "front garment stays opaque and coherent; rear copy is a faint violet wireframe of tentacles" }
    2 = @{ denoise = 0.32; seedKey = "tentacle-armor-v11-hifi-b"; extra = "three-quarter view; a looping tendril visibly knits the rear duplicate from the waist upward" }
    3 = @{ denoise = 0.36; seedKey = "tentacle-armor-v11-hifi-c"; extra = "many distinct flexible tendrils form layered armor plates without becoming human limbs" }
    4 = @{ denoise = 0.40; seedKey = "tentacle-armor-v11-hifi-d"; extra = "strongest organic tentacle construction, translucent duplicate clearly separated behind and to one side" }
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [int64]([BitConverter]::ToUInt64($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0) -band 0x001FFFFFFFFFFFFF) }
    finally { $sha.Dispose() }
}

function Upload([string]$path, [string]$subfolder, [string]$name) {
    $form = @{ image = Get-Item -LiteralPath $path; subfolder = $subfolder; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-Graph([int]$variant, $spec, [int64]$seed) {
    $positive = "$positiveBase, $($spec.extra)"
    [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("1", 1); stop_at_clip_layer = -2 } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = $positive } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = $negative } }
        "5" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
        "6" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
        "7" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV11/style/065_ReflectiveBarrier_反射屏障.png" } }
        "8" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("1", 0); ipadapter = @("5", 0); image = @("7", 0); weight = 0.25
            weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.58
            embeds_scaling = "V only"; clip_vision = @("6", 0)
        } }
        "9" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV11/base/078_TentacleArmor_hifi_v4_01.png" } }
        "10" = @{ class_type = "ImageScale"; inputs = @{ image = @("9", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "11" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("10", 0); vae = @("1", 2) } }
        "12" = @{ class_type = "KSampler"; inputs = @{
            model = @("8", 0); seed = $seed; steps = 36; cfg = 5.6; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("3", 0); negative = @("4", 0); latent_image = @("11", 0); denoise = [double]$spec.denoise
        } }
        "13" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("12", 0); vae = @("1", 2) } }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV11/078_TentacleArmor_LOCAL_COMFYUI_v11_{0:D2}" -f $variant); images = @("13", 0) } }
    }
}

function Invoke-Graph($graph, [string]$label) {
    $body = @{ prompt = $graph; client_id = [guid]::NewGuid().ToString() } | ConvertTo-Json -Depth 35 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected $label`: $($response.node_errors | ConvertTo-Json -Depth 10 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  $label -> $promptId"
    $deadline = (Get-Date).AddMinutes(15)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $root = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 15
        $property = $root.PSObject.Properties[$promptId]
        if ($null -eq $property) { continue }
        $images = @($property.Value.outputs."40".images)
        if ($images.Count -eq 0) { throw "ComfyUI completed without output for $label" }
        return $images[0]
    }
    throw "Timed out waiting for $label"
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Upload $basePath "MaidenSuccubus/EroticLocalV11/base" "078_TentacleArmor_hifi_v4_01.png"
Upload $stylePath "MaidenSuccubus/EroticLocalV11/style" "065_ReflectiveBarrier_反射屏障.png"

$jobs = @()
foreach ($variant in $Variants) {
    if (-not $specs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $specs[$variant]
    $seed = Get-StableSeed "$($spec.seedKey):20260919"
    $targetName = "078_TentacleArmor_淫触魔衣_LOCAL_COMFYUI_v11_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $jobs += [ordered]@{ index = 78; class = "TentacleArmor"; title = "淫触魔衣"; variant = $variant; seed = $seed; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [78:$variant] 淫触魔衣"; continue }
    $graph = New-Graph $variant $spec $seed
    $graph | ConvertTo-Json -Depth 35 | Set-Content -LiteralPath (Join-Path $workflowDir ("078_TentacleArmor_v11_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $image = Invoke-Graph $graph "[78:$variant] tentacle-armor-img2img"
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE [78:$variant] 淫触魔衣"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o"); pipeline = "LOCAL_COMFYUI_V11_TENTACLE_ARMOR"; outputSize = "1000x760"
    checkpoint = $checkpoint; styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    base = "EroticRedrawV4_20260914/078_TentacleArmor_hifi_v4_01.png"; jobs = $jobs
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v11.json") -Encoding utf8

Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V11_TENTACLE_ARMOR selected=$($Variants.Count)"
