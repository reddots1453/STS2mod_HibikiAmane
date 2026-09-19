param(
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV8_20260919\050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png"
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV13_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$target = Join-Path $targetDir "050_EcstasyDew_销魂露_粉色液体_LOCAL_COMFYUI_v13_04.png"
$maskTarget = Join-Path $targetDir "050_EcstasyDew_粉色液体_mask_v13_04.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$form = @{ image = Get-Item -LiteralPath $source; subfolder = "MaidenSuccubus/EroticLocalV13"; type = "input"; overwrite = "true" }
[void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)

# Keep the existing composition exactly. An ellipse follows the inner flask wall;
# intersecting it with a lower rectangle creates a physically level liquid surface
# while excluding the neck hardware. A soft edge preserves the glass highlight.
$graph = [ordered]@{
    "1" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV13/050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png" } }
    "2" = @{ class_type = "ImageCrop"; inputs = @{ image = @("1", 0); width = 804; height = 760; x = 98; y = 0 } }
    "3" = @{ class_type = "ImageScale"; inputs = @{ image = @("2", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    "4" = @{ class_type = "CreateShapeMask"; inputs = @{ shape = "circle"; frames = 1; location_x = 397; location_y = 520; grow = 0; frame_width = 1000; frame_height = 760; shape_width = 560; shape_height = 350 } }
    "5" = @{ class_type = "CreateShapeMask"; inputs = @{ shape = "square"; frames = 1; location_x = 397; location_y = 550; grow = 0; frame_width = 1000; frame_height = 760; shape_width = 600; shape_height = 300 } }
    "6" = @{ class_type = "BitwiseAndMask"; inputs = @{ mask1 = @("4", 0); mask2 = @("5", 0) } }
    "7" = @{ class_type = "FeatherMask"; inputs = @{ mask = @("6", 0); left = 8; top = 5; right = 8; bottom = 8 } }
    "8" = @{ class_type = "Mask Smooth Region"; inputs = @{ masks = @("7", 0); sigma = 2.0 } }
    "9" = @{ class_type = "EmptyImage"; inputs = @{ width = 1000; height = 760; batch_size = 1; color = 16030657 } }
    "10" = @{ class_type = "Image Blending Mode"; inputs = @{ image_a = @("3", 0); image_b = @("9", 0); mode = "color"; blend_percentage = 0.82 } }
    "11" = @{ class_type = "ImageCompositeMasked"; inputs = @{ destination = @("3", 0); source = @("10", 0); x = 0; y = 0; resize_source = $false; mask = @("8", 0) } }
    "12" = @{ class_type = "MaskToImage"; inputs = @{ mask = @("8", 0) } }
    "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticLocalV13/050_EcstasyDew_PinkLiquid_LOCAL_COMFYUI_v13_04"; images = @("11", 0) } }
    "41" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticLocalV13/050_EcstasyDew_PinkLiquid_mask_v13_04"; images = @("12", 0) } }
}

$workflowPath = Join-Path $workflowDir "050_EcstasyDew_pink_liquid_v13_04_api.json"
$graph | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $workflowPath -Encoding utf8
$body = @{ prompt = $graph; client_id = [guid]::NewGuid().ToString() } | ConvertTo-Json -Depth 20 -Compress
$response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw ($response.node_errors | ConvertTo-Json -Depth 10) }
$promptId = [string]$response.prompt_id
$deadline = (Get-Date).AddMinutes(5)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 15
    $entry = $history.PSObject.Properties[$promptId]
    if ($null -eq $entry) { continue }
    foreach ($pair in @(@("40", $target), @("41", $maskTarget))) {
        $image = @($entry.Value.outputs.($pair[0]).images)[0]
        if ($null -eq $image) { throw "ComfyUI completed without node $($pair[0]) output" }
        $output = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
        Copy-Item -LiteralPath $output -Destination $pair[1] -Force
    }
    [ordered]@{
        generatedAt = (Get-Date).ToString("o")
        pipeline = "LOCAL_COMFYUI_V13_LEVEL_LIQUID_SURFACE_MASKED_COLOR_REPLACEMENT"
        source = "EroticLocalV8_20260919/050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png"
        edit = "Change only the potion liquid from ivory white to pink; preserve composition, glass, metal trim, vapor and background."
        mask = @{ shape = "ellipse_and_lower_rectangle_intersection"; ellipseCenter = @(397, 520); ellipseSize = @(560, 350); surfaceY = 400; feather = 8 }
        pink = "#F49BC1"
        outputSize = "1000x760"
        status = "awaiting-player-review"
        output = (Split-Path -Leaf $target)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v13.json") -Encoding utf8
    Write-Host "DONE 050 EcstasyDew pink-liquid candidate generated by local ComfyUI"
    exit 0
}
throw "Timed out waiting for ComfyUI"
