#!/usr/bin/env python3
"""Build a three-column V3 base/refinement comparison sheet."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parent.parent
PILOT_ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
CONFIG_PATH = PILOT_ROOT / "refinement_config.json"
OUTPUT_PATH = PILOT_ROOT / "refinement_contact_sheet.jpg"

THUMB_SIZE = (500, 380)
LABEL_HEIGHT = 58
GUTTER = 18
MARGIN = 24
BACKGROUND = (24, 27, 35)
LABEL_BACKGROUND = (36, 41, 52)
TEXT = (239, 242, 248)
MUTED = (166, 176, 194)


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = (
        Path("C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    )
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


def image_cell(path: Path) -> Image.Image:
    with Image.open(path) as source:
        image = source.convert("RGB")
    image.thumbnail(THUMB_SIZE, Image.Resampling.LANCZOS)
    cell = Image.new("RGB", THUMB_SIZE, (10, 12, 17))
    x = (THUMB_SIZE[0] - image.width) // 2
    y = (THUMB_SIZE[1] - image.height) // 2
    cell.paste(image, (x, y))
    return cell


def main() -> None:
    config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    cards = config["cards"]
    columns = ("V3final base", "refined r01", "refined r02")

    width = MARGIN * 2 + len(columns) * THUMB_SIZE[0] + (len(columns) - 1) * GUTTER
    row_height = LABEL_HEIGHT + THUMB_SIZE[1] + GUTTER
    height = MARGIN * 2 + LABEL_HEIGHT + len(cards) * row_height
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(24)
    label_font = load_font(20)
    small_font = load_font(16)

    for column, heading in enumerate(columns):
        x = MARGIN + column * (THUMB_SIZE[0] + GUTTER)
        draw.rounded_rectangle(
            (x, MARGIN, x + THUMB_SIZE[0], MARGIN + LABEL_HEIGHT - 8),
            radius=10,
            fill=LABEL_BACKGROUND,
        )
        draw.text((x + 16, MARGIN + 10), heading, fill=TEXT, font=title_font)

    for row, card in enumerate(cards):
        y = MARGIN + LABEL_HEIGHT + row * row_height
        index = int(card["index"])
        title = str(card["title"])
        class_name = str(card["class"])
        paths = (
            PILOT_ROOT / "final" / str(card["base"]),
            PILOT_ROOT / "refined" / f"{index:03d}_{class_name}_r01.png",
            PILOT_ROOT / "refined" / f"{index:03d}_{class_name}_r02.png",
        )
        for column, path in enumerate(paths):
            if not path.exists():
                raise FileNotFoundError(path)
            x = MARGIN + column * (THUMB_SIZE[0] + GUTTER)
            sheet.paste(image_cell(path), (x, y + LABEL_HEIGHT))
            label = f"{index:03d}  {title}" if column == 0 else path.stem.split("_", 2)[-1]
            draw.text((x + 10, y + 8), label, fill=TEXT, font=label_font)
            if column == 0:
                draw.text((x + 300, y + 12), class_name, fill=MUTED, font=small_font)

    sheet.save(OUTPUT_PATH, quality=92, subsampling=0)
    print(OUTPUT_PATH)


if __name__ == "__main__":
    main()
