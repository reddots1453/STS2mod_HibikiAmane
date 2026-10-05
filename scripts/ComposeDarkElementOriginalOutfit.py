#!/usr/bin/env python3
"""Composite the exact original-game corruption outfit over ComfyUI backgrounds."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageFilter


MOD_ROOT = Path(__file__).resolve().parent.parent
ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
SUBJECT = ROOT / "references" / "dark_element_original_outfit_subject.png"
BACKGROUND_DIR = ROOT / "dark_element_outfit" / "backgrounds"
OUTPUT_DIR = ROOT / "dark_element_outfit"


def main() -> None:
    subject = Image.open(SUBJECT).convert("RGBA")
    x = (1000 - subject.width) // 2
    y = 760 - subject.height - 8
    alpha = subject.getchannel("A")
    glow_alpha = alpha.filter(ImageFilter.GaussianBlur(18)).point(lambda value: int(value * 0.46))
    glow = Image.new("RGBA", subject.size, (211, 34, 255, 0))
    glow.putalpha(glow_alpha)

    for background_path in sorted(BACKGROUND_DIR.glob("background_*.png")):
        variant = int(background_path.stem.rsplit("_", 1)[1])
        background = Image.open(background_path).convert("RGBA").resize((1000, 760), Image.Resampling.LANCZOS)
        background.alpha_composite(glow, (x, y))
        background.alpha_composite(subject, (x, y))
        output = OUTPUT_DIR / f"049_DarkElement_c{variant:02d}.png"
        background.convert("RGB").save(output)
        print(output)


if __name__ == "__main__":
    main()
