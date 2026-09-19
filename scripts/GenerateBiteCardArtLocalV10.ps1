param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV10_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$basePath = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV5_20260914\076_BiteInvader_redraw_v5_03.png"
$stylePath = Join-Path $modRoot "图片素材\完成版卡图\065_ReflectiveBarrier_反射屏障.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$lora = "celesphonia-1.8.safetensors"
$ipAdapter = "ip-adapter-plus_sdxl_vit-h.safetensors"
$clipVision = "model.safetensors"
$positiveBase = @(
    "masterpiece", "best quality", "amazing quality", "very aesthetic", "newest", "highres",
    "premium anime game illustration", "clean thin colored lineart", "controlled two-step cel shading",
    "crisp facial details", "coherent adult anatomy", "celesphonia", "hibiki amane", "one adult woman",
    "aqua eyes", "long blonde hair fading to pale pink", "simple blue side-ponytail ribbon",
    "extreme close-up of her flushed determined face", "one single thick pink-purple monster tentacle enters from the right",
    "her visible upper and lower front teeth clamp firmly onto the middle of the tentacle",
    "the tentacle is visibly pinched and indented exactly between her teeth", "tense jaw", "defiant focused eyes",
    "small compact cyan stun sparks exactly at the bite point", "the bite contact is the unmistakable focal action",
    "face and bite inside the central safe area", "simple abstract cyan-violet mist background",
    "no concrete location", "no text", "no logo", "no watermark", "no frame", "no UI"
) -join ", "
$negative = @(
    "worst quality", "low quality", "lowres", "blurry face", "deformed face", "asymmetrical eyes", "cross-eyed",
    "open mouth without contact", "closed lips", "tongue licking", "tongue touching tentacle",
    "tentacle resting on lips", "tentacle floating in front of mouth", "tentacle penetrating mouth",
    "extra tentacles", "extra mouth", "duplicate face", "mask over face", "pink muzzle", "split face", "seam across cheek",
    "child", "teenager", "young-looking", "loli", "hands", "fingers", "text", "letters", "logo", "watermark",
    "frame", "border", "UI", "photorealistic", "3d render", "thick black outline", "overly glossy"
) -join ", "

$specs = @{
    1 = @{ denoise = 0.34; seedKey = "bite-v10-a"; extra = "compact natural bite, one small upper canine and lower incisors visible" }
    2 = @{ denoise = 0.38; seedKey = "bite-v10-b"; extra = "clear front-teeth compression, tentacle bends sharply away from her bite" }
    3 = @{ denoise = 0.42; seedKey = "bite-v10-c"; extra = "side three-quarter jaw emphasis, crisp teeth on both sides of the compressed tentacle" }
    4 = @{ denoise = 0.46; seedKey = "bite-v10-d"; extra = "strong recoil curve, two tiny cyan impact sparks, no tongue" }
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
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $lora; strength_model = 0.62; strength_clip = 0.62 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "IPAdapterModelLoader"; inputs = @{ ipadapter_file = $ipAdapter } }
        "7" = @{ class_type = "CLIPVisionLoader"; inputs = @{ clip_name = $clipVision } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV10/style/065_ReflectiveBarrier_反射屏障.png" } }
        "9" = @{ class_type = "IPAdapterAdvanced"; inputs = @{
            model = @("2", 0); ipadapter = @("6", 0); image = @("8", 0); weight = 0.16
            weight_type = "style transfer precise"; combine_embeds = "average"; start_at = 0.0; end_at = 0.55
            embeds_scaling = "V only"; clip_vision = @("7", 0)
        } }
        "10" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV10/base/076_BiteInvader_redraw_v5_03.png" } }
        "11" = @{ class_type = "ImageScale"; inputs = @{ image = @("10", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "12" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("11", 0); vae = @("1", 2) } }
        "13" = @{ class_type = "KSampler"; inputs = @{
            model = @("9", 0); seed = $seed; steps = 36; cfg = 5.4; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("4", 0); negative = @("5", 0); latent_image = @("12", 0); denoise = [double]$spec.denoise
        } }
        "14" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("13", 0); vae = @("1", 2) } }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV10/076_BiteInvader_LOCAL_COMFYUI_v10_{0:D2}" -f $variant); images = @("14", 0) } }
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
Upload $basePath "MaidenSuccubus/EroticLocalV10/base" "076_BiteInvader_redraw_v5_03.png"
Upload $stylePath "MaidenSuccubus/EroticLocalV10/style" "065_ReflectiveBarrier_反射屏障.png"

$jobs = @()
foreach ($variant in $Variants) {
    if (-not $specs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $specs[$variant]
    $seed = Get-StableSeed "$($spec.seedKey):20260919"
    $targetName = "076_BiteInvader_咬_LOCAL_COMFYUI_v10_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $jobs += [ordered]@{ index = 76; class = "BiteInvader"; title = "咬"; variant = $variant; seed = $seed; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [76:$variant] 咬"; continue }
    $graph = New-Graph $variant $spec $seed
    $graph | ConvertTo-Json -Depth 35 | Set-Content -LiteralPath (Join-Path $workflowDir ("076_BiteInvader_v10_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $image = Invoke-Graph $graph "[76:$variant] bite-full-img2img"
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE [76:$variant] 咬"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o"); pipeline = "LOCAL_COMFYUI_V10_FULL_FRAME_BITE"; outputSize = "1000x760"
    checkpoint = $checkpoint; lora = $lora; styleReference = "完成版卡图/065_ReflectiveBarrier_反射屏障.png"
    base = "EroticRedrawV5_20260914/076_BiteInvader_redraw_v5_03.png"; jobs = $jobs
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v10.json") -Encoding utf8

Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V10_FULL_FRAME_BITE selected=$($Variants.Count)"
