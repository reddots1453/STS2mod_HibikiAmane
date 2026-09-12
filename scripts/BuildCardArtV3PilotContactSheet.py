from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
PILOT_ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
SOURCE_DIR = PILOT_ROOT / "final"
OUTPUT_PATH = PILOT_ROOT / "pilot_contact_sheet.jpg"

THUMBNAIL = (500, 380)
LABEL_HEIGHT = 34
COLUMNS = 3
BACKGROUND = (24, 27, 34)
LABEL_COLOR = (235, 238, 244)


def main() -> None:
    paths = sorted(SOURCE_DIR.glob("*.png"))
    if not paths:
        raise SystemExit(f"No pilot images found in {SOURCE_DIR}")

    rows = (len(paths) + COLUMNS - 1) // COLUMNS
    sheet = Image.new(
        "RGB",
        (THUMBNAIL[0] * COLUMNS, (THUMBNAIL[1] + LABEL_HEIGHT) * rows),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=22)

    for index, path in enumerate(paths):
        column = index % COLUMNS
        row = index // COLUMNS
        x = column * THUMBNAIL[0]
        y = row * (THUMBNAIL[1] + LABEL_HEIGHT)

        with Image.open(path) as source:
            image = source.convert("RGB")
            if image.size != (1000, 760):
                raise ValueError(f"Unexpected dimensions for {path.name}: {image.size}")
            image.thumbnail(THUMBNAIL, Image.Resampling.LANCZOS)
            sheet.paste(image, (x, y))

        draw.text((x + 8, y + THUMBNAIL[1] + 5), path.stem, font=font, fill=LABEL_COLOR)

    sheet.save(OUTPUT_PATH, quality=92, subsampling=0)
    print(f"Wrote {OUTPUT_PATH} ({len(paths)} images)")


if __name__ == "__main__":
    main()
