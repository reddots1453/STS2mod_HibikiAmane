from __future__ import annotations

import argparse
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def natural_key(path: Path) -> tuple[int, int, str]:
    match = re.fullmatch(r"Eve_(\d+)(?:_(\d+))?\.png", path.name, re.IGNORECASE)
    if not match:
        return (10_000, 10_000, path.name)
    return (int(match.group(1)), int(match.group(2) or 0), path.name)


def first_variant_per_event(paths: list[Path]) -> list[Path]:
    selected: dict[int, Path] = {}
    for path in sorted(paths, key=natural_key):
        event, _, _ = natural_key(path)
        selected.setdefault(event, path)
    return list(selected.values())


def build_sheet(paths: list[Path], output: Path, columns: int = 5) -> None:
    thumb_w, thumb_h = 320, 180
    label_h = 28
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * thumb_w, rows * (thumb_h + label_h)), "#181a22")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()

    for index, path in enumerate(paths):
        with Image.open(path) as source:
            image = source.convert("RGBA")
            canvas = Image.new("RGBA", image.size, "#202430")
            canvas.alpha_composite(image)
            canvas.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)

        x = (index % columns) * thumb_w
        y = (index // columns) * (thumb_h + label_h)
        paste_x = x + (thumb_w - canvas.width) // 2
        paste_y = y + (thumb_h - canvas.height) // 2
        sheet.paste(canvas.convert("RGB"), (paste_x, paste_y))
        draw.text((x + 8, y + thumb_h + 7), path.stem, fill="white", font=font)

    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, quality=92)


def main() -> None:
    parser = argparse.ArgumentParser(description="Build an overview of original Celesphonia event CG poses.")
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--all-variants", action="store_true")
    args = parser.parse_args()

    paths = [path for path in args.source.glob("Eve_*.png") if re.fullmatch(r"Eve_\d+(?:_\d+)?\.png", path.name, re.IGNORECASE)]
    if not args.all_variants:
        paths = first_variant_per_event(paths)
    else:
        paths.sort(key=natural_key)
    build_sheet(paths, args.output)
    print(f"wrote {args.output} ({len(paths)} images)")


if __name__ == "__main__":
    main()
