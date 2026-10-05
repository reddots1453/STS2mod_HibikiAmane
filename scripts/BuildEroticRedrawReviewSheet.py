from __future__ import annotations

import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制" / "EroticRedrawV4_20260914"
OUTPUT = ROOT / "redraw_v4_all_review.jpg"
PATTERN = re.compile(r"^(\d{3})_(.+)_redraw_v4_(\d{2})\.png$")


def sort_key(path: Path) -> tuple[int, int]:
    match = PATTERN.match(path.name)
    if not match:
        return (9999, 9999)
    return (int(match.group(1)), int(match.group(3)))


def main() -> None:
    paths = sorted((path for path in ROOT.glob("*_redraw_v4_*.png") if PATTERN.match(path.name)), key=sort_key)
    if not paths:
        raise SystemExit(f"No candidates found in {ROOT}")

    columns = 3
    thumb_w, thumb_h = 400, 304
    label_h = 34
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * thumb_w, rows * (thumb_h + label_h)), "#181a22")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=18)

    for index, path in enumerate(paths):
        with Image.open(path) as source:
            image = source.convert("RGB")
            if image.size != (1000, 760):
                raise ValueError(f"Unexpected size for {path.name}: {image.size}")
            image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = index % columns * thumb_w
        y = index // columns * (thumb_h + label_h)
        sheet.paste(image, (x, y))
        draw.text((x + 8, y + thumb_h + 6), path.stem, fill="white", font=font)

    sheet.save(OUTPUT, quality=92, subsampling=0)
    print(f"wrote {OUTPUT} ({len(paths)} candidates)")


if __name__ == "__main__":
    main()
