from __future__ import annotations

import argparse
import colorsys
import math
import shutil
from pathlib import Path

from PIL import Image


CANVAS_SIZE = (922, 1250)
LEG_CROP = (411, 710, 649, 1173)
LEG_PASTE_AT = (411, 710)
LEG_CLEAR_BOX = (410, 745, 660, 1215)
ARMOR_TO_CLOTH = {
    3: 27,
    2: 28,
    1: 29,
}


def recolor_lower_legs(source: Image.Image) -> Image.Image:
    """Convert the approved holy lower-leg geometry to the 邪瘴天衣 palette."""
    image = source.convert("RGBA")
    pixels = image.load()

    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue
            if alpha < 32:
                pixels[x, y] = (0, 0, 0, 0)
                continue

            hue, saturation, value = colorsys.rgb_to_hsv(
                red / 255.0,
                green / 255.0,
                blue / 255.0,
            )

            # Preserve the original gold piping so it matches the waist and
            # stocking trim of the source-game 邪瘴天衣 layers.
            is_gold = 0.07 <= hue <= 0.18 and saturation >= 0.30 and value >= 0.25
            if is_gold:
                continue

            # Cyan/blue armour accents become violet-lavender while retaining
            # the hand-painted highlights and anti-aliased outline values.
            is_blue = 0.48 <= hue <= 0.72 and saturation >= 0.12
            if is_blue:
                new_hue = 0.74 + min(0.06, (hue - 0.48) * 0.20)
                new_saturation = min(0.82, max(0.38, saturation * 1.05))
                new_value = min(1.0, value * 0.94)
            else:
                # White cloth and pale armour become glossy charcoal-black.
                # Luminance is compressed instead of flattened, preserving
                # folds, edge highlights and the approved leg anatomy.
                new_hue = 0.78
                new_saturation = 0.08 + 0.12 * saturation
                new_value = 0.06 + 0.24 * (value ** 1.80)

            nr, ng, nb = colorsys.hsv_to_rgb(new_hue, new_saturation, new_value)
            pixels[x, y] = (
                round(nr * 255),
                round(ng * 255),
                round(nb * 255),
                alpha,
            )

    return image


def recolor_skin(source: Image.Image) -> Image.Image:
    """Create a skin-shaded version while preserving the approved silhouette."""
    image = source.convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha < 32:
                pixels[x, y] = (0, 0, 0, 0)
                continue

            value = max(red, green, blue) / 255.0
            # Retain the source highlights and inked edges, but move the local
            # colour into the same warm skin range as pose-01's body layer.
            skin_value = 0.56 + 0.42 * (value ** 0.55)
            skin_saturation = 0.27 + 0.12 * (1.0 - value)
            nr, ng, nb = colorsys.hsv_to_rgb(0.055, skin_saturation, skin_value)
            pixels[x, y] = (
                round(nr * 255),
                round(ng * 255),
                round(nb * 255),
                alpha,
            )
    return image


def feather_top(image: Image.Image) -> Image.Image:
    image = image.copy()
    pixels = image.load()
    feather_height = 30
    for y in range(min(feather_height, image.height)):
        factor = y / feather_height
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            pixels[x, y] = (red, green, blue, round(alpha * factor))
    return image


def make_stage_lower_legs(
    dark: Image.Image,
    skin: Image.Image,
    armor: int,
) -> Image.Image:
    """Derive readable intact / torn / severely torn lower-body variants."""
    stage = dark.copy()

    if armor == 1:
        # cloth_0029 exposes the viewer-right leg.  Continue that bare leg down
        # to an irregular over-knee boot line; its smooth upper section is the
        # only source region recoloured, so armour engraving cannot turn into
        # skin-coloured relief.
        mask = Image.new("L", stage.size, 0)
        mask_pixels = mask.load()
        source_alpha = dark.getchannel("A")
        for y in range(stage.height):
            for x in range(100, stage.width):
                cutoff = 103 + round(6 * math.sin(x * 0.18) + 3 * math.sin(x * 0.41))
                if y < cutoff and source_alpha.getpixel((x, y)) >= 32:
                    mask_pixels[x, y] = 255
        stage.paste(skin, (0, 0), mask)

    return feather_top(stage)


def compose_character(
    body: Image.Image,
    cloth: Image.Image,
    face: Image.Image,
    lower_legs: Image.Image,
) -> Image.Image:
    image = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    image.alpha_composite(body, (0, 0))
    image.alpha_composite(cloth, (0, 0))
    image.alpha_composite(face, (0, 0))

    clear_width = LEG_CLEAR_BOX[2] - LEG_CLEAR_BOX[0]
    clear_height = LEG_CLEAR_BOX[3] - LEG_CLEAR_BOX[1]
    image.paste(
        Image.new("RGBA", (clear_width, clear_height), (0, 0, 0, 0)),
        LEG_CLEAR_BOX[:2],
    )
    image.alpha_composite(lower_legs, LEG_PASTE_AT)
    return image


def save_png(image: Image.Image, path: Path, overwrite: bool) -> None:
    if path.exists() and not overwrite:
        raise FileExistsError(f"Refusing to overwrite existing asset: {path}")
    image.save(path, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Build the three usable 邪瘴天衣 combat portraits."
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(
            r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"
            r"\www\img\pictures"
        ),
        help="Original-game pictures directory.",
    )
    parser.add_argument(
        "--character-dir",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "MaidenSuccubus"
        / "images"
        / "character",
    )
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()

    character_dir = args.character_dir
    body = Image.open(character_dir / "body.png").convert("RGBA")
    face = Image.open(character_dir / "face.png").convert("RGBA")
    holy_character = Image.open(character_dir / "character_armor_3.png").convert("RGBA")

    if body.size != (922, 922) or face.size != (922, 922):
        raise ValueError("Expected the existing pose-01 body and face to be 922x922.")
    if holy_character.size != CANVAS_SIZE:
        raise ValueError("Expected character_armor_3.png to be 922x1250.")

    source_lower_legs = holy_character.crop(LEG_CROP)
    lower_legs = recolor_lower_legs(source_lower_legs)
    skin_lower_legs = recolor_skin(source_lower_legs)
    save_png(lower_legs, character_dir / "corrupt_lower_legs.png", args.overwrite)

    for armor, cloth_id in ARMOR_TO_CLOTH.items():
        source_cloth = args.source / "cloth" / f"01_{cloth_id:04d}.png"
        if not source_cloth.exists():
            raise FileNotFoundError(source_cloth)

        local_cloth = character_dir / f"cloth_{cloth_id:04d}.png"
        if local_cloth.exists() and not args.overwrite:
            raise FileExistsError(f"Refusing to overwrite existing asset: {local_cloth}")
        shutil.copyfile(source_cloth, local_cloth)

        cloth = Image.open(local_cloth).convert("RGBA")
        if cloth.size != (922, 922):
            raise ValueError(f"Expected 922x922 source cloth: {source_cloth}")

        output = character_dir / f"character_corrupt_armor_{armor}.png"
        save_png(
            compose_character(
                body,
                cloth,
                face,
                make_stage_lower_legs(lower_legs, skin_lower_legs, armor),
            ),
            output,
            args.overwrite,
        )

    print("Built 邪瘴天衣 armor 3/2/1 portraits at 922x1250 with RGBA transparency.")


if __name__ == "__main__":
    main()
