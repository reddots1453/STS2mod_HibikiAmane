param(
    [Parameter(Mandatory = $true)][int]$Sequence,
    [Parameter(Mandatory = $true)][ValidateSet('pending','generated','review-passed','review-failed','blocked')][string]$Status,
    [string]$Output,
    [string]$Review,
    [string]$ModRoot = (Split-Path -Parent $PSScriptRoot)
)

$manifestPath = Join-Path $ModRoot '图片素材\卡图生成_内置生图_20260920\inventory.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$item = $manifest.items | Where-Object { [int]$_.sequence -eq $Sequence } | Select-Object -First 1
if ($null -eq $item) {
    throw "Unknown card-art inventory sequence: $Sequence"
}

$item.status = $Status
$item.output = if ([string]::IsNullOrWhiteSpace($Output)) { $null } else { $Output }
$item.review = if ([string]::IsNullOrWhiteSpace($Review)) { $null } else { $Review }
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Output "$Sequence`t$($item.type)`t$Status`t$($item.output)"
