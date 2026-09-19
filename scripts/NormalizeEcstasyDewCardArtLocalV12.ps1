param(
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV8_20260919\050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png"
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticLocalV12_20260919"
$workflowDir = Join-Path $targetDir "workflow_api"
$target = Join-Path $targetDir "050_EcstasyDew_销魂露_LOCAL_COMFYUI_v12_01.png"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir, $workflowDir | Out-Null

$form = @{ image = Get-Item -LiteralPath $source; subfolder = "MaidenSuccubus/EroticLocalV12"; type = "input"; overwrite = "true" }
[void](Invoke-RestMethod -Method Post -Uri "$ComfyUrl/upload/image" -Form $form)

$graph = [ordered]@{
    "1" = @{ class_type = "LoadImage"; inputs = @{ image = "MaidenSuccubus/EroticLocalV12/050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png" } }
    "2" = @{ class_type = "ImageCrop"; inputs = @{ image = @("1", 0); width = 804; height = 760; x = 98; y = 0 } }
    "3" = @{ class_type = "ImageScale"; inputs = @{ image = @("2", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
    "40" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticLocalV12/050_EcstasyDew_LOCAL_COMFYUI_v12_01"; images = @("3", 0) } }
}
$graph | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $workflowDir "050_EcstasyDew_v12_01_api.json") -Encoding utf8
$body = @{ prompt = $graph; client_id = [guid]::NewGuid().ToString() } | ConvertTo-Json -Depth 20 -Compress
$response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw ($response.node_errors | ConvertTo-Json -Depth 10) }
$promptId = [string]$response.prompt_id
$deadline = (Get-Date).AddMinutes(3)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 15
    $entry = $history.PSObject.Properties[$promptId]
    if ($null -eq $entry) { continue }
    $image = @($entry.Value.outputs."40".images)[0]
    if ($null -eq $image) { throw "ComfyUI completed without an image" }
    $output = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $output -Destination $target -Force
    [ordered]@{
        generatedAt = (Get-Date).ToString("o")
        pipeline = "LOCAL_COMFYUI_V12_DETERMINISTIC_CROP"
        source = "EroticLocalV8_20260919/050_EcstasyDew_销魂露_LOCAL_COMFYUI_v8_01.png"
        crop = @{ x = 98; y = 0; width = 804; height = 760 }
        outputSize = "1000x760"
        output = (Split-Path -Leaf $target)
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec_v12.json") -Encoding utf8
    Write-Host "DONE 050 EcstasyDew normalized by local ComfyUI"
    exit 0
}
throw "Timed out waiting for ComfyUI"
