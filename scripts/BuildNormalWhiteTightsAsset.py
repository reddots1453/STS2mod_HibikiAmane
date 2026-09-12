from __future__ import annotations

import argparse
import colorsys
import hashlib
import shutil
from pathlib import Path

from PIL import Image


CANVAS_SIZE = (922, 1250)
SOURCE_LAYER_SIZE = (922, 922)
LEG_TINT_BOX = (420, 675, 620, 1080)
LEG_TINT_RAMP_END = 735
OPTION_FADE_START = 820
OPTION_FADE_END = 922
TINT_RGB = (246, 249, 255)
TINT_STRENGTH = 0.68


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tint_extended_legs(image: Image.Image) -> Image.Image:
    """Continue the source tights material across the STS2-only leg extension."""
    result = image.convert("RGBA")
    pixels = result.load()
    left, top, right, bottom = LEG_TINT_BOX

    for y in range(top, min(bottom, result.height)):
        ramp = min(1.0, max(0.0, (y - top) / (LEG_TINT_RAMP_END - top)))
        strength = TINT_STRENGTH * ramp
        for x in range(left, min(right, result.width)):
            red, green, blue, alpha = pixels[x, y]
            if alpha < 16:
                continue

            hue, saturation, value = colorsys.rgb_to_hsv(
                red / 255.0,
                green / 255.0,
                blue / 255.0,
            )
            is_skin = (
                (hue <= 0.13 or hue >= 0.95)
                and 0.08 <= saturation <= 0.68
                and value >= 0.38
            )
            if not is_skin:
                continue

            pixels[x, y] = (
                round(red * (1.0 - strength) + TINT_RGB[0] * strength),
                round(green * (1.0 - strength) + TINT_RGB[1] * strength),
                round(blue * (1.0 - strength) + TINT_RGB[2] * strength),
                alpha,
            )

    return result


def fade_source_bottom(option: Image.Image) -> Image.Image:
    """Remove the source canvas's hard lower edge before joining the extension."""
    result = option.convert("RGBA")
    pixels = result.load()
    fade_height = OPTION_FADE_END - OPTION_FADE_START

    for y in range(OPTION_FADE_START, min(OPTION_FADE_END, result.height)):
        factor = 1.0 - (y - OPTION_FADE_START) / fade_height
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            pixels[x, y] = (red, green, blue, round(alpha * factor))

    return result


def mask_behind_cloth(option: Image.Image, cloth: Image.Image) -> Image.Image:
    """Keep the baked school uniform pixel-identical while placing tights behind it."""
    result = option.copy()
    pixels = result.load()
    cloth_alpha = cloth.getchannel("A")
    for y in range(result.height):
        for x in range(result.width):
            if cloth_alpha.getpixel((x, y)) > 0:
                red, green, blue, _ = pixels[x, y]
                pixels[x, y] = (red, green, blue, 0)
    return result


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Build the school-uniform portrait with original white tights."
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(
            r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"
            r"\www\img\pictures\actor01_pose01_option_0012.png"
        ),
        help="Original-game PID 12 white-tights layer.",
    )
    parser.add_argument(
        "--character-dir",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "MaidenSuccubus"
        / "images"
        / "character",
    )
    args = parser.parse_args()

    character_dir = args.character_dir
    output_path = character_dir / "character_normal.png"
    base_path = character_dir / "character_normal_bare_legs.png"
    local_option_path = character_dir / "white_tights_option_0012.png"
    cloth_path = character_dir / "cloth_0001.png"

    if not args.source.exists():
        raise FileNotFoundError(args.source)
    if not output_path.exists():
        raise FileNotFoundError(output_path)
    if not base_path.exists():
        shutil.copyfile(output_path, base_path)
    shutil.copyfile(args.source, local_option_path)

    base = Image.open(base_path).convert("RGBA")
    option = Image.open(local_option_path).convert("RGBA")
    cloth = Image.open(cloth_path).convert("RGBA")
    if base.size != CANVAS_SIZE:
        raise ValueError(f"Expected {base_path.name} to be {CANVAS_SIZE}.")
    if option.size != SOURCE_LAYER_SIZE or cloth.size != SOURCE_LAYER_SIZE:
        raise ValueError("Expected source option and school uniform layers to be 922x922.")

    result = tint_extended_legs(base)
    option = mask_behind_cloth(fade_source_bottom(option), cloth)
    result.alpha_composite(option, (0, 0))
    result.save(output_path, format="PNG", optimize=True)

    if sha256(args.source) != sha256(local_option_path):
        raise RuntimeError("Local PID 12 layer differs from the original source file.")

    print(
        "Built character_normal.png at 922x1250 with the original PID 12 "
        "white-tights layer and a seamless lower-leg extension."
    )


if __name__ == "__main__":
    main()
