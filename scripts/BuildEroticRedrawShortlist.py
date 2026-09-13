from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
V4 = ROOT / "EroticRedrawV4_20260914"
V5 = ROOT / "EroticRedrawV5_20260914"
OUTPUT = V5 / "redraw_v5_shortlist_review.jpg"
ITEMS = (
    ("050 Ecstasy Dew", V4 / "050_EcstasyDew_hifi_v4_01.png"),
    ("058 Masochistic Trance", V5 / "058_MasochisticTrance_redraw_v5_02.png"),
    ("073 Desire Whip", V4 / "073_DesireWhip_hifi_v4_01.png"),
    ("074 Pleasure Garden", V4 / "074_PleasureGarden_hifi_v4_01.png"),
    ("075 Semen Appetite", V4 / "075_SemenAppetite_hifi_v4_01.png"),
    ("076 Bite", V5 / "076_BiteInvader_redraw_v5_03.png"),
    ("078 Tentacle Armor", V4 / "078_TentacleArmor_hifi_v4_01.png"),
)


def main() -> None:
    columns = 2
    thumb_w, thumb_h = 500, 380
    label_h = 42
    rows = (len(ITEMS) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * thumb_w, rows * (thumb_h + label_h)), "#181a22")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=21)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    for index, (label, path) in enumerate(ITEMS):
        with Image.open(path) as source:
            image = source.convert("RGB")
            if image.size != (1000, 760):
                raise ValueError(f"Unexpected size for {path.name}: {image.size}")
            image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = index % columns * thumb_w
        y = index // columns * (thumb_h + label_h)
        sheet.paste(image, (x, y))
        draw.text((x + 10, y + thumb_h + 8), label, fill="white", font=font)

    sheet.save(OUTPUT, quality=94, subsampling=0)
    print(f"wrote {OUTPUT} ({len(ITEMS)} shortlisted candidates)")


if __name__ == "__main__":
    main()
