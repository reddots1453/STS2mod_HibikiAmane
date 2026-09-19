from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
ART_ROOT = MOD_ROOT / "图片素材"
TRIAL = ART_ROOT / "第一批卡图V3试制"
OUTPUT_DIR = TRIAL / "EroticLocalFinal_20260919"
OUTPUT = OUTPUT_DIR / "erotic_local_final_review.jpg"
ITEMS = (
    ("019 Binding Insight", ART_ROOT / "完成版卡图" / "019_BindingInsight_绳缚的心得.png"),
    ("050 Ecstasy Dew", TRIAL / "EroticLocalV12_20260919" / "050_EcstasyDew_销魂露_LOCAL_COMFYUI_v12_01.png"),
    ("056 Semen Conversion", ART_ROOT / "完成版卡图" / "056_SemenConversion_精液变换.png"),
    ("058 Masochistic Trance", TRIAL / "EroticLocalV6_20260914" / "058_MasochisticTrance_被虐的恍惚_LOCAL_COMFYUI_v6_03.png"),
    ("073 Desire Whip", TRIAL / "EroticLocalV8_20260919" / "073_DesireWhip_欲望鞭挞_LOCAL_COMFYUI_v8_02.png"),
    ("074 Pleasure Garden", TRIAL / "EroticLocalV8_20260919" / "074_PleasureGarden_淫乐园_LOCAL_COMFYUI_v8_02.png"),
    ("075 Semen Appetite", TRIAL / "EroticLocalV8_20260919" / "075_SemenAppetite_精液食粮_LOCAL_COMFYUI_v8_02.png"),
    ("076 Bite", TRIAL / "EroticLocalV10_20260919" / "076_BiteInvader_咬_LOCAL_COMFYUI_v10_01.png"),
    ("078 Tentacle Armor", TRIAL / "EroticLocalV11_20260919" / "078_TentacleArmor_淫触魔衣_LOCAL_COMFYUI_v11_02.png"),
)


def main() -> None:
    columns = 3
    thumb_w, thumb_h = 500, 380
    label_h = 42
    rows = (len(ITEMS) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * thumb_w, rows * (thumb_h + label_h)), "#181a22")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=20)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
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

    sheet.save(OUTPUT, quality=95, subsampling=0)
    print(f"wrote {OUTPUT} ({len(ITEMS)} final erotic card arts)")


if __name__ == "__main__":
    main()
