param(
    [int[]]$Indexes = @(),
    [switch]$QueueOnly,
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图"
$manifestPath = Join-Path $targetDir "first_batch_manifest.json"
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
$cards = @($manifest.cards)

if ($Indexes.Count -gt 0) {
    $wanted = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($index in $Indexes) { [void]$wanted.Add($index) }
    $cards = @($cards | Where-Object { $wanted.Contains([int]$_.index) })
    $missing = @($Indexes | Where-Object { $_ -notin @($cards.index) })
    if ($missing.Count -gt 0) {
        throw "Manifest does not contain indexes: $($missing -join ', ')"
    }
}

$checkpoint = [string]$manifest.output.model
$lora = [string]$manifest.output.lora
$width = [int]$manifest.output.width
$height = [int]$manifest.output.height
$outputRoot = Join-Path $ComfyRoot "output"
$clientId = [guid]::NewGuid().ToString()

$commonPositive = @(
    "masterpiece, best quality, amazing quality, very aesthetic, highres"
    "polished premium anime fantasy action illustration, clean delicate lineart, refined detailed cel shading, rich controlled colors, cinematic volumetric lighting"
    "celesphonia, 1girl, solo, adult woman, adult magical heroine, blue eyes, blonde hair, pink gradient hair, long hair, cele_ribbon, one_side_up"
    "detailed expressive face, detailed hands, dynamic readable silhouette, clearly visible spell or attack effect, narrative action scene rather than a portrait"
    "landscape composition, subject and face centered in the upper-middle, primary action and important props inside the central 80 percent safe area, generous expendable edge atmosphere"
    "no card frame, no border, no text"
) -join ", "

$commonNegative = @(
    "bad quality, worst quality, worst detail, lowres, blurry, unfinished, messy lineart, thick crude outline, flat shading, muddy colors, oversaturated"
    "3d render, photorealistic, bad anatomy, bad hands, extra fingers, missing fingers, fused fingers, extra limbs, duplicate body, multiple girls"
    "child, loli, underage, male, old woman, text, letters, logo, watermark, signature, subtitles, caption, dialogue box, speech bubble, game user interface, visual novel screenshot, manga panel, comic panel, split screen, card frame, border, decorative frame, ornamental border"
    "subject at edge, face near edge, cropped head, cropped face, important object cut off, empty center, simple portrait, character sheet, pinup, idle pose, static pose, posing for camera, action effect missing"
    "nipples, exposed genitals, explicit sexual act"
) -join ", "

function Get-RoutePrompt([string]$route, [string]$intensity) {
    $routePrompt = switch ($route) {
        "圣洁" { "cele_elbow_gloves, cele_leotard, cele_overskirt, cele_white gloves, cele_bow, cele_brooch, covered navel, cele_thigh boots, white thigh boots, pristine intact white and cyan magical girl outfit, white-gold-blue holy palette, dignified heroic mood" }
        "堕落" { "cele_elbow_gloves, cele_leotard, cele_overskirt, cele_gloves, cele_bow, cele_brooch, cele_thigh boots, black purple and magenta corrupted magical girl outfit, alluring dark fantasy details, violet miasma, dramatic battle wear" }
        default { "cele_elbow_gloves, cele_leotard, cele_overskirt, cele_white gloves, cele_bow, cele_brooch, covered navel, cele_thigh boots, white thigh boots, white and cyan magical girl outfit with balanced blue-gold accents, adventurous fantasy dungeon mood" }
    }

    $intensityPrompt = switch ($intensity) {
        "strong-nonexplicit" { "strong adult sensuality, provocative pose, flushed expression, revealing torn but strategically covering outfit, erotic tension, non-explicit" }
        "suggestive" { "tasteful adult sensuality, mildly revealing battle-damaged outfit, confident body language, non-explicit" }
        default { "tasteful mildly sensual adult heroine, the same erotic intensity as a polished fantasy barrier card, non-explicit" }
    }

    return "$routePrompt, $intensityPrompt"
}

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = $sha.ComputeHash($bytes)
        $seed = [BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF
        return [int64]$seed
    }
    finally {
        $sha.Dispose()
    }
}

function New-PromptGraph($card) {
    $positive = "((the scene must visibly and unambiguously depict this exact action: $($card.concept)):1.35), $commonPositive, $(Get-RoutePrompt $card.route $card.intensity)"
    $seed = Get-StableSeed "$($manifest.designDocSha256):$($card.index):$($card.class):v01"
    $prefix = "MaidenSuccubus/CardArt/FirstBatch/{0:D3}_{1}_{2}_v01" -f [int]$card.index, $card.class, $card.title

    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $checkpoint } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = $lora; strength_model = 0.8; strength_clip = 0.8 } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 1); text = $positive } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 1); text = $commonNegative } }
        "5" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = $width; height = $height; batch_size = 1 } }
        "6" = @{ class_type = "KSampler"; inputs = @{ model = @("2", 0); seed = $seed; steps = 32; cfg = 6.25; sampler_name = "euler_ancestral"; scheduler = "normal"; positive = @("3", 0); negative = @("4", 0); latent_image = @("5", 0); denoise = 1.0 } }
        "7" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("6", 0); vae = @("1", 2) } }
        "8" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = $prefix; images = @("7", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId"
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

if (-not (Test-Path -LiteralPath $outputRoot)) {
    throw "ComfyUI output directory not found: $outputRoot"
}

$queue = @()
foreach ($card in $cards) {
    $targetName = "{0:D3}_{1}_{2}_v01.png" -f [int]$card.index, $card.class, $card.title
    $targetPath = Join-Path $targetDir $targetName
    if ((Test-Path -LiteralPath $targetPath) -and -not $Force) {
        Write-Host "SKIP $($card.index)/$($manifest.cards.Count) $($card.title): target already exists"
        continue
    }

    $graph = New-PromptGraph $card
    $body = @{ prompt = $graph; client_id = $clientId } | ConvertTo-Json -Depth 20 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) {
        throw "ComfyUI rejected $($card.title): $($response.node_errors | ConvertTo-Json -Depth 10 -Compress)"
    }
    $queue += [pscustomobject]@{ Card = $card; PromptId = [string]$response.prompt_id; TargetPath = $targetPath }
    Write-Host "QUEUED $($card.index)/$($manifest.cards.Count) $($card.title) -> $($response.prompt_id)"
}

if ($QueueOnly) {
    $queue | Select-Object @{n="index";e={$_.Card.index}}, @{n="title";e={$_.Card.title}}, PromptId, TargetPath
    exit 0
}

$pending = [System.Collections.ArrayList]::new()
foreach ($item in $queue) { [void]$pending.Add($item) }
$completed = @()

while ($pending.Count -gt 0) {
    for ($i = $pending.Count - 1; $i -ge 0; $i--) {
        $item = $pending[$i]
        $history = Get-CompletedHistory $item.PromptId
        if ($null -eq $history) { continue }

        $images = @($history.outputs."8".images)
        if ($images.Count -eq 0) {
            $status = $history.status | ConvertTo-Json -Depth 8 -Compress
            throw "ComfyUI completed $($item.Card.title) without a SaveImage output: $status"
        }

        $image = $images[0]
        $sourcePath = Join-Path $outputRoot ([string]$image.subfolder)
        $sourcePath = Join-Path $sourcePath ([string]$image.filename)
        Copy-Item -LiteralPath $sourcePath -Destination $item.TargetPath -Force

        $info = Get-Item -LiteralPath $item.TargetPath
        $completed += [pscustomobject]@{
            index = [int]$item.Card.index
            route = [string]$item.Card.route
            class = [string]$item.Card.class
            title = [string]$item.Card.title
            promptId = $item.PromptId
            file = $info.Name
            bytes = $info.Length
        }
        $pending.RemoveAt($i)
        Write-Host "DONE $($item.Card.index)/$($manifest.cards.Count) $($item.Card.title) -> $($info.Name) [$($pending.Count) pending]"
    }

    if ($pending.Count -gt 0) { Start-Sleep -Seconds 2 }
}

$statusRows = foreach ($card in $manifest.cards) {
    $name = "{0:D3}_{1}_{2}_v01.png" -f [int]$card.index, $card.class, $card.title
    $path = Join-Path $targetDir $name
    $file = Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
    [pscustomobject]@{
        index = [int]$card.index
        route = [string]$card.route
        class = [string]$card.class
        title = [string]$card.title
        file = $name
        exists = $null -ne $file
        bytes = if ($null -ne $file) { $file.Length } else { 0 }
    }
}
$logPath = Join-Path $targetDir "generation_status.json"
$statusRows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $logPath -Encoding utf8
$existingCount = @($statusRows | Where-Object exists).Count
Write-Host "COMPLETE generated_this_run=$($completed.Count) existing_total=$existingCount/$($manifest.cards.Count) status=$logPath"
