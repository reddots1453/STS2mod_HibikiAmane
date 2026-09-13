param(
    [int[]]$Variants = @(1, 2, 3, 4),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\BindingInsight_LocalNSFW_20260913"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, highres, polished anime game illustration, clean delicate colored lineart, refined soft cel shading, smooth two-step shadows, restrained highlights, coherent adult anatomy, detailed face, detailed hands"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, white blue and gold magical girl outfit, cele_leotard, cele_overskirt, cele_white gloves, cele_elbow_gloves, cele_bow, cele_brooch, cele_thigh boots"
$common = "(shibari:1.45), (rope bondage:1.5), (bound wrists behind back:1.55), adult consensual erotic fantasy bondage, natural tan hemp rope, tight rope harness crossing around her chest, waist, upper arms and thighs, clearly visible rope fibers and several functional knots, breast bondage over the costume, thigh bondage, clothing disheveled and partially torn but characteristic white blue gold costume remains recognizable, deep cleavage, flushed cheeks, beads of sweat, half-lidded eyes, parted lips, embarrassed arousal, body straining against the rope, one wrist knot beginning to loosen and emit a restrained cyan-gold energy spark to imply learning how to escape, subject and decisive action centered slightly above the middle, 1000 by 760 landscape composition, abstract dark indigo and muted magenta gradient background, soft bokeh only, no concrete location, all important anatomy inside the central eighty percent safe area, no text, no border, no interface, no card, no card-shaped object"
$negative = "worst quality, low quality, lowres, blurry, jpeg artifacts, rough sketch, painterly impasto, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple people, explicit penetration, visible genitals, censorship bar, mosaic, free hands, arms in front, unbound wrists, beads, pearls, bead chain, jewelry chain, metal chain, handcuffs, bracelets, extra arms, extra legs, extra fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicated body, rope replacing limbs, rope through body, collar covering face, concrete room, dungeon cell, furniture, architecture, landscape, text, logo, watermark, border, interface, playing card, tarot card"

$variantSpecs = @{
    1 = @{ seed = 486341902731; pose = "front three-quarter medium-full shot, kneeling with thighs apart, both forearms drawn behind her torso, shoulders pulled back by the rope harness, face turned toward the viewer" }
    2 = @{ seed = 728105624913; pose = "high-angle view looking down, kneeling and leaning forward while bound arms remain behind her back, rope pattern and flushed expression clearly visible" }
    3 = @{ seed = 951773406284; pose = "rear three-quarter view, wrists bound behind her waist, upper body twisting to look back over one shoulder, rope harness emphasizing the strained escape motion" }
    4 = @{ seed = 317845290672; pose = "low oblique side view, seated with one knee raised and back arched against the rope tension, one loosened wrist loop glowing as she discovers the escape technique" }
}

function New-PromptGraph([int]$variant, [hashtable]$spec) {
    $positive = @($quality, $identity, [string]$spec.pose, $common) -join ", "
    $prefix = "019_BindingInsight_local_nsfw_v{0:D2}" -f $variant
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
    $target = Join-Path $targetDir ("019_BindingInsight_local_nsfw_v{0:D2}.png" -f $variant)
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
