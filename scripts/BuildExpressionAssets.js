const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const modRoot = path.resolve(__dirname, '..');
const sourceRoot = path.join(modRoot, '图片素材', '原作表情素材', '基础表情差分', 'pose01');
const outputRoot = path.join(modRoot, '图片素材', '角色表情变化', '正式表情层');
const portraitArtRoot = path.join(modRoot, '图片素材', '变身形态');
const layerRoot = path.join(modRoot, '图片素材', '角色表情变化', '分层立绘');
const characterSourceRoot = path.join(modRoot, 'MaidenSuccubus', 'images', 'character');
const transformedHeadRoot = path.join(portraitArtRoot, '原作变身头部图层');

const source = (id) => path.join(sourceRoot, `actor01_pose01_face_${id}.png`);

const directCopies = {
  'holy_desire_0_4.png': '0007',
  'holy_desire_5_7.png': '0032',
  'holy_desire_8_9.png': '0034',
  'holy_desire_10.png': '0041',
  'neutral_desire_0_4.png': '0002',
  'neutral_desire_5_7.png': '0031',
  'neutral_desire_8_9.png': '0035',
  'neutral_desire_10.png': '0041',
  'corrupt_desire_0_4.png': '0050',
};

const redEyeVariants = {
  'corrupt_desire_5_7.png': '0033',
  'corrupt_desire_8_9.png': '0060',
  'corrupt_desire_10.png': '0041',
};

function rgbToHsv(r, g, b) {
  r /= 255; g /= 255; b /= 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const delta = max - min;
  let h = 0;
  if (delta !== 0) {
    if (max === r) h = ((g - b) / delta) % 6;
    else if (max === g) h = (b - r) / delta + 2;
    else h = (r - g) / delta + 4;
    h /= 6;
    if (h < 0) h += 1;
  }
  return [h, max === 0 ? 0 : delta / max, max];
}

function hsvToRgb(h, s, v) {
  const i = Math.floor(h * 6);
  const f = h * 6 - i;
  const p = v * (1 - s);
  const q = v * (1 - f * s);
  const t = v * (1 - (1 - f) * s);
  const values = [[v,t,p],[q,v,p],[p,v,t],[p,q,v],[t,p,v],[v,p,q]][i % 6];
  return values.map((x) => Math.round(x * 255));
}

function isInsideIrisRegion(x, y) {
  const leftEye = x >= 464 && x <= 485;
  const rightEye = x >= 507 && x <= 530;
  return y >= 254 && y <= 276 && (leftEye || rightEye);
}

async function makeRedEyeVariant(input, output) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (info.width !== 922 || info.height !== 922 || info.channels !== 4) {
    throw new Error(`Unexpected source format: ${input} => ${info.width}x${info.height}, ${info.channels} channels`);
  }

  let changed = 0;
  for (let y = 254; y <= 276; y += 1) {
    for (let x = 464; x <= 530; x += 1) {
      if (!isInsideIrisRegion(x, y)) continue;
      const index = (y * info.width + x) * 4;
      const r = data[index];
      const g = data[index + 1];
      const b = data[index + 2];
      const a = data[index + 3];
      if (a === 0) continue;

      const [, saturation, value] = rgbToHsv(r, g, b);
      const isCyanIris = g > r * 1.04 && b > r * 1.06 && saturation >= 0.16;
      if (!isCyanIris) continue;

      // Hue 0.97 reproduces the crimson-magenta family used by original face 0050.
      const targetSaturation = Math.min(0.92, Math.max(0.55, saturation * 1.15));
      const [nr, ng, nb] = hsvToRgb(0.97, targetSaturation, value);
      data[index] = nr;
      data[index + 1] = ng;
      data[index + 2] = nb;
      changed += 1;
    }
  }

  await sharp(data, { raw: info }).png().toFile(output);
  return changed;
}

const layeredPortraits = [
  ['holy_armor_3.png', '无垢天衣/魔装耐久3_无损.png', 'cloth_0021.png', false],
  ['holy_armor_2.png', '无垢天衣/魔装耐久2_破损.png', 'cloth_0022.png', false],
  ['holy_armor_1.png', '无垢天衣/魔装耐久1_严重破损.png', 'cloth_0023.png', false],
  ['corrupt_armor_3.png', '邪瘴天衣/魔装耐久3_无损.png', 'cloth_0027.png', true],
  ['corrupt_armor_2.png', '邪瘴天衣/魔装耐久2_破损.png', 'cloth_0028.png', true],
  ['corrupt_armor_1.png', '邪瘴天衣/魔装耐久1_严重破损.png', 'cloth_0029.png', true],
  ['eternal_armor_3.png', '永恒天衣/魔装耐久3_无损.png', 'cloth_eternal_0046b.png', false],
  ['eternal_armor_2.png', '永恒天衣/魔装耐久2_破损.png', 'cloth_eternal_0024b.png', false],
  ['eternal_armor_1.png', '永恒天衣/魔装耐久1_严重破损.png', 'cloth_eternal_0025b.png', false],
];

async function buildLayeredPortraitAssets() {
  const baseOutputRoot = path.join(layerRoot, '无表情底图');
  const foregroundOutputRoot = path.join(layerRoot, '前景头发层');
  fs.mkdirSync(baseOutputRoot, { recursive: true });
  fs.mkdirSync(foregroundOutputRoot, { recursive: true });

  const body = path.join(transformedHeadRoot, 'actor01_pose01_body_0003.png');
  const backHair = path.join(transformedHeadRoot, 'hair_01_2_back.png');
  const hairOrnament = path.join(transformedHeadRoot, 'hairDress_01_2.png');
  const hairShadow = path.join(transformedHeadRoot, 'hair_01_2_shadow.png');
  const frontHair = path.join(transformedHeadRoot, 'hair_01_2_front.png');
  const canvas = () => sharp({
    create: { width: 922, height: 1250, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
  });

  for (const [outputName, archiveRelative, clothName, corruptOrnament] of layeredPortraits) {
    const existing = path.join(portraitArtRoot, archiveRelative);
    const lowerBody = await sharp(existing)
      .extract({ left: 0, top: 710, width: 922, height: 540 })
      .png()
      .toBuffer();
    let ornamentInput = hairOrnament;
    if (corruptOrnament) {
      ornamentInput = await sharp(hairOrnament)
        .ensureAlpha()
        .linear([1, 1, 1, 1], [-20, -60, 40, 0])
        .png()
        .toBuffer();
    }

    await canvas().composite([
      { input: body, left: 0, top: 0 },
      { input: backHair, left: 0, top: 0 },
      { input: ornamentInput, left: 0, top: 0 },
      { input: path.join(characterSourceRoot, clothName), left: 0, top: 0 },
      { input: lowerBody, left: 0, top: 710 },
    ]).png().toFile(path.join(baseOutputRoot, outputName));
  }

  // Normal hair is baked into body.png. The face rectangle contains no cloth,
  // so replacing it with pristine body pixels removes the baked default face.
  const normalExisting = path.join(portraitArtRoot, '校服形态', '普通校服.png');
  const normalFaceUnderlay = await sharp(path.join(characterSourceRoot, 'body.png'))
    .extract({ left: 450, top: 235, width: 96, height: 84 })
    .png()
    .toBuffer();
  await sharp(normalExisting).composite([
    { input: normalFaceUnderlay, left: 450, top: 235 },
  ]).png().toFile(path.join(baseOutputRoot, 'normal_school.png'));

  await canvas().composite([
    { input: hairShadow, left: 0, top: 0, opacity: 200 / 255 },
    { input: frontHair, left: 0, top: 0 },
  ]).png().toFile(path.join(foregroundOutputRoot, 'transformed_front_hair.png'));
}

async function buildGameCanvasFaceLayers() {
  const gameCanvasRoot = path.join(modRoot, '图片素材', '角色表情变化', '游戏画布表情层_922x1250');
  fs.mkdirSync(gameCanvasRoot, { recursive: true });
  for (const name of [...Object.keys(directCopies), ...Object.keys(redEyeVariants)]) {
    await sharp(path.join(outputRoot, name))
      .extend({
        top: 0,
        bottom: 328,
        left: 0,
        right: 0,
        background: { r: 0, g: 0, b: 0, alpha: 0 },
      })
      .png()
      .toFile(path.join(gameCanvasRoot, name));
  }
}

async function buildReviewMatrix() {
  const baseRoot = path.join(layerRoot, '无表情底图');
  const frontHair = path.join(layerRoot, '前景头发层', 'transformed_front_hair.png');
  const rows = [
    ['HOLY  -5..-3', 'holy_armor_3.png', ['holy_desire_0_4.png', 'holy_desire_5_7.png', 'holy_desire_8_9.png', 'holy_desire_10.png'], true],
    ['NEUTRAL  -2..+2', 'normal_school.png', ['neutral_desire_0_4.png', 'neutral_desire_5_7.png', 'neutral_desire_8_9.png', 'neutral_desire_10.png'], false],
    ['CORRUPT  +3..+5', 'corrupt_armor_3.png', ['corrupt_desire_0_4.png', 'corrupt_desire_5_7.png', 'corrupt_desire_8_9.png', 'corrupt_desire_10.png'], true],
  ];
  const labels = ['DESIRE 0..4', 'DESIRE 5..7', 'DESIRE 8..9', 'DESIRE 10'];
  const tiles = [];
  for (let row = 0; row < rows.length; row += 1) {
    const [, baseName, faces, transformed] = rows[row];
    for (let column = 0; column < faces.length; column += 1) {
      const layers = [{ input: path.join(outputRoot, faces[column]), left: 0, top: 0 }];
      if (transformed) layers.push({ input: frontHair, left: 0, top: 0 });
      const portrait = await sharp(path.join(baseRoot, baseName)).composite(layers).png().toBuffer();
      const head = await sharp(portrait)
        .extract({ left: 330, top: 15, width: 320, height: 350 })
        .resize(250, 273, { fit: 'contain' })
        .png()
        .toBuffer();
      tiles.push({ input: head, left: column * 290 + 20, top: row * 320 + 32 });
    }
  }
  const svg = `<svg width="1160" height="960" xmlns="http://www.w3.org/2000/svg">
    <style>.r{fill:#f4f6fa;font:22px sans-serif;font-weight:700}.c{fill:#dce4ef;font:17px sans-serif}</style>
    ${rows.map((item, row) => `<text class="r" x="12" y="${row * 320 + 27}">${item[0]}</text>`).join('')}
    ${rows.flatMap((_, row) => labels.map((label, column) => `<text class="c" x="${column * 290 + 76}" y="${row * 320 + 313}">${label}</text>`)).join('')}
  </svg>`;
  await sharp({ create: { width: 1160, height: 960, channels: 4, background: { r: 28, g: 35, b: 48, alpha: 1 } } })
    .composite([...tiles, { input: Buffer.from(svg), left: 0, top: 0 }])
    .png()
    .toFile(path.join(modRoot, '图片素材', '角色表情变化', '正式表情矩阵预览.png'));
}

async function main() {
  fs.mkdirSync(outputRoot, { recursive: true });

  for (const [name, id] of Object.entries(directCopies)) {
    fs.copyFileSync(source(id), path.join(outputRoot, name));
    console.log(`${name}: copied original ${id}`);
  }

  for (const [name, id] of Object.entries(redEyeVariants)) {
    const changed = await makeRedEyeVariant(source(id), path.join(outputRoot, name));
    console.log(`${name}: recolored ${changed} iris pixels from original ${id}`);
  }

  await buildGameCanvasFaceLayers();
  console.log('Built 12 face layers on the common 922x1250 game canvas.');

  await buildLayeredPortraitAssets();
  console.log('Built 10 faceless body/outfit layers and 1 transformed front-hair layer.');
  await buildReviewMatrix();
  console.log('Built final expression review matrix.');
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
