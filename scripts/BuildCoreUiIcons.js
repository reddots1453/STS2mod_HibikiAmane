const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const modRoot = path.resolve(__dirname, "..");
const root = path.join(modRoot, "图片素材", "UI核心图标");
const raw = path.join(root, "生成母图");
const out = path.join(root, "正式素材");

const sources = {
  character: path.join(raw, "character_icon_generated.png"),
  energy: path.join(raw, "energy_cost_generated.png"),
  desire: path.join(raw, "desire_icon_generated.png"),
};

function ensureDirs() {
  for (const dir of [root, raw, out, path.join(out, "角色图标"), path.join(out, "费用图标"), path.join(out, "欲望值图标")]) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

async function removeBakedCheckerboard(input) {
  const { data, info } = await sharp(input).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width: w, height: h } = info;
  const count = w * h;
  const bg = new Uint8Array(count);
  const queued = new Uint8Array(count);
  const queue = new Int32Array(count);
  let head = 0;
  let tail = 0;

  const isChecker = (idx) => {
    const i = idx * 3;
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    return max - min <= 7 && min >= 92 && max <= 224;
  };
  const push = (idx) => {
    if (!queued[idx] && isChecker(idx)) {
      queued[idx] = 1;
      queue[tail++] = idx;
    }
  };

  for (let x = 0; x < w; x++) {
    push(x);
    push((h - 1) * w + x);
  }
  for (let y = 0; y < h; y++) {
    push(y * w);
    push(y * w + w - 1);
  }
  while (head < tail) {
    const idx = queue[head++];
    bg[idx] = 1;
    const x = idx % w;
    const y = Math.floor(idx / w);
    if (x > 0) push(idx - 1);
    if (x + 1 < w) push(idx + 1);
    if (y > 0) push(idx - w);
    if (y + 1 < h) push(idx + w);
  }

  const rgba = Buffer.alloc(count * 4);
  for (let idx = 0; idx < count; idx++) {
    const si = idx * 3;
    const di = idx * 4;
    rgba[di] = data[si];
    rgba[di + 1] = data[si + 1];
    rgba[di + 2] = data[si + 2];
    rgba[di + 3] = bg[idx] ? 0 : 255;
  }

  // Generated checkerboards can contain isolated compression speckles. The
  // character head is one connected foreground component, so retain only the
  // largest component and discard detached noise without touching its pixels.
  const seen = new Uint8Array(count);
  let largest = [];
  const componentQueue = new Int32Array(count);
  for (let seed = 0; seed < count; seed++) {
    if (seen[seed] || rgba[seed * 4 + 3] === 0) continue;
    let componentHead = 0;
    let componentTail = 0;
    const component = [];
    seen[seed] = 1;
    componentQueue[componentTail++] = seed;
    while (componentHead < componentTail) {
      const idx = componentQueue[componentHead++];
      component.push(idx);
      const x = idx % w;
      const y = Math.floor(idx / w);
      const neighbors = [];
      if (x > 0) neighbors.push(idx - 1);
      if (x + 1 < w) neighbors.push(idx + 1);
      if (y > 0) neighbors.push(idx - w);
      if (y + 1 < h) neighbors.push(idx + w);
      for (const next of neighbors) {
        if (!seen[next] && rgba[next * 4 + 3] !== 0) {
          seen[next] = 1;
          componentQueue[componentTail++] = next;
        }
      }
    }
    if (component.length > largest.length) largest = component;
  }
  const retained = new Uint8Array(count);
  for (const idx of largest) retained[idx] = 1;
  for (let idx = 0; idx < count; idx++) {
    if (!retained[idx]) rgba[idx * 4 + 3] = 0;
  }
  return sharp(rgba, { raw: { width: w, height: h, channels: 4 } })
    .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png()
    .toBuffer();
}

async function normalizeSquare(input, size, paddingRatio = 0.06) {
  const image = sharp(input);
  const meta = await image.metadata();
  const pad = Math.round(size * paddingRatio);
  const fit = size - pad * 2;
  return image
    .resize(fit, fit, { fit: "contain", kernel: sharp.kernel.lanczos3 })
    .extend({
      top: pad,
      bottom: pad,
      left: pad,
      right: pad,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    })
    .resize(size, size, { fit: "fill", kernel: sharp.kernel.lanczos3 })
    .png()
    .toBuffer();
}

async function exportSizes(master, dir, baseName, sizes) {
  for (const size of sizes) {
    await sharp(master)
      .resize(size, size, { fit: "contain", kernel: sharp.kernel.lanczos3 })
      .png()
      .toFile(path.join(dir, `${baseName}_${size}.png`));
  }
}

async function buildCharacter() {
  const cleaned = await removeBakedCheckerboard(sources.character);
  const master = await normalizeSquare(cleaned, 1024, 0.035);
  const dir = path.join(out, "角色图标");
  await sharp(master).png().toFile(path.join(dir, "hibiki_amane_character_icon_master.png"));
  await exportSizes(master, dir, "hibiki_amane_character_icon", [256, 128, 64]);

  const alpha = await sharp(master).ensureAlpha().extractChannel(3).blur(5).threshold(16).toBuffer();
  await sharp({
    create: { width: 1024, height: 1024, channels: 3, background: { r: 242, g: 229, b: 255 } },
  })
    .joinChannel(alpha)
    .png()
    .toFile(path.join(dir, "hibiki_amane_character_icon_outline_master.png"));
  await exportSizes(path.join(dir, "hibiki_amane_character_icon_outline_master.png"), dir, "hibiki_amane_character_icon_outline", [256, 128, 64]);
}

async function buildSimple(input, folder, baseName, sizes, padding) {
  const dir = path.join(out, folder);
  const master = await normalizeSquare(input, 1024, padding);
  await sharp(master).png().toFile(path.join(dir, `${baseName}_master.png`));
  await exportSizes(master, dir, baseName, sizes);
}

async function buildEnergyOrb() {
  const meta = await sharp(sources.energy).metadata();
  const side = Math.min(meta.width, meta.height);
  const cropSide = Math.round(side * 0.40);
  const left = Math.round((meta.width - cropSide) / 2);
  const top = Math.round((meta.height - cropSide) / 2);
  const circle = Buffer.from(`<svg width="${cropSide}" height="${cropSide}" xmlns="http://www.w3.org/2000/svg"><circle cx="${cropSide / 2}" cy="${cropSide / 2}" r="${cropSide * 0.49}" fill="white"/></svg>`);
  const orb = await sharp(sources.energy)
    .extract({ left, top, width: cropSide, height: cropSide })
    .ensureAlpha()
    .composite([{ input: circle, blend: "dest-in" }])
    .png()
    .toBuffer();
  await buildSimple(orb, "费用图标", "magic_energy_cost_icon", [256, 128, 64, 32], 0.07);
}

async function buildPreview() {
  const items = [
    ["角色图标", "hibiki_amane_character_icon_256.png"],
    ["费用图标", "magic_energy_cost_icon_256.png"],
    ["欲望值图标", "desire_resource_icon_256.png"],
  ];
  const bg = Buffer.from(`<svg width="960" height="360" xmlns="http://www.w3.org/2000/svg"><rect width="960" height="360" rx="24" fill="#243844"/><text x="480" y="326" fill="#f4ead6" font-size="30" text-anchor="middle" font-family="sans-serif">Hibiki Amane · Core UI Icons</text></svg>`);
  const comps = [];
  for (let i = 0; i < items.length; i++) {
    const [folder, file] = items[i];
    const input = await sharp(path.join(out, folder, file)).resize(232, 232).png().toBuffer();
    comps.push({ input, left: 64 + i * 304, top: 46 });
  }
  await sharp(bg).composite(comps).png().toFile(path.join(root, "核心图标总览.png"));
}

async function main() {
  ensureDirs();
  for (const file of Object.values(sources)) {
    if (!fs.existsSync(file)) throw new Error(`Missing source: ${file}`);
  }
  await buildCharacter();
  await buildEnergyOrb();
  await buildSimple(sources.desire, "欲望值图标", "desire_resource_icon", [256, 128, 64, 32, 24], 0.08);
  await buildPreview();
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
