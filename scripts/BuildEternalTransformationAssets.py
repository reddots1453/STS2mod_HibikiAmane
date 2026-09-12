from __future__ import annotations

import argparse
import hashlib
import shutil
from pathlib import Path

from PIL import Image


CANVAS_SIZE = (922, 1250)
SOURCE_LAYER_SIZE = (922, 922)
LEG_CROP = (411, 710, 649, 1173)
LEG_PASTE_AT = (411, 710)
LEG_CLEAR_BOX = (410, 745, 660, 1215)
ARMOR_TO_CLOTH = {
    3: "0046b",
    2: "0024b",
    1: "0025b",
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def feather_top(image: Image.Image) -> Image.Image:
    result = image.convert("RGBA")
    pixels = result.load()
    feather_height = 30
    for y in range(min(feather_height, result.height)):
        factor = y / feather_height
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            pixels[x, y] = (red, green, blue, round(alpha * factor))
    return result


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
    image.alpha_composite(feather_top(lower_legs), LEG_PASTE_AT)
    return image


def save_png(image: Image.Image, path: Path, overwrite: bool) -> None:
    if path.exists() and not overwrite:
        raise FileExistsError(f"Refusing to overwrite existing asset: {path}")
    image.save(path, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Build three runtime-ready Eternal Robe combat portraits."
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(
            r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"
            r"\www\img\pictures\cloth"
        ),
        help="Original-game cloth layer directory.",
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
    if body.size != SOURCE_LAYER_SIZE or face.size != SOURCE_LAYER_SIZE:
        raise ValueError("Expected pose-01 body and face layers to be 922x922.")

    for armor, cloth_id in ARMOR_TO_CLOTH.items():
        source_cloth = args.source / f"01_{cloth_id}.png"
        local_cloth = character_dir / f"cloth_eternal_{cloth_id}.png"
        source_lower_legs = character_dir / f"character_armor_{armor}.png"
        output = character_dir / f"character_eternal_armor_{armor}.png"

        if not source_cloth.exists():
            raise FileNotFoundError(source_cloth)
        if not source_lower_legs.exists():
            raise FileNotFoundError(source_lower_legs)
        if local_cloth.exists() and not args.overwrite:
            raise FileExistsError(f"Refusing to overwrite existing asset: {local_cloth}")
        shutil.copyfile(source_cloth, local_cloth)
        if sha256(source_cloth) != sha256(local_cloth):
            raise RuntimeError(f"Copied cloth differs from source: {cloth_id}")

        cloth = Image.open(local_cloth).convert("RGBA")
        lower_source = Image.open(source_lower_legs).convert("RGBA")
        if cloth.size != SOURCE_LAYER_SIZE:
            raise ValueError(f"Expected 922x922 source cloth: {source_cloth}")
        if lower_source.size != CANVAS_SIZE:
            raise ValueError(f"Expected 922x1250 lower-leg source: {source_lower_legs}")

        lower_legs = lower_source.crop(LEG_CROP)
        save_png(
            compose_character(body, cloth, face, lower_legs),
            output,
            args.overwrite,
        )

    print(
        "Built Eternal Robe armor 3/2/1 portraits at 922x1250 with RGBA "
        "transparency from the original 0046b/0024b/0025b layers."
    )


if __name__ == "__main__":
    main()
