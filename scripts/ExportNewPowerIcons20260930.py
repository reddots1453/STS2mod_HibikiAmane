"""Export two reviewed Power-icon masters at runtime preview sizes."""

from pathlib import Path

from PIL import Image, ImageDraw


MOD = Path(__file__).resolve().parents[1]
ROOT = MOD / "图片素材" / "状态图标" / "新增Power图标_20260930"
FORMAL = MOD / "图片素材" / "状态图标" / "正式素材" / "Power图标"
ICONS = {
    "chain_destruction_replay": "chain_destruction_replay_power_master.png",
    "yarus_library": "yarus_library_power_master.png",
}


def export() -> None:
    for size in (64, 256):
        (ROOT / f"{size}x{size}").mkdir(parents=True, exist_ok=True)
    (ROOT / "预览").mkdir(parents=True, exist_ok=True)

    for basename, master_name in ICONS.items():
        with Image.open(ROOT / "母图" / master_name) as source:
            rgba = source.convert("RGBA")
            for size in (64, 256):
                suffix = "_power.png" if size == 64 else "_power_big.png"
                target = ROOT / f"{size}x{size}" / f"{basename}{suffix}"
                rgba.resize((size, size), Image.Resampling.LANCZOS).save(target)

    examples = [
        ("Old chain", FORMAL / "64x64" / "chain_destruction_power.png"),
        ("Replay", ROOT / "64x64" / "chain_destruction_replay_power.png"),
        ("Old room", FORMAL / "64x64" / "recollection_room_power.png"),
        ("Library", ROOT / "64x64" / "yarus_library_power.png"),
    ]
    sheet = Image.new("RGB", (440, 260), (37, 44, 53))
    draw = ImageDraw.Draw(sheet)
    for index, (label, path) in enumerate(examples):
        x = 45 + (index % 2) * 220
        y = 12 + (index // 2) * 130
        with Image.open(path) as source:
            icon = source.convert("RGBA").resize((96, 96), Image.Resampling.NEAREST)
            sheet.paste(icon, (x, y), icon)
        draw.text((x, y + 100), label, fill="white")
    sheet.save(ROOT / "预览" / "power_icon_compare_64px.png")


if __name__ == "__main__":
    export()
