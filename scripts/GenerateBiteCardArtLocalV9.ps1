param(
    [int[]]$Variants = @(1, 2, 3),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV9_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$baseDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticRedrawV2_20260913\composition_bases"
$basePath = Join-Path $baseDir "076_BiteBarrierStyle_base.png"
$maskPath = Join-Path $baseDir "076_BiteMouth_mask.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$checkpoint = "waiNSFWIllustrious_v140.safetensors"
$lora = "celesphonia-1.8.safetensors"
$quality = "masterpiece, best quality, amazing quality, very aesthetic, newest, highres, premium anime game illustration, clean thin colored lineart, controlled two-step cel shading, crisp facial details, coherent adult anatomy"
$identity = "celesphonia, hibiki amane, one adult woman, aqua eyes, long blonde hair fading to pale pink, one side ponytail tied with a simple blue ribbon"
$subject = "extreme close-up of her determined flushed face, (upper and lower front teeth visibly clamped around one thick pink-purple monster tentacle:1.6), (clear compressed bite indentation in the tentacle between her teeth:1.5), tentacle enters from the right and recoils away, tense jaw, defiant focused eyes, small cyan stun sparks at the bite, no tongue licking, no tentacle merely resting on lips, no penetration, no second person, no hands, no fingers"
$layout = "1000 by 760 horizontal card illustration, face and bite occupy the central safe area, simple abstract cyan and violet impact mist, no concrete background, no text, no logo, no watermark, no frame, no UI"
$negative = "worst quality, low quality, lowres, blurry face, deformed face, asymmetrical eyes, cross-eyed, open mouth without teeth contact, tongue touching tentacle, tentacle in front of closed lips, tentacle floating near mouth, extra tentacles, extra mouth, duplicate face, child, teenager, young-looking, loli, hands, fingers, text, letters, logo, watermark, frame, border, UI"

$specs = @{
    1 = @{ denoise = 1.0; seedKey = "bite-v9-a-full"; extra = "teeth contact is the unmistakable focal action, compact bite mark" }
    2 = @{ denoise = 1.0; seedKey = "bite-v9-b-full"; extra = "stronger recoil curve and deeper visible teeth compression" }
    3 = @{ denoise = 1.0; seedKey = "bite-v9-c-full"; extra = "side three-quarter jaw emphasis, crisp white teeth on both sides of the tentacle" }
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [int64]([BitConverter]::ToUInt64($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0) -band 0x001FFFFFFFFFFFFF) }
    finally { $sha.Dispose() }
}

function Upload([string]$path, [string]$name) {
    $form = @{ image = Get-Item -LiteralPath $path; subfolder = "MaidenSuccubus/EroticLocalV9"; type = "input"; overwrite = "true" }
    [void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)
}

function New-Graph([int]$variant, $spec, [int64]$seed) {
    $positive = @($quality, $identity, $subject, $spec.extra, $layout) -join ", "
    [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $lora; strength_model = 0.72; strength_clip = 0.72 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV9/076_BiteBarrierStyle_base.png" } }
        "7" = @{ class_type = "ImageScale"; inputs = @{ image = @("6", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "8" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV9/076_BiteMouth_mask.png" } }
        "9" = @{ class_type = "ImageToMask"; inputs = @{ image = @("8", 0); channel = "red" } }
        "10" = @{ class_type = "VAEEncodeForInpaint"; inputs = @{ pixels = @("7", 0); vae = @("1", 2); mask = @("9", 0); grow_mask_by = 8 } }
        "11" = @{ class_type = "KSampler"; inputs = @{
            model = @("2", 0); seed = $seed; steps = 34; cfg = 5.6; sampler_name = "dpmpp_2m_sde_gpu"; scheduler = "karras"
            positive = @("4", 0); negative = @("5", 0); latent_image = @("10", 0); denoise = [double]$spec.denoise
        } }
        "12" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("11", 0); vae = @("1", 2) } }
        "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = ("MaidenSuccubus/CardArt/EroticLocalV9/076_BiteInvader_LOCAL_COMFYUI_v9_{0:D2}" -f $variant); images = @("12", 0) } }
    }
}

function Invoke-Graph($graph, [string]$label) {
    $body = @{ prompt = $graph; client_id = [guid]::NewGuid().ToString() } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected $label`: $($response.node_errors | ConvertTo-Json -Depth 10 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  $label -> $promptId"
    $deadline = (Get-Date).AddMinutes(15)
    $failures = 0
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        try { $root = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 15; $failures = 0 }
        catch { $failures++; if ($failures -ge 3) { throw "ComfyUI stopped responding during $label" }; continue }
        $property = $root.PSObject.Properties[$promptId]
        if ($null -eq $property) { continue }
        $images = @($property.Value.outputs."40".images)
        if ($images.Count -eq 0) { throw "ComfyUI completed without output for $label" }
        return $images[0]
    }
    throw "Timed out waiting for $label"
}

[void](Invoke-RestMethod -Uri "$ComfyUrl/system_stats" -TimeoutSec 5)
Upload $basePath "076_BiteBarrierStyle_base.png"
Upload $maskPath "076_BiteMouth_mask.png"

$jobs = @()
foreach ($variant in $Variants) {
    if (-not $specs.ContainsKey($variant)) { throw "Unknown variant: $variant" }
    $spec = $specs[$variant]
    $seed = Get-StableSeed "$($spec.seedKey):20260919"
    $targetName = "076_BiteInvader_咬_LOCAL_COMFYUI_v9_{0:D2}.png" -f $variant
    $target = Join-Path $targetDir $targetName
    $jobs += [ordered]@{ index = 76; class = "BiteInvader"; title = "咬"; variant = $variant; seed = $seed; denoise = $spec.denoise; output = $targetName }
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [76:$variant] 咬"; continue }
    $graph = New-Graph $variant $spec $seed
    $graph | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $workflowDir ("076_BiteInvader_v9_{0:D2}_api.json" -f $variant)) -Encoding utf8
    $image = Invoke-Graph $graph "[76:$variant] bite-inpaint"
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE [76:$variant] 咬"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o"); pipeline = "LOCAL_COMFYUI_V9_MASKED_BITE_INPAINT"; outputSize = "1000x760"
    checkpoint = $checkpoint; lora = $lora; base = "076_BiteBarrierStyle_base.png"; mask = "076_BiteMouth_mask.png"; jobs = $jobs
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v9.json") -Encoding utf8

Invoke-RestMethod -Method Post -Uri "$ComfyUrl/free" -ContentType "application/json" -Body '{"unload_models":true,"free_memory":true}' | Out-Null
Write-Host "COMPLETE pipeline=LOCAL_COMFYUI_V9_MASKED_BITE_INPAINT selected=$($Variants.Count)"
