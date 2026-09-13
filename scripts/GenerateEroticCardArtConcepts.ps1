param(
    [int[]]$Indexes = @(50, 58, 73, 74, 75, 76, 78),
    [int[]]$Variants = @(1, 2),
    [switch]$Force,
    [string]$ComfyUrl = "http://127.0.0.1:8188",
    [string]$ComfyRoot = "D:\Ai\ai_painting\ComfyUI_windows_portable\ComfyUI"
)

$ErrorActionPreference = "Stop"
$modRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $modRoot "图片素材\第一批卡图V3试制\EroticCardConcepts_20260913"
$outputRoot = Join-Path $ComfyRoot "output"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$quality = "masterpiece, best quality, amazing quality, very aesthetic, highres, polished anime game illustration, clean delicate colored lineart, refined soft cel shading, smooth two-step shadows, restrained highlights, coherent adult anatomy, detailed face, detailed hands"
$identity = "celesphonia, hibikiamane, 1girl, solo, adult woman, mature body, aqua eyes, blonde hair with pale pink gradient, long hair, one side ponytail tied with a simple blue ribbon, no hair rings"
$holyOutfit = "white blue and gold magical girl outfit, cele_leotard, cele_overskirt, cele_white gloves, cele_elbow_gloves, cele_bow, cele_brooch, cele_thigh boots"
$corruptOutfit = "black purple and gold corrupted magical girl outfit, black high-cut bodysuit, purple waist corset with gold thorn patterns, black elbow gloves, purple rose ornaments, black thighhigh boots with purple-gold armor, revealing but covered"
$fullDamageOutfit = "exact severely destroyed black purple and gold corrupted magical girl outfit, exposed mature breasts with visible nipples, torn waist cloth exposing abdomen, destroyed crotch panel with bare vulva visible, one torn black thighhigh, purple-gold armored boots, remaining purple waist corset and rose ornament"
$layout = "1000 by 760 landscape card art, subject centered slightly above the middle, all important anatomy and props inside the central eighty percent safe area, simple abstract gradient or soft bokeh background only, no identifiable location, no scenery, no text, no border, no interface, no card, no card-shaped object"
$negative = "worst quality, low quality, lowres, blurry, jpeg artifacts, rough sketch, thick black outline, flat unfinished coloring, photorealistic, 3d, chibi, child, young-looking, loli, schoolgirl, male, multiple heroines, duplicate person, extra arms, extra legs, extra fingers, six fingers, missing fingers, fused fingers, malformed hands, twisted wrists, disconnected limbs, broken anatomy, duplicated breasts, comic, manga, manga panel, comic panel, split panel, speech bubble, dialogue, caption box, decorative frame, concrete room, dungeon, bedroom, street, building, forest, literal garden, landscape, wall, floor, horizon, text, letters, logo, watermark, frame, border, interface, playing card, tarot card, card-shaped panel"

$cards = @(
    [pscustomobject]@{
        index = 50; class = "EcstasyDew"; title = "销魂露"; outfit = $corruptOutfit
        common = "one small transparent round glass potion vial clearly containing glowing pink aphrodisiac dew, adult heroine raises the vial close to her parted lips, two pink desire droplets and a little fragrant mist spiral upward, sensual intoxication, non-explicit"
        v1 = "intimate three-quarter bust portrait, one anatomically correct gloved hand pinching the vial by its neck, half-lidded curious eyes, deep blush"
        v2 = "high-angle close view, heroine looking upward, vial held beneath her lower lip, pink mist curling around throat and collarbone, surprised aroused expression"
        v3 = "holding potion, round potion bottle clearly gripped in one five-fingered hand, potion bottle at mouth, three-quarter close-up, drinking pink aphrodisiac, half-lidded eyes, flushed face"
        v4 = "side-profile bust, licking one glowing pink droplet from the mouth of a small round glass vial, vial and lips both clearly visible, embarrassed aroused expression"
    }
    [pscustomobject]@{
        index = 58; class = "MasochisticTrance"; title = "被虐的恍惚"; outfit = $fullDamageOutfit
        common = "explicit adult erotic battle aftermath, loose black-purple restraint ribbons around wrists and thighs, several glowing red-purple curse marks on skin, every curse mark emits a thin cyan layer that joins into a protective aura, no sexual partner, no penetration"
        v1 = "high-angle three-quarter view, adult heroine kneeling with shoulders pulled back and both bound hands visible behind her, body arched, flushed ecstatic trance, sweat, teary half-lidded eyes, parted lips"
        v2 = "side three-quarter medium shot, adult heroine reclining backward against floating restraint ribbons, one knee raised, cyan shield blooming around her, overwhelmed blissful expression"
        v3 = "bondage, bound arms behind back, kneeling, arched back, wrists tied together, thighs tied, orgasm, teary eyes, drooling, flushed skin, cyan protective aura around the body"
        v4 = "high-angle close-up, rope bondage, wrists bound behind back, body straining against restraints, curse marks glowing on skin, ecstatic expression, thin cyan shield outline"
    }
    [pscustomobject]@{
        index = 73; class = "DesireWhip"; title = "欲望鞭挞"; outfit = $corruptOutfit
        common = "one glowing pink-black energy whip held by the adult heroine, whip striking and stunning one abstract faceless black captor silhouette bearing a simple restraint symbol, sensual confident combat action, no concrete enemy anatomy"
        v1 = "over-the-shoulder rear three-quarter action view, heroine cracking the whip forward in a clear S-curve, her face not visible, dynamic torso twist"
        v2 = "low side angle, heroine lunging with one five-fingered hand controlling the whip and the other open for balance, fierce teasing smile"
        v3 = "from behind, over-the-shoulder action, wielding whip, cracking one long glowing pink-black whip toward a black monster silhouette, whip fully visible in an S curve, motion lines"
        v4 = "side view, dynamic lunge, whipping, one long energy whip striking a restrained enemy silhouette, clear impact spark, fierce teasing grin"
    }
    [pscustomobject]@{
        index = 74; class = "PleasureGarden"; title = "淫乐园"; outfit = $fullDamageOutfit
        common = "explicit adult erotic magical tableau, abstract pink-purple flower-shaped magic and luminous vine motifs radiate from the adult heroine, three distant faceless black enemy silhouettes entranced by desire, symbolic forbidden paradise rather than a literal garden, no penetration"
        v1 = "high top-down view, adult heroine floating on her back at the center of a circular flower-like magic sigil, arms open, blissful inviting expression, elegant radial composition"
        v2 = "centered frontal full-body pose, one leg slightly bent and both hands lifting streams of floral desire magic, confident seductive smile, symmetrical icon-like composition"
        v3 = "top-down view, lying on back, arms open, legs apart, centered inside one pink-purple flower-shaped magic circle, erotic trance, three faceless enemy silhouettes around the outer circle"
        v4 = "floating full body, frontal view, seductive pose, luminous vines and flower petals form a circular aura behind her, enemy silhouettes entranced at the edge"
    }
    [pscustomobject]@{
        index = 75; class = "SemenAppetite"; title = "精液食粮"; outfit = $corruptOutfit
        common = "adult erotic forbidden alchemy, luminous pearly-white essence flows from one dissolving black curse orb and transforms into bright green-gold life energy entering the adult heroine, clear conversion from curse to permanent vitality, no card or card-shaped object"
        v1 = "intimate side-profile close-up, pearly essence touching tongue and lips before turning into green life sparks, flushed eager expression, one anatomically correct hand holding the dissolving orb"
        v2 = "high-angle three-quarter medium shot, pearly ribbon of essence entering a glowing sigil over her abdomen while the dark curse shell breaks apart, satisfied relieved expression"
        v3 = "side profile close-up, mouth open, licking luminous pearly-white fluid from one small black curse orb held in a five-fingered hand, the white fluid changes into green healing sparks at her lips"
        v4 = "three-quarter bust, swallowing a ribbon of luminous pearly-white essence, broken black curse orb in one hand, green-gold vitality aura growing around chest and abdomen"
    }
    [pscustomobject]@{
        index = 76; class = "BiteInvader"; title = "咬"; outfit = $fullDamageOutfit
        common = "adult heroine fiercely bites completely through one thick black-purple restraining tentacle beside her shoulder, her teeth visibly clamp the tentacle, the severed tentacle recoils, seven small violet weakening seals appear along it, erotic peril transformed into aggressive escape, no gore"
        v1 = "tight three-quarter portrait, bite action unmistakable, both shoulders anatomically connected, one five-fingered hand gripping the tentacle, angry defiant eyes"
        v2 = "rear three-quarter medium view, heroine turns her head over her shoulder to bite the tentacle pulling across her upper arm, face in clear profile, dynamic struggle"
        v3 = "biting tentacle, thick black-purple tentacle clenched between teeth, teeth visibly piercing and severing the tentacle, side-profile close-up, one hand gripping tentacle, angry defiant eyes"
        v4 = "rear three-quarter view, looking over shoulder, biting a restraining tentacle wrapped around upper arm, mouth and tentacle contact clearly visible, dynamic struggle"
    }
    [pscustomobject]@{
        index = 78; class = "TentacleArmor"; title = "淫触魔衣"; outfit = ""
        common = "adult heroine wearing living black-purple tentacle armor, elegant glossy tendrils weave themselves into a revealing but strategically covered magical bodysuit with purple-gold trim, a second layer of tendrils forms behind her to symbolize the copied armor, sensual transformation, no penetration, no partner"
        v1 = "front three-quarter full-body view, two five-fingered hands held away from the body while living armor wraps around waist chest and thighs, startled aroused expression"
        v2 = "rear three-quarter view, heroine glancing over her shoulder while living armor closes across her back and hips, confident seductive expression, clear silhouette"
        v3 = "tentacles, tentacle wrap, living tentacle armor, black-purple tendrils tightly woven into a revealing bodysuit over chest waist hips and thighs, front three-quarter full body, arms held outward"
        v4 = "from behind, tentacle dress, living black-purple tendrils wrapping back hips and thighs into protective armor, looking over shoulder, second layer of tendrils forming a mantle"
    }
)

function Get-StableSeed([string]$value) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value))
        return [int64]([BitConverter]::ToUInt64($hash, 0) -band 0x001FFFFFFFFFFFFF)
    }
    finally { $sha.Dispose() }
}

function New-PromptGraph($job) {
    $positive = @($quality, $identity, $job.outfit, $job.variantPrompt, $job.common, $layout) -join ", "
    $prefix = "{0:D3}_{1}_concept_v{2:D2}" -f $job.index, $job.class, $job.variant
    return [ordered]@{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = "waiNSFWIllustrious_v140.safetensors" } }
        "2" = @{ class_type = "LoraLoader"; inputs = @{ model = @("1", 0); clip = @("1", 1); lora_name = "celesphonia-1.8.safetensors"; strength_model = 0.82; strength_clip = 0.82 } }
        "3" = @{ class_type = "CLIPSetLastLayer"; inputs = @{ clip = @("2", 1); stop_at_clip_layer = -2 } }
        "4" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $positive } }
        "5" = @{ class_type = "CLIPTextEncode"; inputs = @{ clip = @("3", 0); text = $negative } }
        "6" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 800; height = 608; batch_size = 1 } }
        "7" = @{ class_type = "KSampler"; inputs = @{ model = @("2", 0); seed = [int64]$job.seed; steps = 24; cfg = 5.5; sampler_name = "euler_ancestral"; scheduler = "normal"; positive = @("4", 0); negative = @("5", 0); latent_image = @("6", 0); denoise = 1.0 } }
        "8" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("7", 0); vae = @("1", 2) } }
        "9" = @{ class_type = "ImageScale"; inputs = @{ image = @("8", 0); upscale_method = "lanczos"; width = 1000; height = 760; crop = "disabled" } }
        "10" = @{ class_type = "SaveImage"; inputs = @{ filename_prefix = "MaidenSuccubus/CardArt/EroticConcepts/$prefix"; images = @("9", 0) } }
    }
}

function Get-CompletedHistory([string]$promptId) {
    $history = Invoke-RestMethod -Uri "$ComfyUrl/history/$promptId" -TimeoutSec 10
    $property = $history.PSObject.Properties[$promptId]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$jobs = @()
foreach ($card in $cards) {
    if ($Indexes.Count -gt 0 -and $card.index -notin $Indexes) { continue }
    foreach ($variant in $Variants) {
        if ($variant -notin 1, 2, 3, 4) { throw "Unknown variant $variant" }
        $jobs += [pscustomobject]@{
            index = $card.index; class = $card.class; title = $card.title; outfit = $card.outfit; common = $card.common
            variant = $variant; variantPrompt = [string]$card.("v$variant")
            seed = Get-StableSeed "$($card.class):local-concept-20260913:v$variant"
        }
    }
}

$clientId = [guid]::NewGuid().ToString()
$generated = 0
foreach ($job in $jobs) {
    $target = Join-Path $targetDir ("{0:D3}_{1}_concept_v{2:D2}.png" -f $job.index, $job.class, $job.variant)
    if ((Test-Path -LiteralPath $target) -and -not $Force) { Write-Host "SKIP [$($job.index):$($job.variant)] $($job.title)"; continue }
    $body = @{ prompt = (New-PromptGraph $job); client_id = $clientId } | ConvertTo-Json -Depth 30 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$ComfyUrl/prompt" -ContentType "application/json" -Body $body
    if ($response.node_errors.PSObject.Properties.Count -gt 0) { throw "ComfyUI rejected [$($job.index):$($job.variant)] $($job.title): $($response.node_errors | ConvertTo-Json -Depth 12 -Compress)" }
    $promptId = [string]$response.prompt_id
    Write-Host "RUN  [$($job.index):$($job.variant)] $($job.title) -> $promptId"
    $history = $null
    while ($null -eq $history) { Start-Sleep -Seconds 2; $history = Get-CompletedHistory $promptId }
    $images = @($history.outputs."10".images)
    if ($images.Count -eq 0) { throw "ComfyUI completed without output for [$($job.index):$($job.variant)] $($job.title)" }
    $image = $images[0]
    $source = Join-Path (Join-Path $outputRoot ([string]$image.subfolder)) ([string]$image.filename)
    Copy-Item -LiteralPath $source -Destination $target -Force
    $generated++
    Write-Host "DONE [$($job.index):$($job.variant)] $($job.title) ($generated/$($jobs.Count))"
}

[ordered]@{
    generatedAt = (Get-Date).ToString("o")
    checkpoint = "waiNSFWIllustrious_v140.safetensors"
    lora = "celesphonia-1.8.safetensors"
    stage = "local text-to-image composition concepts"
    outputSize = "1000x760"
    jobs = @($jobs | Select-Object index, class, title, variant, seed, outfit, variantPrompt, common)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $targetDir "generation_spec.json") -Encoding utf8

Write-Host "COMPLETE generated_this_run=$generated selected=$($jobs.Count)"
