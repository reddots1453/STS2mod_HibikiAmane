from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image


POSES = (1, 2, 4, 5, 6, 7, 8, 9, 10)
FACE_VARIANTS = (1, 5, 9, 12, 15, 18, 26, 33, 41, 50)


def compose(source: Path, pose: int, cloth: int, face: int = 1) -> tuple[Image.Image, list[str]]:
    prefix = f"actor01_pose{pose:02d}"
    layers = [
        source / f"{prefix}_body_0001.png",
        source / f"{prefix}_cloth_{cloth:04d}.png",
        source / f"{prefix}_face_{face:04d}.png",
    ]
    image = Image.new("RGBA", (922, 922), (0, 0, 0, 0))
    for index, layer in enumerate(layers):
        if not layer.exists():
            if index < 2:
                raise FileNotFoundError(layer)
            continue
        image.alpha_composite(Image.open(layer).convert("RGBA"))
    return image, [str(path) for path in layers]


def export_reference(
    source: Path,
    output: Path,
    filename: str,
    pose: int,
    cloth: int,
    face: int = 1,
) -> dict[str, object]:
    image, layers = compose(source, pose, cloth, face)
    bounds = image.getbbox()
    if bounds is None:
        raise ValueError(f"Reference is empty: pose={pose}, cloth={cloth}, face={face}")

    subject = image.crop(bounds)
    subject.thumbnail((880, 880), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (1024, 1024), (228, 229, 233))
    position = ((1024 - subject.width) // 2, (1024 - subject.height) // 2)
    canvas.paste(subject, position, subject)
    canvas.save(output / filename)
    return {
        "file": filename,
        "pose": pose,
        "cloth": cloth,
        "face": face,
        "layers": layers,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Build Celesphonia reference boards for first-batch V2 art.")
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img\pictures"),
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "图片素材" / "第一批卡图V2" / "_references",
    )
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)

    records = [
        export_reference(args.source, args.output, "style_normal_school.png", 1, 1),
        export_reference(args.source, args.output, "style_holy_intact.png", 1, 21),
        export_reference(args.source, args.output, "style_holy_half.png", 1, 22),
        export_reference(args.source, args.output, "style_holy_broken.png", 1, 23),
        export_reference(args.source, args.output, "style_corrupt_succubus.png", 1, 26),
    ]
    for pose in POSES:
        records.append(export_reference(args.source, args.output, f"pose_{pose:02d}_holy.png", pose, 21))

    for variant, face in enumerate(FACE_VARIANTS, start=1):
        neutral_cloth = 1 if variant in (2, 5, 8) else 21
        holy_cloth = 21 if variant <= 6 else (22 if variant <= 8 else 23)
        records.append(
            export_reference(
                args.source, args.output, f"neutral_v{variant:02d}.png", 1, neutral_cloth, face
            )
        )
        records.append(
            export_reference(args.source, args.output, f"holy_v{variant:02d}.png", 1, holy_cloth, face)
        )
        records.append(
            export_reference(args.source, args.output, f"corrupt_v{variant:02d}.png", 1, 26, face)
        )

    metadata = {
        "sourceGame": "魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1",
        "notes": {
            "cloth_0021": "无损无垢天衣",
            "cloth_0022": "半损无垢天衣",
            "cloth_0023": "全损无垢天衣",
            "cloth_0026_pose01": "紫黑魅魔装；原作其余姿势并非同一衣装，因此只作风格参考",
            "variant_reference_policy": "每个变体使用同路线、不同原作表情/服装差分的单人参考图；镜头与动作由文本控制，避免双参考引入重复人物",
        },
        "references": records,
    }
    (args.output / "reference_sources.json").write_text(
        json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Built {len(records)} references in {args.output}")


if __name__ == "__main__":
    main()
