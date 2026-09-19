param(
    [string]$ModRoot = (Split-Path -Parent $PSScriptRoot)
)

$auditPath = Join-Path $ModRoot '.review\card_localization_audit.json'
$formalManifestPath = Join-Path $ModRoot '图片素材\完成版卡图\manifest.json'
$outputDir = Join-Path $ModRoot '图片素材\卡图生成_内置生图_20260920'
$outputPath = Join-Path $outputDir 'inventory.json'

$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
$formalTypes = (Get-Content -LiteralPath $formalManifestPath -Raw | ConvertFrom-Json).items.class

# These cards intrinsically require erotic depiction and belong to the separate local-model pipeline.
$eroticTypes = @(
    'ChangePanties', 'PleasureDrowning', 'Exhibitionist', 'MasochisticTrance',
    'MasochisticGirl', 'ExposePlay', 'DesireWhip', 'PleasureGarden',
    'SemenAppetite', 'BiteInvader', 'TentacleArmor', 'ArousalStatus',
    'NakedDesireStatus', 'LewdMarkMinorCurse', 'LewdMarkSpreadCurse',
    'LewdMarkCompleteCurse', 'TransparentOutfitCurse', 'GagCurse',
    'SemenCurse', 'FoulSlimeCurse', 'AphrodisiacCurse', 'SporeMucusCurse',
    'ParalyticSlimeCurse', 'CorrosiveSlimeCurse', 'InsectEggCurse',
    'ParasiticEggCurse', 'InkFluidCurse', 'ScorchingFluidCurse',
    'EctoplasmResidueCurse', 'MagicResidueCurse', 'VineSeedCurse',
    'SludgeSemenCurse', 'DeepSeaSlimeCurse', 'ExperimentalLiquidCurse',
    'RoyalEssenceCurse'
)

$items = @(
    $audit |
        Where-Object {
            $_.status -eq 'DESIGN' -and
            $formalTypes -notcontains $_.type -and
            $eroticTypes -notcontains $_.type
        } |
        ForEach-Object {
            [pscustomobject]@{
                line = [int]$_.design.line
                type = $_.type
                title = $_.title
                cardType = $_.design.type
                rarity = $_.design.rarity
                effect = $_.designEffect
                source = 'card_localization_audit'
            }
        }
)

# Explicit DesignDoc entries not captured as complete DESIGN records by the audit parser.
$items += @(
    [pscustomobject]@{ line = 1119; type = 'IceMist'; title = '冰雾'; cardType = '技能牌'; rarity = '普通'; effect = '0费 获得2/3点临时敏捷。保留。消耗。'; source = 'DesignDoc-derived' }
    [pscustomobject]@{ line = 1448; type = 'MaidenStrike'; title = '打击'; cardType = '攻击牌'; rarity = '基础'; effect = '1费 造成6/9点伤害。'; source = 'DesignDoc-base-card' }
    [pscustomobject]@{ line = 1449; type = 'MaidenDefend'; title = '防御'; cardType = '技能牌'; rarity = '基础'; effect = '1费 获得5/8点格挡。'; source = 'DesignDoc-base-card' }
    [pscustomobject]@{ line = 1525; type = 'CounterBarrierII'; title = '功性魔防壁II'; cardType = '能力牌'; rarity = '稀有'; effect = '1费 获得3荆棘。将1张功性魔防壁III放入弃牌堆。'; source = 'DesignDoc-derived' }
    [pscustomobject]@{ line = 1701; type = 'DrowsyStatus'; title = '困了'; cardType = '状态牌'; rarity = '状态'; effect = '无法被打出。保留。'; source = 'DesignDoc-derived' }
    [pscustomobject]@{ line = 2018; type = 'CalmMind'; title = '心神宁静'; cardType = '技能牌'; rarity = '无色罕见'; effect = '0费 保留。如果你的手牌有6张或更多，则抽2/3张牌并获得2/3费。'; source = 'DesignDoc-event-derived' }
)

$ordered = @($items | Sort-Object line, type)
$manifestItems = for ($i = 0; $i -lt $ordered.Count; $i++) {
    $item = $ordered[$i]
    [ordered]@{
        sequence = $i + 1
        batch = [math]::Floor($i / 10) + 1
        line = $item.line
        type = $item.type
        title = $item.title
        cardType = $item.cardType
        rarity = $item.rarity
        effect = $item.effect
        source = $item.source
        status = 'pending'
        output = $null
        review = $null
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    generatedAt = '2026-09-20'
    sourceDocument = '../../DesignDoc.md'
    generator = 'built-in imagegen only'
    styleReference = '../完成版卡图/065_ReflectiveBarrier_反射屏障.png'
    outputSize = '1000x760'
    batchSize = 10
    formalCardCountAtStart = $formalTypes.Count
    excludedEroticCount = $eroticTypes.Count
    targetCount = $manifestItems.Count
    acceptanceRule = 'Every batch of ten must be visually inspected; failed items are regenerated. Outputs remain candidates until the player explicitly accepts them.'
    items = $manifestItems
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outputPath -Encoding utf8
Write-Output $outputPath
Write-Output "TARGET_COUNT=$($manifestItems.Count)"
