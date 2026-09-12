#!/usr/bin/env python3
"""Materialize the human-review V3 shortlist and build a labeled contact sheet."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parent.parent
ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
MANIFEST = ROOT / "selection_manifest.json"
SELECTED = ROOT / "selected"
SHEET = ROOT / "selection_contact_sheet.jpg"
THUMB = (500, 380)


def font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for candidate in ("C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/simhei.ttf"):
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    items = manifest["items"]
    SELECTED.mkdir(parents=True, exist_ok=True)
    width = 2 * 24 + 3 * THUMB[0] + 2 * 18
    rows = (len(items) + 2) // 3
    height = 24 + rows * (THUMB[1] + 88 + 18)
    sheet = Image.new("RGB", (width, height), (24, 27, 35))
    draw = ImageDraw.Draw(sheet)
    title_font = font(22)
    small_font = font(16)

    for position, item in enumerate(items):
        source = ROOT / item["source"]
        if not source.exists():
            raise FileNotFoundError(source)
        destination = SELECTED / f"{int(item['index']):03d}_{item['class']}.png"
        shutil.copy2(source, destination)

        with Image.open(source) as opened:
            image = opened.convert("RGB")
        image.thumbnail(THUMB, Image.Resampling.LANCZOS)
        column = position % 3
        row = position // 3
        x = 24 + column * (THUMB[0] + 18)
        y = 24 + row * (THUMB[1] + 106)
        sheet.paste(image, (x, y))
        status_color = {
            "accepted": (90, 224, 139),
            "recommended": (112, 219, 157),
            "provisional": (244, 193, 96),
        }.get(item["status"], (205, 210, 220))
        draw.text((x, y + THUMB[1] + 8), f"{int(item['index']):03d}  {item['title']}", fill=(242, 245, 250), font=title_font)
        draw.text((x, y + THUMB[1] + 42), item["status"], fill=status_color, font=small_font)
        source_label = Path(item["source"]).name
        draw.text((x + 135, y + THUMB[1] + 42), source_label, fill=(166, 176, 194), font=small_font)

    sheet.save(SHEET, quality=92, subsampling=0)
    print(f"selected={len(items)}")
    print(SHEET)


if __name__ == "__main__":
    main()
