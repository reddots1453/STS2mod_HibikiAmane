#!/usr/bin/env python3
"""Create deterministic masks for the V3 semantic-correction pass."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


MOD_ROOT = Path(__file__).resolve().parent.parent
PILOT_ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制"
CONFIG_PATH = PILOT_ROOT / "targeted_inpaint_config.json"


def main() -> None:
    config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    width = int(config["output"]["width"])
    height = int(config["output"]["height"])
    for card in config["cards"]:
        mask = Image.new("RGB", (width, height), "black")
        draw = ImageDraw.Draw(mask)
        for shape in card["shapes"]:
            if shape["type"] == "polygon":
                draw.polygon([tuple(point) for point in shape["points"]], fill="white")
            elif shape["type"] == "ellipse":
                draw.ellipse(tuple(shape["bounds"]), fill="white")
            else:
                raise ValueError(f"Unsupported mask shape: {shape['type']}")
        output_path = PILOT_ROOT / card["mask"]
        output_path.parent.mkdir(parents=True, exist_ok=True)
        mask.save(output_path)
        print(output_path)


if __name__ == "__main__":
    main()
