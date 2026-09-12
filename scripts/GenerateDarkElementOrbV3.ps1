param(
    [int[]]$Variants = @(),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$root = Join-Path $modRoot "图片素材\第一批卡图V3试制"
$targetDir = Join-Path $root "dark_element_orb"
$configPath = Join-Path $root "dark_element_orb_config.json"
$config = Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json
[void](New-Item -ItemType Directory -Force -Path $targetDir)

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [int64]([BitConverter]::ToUInt64(
            $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)), 0
        ) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function Get-History([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

foreach ($variant in $config.variants) {
    $number = [int]$variant.variant
    if ($Variants.Count -gt 0 -and $Variants -notcontains $number) { continue }

    $destination = Join-Path $targetDir ("049_DarkElement_orb_{0:D2}.png" -f $number)
    if ((Test-Path -LiteralPath $destination) -and -not $Force) {
        Write-Host "SKIP $destination"
        continue
    }

    $output = $config.output
    $positive = @(
        [string]$config.quality,
        [string]$config.common,
        [string]$variant.prompt
    ) -join ", "
    $seed = Get-StableSeed("DarkElement-orb-v$number")

    $graph = [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = [string]$output.checkpoint } }
        "2" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("1", 1); stop_at_clip_layer = [int]$output.clipSkip } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = $positive } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("2", 0); text = [string]$config.negative } }
        "5" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = [int]$output.width; height = [int]$output.height; batch_size = 1 } }
        "6" = @{ class_type = "KSampler"; inputs = @{
            model = @("1", 0)
            seed = $seed
            steps = [int]$output.steps
            cfg = [double]$output.cfg
            sampler_name = [string]$output.sampler
            scheduler = [string]$output.scheduler
            positive = @("3", 0)
            negative = @("4", 0)
            latent_image = @("5", 0)
            denoise = 1.0
        } }
        "7" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("6", 0); vae = @("1", 2) } }
        "8" = @{ class_type = "SaveImage"; inputs = @{
            filename_prefix = "MaidenSuccubus/CardArt/DarkElementOrb/orb_$('{0:D2}' -f $number)"
            images = @("7", 0)
        } }
    }

    $body = @{ prompt = $graph } | ConvertTo-Json -Depth 12 -Compress
    $queued = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    $promptId = [string]$queued.prompt_id
    Write-Host "RUN  orb $number -> $promptId"

    do {
        Start-Sleep -Seconds 2
        $history = Get-History $promptId
    } until ($null -ne $history)

    if ($history.status.status_str -ne "success") {
        throw ($history.status.messages | ConvertTo-Json -Depth 10 -Compress)
    }

    $image = @(
        $history.outputs.PSObject.Properties |
            ForEach-Object { $_.Value.images } |
            Where-Object { $null -ne $_ }
    )[-1]
    $sourceDir = Join-Path $ComfyRoot "output"
    if ($image.subfolder) { $sourceDir = Join-Path $sourceDir ([string]$image.subfolder) }
    Copy-Item -LiteralPath (Join-Path $sourceDir ([string]$image.filename)) -Destination $destination -Force
    Write-Host "DONE orb $number -> $destination"
}
