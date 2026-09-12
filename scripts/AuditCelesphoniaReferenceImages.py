from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

from PIL import Image


DEFAULT_SOURCE = Path(
    r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img\pictures"
)


def inspect_image(path: Path) -> dict[str, object]:
    with Image.open(path) as image:
        width, height = image.size
        bands = image.getbands()
        alpha = image.getchannel("A") if "A" in bands else None
        bbox = alpha.getbbox() if alpha else (0, 0, width, height)
        if bbox is None:
            coverage = 0.0
        else:
            bbox_area = (bbox[2] - bbox[0]) * (bbox[3] - bbox[1])
            coverage = bbox_area / (width * height)
        return {
            "name": path.name,
            "width": width,
            "height": height,
            "mode": image.mode,
            "bboxCoverage": round(coverage, 4),
            "bytes": path.stat().st_size,
        }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    rows: list[dict[str, object]] = []
    for path in sorted(args.source.glob("*.png")):
        try:
            rows.append(inspect_image(path))
        except Exception as error:  # retain corrupt-file evidence in the report
            rows.append({"name": path.name, "error": str(error)})

    dimensions = Counter(
        (row["width"], row["height"])
        for row in rows
        if "width" in row
    )
    report = {
        "source": str(args.source),
        "count": len(rows),
        "commonDimensions": [
            {"width": width, "height": height, "count": count}
            for (width, height), count in dimensions.most_common(30)
        ],
        "largeCandidates": [
            row
            for row in rows
            if row.get("width", 0) >= 800
            and row.get("height", 0) >= 500
            and row.get("bboxCoverage", 0) >= 0.35
        ],
    }

    encoded = json.dumps(report, ensure_ascii=False, indent=2)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(encoded, encoding="utf-8")
        print(f"Wrote {args.output}")
    else:
        print(encoded)


if __name__ == "__main__":
    main()
