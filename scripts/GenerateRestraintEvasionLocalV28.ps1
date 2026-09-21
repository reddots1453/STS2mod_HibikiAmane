param(
    [int[]]$Variants = @(1, 2, 3, 4, 5, 6),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\RestraintEvasion_LocalV28_20260921"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, highres, polished anime game illustration, clean delicate colored lineart, refined soft cel shading, smooth two-step shadows, restrained highlights, coherent adult anatomy, detailed face, detailed hands"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, white blue and gold magical girl outfit, cele_leotard, cele_overskirt, cele_white gloves, cele_elbow_gloves, cele_bow, cele_brooch, cele_thigh boots"
$common = "fast evasive combat maneuver, two luminous violet restraint ropes and one dark tendril sweep through the space she occupied an instant ago, one loosened rope loop visibly sliding off her left wrist, she twists and steps sideways through the opening, a thin curved cyan guard streak along her raised forearm, exact intact white blue and gold magical-girl outfit fully covering her body, determined alert expression, readable action of escaping restraint rather than being bound, tasteful non-explicit scene, subject and decisive action centered slightly above the middle, 1000 by 760 landscape composition, abstract dark indigo and muted cyan gradient background, soft bokeh and restrained motion streaks only, no concrete location, all important anatomy and ropes inside the central eighty percent safe area, no text, no border, no interface, no card, no card-shaped object"
$negative = "worst quality, low quality, lowres, blurry, jpeg artifacts, rough sketch, painterly impasto, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple people, nude, exposed breasts, exposed genitals, upside-down, acrobatic split, butt close-up, fetish close-up, tied body, bound torso, rope harness, rope through body, rope replacing limbs, chain, handcuffs, extra arms, extra legs, extra fingers, six fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicated body, covered face, giant shield hiding character, concrete room, dungeon cell, furniture, architecture, landscape, text, logo, watermark, border, interface, playing card, tarot card"

$variantSpecs = @{
    1 = @{ seed = 411281902731; pose = "dynamic side three-quarter full-body view, low evasive slide to the right, left wrist pulling free from a glowing loop, both hands visible" }
    2 = @{ seed = 411722624913; pose = "rear three-quarter tracking view, face only in profile, torso twisting left as two restraint ropes miss behind her, one palm raised defensively" }
    3 = @{ seed = 411953406284; pose = "high-angle diagonal view, one knee bent in a quick sidestep, loosened wrist loop suspended behind her, focused gaze toward the attacker" }
    4 = @{ seed = 411845290672; pose = "low side angle, powerful pivot under a sweeping restraint rope, hair and skirt panels following the motion, both arms anatomically clear" }
    5 = @{ seed = 411548719203; pose = "front three-quarter medium-full shot, leaning away from two crossing restraint lines while slipping one hand through an opening, calm precise evasion" }
    6 = @{ seed = 411690243851; pose = "over-the-shoulder view from behind, target represented only by a distant shadow, heroine ducks below a rope arc and turns into a guarded stance" }
}

function New-PromptGraph([int]$variant, [hashtable]$spec) {
    $positive = @($quality, $identity, [string]$spec.pose, $common) -join ", "
    $prefix = "041_RestraintEvasion_local_v28_{0:D2}" -f $variant
    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.82; strength_clip = 0.82 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 800; height = 608; batch_size = 1 } }
        "7" = @{ class_type = "KSampler"; inputs = @{ model = @("2", 0); seed = [int64]$spec.seed; steps = 24; cfg = 5.5; sampler_name = "euler_ancestral"; scheduler = "normal"; positive = @("4", 0); negative = @("5", 0); latent_image = @("6", 0); denoise = 1.0 } }
        "8" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("7", 0); vae = @("1", 2) } }
        "9" = @{ class_type = "ImageScale"; inputs = @{ image = @("8", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "10" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/BindingInsightLocal/$prefix"; images = @("9", 0) } }
    }
    return @{ graph = $graph; positive = $positive; prefix = $prefix }
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
    $target = Join-Path $targetDir ("041_RestraintEvasion_拘束回避_LOCAL_v28_{0:D2}.png" -f $variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) {
        Write-Host "SKIP v$variant"
        continue
    }
    $job = New-PromptGraph $variant $spec
    $body = @{ prompt = $job.graph; client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
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
    $images = @($history.outputs."10".images)
    if ($images.Count -eq 0 -or $null -eq $images[0] -or [string]::IsNullOrWhiteSpace([string]$images[0].filename)) {
        throw "ComfyUI completed without output for v$variant"
    }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "DONE v$variant -> $target"
}

Write-Host "COMPLETE"
