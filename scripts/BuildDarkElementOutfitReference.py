#!/usr/bin/env python3
"""Compose the original-game corruption outfit layers into a clean IP-Adapter reference."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


DEFAULT_SOURCE = Path(r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img\pictures")
MOD_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUTPUT = MOD_ROOT / "图片素材" / "第一批卡图V3试制" / "references" / "dark_element_original_outfit.png"
DEFAULT_SUBJECT_OUTPUT = MOD_ROOT / "图片素材" / "第一批卡图V3试制" / "references" / "dark_element_original_outfit_subject.png"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--subject-output", type=Path, default=DEFAULT_SUBJECT_OUTPUT)
    args = parser.parse_args()

    layer_names = (
        "actor01_pose01_body_0001.png",
        "actor01_pose01_cloth_0026.png",
        "actor01_pose01_face_0001.png",
    )
    layers = [Image.open(args.source / name).convert("RGBA") for name in layer_names]
    composite = Image.new("RGBA", layers[0].size, (0, 0, 0, 0))
    for layer in layers:
        composite.alpha_composite(layer)

    alpha_box = composite.getchannel("A").getbbox()
    if alpha_box is None:
        raise RuntimeError("Original-game outfit composite is empty.")
    subject = composite.crop(alpha_box)
    subject.thumbnail((720, 730), Image.Resampling.LANCZOS)
    args.subject_output.parent.mkdir(parents=True, exist_ok=True)
    subject.save(args.subject_output)

    width, height = 1000, 760
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(height):
        ratio = y / max(1, height - 1)
        color = (int(15 + 20 * ratio), int(8 + 5 * ratio), int(28 + 32 * ratio), 255)
        draw.line((0, y, width, y), fill=color)
    for radius, alpha in ((310, 34), (235, 40), (165, 48)):
        glow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        glow_draw = ImageDraw.Draw(glow)
        center = (550, 370)
        glow_draw.ellipse(
            (center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius),
            fill=(174, 27, 206, alpha),
        )
        canvas = Image.alpha_composite(canvas, glow)

    x = (width - subject.width) // 2
    y = height - subject.height - 8
    canvas.alpha_composite(subject, (x, y))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(args.output)
    print(args.output)
    print(args.subject_output)


if __name__ == "__main__":
    main()
