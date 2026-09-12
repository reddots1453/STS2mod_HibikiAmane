from __future__ import annotations

import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SOURCE = Path(
    r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img\pictures"
)
MOD_ROOT = Path(__file__).resolve().parents[1]
OUTPUT = MOD_ROOT / "图片素材" / "第一批卡图V3试制" / "eve_reference_sheet.jpg"

GROUP_PATTERN = re.compile(r"^(Eve_.+?)_\d+[a-z]?$", re.IGNORECASE)
COLUMNS = 5
THUMBNAIL = (320, 240)
LABEL_HEIGHT = 30


def group_name(path: Path) -> str:
    match = GROUP_PATTERN.match(path.stem)
    return match.group(1).lower() if match else path.stem.lower()


def main() -> None:
    first_by_group: dict[str, Path] = {}
    for path in sorted(SOURCE.glob("[Ee][Vv][Ee]_*.png")):
        first_by_group.setdefault(group_name(path), path)

    paths = [first_by_group[key] for key in sorted(first_by_group)]
    rows = (len(paths) + COLUMNS - 1) // COLUMNS
    sheet = Image.new(
        "RGB",
        (THUMBNAIL[0] * COLUMNS, (THUMBNAIL[1] + LABEL_HEIGHT) * rows),
        (22, 25, 31),
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=18)

    for index, path in enumerate(paths):
        x = (index % COLUMNS) * THUMBNAIL[0]
        y = (index // COLUMNS) * (THUMBNAIL[1] + LABEL_HEIGHT)
        with Image.open(path) as source:
            image = source.convert("RGB")
            image.thumbnail(THUMBNAIL, Image.Resampling.LANCZOS)
            offset_x = x + (THUMBNAIL[0] - image.width) // 2
            offset_y = y + (THUMBNAIL[1] - image.height) // 2
            sheet.paste(image, (offset_x, offset_y))
        draw.text((x + 6, y + THUMBNAIL[1] + 4), path.name, font=font, fill=(240, 242, 247))

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT, quality=90, subsampling=0)
    print(f"Wrote {OUTPUT} ({len(paths)} groups)")


if __name__ == "__main__":
    main()
