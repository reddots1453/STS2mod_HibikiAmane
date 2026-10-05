const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const modRoot = path.resolve(__dirname, "..");
const artRoot = path.join(modRoot, "图片素材", "状态图标");
const batchRoot = path.join(artRoot, "新增状态图标_第二轮");
const sourceRoot = path.join(batchRoot, "生成母图");
const formalRoot = path.join(artRoot, "正式素材", "Power图标");

const icons = [
  { base: "corrupt_robe", source: "corrupt_robe_generated.png" },
  { base: "holy_flame", source: "holy_flame_generated.png" },
  { base: "opening_prayer", source: "opening_prayer_generated.png" },
  { base: "wet", source: "wet_generated.png" },
];

async function normalize(input, size, innerSize) {
  const { data, info } = await sharp(input)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4) {
    // Image generation returned an RGBA file whose nominally transparent field
    // still contains opaque pure-black pixels.  The real outlines are deep navy,
    // so removing only this black matte preserves the illustrated contour.
    if (data[i] <= 8 && data[i + 1] <= 8 && data[i + 2] <= 8) data[i + 3] = 0;
  }
  const cleaned = await sharp(data, { raw: info }).png().toBuffer();
  const trimmed = await sharp(cleaned)
    .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png()
    .toBuffer();
  return sharp(trimmed)
    .resize(innerSize, innerSize, {
      fit: "contain",
      kernel: sharp.kernel.lanczos3,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    })
    .extend({
      top: Math.floor((size - innerSize) / 2),
      bottom: Math.ceil((size - innerSize) / 2),
      left: Math.floor((size - innerSize) / 2),
      right: Math.ceil((size - innerSize) / 2),
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    })
    .png()
    .toBuffer();
}

async function buildIcon(icon) {
  const source = path.join(sourceRoot, icon.source);
  if (!fs.existsSync(source)) throw new Error(`Missing source: ${source}`);
  const master = await normalize(source, 1254, 1160);
  const big = await sharp(master)
    .resize(256, 256, { kernel: sharp.kernel.lanczos3 })
    .png()
    .toBuffer();
  const small = await sharp(master)
    .resize(64, 64, { kernel: sharp.kernel.lanczos3 })
    .png()
    .toBuffer();

  await sharp(master).toFile(path.join(formalRoot, "母图", `${icon.base}_power_master.png`));
  await sharp(big).toFile(path.join(formalRoot, "256x256", `${icon.base}_power_big.png`));
  await sharp(small).toFile(path.join(formalRoot, "64x64", `${icon.base}_power.png`));
  return big;
}

async function buildOverview(images) {
  const cells = [
    { left: 24, top: 24 },
    { left: 320, top: 24 },
    { left: 24, top: 320 },
    { left: 320, top: 320 },
  ];
  const composites = [];
  for (let i = 0; i < images.length; i += 1) {
    const input = await sharp(images[i])
      .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .resize(248, 248, { fit: "inside", kernel: sharp.kernel.lanczos3 })
      .png()
      .toBuffer();
    const metadata = await sharp(input).metadata();
    composites.push({
      input,
      left: cells[i].left + Math.round((272 - metadata.width) / 2),
      top: cells[i].top + Math.round((272 - metadata.height) / 2),
    });
  }
  await sharp({
    create: {
      width: 640,
      height: 640,
      channels: 4,
      background: { r: 36, g: 56, b: 68, alpha: 1 },
    },
  })
    .composite(composites)
    .png()
    .toFile(path.join(batchRoot, "第二轮新增Power图标总览.png"));
}

async function main() {
  for (const dir of [
    sourceRoot,
    path.join(formalRoot, "母图"),
    path.join(formalRoot, "256x256"),
    path.join(formalRoot, "64x64"),
  ]) fs.mkdirSync(dir, { recursive: true });

  const images = [];
  for (const icon of icons) images.push(await buildIcon(icon));
  await buildOverview(images);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
