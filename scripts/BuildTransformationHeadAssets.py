"""Rebuild transformation portraits with Fania's original transformed head layers.

The old portraits used actor01_pose01_body_0001.png, whose normal short hair is
baked into the body image.  The original game instead uses the hairless
actor01_pose01_body_0003.png while transformed, then composites hair style 2.

This script follows CallStand.js's layer order for the relevant layers and
preserves the already approved STS2 lower-body extension below y=710.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import tempfile
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


CANVAS_SIZE = (922, 1250)
ORIGINAL_SIZE = (922, 922)
PRESERVE_LOWER_FROM_Y = 710
SHADOW_OPACITY = 200


@dataclass(frozen=True)
class PortraitSpec:
    route: str
    durability: int
    cloth: str
    runtime: str
    archive: str
    corrupt_hair_ornament: bool = False


PORTRAITS = (
    PortraitSpec("holy", 3, "cloth_0021.png", "character_armor_3.png", "无垢天衣/魔装耐久3_无损.png"),
    PortraitSpec("holy", 2, "cloth_0022.png", "character_armor_2.png", "无垢天衣/魔装耐久2_破损.png"),
    PortraitSpec("holy", 1, "cloth_0023.png", "character_armor_1.png", "无垢天衣/魔装耐久1_严重破损.png"),
    PortraitSpec("corrupt", 3, "cloth_0027.png", "character_corrupt_armor_3.png", "邪瘴天衣/魔装耐久3_无损.png", True),
    PortraitSpec("corrupt", 2, "cloth_0028.png", "character_corrupt_armor_2.png", "邪瘴天衣/魔装耐久2_破损.png", True),
    PortraitSpec("corrupt", 1, "cloth_0029.png", "character_corrupt_armor_1.png", "邪瘴天衣/魔装耐久1_严重破损.png", True),
    PortraitSpec("eternal", 3, "cloth_eternal_0046b.png", "character_eternal_armor_3.png", "永恒天衣/魔装耐久3_无损.png"),
    PortraitSpec("eternal", 2, "cloth_eternal_0024b.png", "character_eternal_armor_2.png", "永恒天衣/魔装耐久2_破损.png"),
    PortraitSpec("eternal", 1, "cloth_eternal_0025b.png", "character_eternal_armor_1.png", "永恒天衣/魔装耐久1_严重破损.png"),
)


def load_rgba(path: Path, expected_size: tuple[int, int] | None = None) -> Image.Image:
    if not path.is_file():
        raise FileNotFoundError(path)
    image = Image.open(path).convert("RGBA")
    if expected_size and image.size != expected_size:
        raise ValueError(f"{path} has size {image.size}, expected {expected_size}")
    return image


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def save_png_atomic(image: Image.Image, path: Path) -> None:
    """Encode beside the destination, then atomically replace it.

    Besides preventing partial PNGs, this avoids intermittent Windows failures
    observed when Pillow directly truncates an asset currently being previewed.
    """
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=".portrait_", suffix=".png", dir=path.parent)
    os.close(descriptor)
    temporary_path = Path(temporary_name)
    try:
        image.save(temporary_path, format="PNG", optimize=True)
        os.replace(temporary_path, path)
    finally:
        temporary_path.unlink(missing_ok=True)


def apply_tone(image: Image.Image, red: int, green: int, blue: int) -> Image.Image:
    """Apply RPG Maker's additive RGB tone (gray component is zero here)."""
    r, g, b, a = image.split()
    r = r.point(lambda value: max(0, min(255, value + red)))
    g = g.point(lambda value: max(0, min(255, value + green)))
    b = b.point(lambda value: max(0, min(255, value + blue)))
    return Image.merge("RGBA", (r, g, b, a))


def apply_opacity(image: Image.Image, opacity: int) -> Image.Image:
    result = image.copy()
    alpha = result.getchannel("A").point(lambda value: value * opacity // 255)
    result.putalpha(alpha)
    return result


def composite(canvas: Image.Image, layer: Image.Image) -> None:
    canvas.alpha_composite(layer, (0, 0))


def build_portrait(
    existing: Image.Image,
    body: Image.Image,
    back_hair: Image.Image,
    hair_ornament: Image.Image,
    cloth: Image.Image,
    face: Image.Image,
    hair_shadow: Image.Image,
    front_hair: Image.Image,
    corrupt_hair_ornament: bool,
) -> Image.Image:
    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))

    # Relevant CallStand.js order: body -> rear hair -> hair ornament ->
    # clothing -> face -> hair shadow -> front hair.
    composite(canvas, body)
    composite(canvas, back_hair)
    ornament = apply_tone(hair_ornament, -20, -60, 40) if corrupt_hair_ornament else hair_ornament
    composite(canvas, ornament)
    composite(canvas, cloth)
    composite(canvas, face)
    composite(canvas, apply_opacity(hair_shadow, SHADOW_OPACITY))
    composite(canvas, front_hair)

    # The RPG Maker originals stop at 922 px. Keep the previously completed
    # STS2 leg/foot extension exactly, including its alpha and anchor.
    lower = existing.crop((0, PRESERVE_LOWER_FROM_Y, CANVAS_SIZE[0], CANVAS_SIZE[1]))
    canvas.paste((0, 0, 0, 0), (0, PRESERVE_LOWER_FROM_Y, CANVAS_SIZE[0], CANVAS_SIZE[1]))
    canvas.alpha_composite(lower, (0, PRESERVE_LOWER_FROM_Y))
    return canvas


def make_review_sheet(runtime_dir: Path, output: Path) -> None:
    thumb_size = (221, 300)
    margin = 20
    label_height = 26
    sheet = Image.new("RGB", (margin * 4 + thumb_size[0] * 3, margin * 4 + (thumb_size[1] + label_height) * 3), (31, 38, 49))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, spec in enumerate(PORTRAITS):
        row, column = divmod(index, 3)
        image = load_rgba(runtime_dir / spec.runtime, CANVAS_SIZE)
        image.thumbnail(thumb_size, Image.Resampling.LANCZOS)
        x = margin + column * (thumb_size[0] + margin)
        y = margin + row * (thumb_size[1] + label_height + margin)
        tile = Image.new("RGBA", thumb_size, (44, 54, 68, 255))
        tile.alpha_composite(image, ((thumb_size[0] - image.width) // 2, thumb_size[1] - image.height))
        sheet.paste(tile.convert("RGB"), (x, y))
        draw.text((x, y + thumb_size[1] + 5), f"{spec.route}  armor {spec.durability}", fill=(235, 239, 246), font=font)
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, quality=95)


def main() -> None:
    script_dir = Path(__file__).resolve().parent
    mod_dir = script_dir.parent
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--game-dir",
        type=Path,
        default=Path(r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"),
        help="Root of the original RPG Maker game.",
    )
    args = parser.parse_args()

    source_dir = args.game_dir / "www" / "img" / "pictures"
    runtime_dir = mod_dir / "MaidenSuccubus" / "images" / "character"
    archive_dir = mod_dir / "图片素材" / "变身形态"
    source_archive = archive_dir / "原作变身头部图层"
    source_archive.mkdir(parents=True, exist_ok=True)

    source_files = {
        "body": source_dir / "actor01_pose01_body_0003.png",
        "face": source_dir / "actor01_pose01_face_0001.png",
        "back_hair": source_dir / "hair" / "01_2.png",
        "front_hair": source_dir / "hair" / "01_2f.png",
        "hair_shadow": source_dir / "hair" / "01_2s.png",
        "hair_ornament": source_dir / "hairDress" / "01_2.png",
    }
    archive_names = {
        "body": "actor01_pose01_body_0003.png",
        "face": "actor01_pose01_face_0001.png",
        "back_hair": "hair_01_2_back.png",
        "front_hair": "hair_01_2_front.png",
        "hair_shadow": "hair_01_2_shadow.png",
        "hair_ornament": "hairDress_01_2.png",
    }
    for key, source in source_files.items():
        if not source.is_file():
            raise FileNotFoundError(source)
        shutil.copy2(source, source_archive / archive_names[key])

    body = load_rgba(source_files["body"], ORIGINAL_SIZE)
    face = load_rgba(source_files["face"], ORIGINAL_SIZE)
    back_hair = load_rgba(source_files["back_hair"], ORIGINAL_SIZE)
    front_hair = load_rgba(source_files["front_hair"], ORIGINAL_SIZE)
    hair_shadow = load_rgba(source_files["hair_shadow"], ORIGINAL_SIZE)
    hair_ornament = load_rgba(source_files["hair_ornament"], ORIGINAL_SIZE)

    outputs: list[dict[str, object]] = []
    for spec in PORTRAITS:
        runtime_path = runtime_dir / spec.runtime
        archive_path = archive_dir / spec.archive
        existing = load_rgba(runtime_path, CANVAS_SIZE)
        cloth = load_rgba(runtime_dir / spec.cloth, ORIGINAL_SIZE)
        result = build_portrait(
            existing,
            body,
            back_hair,
            hair_ornament,
            cloth,
            face,
            hair_shadow,
            front_hair,
            spec.corrupt_hair_ornament,
        )
        runtime_path.parent.mkdir(parents=True, exist_ok=True)
        archive_path.parent.mkdir(parents=True, exist_ok=True)
        save_png_atomic(result, runtime_path)
        save_png_atomic(result, archive_path)
        outputs.append(
            {
                "route": spec.route,
                "durability": spec.durability,
                "runtime": str(runtime_path.relative_to(mod_dir)).replace("\\", "/"),
                "archive": str(archive_path.relative_to(mod_dir)).replace("\\", "/"),
                "sha256": sha256(runtime_path),
            }
        )

    review_sheet = archive_dir / "变身头部修正_九宫格预览.jpg"
    make_review_sheet(runtime_dir, review_sheet)
    manifest = {
        "canvas": list(CANVAS_SIZE),
        "preserved_lower_body_from_y": PRESERVE_LOWER_FROM_Y,
        "source_game": str(args.game_dir),
        "source_layers": {
            key: {
                "original": str(path),
                "archived": str((source_archive / archive_names[key]).relative_to(mod_dir)).replace("\\", "/"),
                "sha256": sha256(path),
            }
            for key, path in source_files.items()
        },
        "notes": [
            "Layer order follows www/js/plugins/CallStand.js.",
            "Transformed hairStyle=2; normal hair baked into body_0001 is no longer used.",
            "The original default transformed face layer retains cyan irises.",
            "Corrupt hair ornament uses original ChangeHairD tone [-20,-60,40,0].",
        ],
        "outputs": outputs,
    }
    (archive_dir / "变身头部拼接清单.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"Rebuilt {len(PORTRAITS)} transformation portraits.")
    print(review_sheet)


if __name__ == "__main__":
    main()
